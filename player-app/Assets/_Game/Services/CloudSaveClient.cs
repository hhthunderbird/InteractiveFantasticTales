using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
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

    public class CloudSaveClient : MonoBehaviour
    {
        public static CloudSaveClient Instance;

        public event Action<bool> OnSyncCompleted;
        public event Action<string, string> OnConflictDetected;

        private FirebaseFirestore _db;
        private bool _firestoreReady;
        private bool _isSyncing;
        private ConflictResolution _defaultResolution = ConflictResolution.KeepLocal;
        private Coroutine _autoSyncCoroutine;

        public bool IsSyncing => _isSyncing;
        public bool IsOnline => Application.internetReachability != NetworkReachability.NotReachable;

        public enum ConflictResolution { KeepLocal, KeepCloud, Merge }

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

        private void Start()
        {
            if (FirebaseBootstrap.Instance != null)
            {
                FirebaseBootstrap.Instance.OnFirebaseReady += OnFirebaseReady;
                if (FirebaseBootstrap.Instance.IsReady)
                    BindFirestore();
            }
        }

        private void OnFirebaseReady(bool ready)
        {
            FirebaseBootstrap.Instance.OnFirebaseReady -= OnFirebaseReady;
            if (ready) BindFirestore();
        }

        private void BindFirestore()
        {
            try
            {
                _db = FirebaseFirestore.DefaultInstance;
                _firestoreReady = true;
                Debug.Log("[CloudSaveClient] Bound to Firestore");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveClient] Firestore bind failed: {e.Message}");
                _firestoreReady = false;
            }
        }

        private bool CanUseFirestore()
        {
            return _firestoreReady && _db != null && IsOnline;
        }

        private string GetUserId()
        {
            var userId = CloudRunAuthService.Instance?.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return "mock_user_ftales_dev1";
#else
                throw new InvalidOperationException("User not authenticated");
#endif
            }
            return userId;
        }

        // ── Save Upload ───────────────────────────────────────────────

        public async Task<bool> UploadSave(string storyId, int slot, string saveData)
        {
            if (string.IsNullOrEmpty(storyId) || string.IsNullOrEmpty(saveData))
                return false;

            _isSyncing = true;
            try
            {
                if (CanUseFirestore())
                {
                    await UploadToFirestore(storyId, slot, saveData);
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    await Task.Delay(300);
                    WriteMockSave(storyId, slot, saveData);
#else
                    throw new InvalidOperationException("Firestore not available and no mock fallback in production");
#endif
                }

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

        private async Task UploadToFirestore(string storyId, int slot, string saveData)
        {
            var userId = GetUserId();
            var saveRef = _db
                .Collection("users").Document(userId)
                .Collection("storyProgress").Document(storyId)
                .Collection("saves").Document(slot.ToString());

            var data = new Dictionary<string, object>
            {
                ["slot"] = slot,
                ["characterSnapshot"] = saveData,
                ["updatedAt"] = FieldValue.ServerTimestamp,
                ["createdAt"] = FieldValue.ServerTimestamp,
            };

            await saveRef.SetAsync(data, SetOptions.MergeAll);
        }

        // ── Save Download ─────────────────────────────────────────────

        public async Task<string> DownloadSave(string storyId, int slot)
        {
            if (string.IsNullOrEmpty(storyId))
                return null;

            _isSyncing = true;
            try
            {
                if (CanUseFirestore())
                {
                    return await DownloadFromFirestore(storyId, slot);
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                await Task.Delay(200);
                return ReadMockSave(storyId, slot);
#else
                return null;
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

        private async Task<string> DownloadFromFirestore(string storyId, int slot)
        {
            var userId = GetUserId();
            var saveRef = _db
                .Collection("users").Document(userId)
                .Collection("storyProgress").Document(storyId)
                .Collection("saves").Document(slot.ToString());

            var snapshot = await saveRef.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            return snapshot.GetValue<string>("characterSnapshot");
        }

        // ── Save Delete ───────────────────────────────────────────────

        public async Task<bool> DeleteCloudSave(string storyId, int slot)
        {
            _isSyncing = true;
            try
            {
                if (CanUseFirestore())
                {
                    await DeleteFromFirestore(storyId, slot);
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else
                {
                    await Task.Delay(150);
                    DeleteMockSave(storyId, slot);
                }
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

        private async Task DeleteFromFirestore(string storyId, int slot)
        {
            var userId = GetUserId();
            var saveRef = _db
                .Collection("users").Document(userId)
                .Collection("storyProgress").Document(storyId)
                .Collection("saves").Document(slot.ToString());

            await saveRef.DeleteAsync();
        }

        // ── List Saves ────────────────────────────────────────────────

        public async Task<List<CloudSaveSlot>> ListCloudSaves(string storyId)
        {
            _isSyncing = true;
            try
            {
                if (CanUseFirestore())
                {
                    return await ListFromFirestore(storyId);
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                await Task.Delay(200);
                return ListMockSaves(storyId);
#else
                return new List<CloudSaveSlot>();
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

        private async Task<List<CloudSaveSlot>> ListFromFirestore(string storyId)
        {
            var userId = GetUserId();
            var snapshot = await _db
                .Collection("users").Document(userId)
                .Collection("storyProgress").Document(storyId)
                .Collection("saves")
                .GetSnapshotAsync();

            var slots = new List<CloudSaveSlot>();
            foreach (var doc in snapshot.Documents)
            {
                if (!doc.Exists) continue;
                slots.Add(new CloudSaveSlot
                {
                    slot = doc.GetValue<int?>("slot") ?? 0,
                    name = doc.GetValue<string>("name") ?? $"Save {doc.Id}",
                    sectionId = doc.GetValue<int?>("sectionId") ?? 0,
                    characterSnapshot = doc.GetValue<string>("characterSnapshot"),
                    createdAt = doc.GetValue<string>("createdAt"),
                    updatedAt = doc.GetValue<string>("updatedAt"),
                });
            }

            return slots;
        }

        // ── Progress Sync ─────────────────────────────────────────────

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
                if (CanUseFirestore())
                {
                    await SyncProgressToFirestore(storyId, progress);
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else
                {
                    await Task.Delay(250);
                    WriteMockProgress(storyId, progress);
                }
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

        private async Task SyncProgressToFirestore(string storyId, StoryProgressData progress)
        {
            var userId = GetUserId();
            var progressRef = _db
                .Collection("users").Document(userId)
                .Collection("storyProgress").Document(storyId);

            var data = new Dictionary<string, object>
            {
                ["storyId"] = storyId,
                ["lastPlayedAt"] = progress.lastPlayedAt ?? DateTime.UtcNow.ToString("o"),
                ["totalPlaytimeSeconds"] = progress.totalPlaytimeSeconds,
                ["sessionsPlayed"] = progress.sessionsPlayed,
                ["sectionsVisited"] = progress.sectionsVisited,
                ["uniqueSectionsVisited"] = progress.uniqueSectionsVisited,
                ["totalSections"] = progress.totalSections,
                ["choicesMade"] = progress.choicesMade,
                ["endingsFoundCount"] = progress.endingsFoundCount,
                ["totalEndings"] = progress.totalEndings,
                ["completionPercent"] = progress.completionPercent,
                ["combatsWon"] = progress.combatsWon,
                ["combatsLost"] = progress.combatsLost,
                ["combatsFled"] = progress.combatsFled,
                ["characterDeaths"] = progress.characterDeaths,
                ["rewindsUsed"] = progress.rewindsUsed,
                ["lastSectionId"] = progress.lastSectionId,
                ["hasActiveGame"] = progress.hasActiveGame,
                ["activeSaveSlot"] = progress.activeSaveSlot,
                ["updatedAt"] = FieldValue.ServerTimestamp,
            };

            await progressRef.SetAsync(data, SetOptions.MergeAll);
        }

        public async Task SyncAllProgress()
        {
            if (!IsOnline) return;

            var lib = LibraryManager.Instance;
            if (lib == null) return;

            var all = lib.GetAllProgress();
            Debug.Log($"[CloudSaveClient] Iniciando sync de {all.Count} historia(s)...");

            if (CanUseFirestore() && all.Count > 0)
            {
                var userId = GetUserId();
                var batch = _db.StartBatch();

                foreach (var progress in all)
                {
                    var progressRef = _db
                        .Collection("users").Document(userId)
                        .Collection("storyProgress").Document(progress.storyId);

                    var data = new Dictionary<string, object>
                    {
                        ["lastPlayedAt"] = progress.lastPlayedAt ?? DateTime.UtcNow.ToString("o"),
                        ["completionPercent"] = progress.completionPercent,
                        ["lastSectionId"] = progress.lastSectionId,
                        ["hasActiveGame"] = progress.hasActiveGame,
                        ["updatedAt"] = FieldValue.ServerTimestamp,
                    };

                    batch.Set(progressRef, data, SetOptions.MergeAll);
                }

                try
                {
                    await batch.CommitAsync();
                    Debug.Log($"[CloudSaveClient] Batch sync: {all.Count} historia(s) sincronizadas.");
                    OnSyncCompleted?.Invoke(true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CloudSaveClient] Batch sync falhou: {e.Message}");
                    OnSyncCompleted?.Invoke(false);
                }
            }
            else
            {
                foreach (var progress in all)
                    await SyncProgress(progress.storyId, progress);
            }

            Debug.Log("[CloudSaveClient] Sync completo de todas as historias.");
        }

        // ── Conflict Resolution ───────────────────────────────────────

        public void SetConflictResolution(ConflictResolution resolution)
        {
            _defaultResolution = resolution;
            Debug.Log($"[CloudSaveClient] Resolucao de conflito alterada para: {resolution}");
        }

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

        // ── Auto-Sync ─────────────────────────────────────────────────

        public void EnableAutoSync(int intervalMinutes = 15)
        {
            DisableAutoSync();
            _autoSyncCoroutine = StartCoroutine(AutoSyncRoutine(intervalMinutes));
            Debug.Log($"[CloudSaveClient] Auto-sync ativado: cada {intervalMinutes} min.");
        }

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
                    _ = SyncAllProgress();
                }
            }
        }

        // ── Mock Persistence (Editor / Dev Build) ────────────────────

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string MockCloudPath(string storyId, int slot)
        {
            var dir = System.IO.Path.Combine(Application.persistentDataPath, "mock_cloud", storyId);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, $"slot_{slot}.json");
        }

        private string MockProgressPath(string storyId)
        {
            var dir = System.IO.Path.Combine(Application.persistentDataPath, "mock_cloud", storyId);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "progress.json");
        }

        private void WriteMockSave(string storyId, int slot, string saveData)
        {
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
            SecureStorage.SaveToFile(MockCloudPath(storyId, slot), json);
        }

        private string ReadMockSave(string storyId, int slot)
        {
            var json = SecureStorage.LoadFromFile(MockCloudPath(storyId, slot));
            if (json == null) return null;
            var slotData = JsonUtility.FromJson<CloudSaveSlot>(json);
            return slotData?.characterSnapshot;
        }

        private void DeleteMockSave(string storyId, int slot)
        {
            SecureStorage.DeleteFile(MockCloudPath(storyId, slot));
        }

        private List<CloudSaveSlot> ListMockSaves(string storyId)
        {
            var dir = System.IO.Path.Combine(Application.persistentDataPath, "mock_cloud", storyId);
            if (!System.IO.Directory.Exists(dir)) return new List<CloudSaveSlot>();

            var slots = new List<CloudSaveSlot>();
            foreach (var file in System.IO.Directory.GetFiles(dir, "slot_*.json"))
            {
                var json = SecureStorage.LoadFromFile(file);
                if (json != null)
                {
                    var slot = JsonUtility.FromJson<CloudSaveSlot>(json);
                    if (slot != null) slots.Add(slot);
                }
            }
            return slots;
        }

        private void WriteMockProgress(string storyId, StoryProgressData progress)
        {
            var json = JsonUtility.ToJson(progress);
            SecureStorage.SaveToFile(MockProgressPath(storyId), json);
        }
#endif
    }
}
