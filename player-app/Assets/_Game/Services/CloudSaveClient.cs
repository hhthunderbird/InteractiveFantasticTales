using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.Services
{
    [Serializable]
    public class CloudSaveSlot
    {
        public int slot;
        public string name;
        public int sectionId;
        public string characterSnapshot;
        public string createdAt;
        public string updatedAt;
    }

    /// <summary>
    /// Cliente de sincronizacao com cloud save (Firestore). Modo mock no Editor/Dev Build.
    /// Singleton (DontDestroyOnLoad). Gerencia upload/download de saves e progresso.
    /// </summary>
    public class CloudSaveClient : MonoBehaviour
    {
        public static CloudSaveClient Instance;

        /// <summary>Disparado ao finalizar uma sincronizacao. Param: true = sucesso.</summary>
        public event Action<bool> OnSyncCompleted;

        /// <summary>Disparado quando um conflito de versao e detectado entre local e cloud.</summary>
        public event Action<string, string> OnConflictDetected;

        private bool _isSyncing;
        private ConflictResolution _defaultResolution = ConflictResolution.KeepLocal;
        private Coroutine _autoSyncCoroutine;

        /// <summary>True se uma sincronizacao esta em andamento.</summary>
        public bool IsSyncing => _isSyncing;

        /// <summary>True se o dispositivo tem conectividade com a internet.</summary>
        public bool IsOnline => Application.internetReachability != NetworkReachability.NotReachable;

        /// <summary>Resolucao de conflito padrao quando versoes divergem.</summary>
        public enum ConflictResolution { KeepLocal, KeepCloud, Merge }

        private string MockCloudPath(string storyId, int slot)
        {
            var dir = Path.Combine(Application.persistentDataPath, "mock_cloud", storyId);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"slot_{slot}.json");
        }

        private string MockProgressPath(string storyId)
        {
            var dir = Path.Combine(Application.persistentDataPath, "mock_cloud", storyId);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "progress.json");
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
        }

        // ── Save Sync ───────────────────────────────────────────────

        /// <summary>Faz upload de um save para a cloud. Retorna true em caso de sucesso.</summary>
        public async Task<bool> UploadSave(string storyId, int slot, string saveData)
        {
            if (string.IsNullOrEmpty(storyId) || string.IsNullOrEmpty(saveData))
                return false;

            _isSyncing = true;
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                await Task.Delay(300);
                var path = MockCloudPath(storyId, slot);
                var slotData = new CloudSaveSlot
                {
                    slot = slot,
                    name = $"Save {slot}",
                    sectionId = 0,
                    characterSnapshot = saveData,
                    createdAt = DateTime.UtcNow.ToString("o"),
                    updatedAt = DateTime.UtcNow.ToString("o")
                };
                var json = JsonUtility.ToJson(slotData);
                SecureStorage.SaveToFile(path, json);
#else
                await Task.Delay(500);
                await UploadToFirestore(storyId, slot, saveData);
#endif
                Debug.Log($"[CloudSaveClient] Upload concluido: {storyId}/slot_{slot}");
                OnSyncCompleted?.Invoke(true);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveClient] Upload falhou: {e.Message}");
                OnSyncCompleted?.Invoke(false);
                return false;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>Faz download de um save da cloud. Retorna JSON ou null se nao encontrado.</summary>
        public async Task<string> DownloadSave(string storyId, int slot)
        {
            if (string.IsNullOrEmpty(storyId))
                return null;

            _isSyncing = true;
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                await Task.Delay(200);
                var path = MockCloudPath(storyId, slot);
                var json = SecureStorage.LoadFromFile(path);
                if (json == null)
                {
                    Debug.Log($"[CloudSaveClient] Save nao encontrado na cloud: {storyId}/slot_{slot}");
                    return null;
                }
                var slotData = JsonUtility.FromJson<CloudSaveSlot>(json);
                return slotData?.characterSnapshot;
#else
                await Task.Delay(400);
                return await DownloadFromFirestore(storyId, slot);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveClient] Download falhou: {e.Message}");
                OnSyncCompleted?.Invoke(false);
                return null;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>Remove um save da cloud. Retorna true em caso de sucesso.</summary>
        public async Task<bool> DeleteCloudSave(string storyId, int slot)
        {
            _isSyncing = true;
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                await Task.Delay(150);
                var path = MockCloudPath(storyId, slot);
                SecureStorage.DeleteFile(path);
#else
                await Task.Delay(300);
                await DeleteFromFirestore(storyId, slot);
#endif
                Debug.Log($"[CloudSaveClient] Save removido da cloud: {storyId}/slot_{slot}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveClient] Erro ao remover save: {e.Message}");
                return false;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>Lista todos os saves disponiveis na cloud para uma historia.</summary>
        public async Task<List<CloudSaveSlot>> ListCloudSaves(string storyId)
        {
            _isSyncing = true;
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                await Task.Delay(200);
                var dir = Path.Combine(Application.persistentDataPath, "mock_cloud", storyId);
                if (!Directory.Exists(dir))
                    return new List<CloudSaveSlot>();

                var slots = new List<CloudSaveSlot>();
                foreach (var file in Directory.GetFiles(dir, "slot_*.json"))
                {
                    var json = SecureStorage.LoadFromFile(file);
                    if (json != null)
                    {
                        var slot = JsonUtility.FromJson<CloudSaveSlot>(json);
                        if (slot != null) slots.Add(slot);
                    }
                }
                return slots;
#else
                await Task.Delay(400);
                return await ListFromFirestore(storyId);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveClient] Erro ao listar saves: {e.Message}");
                return new List<CloudSaveSlot>();
            }
            finally
            {
                _isSyncing = false;
            }
        }

        // ── Progress Sync ───────────────────────────────────────────

        /// <summary>Sincroniza o progresso de uma historia com a cloud.</summary>
        public async Task<bool> SyncProgress(string storyId, StoryProgressData progress)
        {
            if (string.IsNullOrEmpty(storyId) || progress == null)
                return false;

            if (!IsOnline)
            {
                Debug.LogWarning("[CloudSaveClient] Offline — sync progress adiado.");
                return false;
            }

            _isSyncing = true;
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                await Task.Delay(250);
                var path = MockProgressPath(storyId);
                var json = JsonUtility.ToJson(progress);
                SecureStorage.SaveToFile(path, json);
#else
                await Task.Delay(500);
                await SyncProgressToFirestore(storyId, progress);
#endif
                Debug.Log($"[CloudSaveClient] Progresso sincronizado: {storyId}");
                OnSyncCompleted?.Invoke(true);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveClient] Sync progress falhou: {e.Message}");
                OnSyncCompleted?.Invoke(false);
                return false;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>Sincroniza o progresso de TODAS as historias locais com a cloud.</summary>
        public async Task SyncAllProgress()
        {
            if (!IsOnline) return;

            var lib = LibraryManager.Instance;
            if (lib == null) return;

            var all = lib.GetAllProgress();
            Debug.Log($"[CloudSaveClient] Iniciando sync de {all.Count} historia(s)...");

            foreach (var progress in all)
            {
                await SyncProgress(progress.storyId, progress);
            }

            Debug.Log("[CloudSaveClient] Sync completo de todas as historias.");
        }

        // ── Conflict Resolution ─────────────────────────────────────

        /// <summary>Define a estrategia de resolucao de conflito padrao.</summary>
        public void SetConflictResolution(ConflictResolution resolution)
        {
            _defaultResolution = resolution;
            Debug.Log($"[CloudSaveClient] Resolucao de conflito alterada para: {resolution}");
        }

        /// <summary>Resolve um conflito manualmente quando KeepLocal ou KeepCloud nao bastam.</summary>
        public async Task<bool> ResolveConflict(string storyId, ConflictResolution resolution)
        {
            Debug.Log($"[CloudSaveClient] Resolvendo conflito para {storyId}: {resolution}");
            _isSyncing = true;
            try
            {
                switch (resolution)
                {
                    case ConflictResolution.KeepLocal:
                        OnConflictDetected?.Invoke("local", "cloud");
                        return true;
                    case ConflictResolution.KeepCloud:
                        await Task.Delay(200);
                        OnConflictDetected?.Invoke("cloud", "local");
                        return true;
                    case ConflictResolution.Merge:
                        await Task.Delay(300);
                        OnConflictDetected?.Invoke("merged", "merged");
                        return true;
                    default:
                        return false;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        // ── Auto-Sync ───────────────────────────────────────────────

        /// <summary>Ativa auto-sync periodico no intervalo especificado (minutos).</summary>
        public void EnableAutoSync(int intervalMinutes = 15)
        {
            DisableAutoSync();
            _autoSyncCoroutine = StartCoroutine(AutoSyncRoutine(intervalMinutes));
            Debug.Log($"[CloudSaveClient] Auto-sync ativado: cada {intervalMinutes} min.");
        }

        /// <summary>Desativa o auto-sync periodico.</summary>
        public void DisableAutoSync()
        {
            if (_autoSyncCoroutine != null)
            {
                StopCoroutine(_autoSyncCoroutine);
                _autoSyncCoroutine = null;
                Debug.Log("[CloudSaveClient] Auto-sync desativado.");
            }
        }

        private IEnumerator AutoSyncRoutine(int intervalMinutes)
        {
            while (true)
            {
                yield return new WaitForSeconds(intervalMinutes * 60f);
                if (IsOnline && !_isSyncing)
                {
                    Debug.Log("[CloudSaveClient] Auto-sync disparado.");
                    yield return SyncAllProgress();
                }
            }
        }

        // ── Stubs: Firestore Integration (production) ───────────────

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        private async Task UploadToFirestore(string storyId, int slot, string saveData)
        {
            throw new NotImplementedException("Firestore upload integration not yet implemented.");
        }

        private async Task<string> DownloadFromFirestore(string storyId, int slot)
        {
            throw new NotImplementedException("Firestore download integration not yet implemented.");
        }

        private async Task DeleteFromFirestore(string storyId, int slot)
        {
            throw new NotImplementedException("Firestore delete integration not yet implemented.");
        }

        private async Task<List<CloudSaveSlot>> ListFromFirestore(string storyId)
        {
            throw new NotImplementedException("Firestore list integration not yet implemented.");
        }

        private async Task SyncProgressToFirestore(string storyId, StoryProgressData progress)
        {
            throw new NotImplementedException("Firestore progress sync not yet implemented.");
        }
#endif
    }
}
