using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.Services
{
    [Serializable]
    public class StoryProgressData
    {
        public string storyId;
        public bool owned;
        public string purchasedAt;
        public string lastPlayedAt;
        public int totalPlaytimeSeconds;
        public int sessionsPlayed;
        public int sectionsVisited;
        public int uniqueSectionsVisited;
        public int totalSections;
        public int choicesMade;
        public string[] endingsFound;
        public int endingsFoundCount;
        public int totalEndings;
        public float completionPercent;
        public int combatsWon;
        public int combatsLost;
        public int combatsFled;
        public int characterDeaths;
        public int rewindsUsed;
        public int lastSectionId;
        public bool hasActiveGame;
        public int activeSaveSlot;
    }

    /// <summary>
    /// Gerencia a biblioteca do jogador: historias possuidas, progresso e sincronizacao com cloud save.
    /// Singleton (DontDestroyOnLoad). Persiste dados via SecureStorage.ObfuscatedPrefs.
    /// </summary>
    public class LibraryManager : MonoBehaviour
    {
        public static LibraryManager Instance;

        /// <summary>Disparado sempre que o progresso de qualquer historia e alterado.</summary>
        public event Action OnProgressUpdated;

        private Dictionary<string, StoryProgressData> _progress = new Dictionary<string, StoryProgressData>();
        private System.Diagnostics.Stopwatch _sessionTimer;
        private string _activeStoryId;

        private const string PrefPrefix = "ift_lib_progress_";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProgress();
        }

        private void OnApplicationQuit()
        {
            if (!string.IsNullOrEmpty(_activeStoryId))
                EndSession(_activeStoryId, 0);
            SaveProgress();
        }

        // -- API: Query --------------------------------------------------

        /// <summary>Retorna a lista completa de progresso de todas as historias.</summary>
        public List<StoryProgressData> GetAllProgress()
        {
            return _progress.Values.ToList();
        }

        /// <summary>Retorna o progresso de uma historia especifica, ou null se nunca registrada.</summary>
        public StoryProgressData GetProgress(string storyId)
        {
            _progress.TryGetValue(storyId, out var data);
            return data;
        }

        /// <summary>Verifica se o jogador possui uma historia.</summary>
        public bool IsOwned(string storyId)
        {
            return _progress.TryGetValue(storyId, out var data) && data.owned;
        }

        /// <summary>Verifica se ha um jogo ativo (nao finalizado) para a historia.</summary>
        public bool HasActiveGame(string storyId)
        {
            return _progress.TryGetValue(storyId, out var data) && data.hasActiveGame;
        }

        // -- API: Progress Tracking (chamado por eventos do GameEngine) ---

        /// <summary>Registra a visita a uma secao. Incrementa contadores de secoes.</summary>
        public void TrackSectionVisited(int sectionId)
        {
            if (string.IsNullOrEmpty(_activeStoryId)) return;
            var p = GetOrCreateProgress(_activeStoryId);
            p.sectionsVisited++;
            p.lastSectionId = sectionId;
            OnProgressUpdated?.Invoke();
        }

        /// <summary>Registra que uma escolha foi feita pelo jogador.</summary>
        public void TrackChoiceMade()
        {
            if (string.IsNullOrEmpty(_activeStoryId)) return;
            var p = GetOrCreateProgress(_activeStoryId);
            p.choicesMade++;
            OnProgressUpdated?.Invoke();
        }

        /// <summary>Registra resultado de combate. Resultado: "victory", "defeat" ou "fled".</summary>
        public void TrackCombatResult(string result)
        {
            if (string.IsNullOrEmpty(_activeStoryId)) return;
            var p = GetOrCreateProgress(_activeStoryId);
            switch (result)
            {
                case "victory": p.combatsWon++; break;
                case "defeat": p.combatsLost++; break;
                case "fled": p.combatsFled++; break;
                default:
                    Debug.LogWarning($"[LibraryManager] Combat result '{result}' desconhecido.");
                    break;
            }
            OnProgressUpdated?.Invoke();
        }

        /// <summary>Registra um final encontrado. Tipo: "good", "bad", "neutral", "secret", etc.</summary>
        public void TrackEndingFound(string endingId, string endingType)
        {
            if (string.IsNullOrEmpty(_activeStoryId)) return;
            var p = GetOrCreateProgress(_activeStoryId);
            if (p.endingsFound == null)
                p.endingsFound = new string[0];

            if (!p.endingsFound.Contains(endingId))
            {
                var list = new List<string>(p.endingsFound) { endingId };
                p.endingsFound = list.ToArray();
                p.endingsFoundCount = p.endingsFound.Length;
            }

            p.hasActiveGame = false;
            OnProgressUpdated?.Invoke();
            Debug.Log($"[LibraryManager] Ending found: {endingId} ({endingType})");
        }

        /// <summary>Registra a morte do personagem (game over).</summary>
        public void TrackCharacterDeath()
        {
            if (string.IsNullOrEmpty(_activeStoryId)) return;
            var p = GetOrCreateProgress(_activeStoryId);
            p.characterDeaths++;
            OnProgressUpdated?.Invoke();
        }

        /// <summary>Registra uso de rewind/voltar secao.</summary>
        public void TrackRewind()
        {
            if (string.IsNullOrEmpty(_activeStoryId)) return;
            var p = GetOrCreateProgress(_activeStoryId);
            p.rewindsUsed++;
            OnProgressUpdated?.Invoke();
        }

        // -- Session Management ------------------------------------------

        /// <summary>Inicia uma sessao de jogo para a historia informada. Inicia o timer de playtime.</summary>
        public void StartSession(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;
            _activeStoryId = storyId;
            var p = GetOrCreateProgress(storyId);
            p.hasActiveGame = true;
            p.sessionsPlayed++;
            p.lastPlayedAt = DateTime.UtcNow.ToString("o");

            _sessionTimer = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log($"[LibraryManager] Sessao iniciada: {storyId}");
        }

        /// <summary>Finaliza a sessao ativa. Acumula o playtime e salva o progresso.</summary>
        public void EndSession(string storyId, int lastSectionId)
        {
            if (string.IsNullOrEmpty(storyId)) return;
            var p = GetOrCreateProgress(storyId);

            if (_sessionTimer != null && _sessionTimer.IsRunning)
            {
                _sessionTimer.Stop();
                p.totalPlaytimeSeconds += (int)_sessionTimer.Elapsed.TotalSeconds;
                _sessionTimer = null;
            }

            p.lastSectionId = lastSectionId;
            p.lastPlayedAt = DateTime.UtcNow.ToString("o");
            _activeStoryId = null;

            SaveProgress();
            Debug.Log($"[LibraryManager] Sessao finalizada: {storyId} - playtime total: {p.totalPlaytimeSeconds}s");
        }

        // -- Ownership ---------------------------------------------------

        /// <summary>Adiciona uma historia ao inventario do jogador como comprada.</summary>
        public void AddOwnedStory(string storyId)
        {
            var p = GetOrCreateProgress(storyId);
            p.owned = true;
            p.purchasedAt = DateTime.UtcNow.ToString("o");
            SaveProgress();
            OnProgressUpdated?.Invoke();
            Debug.Log($"[LibraryManager] Historia adquirida: {storyId}");
        }

        // -- Save/Load ---------------------------------------------------

        /// <summary>Salva todo o progresso no SecureStorage e dispara sync com cloud.</summary>
        public void SaveProgress()
        {
            try
            {
                var json = JsonUtility.ToJson(new ProgressWrapper { items = _progress.Values.ToList() });
                SecureStorage.ObfuscatedPrefs.SetString(PrefPrefix + "all", json);
                SecureStorage.ObfuscatedPrefs.Save();
                Debug.Log("[LibraryManager] Progresso salvo localmente.");

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
                if (CloudSaveClient.Instance != null)
                    _ = CloudSaveClient.Instance.SyncAllProgress();
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[LibraryManager] Erro ao salvar progresso: {e.Message}");
            }
        }

        /// <summary>Carrega o progresso salvo do SecureStorage.</summary>
        public void LoadProgress()
        {
            try
            {
                var json = SecureStorage.ObfuscatedPrefs.GetString(PrefPrefix + "all", "");
                if (string.IsNullOrEmpty(json))
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log("[LibraryManager] Nenhum progresso salvo - populando dados mock demo.");
                    PopulateMockData();
#endif
                    return;
                }

                var wrapper = JsonUtility.FromJson<ProgressWrapper>(json);
                if (wrapper?.items != null)
                {
                    _progress.Clear();
                    foreach (var item in wrapper.items)
                        _progress[item.storyId] = item;
                    Debug.Log($"[LibraryManager] Progresso carregado: {_progress.Count} historia(s).");
                }

                OnProgressUpdated?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LibraryManager] Erro ao carregar progresso: {e.Message}");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                PopulateMockData();
#endif
            }
        }

        // -- Data Clearing -----------------------------------------------

        /// <summary>Remove todo o progresso salvo de uma historia especifica.</summary>
        public void ClearStoryData(string storyId)
        {
            if (_progress.Remove(storyId))
            {
                SaveProgress();
                OnProgressUpdated?.Invoke();
                Debug.Log($"[LibraryManager] Dados removidos: {storyId}");
            }
        }

        // -- Helpers -----------------------------------------------------

        private StoryProgressData GetOrCreateProgress(string storyId)
        {
            if (!_progress.TryGetValue(storyId, out var data))
            {
                data = new StoryProgressData { storyId = storyId };
                _progress[storyId] = data;
            }
            return data;
        }

        // -- Mock Data (Editor / Dev Build) ------------------------------

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void PopulateMockData()
        {
            _progress.Clear();

            var demo = new StoryProgressData
            {
                storyId = "demo",
                owned = true,
                purchasedAt = DateTime.UtcNow.AddDays(-30).ToString("o"),
                lastPlayedAt = DateTime.UtcNow.AddHours(-2).ToString("o"),
                totalPlaytimeSeconds = 3840,
                sessionsPlayed = 7,
                sectionsVisited = 42,
                uniqueSectionsVisited = 11,
                totalSections = 13,
                choicesMade = 28,
                endingsFound = new[] { "demo_end_good" },
                endingsFoundCount = 1,
                totalEndings = 3,
                completionPercent = 53.8f,
                combatsWon = 3,
                combatsLost = 1,
                combatsFled = 0,
                characterDeaths = 2,
                rewindsUsed = 4,
                lastSectionId = 7,
                hasActiveGame = true,
                activeSaveSlot = 0
            };

            var starPortal = new StoryProgressData
            {
                storyId = "star-portal",
                owned = true,
                purchasedAt = DateTime.UtcNow.AddDays(-7).ToString("o"),
                lastPlayedAt = DateTime.UtcNow.AddDays(-1).ToString("o"),
                totalPlaytimeSeconds = 1200,
                sessionsPlayed = 2,
                sectionsVisited = 18,
                uniqueSectionsVisited = 8,
                totalSections = 50,
                choicesMade = 10,
                endingsFound = new string[0],
                endingsFoundCount = 0,
                totalEndings = 4,
                completionPercent = 16f,
                combatsWon = 1,
                combatsLost = 0,
                combatsFled = 2,
                characterDeaths = 0,
                rewindsUsed = 1,
                lastSectionId = 12,
                hasActiveGame = false,
                activeSaveSlot = 0
            };

            _progress["demo"] = demo;
            _progress["star-portal"] = starPortal;

            Debug.Log("[LibraryManager] Dados mock populados: demo + star-portal.");
            OnProgressUpdated?.Invoke();
        }
#endif

        [Serializable]
        private class ProgressWrapper
        {
            public List<StoryProgressData> items;
        }
    }
}
