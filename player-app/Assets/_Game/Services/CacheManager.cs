using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InteractiveFantasticTales.Core;
using UnityEngine;

namespace InteractiveFantasticTales.Services
{
    [Serializable]
    public class CacheInfo
    {
        public string storyId;
        public string version;
        public long sizeBytes;
        public string lastAccessedAt;
        public string downloadedAt;
        public bool isComplete;
        public int accessCount;
    }

    [Serializable]
    public class CacheManifest
    {
        public List<CacheInfo> entries = new List<CacheInfo>();
        public long totalSizeBytes;
        public string lastCleanedAt;
    }

    /// <summary>
    /// Gerencia o cache local de historias com politica LRU e limites de armazenamento.
    /// Persiste o manifesto em JSON (cache_manifest.json). Integra com DownloadManager
    /// para estimativas de tamanho e registro de downloads.
    /// Singleton (DontDestroyOnLoad).
    /// </summary>
    public class CacheManager : MonoBehaviour
    {
        public static CacheManager Instance;

        public event Action OnCacheUpdated;
        public event Action<string> OnStoryEvicted;

        [SerializeField] private long _softLimitBytes = 400_000_000;
        [SerializeField] private long _hardLimitBytes = 500_000_000;
        [SerializeField] private int _maxStories = 10;
        [SerializeField] private int _maxInactiveDays = 90;

        private CacheManifest _manifest;
        private string _cacheRoot;
        private bool _manifestLoaded;

        private string ManifestPath
        {
            get { return _cacheRoot == null ? null : Path.Combine(_cacheRoot, "cache_manifest.json"); }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _cacheRoot = GetCacheRoot();
            if (!Directory.Exists(_cacheRoot))
                Directory.CreateDirectory(_cacheRoot);

            LoadManifest();
        }

        // -- API: Query --------------------------------------------------

        /// <summary>Retorna as informacoes de cache de uma historia, ou null.</summary>
        public CacheInfo GetStoryCacheInfo(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return null;
            return _manifest.entries.FirstOrDefault(e =>
                string.Equals(e.storyId, storyId, StringComparison.Ordinal));
        }

        /// <summary>Verifica se uma historia esta em cache (qualquer estado).</summary>
        public bool IsStoryCached(string storyId)
        {
            return GetStoryCacheInfo(storyId) != null;
        }

        /// <summary>Verifica se uma historia esta completamente baixada em cache.</summary>
        public bool IsStoryComplete(string storyId)
        {
            var info = GetStoryCacheInfo(storyId);
            return info != null && info.isComplete;
        }

        /// <summary>Retorna o diretorio onde a historia esta (ou estaria) em cache.</summary>
        public string GetCachedStoryDirectory(string storyId)
        {
            return Path.Combine(_cacheRoot, storyId);
        }

        /// <summary>Retorna o tamanho total do cache em bytes.</summary>
        public long GetCacheSizeBytes()
        {
            RefreshTotalSize();
            return _manifest.totalSizeBytes;
        }

        /// <summary>Retorna o espaco livre estimado em disco em bytes.</summary>
        public long GetFreeSpaceBytes()
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(_cacheRoot));
                return drive.AvailableFreeSpace;
            }
            catch
            {
                return long.MaxValue;
            }
        }

        /// <summary>Verifica se ha espaco suficiente para uma historia do tamanho informado.</summary>
        public bool CanFitStory(long storySizeBytes)
        {
            RefreshTotalSize();

            if (_manifest.entries.Count >= _maxStories && !IsStoryCached("_placeholder_"))
                return false;

            var afterDownload = _manifest.totalSizeBytes + storySizeBytes;
            if (afterDownload > _hardLimitBytes)
                return false;

            var freeSpace = GetFreeSpaceBytes();
            if (freeSpace < long.MaxValue && storySizeBytes > freeSpace)
                return false;

            return true;
        }

        // -- API: Registro -----------------------------------------------

        /// <summary>
        /// Registra que uma historia foi baixada (ou esta em download).
        /// Atualiza ou cria a entrada no manifesto.
        /// </summary>
        public void RegisterDownload(string storyId, string version, long sizeBytes)
        {
            if (string.IsNullOrEmpty(storyId))
                throw new ArgumentNullException(nameof(storyId));

            var entry = GetStoryCacheInfo(storyId);
            var now = DateTime.UtcNow.ToString("o");

            if (entry == null)
            {
                // Verifica se precisa liberar espaco antes de adicionar
                if (_manifest.entries.Count >= _maxStories)
                {
                    EvictLeastRecentlyUsed(1);
                }

                if (_manifest.totalSizeBytes + sizeBytes > _hardLimitBytes)
                {
                    var needed = (_manifest.totalSizeBytes + sizeBytes) - _hardLimitBytes;
                    EvictForSpace(needed);
                }

                entry = new CacheInfo
                {
                    storyId = storyId,
                    version = version ?? "1.0.0",
                    sizeBytes = sizeBytes,
                    lastAccessedAt = now,
                    downloadedAt = now,
                    isComplete = false,
                    accessCount = 1
                };
                _manifest.entries.Add(entry);
            }
            else
            {
                entry.version = version ?? entry.version;
                entry.sizeBytes = Math.Max(entry.sizeBytes, sizeBytes);
                entry.downloadedAt = now;
                entry.lastAccessedAt = now;
                entry.accessCount++;
                entry.isComplete = false;
            }

            RefreshTotalSize();
            SaveManifest();
            OnCacheUpdated?.Invoke();
            Debug.Log($"[CacheManager] Download registrado: {storyId} v{entry.version} ({entry.sizeBytes} bytes)");
        }

        /// <summary>
        /// Marca o acesso a uma historia (atualiza lastAccessedAt e accessCount).
        /// Usado para ordenacao LRU.
        /// </summary>
        public void MarkAccess(string storyId)
        {
            var entry = GetStoryCacheInfo(storyId);
            if (entry == null)
            {
                Debug.LogWarning($"[CacheManager] Tentativa de marcar acesso a historia nao cacheada: {storyId}");
                return;
            }

            entry.lastAccessedAt = DateTime.UtcNow.ToString("o");
            entry.accessCount++;
            SaveManifest();
        }

        /// <summary>Marca que todos os assets de uma historia foram baixados.</summary>
        public void MarkComplete(string storyId)
        {
            var entry = GetStoryCacheInfo(storyId);
            if (entry == null)
            {
                Debug.LogWarning($"[CacheManager] Tentativa de marcar completa historia nao cacheada: {storyId}");
                return;
            }

            entry.isComplete = true;
            RefreshTotalSize();
            SaveManifest();
            OnCacheUpdated?.Invoke();
            Debug.Log($"[CacheManager] Historia marcada como completa: {storyId}");
        }

        // -- API: Eviccao ------------------------------------------------

        /// <summary>Remove manualmente uma historia do cache.</summary>
        public void EvictStory(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            var entry = GetStoryCacheInfo(storyId);
            if (entry == null)
            {
                Debug.LogWarning($"[CacheManager] Historia nao encontrada no cache: {storyId}");
                return;
            }

            var dir = GetCachedStoryDirectory(storyId);
            if (Directory.Exists(dir))
            {
                try { Directory.Delete(dir, true); }
                catch (Exception e)
                {
                    Debug.LogError($"[CacheManager] Erro ao remover diretorio de {storyId}: {e.Message}");
                }
            }

            _manifest.entries.Remove(entry);
            RefreshTotalSize();
            SaveManifest();

            OnCacheUpdated?.Invoke();
            OnStoryEvicted?.Invoke(storyId);
            Debug.Log($"[CacheManager] Historia removida do cache: {storyId}");
        }

        /// <summary>Remove a(s) historia(s) menos recentemente acessada(s).</summary>
        public void EvictLeastRecentlyUsed(int count = 1)
        {
            if (count <= 0 || _manifest.entries.Count == 0) return;

            var ordered = _manifest.entries
                .OrderBy(e => ParseDateTime(e.lastAccessedAt))
                .ToList();

            var toEvict = new List<string>();
            foreach (var entry in ordered)
            {
                if (toEvict.Count >= count) break;

                // Nao evicta historias com jogo ativo (verifica via LibraryManager)
                if (LibraryManager.Instance != null && LibraryManager.Instance.HasActiveGame(entry.storyId))
                {
                    Debug.Log($"[CacheManager] Pulando eviccao de {entry.storyId} — jogo ativo.");
                    continue;
                }

                toEvict.Add(entry.storyId);
            }

            foreach (var id in toEvict)
            {
                EvictStory(id);
            }

            if (toEvict.Count > 0)
                Debug.Log($"[CacheManager] LRU eviction: {toEvict.Count} historia(s) removida(s).");
        }

        /// <summary>Remove historias que nao foram acessadas ha mais de maxDays dias.</summary>
        public void CleanupExpired(int maxDays = 90)
        {
            var cutoff = DateTime.UtcNow.AddDays(-maxDays);
            var expired = new List<string>();

            foreach (var entry in _manifest.entries)
            {
                var lastAccess = ParseDateTime(entry.lastAccessedAt);
                if (lastAccess < cutoff)
                {
                    // Nao evicta historias com jogo ativo
                    if (LibraryManager.Instance != null && LibraryManager.Instance.HasActiveGame(entry.storyId))
                        continue;

                    expired.Add(entry.storyId);
                }
            }

            foreach (var id in expired)
            {
                EvictStory(id);
            }

            if (expired.Count > 0)
            {
                _manifest.lastCleanedAt = DateTime.UtcNow.ToString("o");
                SaveManifest();
                Debug.Log($"[CacheManager] Cleanup expirado: {expired.Count} historia(s) removida(s).");
            }
        }

        /// <summary>
        /// Verifica se o cache precisa de limpeza (acima do soft limit).
        /// </summary>
        public bool NeedsCleanup()
        {
            RefreshTotalSize();
            return _manifest.totalSizeBytes > _softLimitBytes;
        }

        /// <summary>
        /// Retorna uma recomendacao amigavel de limpeza para o usuario.
        /// Retorna null se o cache estiver saudavel.
        /// </summary>
        public string GetCleanupRecommendation()
        {
            RefreshTotalSize();

            if (_manifest.totalSizeBytes > _hardLimitBytes * 0.95)
            {
                var sizeMb = _manifest.totalSizeBytes / 1_000_000f;
                return $"O cache de historias esta quase cheio ({sizeMb:F0} MB). " +
                    "Recomendamos liberar espaco removendo historias que voce ja terminou.";
            }

            if (_manifest.totalSizeBytes > _softLimitBytes)
            {
                var sizeMb = _manifest.totalSizeBytes / 1_000_000f;
                var softMb = _softLimitBytes / 1_000_000f;
                return $"Cache de historias: {sizeMb:F0} MB de {softMb:F0} MB. " +
                    "Considere remover historias antigas para liberar espaco.";
            }

            if (_manifest.entries.Count >= _maxStories)
            {
                return $"Voce atingiu o limite de {_maxStories} historias em cache. " +
                    "Remova uma historia para baixar outra.";
            }

            // Verifica historias expiradas
            var cutoff = DateTime.UtcNow.AddDays(-_maxInactiveDays);
            var expiredCount = _manifest.entries.Count(e => ParseDateTime(e.lastAccessedAt) < cutoff);
            if (expiredCount > 0)
            {
                return $"{expiredCount} historia(s) nao sao jogadas ha mais de {_maxInactiveDays} dias. " +
                    "Remova-as para liberar espaco.";
            }

            return null;
        }

        // -- Persistencia ------------------------------------------------

        private void LoadManifest()
        {
            if (_manifestLoaded) return;

            try
            {
                var path = ManifestPath;
                if (path != null && File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    _manifest = JsonUtility.FromJson<CacheManifest>(json);
                    if (_manifest != null)
                    {
                        // Remove entradas cujo diretorio nao existe mais
                        var orphaned = _manifest.entries
                            .Where(e => !Directory.Exists(GetCachedStoryDirectory(e.storyId)))
                            .ToList();

                        foreach (var orphan in orphaned)
                        {
                            Debug.Log($"[CacheManager] Removendo entrada orfa: {orphan.storyId}");
                            _manifest.entries.Remove(orphan);
                        }

                        if (orphaned.Count > 0)
                            SaveManifest();

                        Debug.Log($"[CacheManager] Manifesto carregado: {_manifest.entries.Count} historias, {_manifest.totalSizeBytes} bytes.");
                    }
                    else
                    {
                        _manifest = new CacheManifest();
                    }
                }
                else
                {
                    _manifest = new CacheManifest();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (IsMockMode())
                        PopulateMockData();
#endif
                }

                RefreshTotalSize();
                _manifestLoaded = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CacheManager] Erro ao carregar manifesto: {e.Message}");
                _manifest = new CacheManifest();
                _manifestLoaded = true;
            }
        }

        private void SaveManifest()
        {
            try
            {
                var path = ManifestPath;
                if (path == null) return;

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonUtility.ToJson(_manifest, true);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CacheManager] Erro ao salvar manifesto: {e.Message}");
            }
        }

        private string GetCacheRoot()
        {
            return Path.Combine(Application.persistentDataPath, "StoryCache");
        }

        // -- Helpers -----------------------------------------------------

        /// <summary>Recalcula o tamanho total varrendo os diretorios em disco.</summary>
        private void RefreshTotalSize()
        {
            if (!Directory.Exists(_cacheRoot))
            {
                _manifest.totalSizeBytes = 0;
                return;
            }

            long total = 0;
            foreach (var entry in _manifest.entries)
            {
                var dir = GetCachedStoryDirectory(entry.storyId);
                if (Directory.Exists(dir))
                {
                    try
                    {
                        total += GetDirectorySize(new DirectoryInfo(dir));
                    }
                    catch
                    {
                        total += entry.sizeBytes;
                    }
                }
            }

            _manifest.totalSizeBytes = total;
        }

        private long GetDirectorySize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
                {
                    try { size += file.Length; }
                    catch { /* skip inaccessible */ }
                }
            }
            catch { /* skip inaccessible */ }
            return size;
        }

        private DateTime ParseDateTime(string isoString)
        {
            if (DateTime.TryParse(isoString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
                return result;
            return DateTime.MinValue;
        }

        /// <summary>
        /// Libera espaco ate atingir o necessario removendo entradas LRU.
        /// </summary>
        private void EvictForSpace(long neededBytes)
        {
            var ordered = _manifest.entries
                .OrderBy(e => ParseDateTime(e.lastAccessedAt))
                .ToList();

            long freed = 0;
            foreach (var entry in ordered)
            {
                if (freed >= neededBytes) break;

                if (LibraryManager.Instance != null && LibraryManager.Instance.HasActiveGame(entry.storyId))
                    continue;

                freed += entry.sizeBytes;
                EvictStory(entry.storyId);
            }
        }

        // -- Mock Data (Editor / Dev Build) ------------------------------

        private static bool IsMockMode()
        {
            var auth = CloudRunAuthService.Instance;
            return auth == null || !auth.IsFirebaseReady;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void PopulateMockData()
        {
            var now = DateTime.UtcNow;

            var demo = new CacheInfo
            {
                storyId = "demo",
                version = "1.0.0",
                sizeBytes = 15_000_000,
                lastAccessedAt = now.AddHours(-2).ToString("o"),
                downloadedAt = now.AddDays(-30).ToString("o"),
                isComplete = true,
                accessCount = 7
            };

            var starPortal = new CacheInfo
            {
                storyId = "star-portal",
                version = "1.0.0",
                sizeBytes = 8_500_000,
                lastAccessedAt = now.AddDays(-1).ToString("o"),
                downloadedAt = now.AddDays(-7).ToString("o"),
                isComplete = true,
                accessCount = 2
            };

            var mountainOfFire = new CacheInfo
            {
                storyId = "mountain-of-fire",
                version = "2.1.0",
                sizeBytes = 45_000_000,
                lastAccessedAt = now.AddDays(-5).ToString("o"),
                downloadedAt = now.AddDays(-14).ToString("o"),
                isComplete = false,
                accessCount = 3
            };

            _manifest.entries.Add(demo);
            _manifest.entries.Add(starPortal);
            _manifest.entries.Add(mountainOfFire);

            RefreshTotalSize();
            SaveManifest();
            Debug.Log($"[CacheManager] Dados mock populados: {_manifest.entries.Count} historias ({_manifest.totalSizeBytes} bytes).");
        }
#endif
    }
}
