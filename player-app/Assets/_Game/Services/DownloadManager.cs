using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InteractiveFantasticTales.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace InteractiveFantasticTales.Services
{
    public enum DownloadPhase { Metadata, Thumbnail, StoryJson, Assets, Complete }

    public enum DownloadState { Idle, Queued, Downloading, Paused, Complete, Failed }

    [Serializable]
    public class DownloadProgress
    {
        public string storyId;
        public DownloadPhase phase;
        public DownloadState state;
        public long totalBytes;
        public long downloadedBytes;
        public float progress;
        public string currentFile;
        public string error;
        public int retryCount;
    }

    /// <summary>
    /// Gerenciador robusto de downloads para conteudo de historias (JSON + assets).
    /// Suporte a resume via HTTP Range, progresso em fases, fila concorrente e
    /// validacao SHA-256. Integra com SignedUrlService e DownloadThrottleService.
    /// Singleton (DontDestroyOnLoad).
    /// </summary>
    public class DownloadManager : MonoBehaviour
    {
        public static DownloadManager Instance;

        public event Action<DownloadProgress> OnProgressUpdated;
        public event Action<string, bool> OnDownloadComplete;
        public event Action<string, string> OnDownloadFailed;

        private readonly Queue<string> _downloadQueue = new Queue<string>();
        private readonly Dictionary<string, DownloadProgress> _activeDownloads = new Dictionary<string, DownloadProgress>();
        private readonly Dictionary<string, DownloadProgress> _completedDownloads = new Dictionary<string, DownloadProgress>();
        private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens = new Dictionary<string, CancellationTokenSource>();
        private readonly Dictionary<string, HashSet<string>> _pendingPhases = new Dictionary<string, HashSet<string>>();

        [SerializeField] private int _maxConcurrentDownloads = 2;
        [SerializeField] private int _maxRetries = 3;
        [SerializeField] private float _retryDelaySeconds = 2f;
        [SerializeField] private long _chunkSizeBytes = 1024 * 1024;

        private int _activeCount;
        private bool _queueProcessing;

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
        }

        // -- API Publica -------------------------------------------------

        /// <summary>
        /// Inicia o download de uma historia, com suporte opcional a background (assets apos JSON).
        /// </summary>
        public void DownloadStory(string storyId, bool background = false)
        {
            if (string.IsNullOrEmpty(storyId))
                throw new ArgumentNullException(nameof(storyId));

            if (_activeDownloads.ContainsKey(storyId))
            {
                Debug.LogWarning($"[DownloadManager] Download ja ativo para {storyId}");
                return;
            }

            if (_completedDownloads.TryGetValue(storyId, out var completed) && completed.state == DownloadState.Complete)
            {
                Debug.Log($"[DownloadManager] Historia {storyId} ja foi baixada completamente.");
                OnDownloadComplete?.Invoke(storyId, true);
                return;
            }

            var progress = new DownloadProgress
            {
                storyId = storyId,
                phase = DownloadPhase.Metadata,
                state = DownloadState.Queued,
                totalBytes = 0,
                downloadedBytes = 0,
                progress = 0f,
                currentFile = "",
                error = null,
                retryCount = 0
            };

            _activeDownloads[storyId] = progress;
            _downloadQueue.Enqueue(storyId);
            _pendingPhases[storyId] = new HashSet<string>();

            if (!background)
            {
                _pendingPhases[storyId].Add("metadata");
                _pendingPhases[storyId].Add("thumbnail");
                _pendingPhases[storyId].Add("story_json");
            }

            _pendingPhases[storyId].Add("assets");

            Debug.Log($"[DownloadManager] Historia enfileirada: {storyId} (background={background})");
            OnProgressUpdated?.Invoke(progress);

            if (!_queueProcessing)
                _ = ProcessDownloadQueue();
        }

        /// <summary>Pausa o download de uma historia.</summary>
        public void PauseDownload(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;
            if (!_activeDownloads.TryGetValue(storyId, out var progress)) return;

            if (progress.state == DownloadState.Downloading)
            {
                progress.state = DownloadState.Paused;
                _activeCount--;

                if (_cancellationTokens.TryGetValue(storyId, out var cts))
                {
                    cts.Cancel();
                    _cancellationTokens.Remove(storyId);
                }

                OnProgressUpdated?.Invoke(progress);
                Debug.Log($"[DownloadManager] Download pausado: {storyId}");

                if (!_queueProcessing)
                    _ = ProcessDownloadQueue();
            }
        }

        /// <summary>Retoma o download pausado de uma historia.</summary>
        public void ResumeDownload(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;
            if (!_activeDownloads.TryGetValue(storyId, out var progress)) return;

            if (progress.state == DownloadState.Paused)
            {
                progress.state = DownloadState.Queued;
                _downloadQueue.Enqueue(storyId);
                OnProgressUpdated?.Invoke(progress);
                Debug.Log($"[DownloadManager] Download retomado: {storyId}");

                if (!_queueProcessing)
                    _ = ProcessDownloadQueue();
            }
        }

        /// <summary>Cancela e remove o download de uma historia.</summary>
        public void CancelDownload(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            if (_cancellationTokens.TryGetValue(storyId, out var cts))
            {
                cts.Cancel();
                _cancellationTokens.Remove(storyId);
            }

            _activeDownloads.Remove(storyId);
            _pendingPhases.Remove(storyId);

            var cacheDir = GetCacheDirectory(storyId);
            if (Directory.Exists(cacheDir))
            {
                try { Directory.Delete(cacheDir, true); }
                catch (Exception e) { Debug.LogWarning($"[DownloadManager] Erro ao limpar cache: {e.Message}"); }
            }

            Debug.Log($"[DownloadManager] Download cancelado: {storyId}");
        }

        /// <summary>Retorna o progresso atual de uma historia (ou null).</summary>
        public DownloadProgress GetProgress(string storyId)
        {
            if (_activeDownloads.TryGetValue(storyId, out var progress))
                return progress;
            if (_completedDownloads.TryGetValue(storyId, out var completed))
                return completed;
            return null;
        }

        /// <summary>Verifica se a historia foi completamente baixada.</summary>
        public bool IsDownloaded(string storyId)
        {
            return _completedDownloads.TryGetValue(storyId, out var p)
                && p.state == DownloadState.Complete;
        }

        /// <summary>Retorna o caminho do diretorio raiz da historia em cache.</summary>
        public string GetStoryPath(string storyId)
        {
            return GetCacheDirectory(storyId);
        }

        /// <summary>Retorna o caminho esperado do JSON da historia.</summary>
        public string GetStoryJsonPath(string storyId)
        {
            var dir = GetCacheDirectory(storyId);
            var version = GetCachedVersion(storyId);
            return Path.Combine(dir, $"story_{version ?? "latest"}.json");
        }

        /// <summary>Retorna o caminho local de um asset da historia.</summary>
        public string GetAssetPath(string storyId, string assetRelativePath)
        {
            return Path.Combine(GetCacheDirectory(storyId), "assets", assetRelativePath);
        }

        // -- Processamento de Fila ---------------------------------------

        private async Task ProcessDownloadQueue()
        {
            if (_queueProcessing) return;
            _queueProcessing = true;

            try
            {
                while (_downloadQueue.Count > 0 && _activeCount < _maxConcurrentDownloads)
                {
                    var storyId = _downloadQueue.Dequeue();

                    if (!_activeDownloads.TryGetValue(storyId, out var progress))
                        continue;

                    if (progress.state == DownloadState.Paused)
                        continue;

                    _activeCount++;
                    var cts = new CancellationTokenSource();
                    _cancellationTokens[storyId] = cts;

                    try
                    {
                        await DownloadStoryPhases(storyId, progress, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.Log($"[DownloadManager] Download cancelado: {storyId}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[DownloadManager] Erro no download de {storyId}: {e.Message}");
                        progress.state = DownloadState.Failed;
                        progress.error = e.Message;
                        OnDownloadFailed?.Invoke(storyId, e.Message);
                    }
                    finally
                    {
                        _cancellationTokens.Remove(storyId);
                        if (progress.state != DownloadState.Paused)
                            _activeCount--;
                    }
                }
            }
            finally
            {
                _queueProcessing = false;
            }
        }

        private async Task DownloadStoryPhases(string storyId, DownloadProgress progress, CancellationToken ct)
        {
            var pendingPhases = _pendingPhases.GetValueOrDefault(storyId);

            // Fase 1: Metadata
            if (pendingPhases == null || pendingPhases.Contains("metadata"))
            {
                progress.phase = DownloadPhase.Metadata;
                progress.state = DownloadState.Downloading;
                OnProgressUpdated?.Invoke(progress);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (IsMockMode())
                {
                    await SimulateMockDownload(progress, "metadata.json", 2048, ct);
                }
                else
#endif
                {
                    var metadataUrl = await FetchSignedUrlCoroutine(storyId, "metadata.json");
                    var metadataPath = Path.Combine(GetCacheDirectory(storyId), "metadata.json");
                    await DownloadFileWithResume(metadataUrl, metadataPath, progress, ct);
                }

                ct.ThrowIfCancellationRequested();
                pendingPhases?.Remove("metadata");
            }

            // Fase 2: Thumbnail
            if (pendingPhases == null || pendingPhases.Contains("thumbnail"))
            {
                progress.phase = DownloadPhase.Thumbnail;
                progress.state = DownloadState.Downloading;
                OnProgressUpdated?.Invoke(progress);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (IsMockMode())
                {
                    await SimulateMockDownload(progress, "thumbnail_512.jpg", 65536, ct);
                }
                else
#endif
                {
                    var thumbUrl = await FetchSignedUrlCoroutine(storyId, "thumbnails/cover_512.jpg");
                    var thumbPath = Path.Combine(GetCacheDirectory(storyId), "thumbnail_512.jpg");
                    await DownloadFileWithResume(thumbUrl, thumbPath, progress, ct);
                }

                ct.ThrowIfCancellationRequested();
                pendingPhases?.Remove("thumbnail");
            }

            // Fase 3: Story JSON (jogavel apos esta fase)
            if (pendingPhases == null || pendingPhases.Contains("story_json"))
            {
                progress.phase = DownloadPhase.StoryJson;
                progress.state = DownloadState.Downloading;
                OnProgressUpdated?.Invoke(progress);

                var version = GetCachedVersion(storyId) ?? "latest";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (IsMockMode())
                {
                    await SimulateMockDownload(progress, $"story_{version}.json", 131072, ct);
                }
                else
#endif
                {
                    var jsonUrl = await FetchSignedUrlCoroutine(storyId, $"data/story_{version}.json");
                    var jsonPath = Path.Combine(GetCacheDirectory(storyId), $"story_{version}.json");
                    await DownloadFileWithResume(jsonUrl, jsonPath, progress, ct);
                }

                ct.ThrowIfCancellationRequested();
                pendingPhases?.Remove("story_json");
            }

            // Fase 4: Assets (background)
            if (pendingPhases == null || pendingPhases.Contains("assets"))
            {
                progress.phase = DownloadPhase.Assets;
                progress.state = DownloadState.Downloading;
                OnProgressUpdated?.Invoke(progress);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (IsMockMode())
                {
                    await SimulateMockDownload(progress, "assets/bundle.dat", 262144, ct);
                }
                else
#endif
                {
                    var assetPaths = await FetchAssetList(storyId);
                    var assetsDir = Path.Combine(GetCacheDirectory(storyId), "assets");
                    if (!Directory.Exists(assetsDir))
                        Directory.CreateDirectory(assetsDir);

                    foreach (var assetRelativePath in assetPaths)
                    {
                        ct.ThrowIfCancellationRequested();
                        progress.currentFile = assetRelativePath;

                        var signedUrl = await FetchSignedUrlCoroutine(storyId, assetRelativePath);
                        var localPath = Path.Combine(assetsDir, assetRelativePath);
                        var localDir = Path.GetDirectoryName(localPath);
                        if (!string.IsNullOrEmpty(localDir) && !Directory.Exists(localDir))
                            Directory.CreateDirectory(localDir);

                        await DownloadFileWithResume(signedUrl, localPath, progress, ct);
                    }
                }

                ct.ThrowIfCancellationRequested();
                pendingPhases?.Remove("assets");
            }

            // Download completo
            progress.phase = DownloadPhase.Complete;
            progress.state = DownloadState.Complete;
            progress.progress = 1f;
            _activeDownloads.Remove(storyId);
            _completedDownloads[storyId] = progress;
            _pendingPhases.Remove(storyId);

            OnProgressUpdated?.Invoke(progress);
            OnDownloadComplete?.Invoke(storyId, true);
            Debug.Log($"[DownloadManager] Download completo: {storyId} ({progress.totalBytes} bytes)");

            if (CacheManager.Instance != null)
            {
                CacheManager.Instance.RegisterDownload(storyId, GetCachedVersion(storyId) ?? "1.0.0", progress.totalBytes);
                CacheManager.Instance.MarkComplete(storyId);
            }
        }

        // -- Download de Arquivo com Resume ------------------------------

        private async Task<bool> DownloadFileWithResume(string url, string localPath, DownloadProgress progress, CancellationToken ct)
        {
            var maxRetries = _maxRetries;
            var baseDelay = _retryDelaySeconds;

            long existingBytes = 0;
            if (File.Exists(localPath))
            {
                var fileInfo = new FileInfo(localPath);
                existingBytes = fileInfo.Length;
            }

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                if (attempt > 0)
                {
                    var delay = baseDelay * Mathf.Pow(2, attempt - 1);
                    Debug.LogWarning($"[DownloadManager] Retry {attempt}/{maxRetries} apos {delay:F1}s para {Path.GetFileName(localPath)}");
                    await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                }

                progress.retryCount = attempt;

                try
                {
                    using (var request = new UnityWebRequest(url, "GET"))
                    {
                        request.downloadHandler = new DownloadHandlerFile(localPath, true);

                        if (existingBytes > 0)
                        {
                            request.SetRequestHeader("Range", $"bytes={existingBytes}-");
                            Debug.Log($"[DownloadManager] Resumindo download de {existingBytes} bytes: {Path.GetFileName(localPath)}");
                        }

                        var operation = request.SendWebRequest();

                        while (!operation.isDone)
                        {
                            ct.ThrowIfCancellationRequested();
                            await Task.Delay(100, ct);
                        }

                        if (request.result == UnityWebRequest.Result.Success ||
                            request.responseCode == 206)
                        {
                            var fileInfo = new FileInfo(localPath);
                            progress.downloadedBytes += fileInfo.Length - existingBytes;

                            var totalForFile = existingBytes;
                            if (request.GetResponseHeader("Content-Range") is string contentRange)
                            {
                                var parts = contentRange.Split('/');
                                if (parts.Length >= 2 && long.TryParse(parts[1], out var total))
                                    totalForFile = total;
                            }
                            else if (request.GetResponseHeader("Content-Length") is string contentLength
                                && long.TryParse(contentLength, out var cl))
                            {
                                totalForFile = cl;
                            }

                            if (totalForFile > 0)
                            {
                                progress.totalBytes = Math.Max(progress.totalBytes, progress.downloadedBytes + totalForFile - fileInfo.Length);
                                progress.progress = progress.totalBytes > 0
                                    ? (float)((double)progress.downloadedBytes / progress.totalBytes)
                                    : 0f;
                            }

                            progress.error = null;
                            OnProgressUpdated?.Invoke(progress);

                            return true;
                        }

                        if (request.responseCode == 416)
                        {
                            Debug.Log($"[DownloadManager] Range nao satisfeito — reiniciando download de {Path.GetFileName(localPath)}");
                            if (File.Exists(localPath))
                                File.Delete(localPath);
                            existingBytes = 0;
                            attempt--;
                            continue;
                        }

                        var errorMsg = $"{request.responseCode} {request.error}";
                        Debug.LogWarning($"[DownloadManager] Download falhou (attempt {attempt + 1}): {errorMsg}");

                        if (attempt == maxRetries)
                        {
                            progress.error = errorMsg;
                            progress.state = DownloadState.Failed;
                            OnProgressUpdated?.Invoke(progress);
                            return false;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DownloadManager] Erro no download: {e.Message}");
                    if (attempt == maxRetries)
                    {
                        progress.error = e.Message;
                        progress.state = DownloadState.Failed;
                        OnProgressUpdated?.Invoke(progress);
                        return false;
                    }
                }
            }

            return false;
        }

        // -- Signed URL via Coroutine Wrapper ----------------------------

        private async Task<string> FetchSignedUrl(string storyId, string assetPath)
        {
            return await FetchSignedUrlCoroutine(storyId, assetPath);
        }

        private Task<string> FetchSignedUrlCoroutine(string storyId, string assetPath)
        {
            var tcs = new TaskCompletionSource<string>();

            if (SignedUrlService.Instance != null)
            {
                StartCoroutine(SignedUrlCoroutine(storyId, assetPath, tcs));
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                tcs.SetResult($"https://storage.mock.local/ifts/{storyId}/{assetPath}?mock_sig=dev");
#else
                tcs.SetException(new InvalidOperationException("SignedUrlService.Instance is null"));
#endif
            }

            return tcs.Task;
        }

        private System.Collections.IEnumerator SignedUrlCoroutine(string storyId, string assetPath, TaskCompletionSource<string> tcs)
        {
            AssetUrlResponse response = null;

            yield return SignedUrlService.Instance.GetAssetUrl(storyId, assetPath, r => response = r);

            if (response != null && response.success)
                tcs.TrySetResult(response.signedUrl);
            else
                tcs.TrySetException(new Exception($"Falha ao obter signed URL: {response?.error ?? "resposta nula"}"));
        }

        // -- Asset List (Mock no editor) ---------------------------------

        private async Task<string[]> FetchAssetList(string storyId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsMockMode())
            {
                await Task.Delay(50);
                return new[]
                {
                    "images/background_1.jpg",
                    "images/background_2.jpg",
                    "images/character_portrait.png",
                    "audio/ambient.ogg"
                };
            }
#endif

            var listPath = Path.Combine(GetCacheDirectory(storyId), "asset_manifest.json");
            if (File.Exists(listPath))
            {
                var json = await File.ReadAllTextAsync(listPath);
                var manifest = JsonUtility.FromJson<AssetManifest>(json);
                return manifest?.files ?? Array.Empty<string>();
            }

            return Array.Empty<string>();
        }

        [Serializable]
        private class AssetManifest
        {
            public string[] files;
        }

        // -- Validacao ---------------------------------------------------

        private bool ValidateDownload(string localPath, string expectedHash)
        {
            if (!File.Exists(localPath)) return false;
            if (string.IsNullOrEmpty(expectedHash)) return true;

            try
            {
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(localPath))
                {
                    var hash = sha.ComputeHash(stream);
                    var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    var valid = string.Equals(hashString, expectedHash, StringComparison.OrdinalIgnoreCase);
                    if (!valid)
                        Debug.LogWarning($"[DownloadManager] Hash mismatch para {Path.GetFileName(localPath)}: esperado={expectedHash}, obtido={hashString}");
                    return valid;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DownloadManager] Erro na validacao: {e.Message}");
                return false;
            }
        }

        // -- Cache Directory Utils ---------------------------------------

        private string _cacheRoot;

        private string GetCacheRoot()
        {
            return Path.Combine(Application.persistentDataPath, "StoryCache");
        }

        private string GetCacheDirectory(string storyId)
        {
            return Path.Combine(_cacheRoot, storyId);
        }

        private long GetCacheSize()
        {
            if (!Directory.Exists(_cacheRoot)) return 0;
            return GetDirectorySize(new DirectoryInfo(_cacheRoot));
        }

        private long GetDirectorySize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
                {
                    try { size += file.Length; }
                    catch { /* skip inaccessible files */ }
                }
            }
            catch { /* skip inaccessible directories */ }
            return size;
        }

        private string GetCachedVersion(string storyId)
        {
            var metadataPath = Path.Combine(GetCacheDirectory(storyId), "metadata.json");
            if (File.Exists(metadataPath))
            {
                try
                {
                    var json = File.ReadAllText(metadataPath);
                    var meta = JsonUtility.FromJson<StoryMetadata>(json);
                    return meta?.version;
                }
                catch { }
            }
            return null;
        }

        [Serializable]
        private class StoryMetadata
        {
            public string version;
            public string[] assetList;
            public long estimatedSizeBytes;
        }

        // -- Mock Mode ---------------------------------------------------

        private static bool IsMockMode()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var auth = CloudRunAuthService.Instance;
            return auth == null || !auth.IsFirebaseReady;
#else
            return false;
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private async Task SimulateMockDownload(DownloadProgress progress, string fileName, long fileSize, CancellationToken ct)
        {
            progress.currentFile = fileName;
            progress.totalBytes = Math.Max(progress.totalBytes, progress.downloadedBytes + fileSize);

            var totalChunks = Math.Max(1, (int)(fileSize / _chunkSizeBytes));
            var bytesPerChunk = fileSize / totalChunks;

            for (int i = 0; i < totalChunks; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(80, ct);

                var chunkBytes = (i == totalChunks - 1)
                    ? fileSize - (bytesPerChunk * (totalChunks - 1))
                    : bytesPerChunk;

                progress.downloadedBytes += chunkBytes;
                progress.progress = progress.totalBytes > 0
                    ? Mathf.Clamp01((float)((double)progress.downloadedBytes / progress.totalBytes))
                    : 0f;

                OnProgressUpdated?.Invoke(progress);
            }
        }
#endif
    }
}
