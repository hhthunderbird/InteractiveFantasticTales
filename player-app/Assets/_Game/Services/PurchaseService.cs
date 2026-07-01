using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.Services
{
    [Serializable]
    public class PurchaseVerifyRequest
    {
        public string storyId;
        public string purchaseToken;
        public string store;
        public string idempotencyKey;
    }

    [Serializable]
    public class PurchaseVerifyResponse
    {
        public bool success;
        public string entitlementId;
        public string storyId;
        public string expiresAt;
        public string error;
    }

    [Serializable]
    public class RestorePurchasesResponse
    {
        public bool success;
        public List<EntitlementData> entitlements;
        public string error;
    }

    [Serializable]
    public class EntitlementData
    {
        public string entitlementId;
        public string storyId;
        public string purchasedAt;
        public string expiresAt;
        public string store;
    }

    public class PurchaseService
    {
        private const string CloudRunBaseUrl = "https://ift-api-201008727431.us-central1.run.app";
        private const string EntitlementsCacheKey = "purchase_entitlements";
        private const int MaxRetries = 3;
        private const int RetryDelayBaseMs = 1000;

        public event Action<EntitlementData> OnPurchaseCompleted;
        public event Action<string> OnPurchaseFailed;
        public event Action<List<EntitlementData>> OnEntitlementsUpdated;

        private CloudRunAuthService _authService;
        private List<EntitlementData> _cachedEntitlements = new List<EntitlementData>();
        private bool _initialized;

        public List<EntitlementData> CachedEntitlements => new List<EntitlementData>(_cachedEntitlements);

        public PurchaseService(CloudRunAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public void Initialize()
        {
            if (_initialized) return;
            LoadEntitlementsFromCache();
            _initialized = true;
            Debug.Log("[PurchaseService] Initialized");
        }

        public string GenerateIdempotencyKey()
        {
            return $"{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        public IEnumerator VerifyPurchase(string storyId, string purchaseToken, string store, Action<PurchaseVerifyResponse> callback)
        {
            if (string.IsNullOrEmpty(storyId)) throw new ArgumentNullException(nameof(storyId));
            if (string.IsNullOrEmpty(purchaseToken)) throw new ArgumentNullException(nameof(purchaseToken));
            if (string.IsNullOrEmpty(store)) throw new ArgumentNullException(nameof(store));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var idempotencyKey = GenerateIdempotencyKey();

            var isMock = IsMockMode();
            if (isMock)
            {
                Debug.Log($"[PurchaseService] MOCK: VerifyPurchase storyId={storyId} store={store}");
                yield return new WaitForSeconds(0.5f);
                var mockResp = new PurchaseVerifyResponse
                {
                    success = true,
                    entitlementId = $"mock_ent_{Guid.NewGuid():N}",
                    storyId = storyId,
                    expiresAt = DateTime.UtcNow.AddYears(1).ToString("o")
                };
                AddEntitlement(new EntitlementData
                {
                    entitlementId = mockResp.entitlementId,
                    storyId = storyId,
                    purchasedAt = DateTime.UtcNow.ToString("o"),
                    expiresAt = mockResp.expiresAt,
                    store = store
                });
                callback(mockResp);
                yield break;
            }

            var requestBody = new PurchaseVerifyRequest
            {
                storyId = storyId,
                purchaseToken = purchaseToken,
                store = store,
                idempotencyKey = idempotencyKey
            };

            var jsonBody = JsonUtility.ToJson(requestBody);

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    var delay = RetryDelayBaseMs * (1 << (attempt - 1));
                    Debug.LogWarning($"[PurchaseService] Retry {attempt}/{MaxRetries} after {delay}ms");
                    yield return new WaitForSeconds(delay / 1000f);
                }

                using (var request = new UnityWebRequest($"{CloudRunBaseUrl}/api/purchases/verify", "POST"))
                {
                    request.SetRequestHeader("Content-Type", "application/json");
                    _authService.AttachAuthHeader(request);

                    var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<PurchaseVerifyResponse>(request.downloadHandler.text);
                        if (response != null && response.success)
                        {
                            AddEntitlement(new EntitlementData
                            {
                                entitlementId = response.entitlementId,
                                storyId = response.storyId,
                                purchasedAt = DateTime.UtcNow.ToString("o"),
                                expiresAt = response.expiresAt,
                                store = store
                            });
                            Debug.Log($"[PurchaseService] Purchase verified: {response.entitlementId}");
                            callback(response);
                            yield break;
                        }

                        var errMsg = response?.error ?? "Unknown error";
                        Debug.LogWarning($"[PurchaseService] Verify failed: {errMsg}");
                        OnPurchaseFailed?.Invoke(errMsg);
                        callback(response ?? new PurchaseVerifyResponse { success = false, error = "Empty response" });
                        yield break;
                    }

                    Debug.LogWarning($"[PurchaseService] Request failed (attempt {attempt + 1}): {request.error}");

                    if (attempt == MaxRetries)
                    {
                        OnPurchaseFailed?.Invoke($"Network error after {MaxRetries + 1} attempts: {request.error}");
                        callback(new PurchaseVerifyResponse { success = false, error = request.error });
                    }
                }
            }
        }

        public IEnumerator RestorePurchases()
        {
            if (IsMockMode())
            {
                Debug.Log("[PurchaseService] MOCK: RestorePurchases");
                OnEntitlementsUpdated?.Invoke(new List<EntitlementData>(_cachedEntitlements));
                yield break;
            }

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    var delay = RetryDelayBaseMs * (1 << (attempt - 1));
                    Debug.LogWarning($"[PurchaseService] Restore retry {attempt}/{MaxRetries} after {delay}ms");
                    yield return new WaitForSeconds(delay / 1000f);
                }

                using (var request = UnityWebRequest.Get($"{CloudRunBaseUrl}/api/purchases/restore"))
                {
                    _authService.AttachAuthHeader(request);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<RestorePurchasesResponse>(request.downloadHandler.text);
                        if (response != null && response.success && response.entitlements != null)
                        {
                            _cachedEntitlements = response.entitlements;
                            SaveEntitlementsToCache();
                            Debug.Log($"[PurchaseService] Restored {response.entitlements.Count} entitlements");
                            OnEntitlementsUpdated?.Invoke(new List<EntitlementData>(_cachedEntitlements));
                        }
                        else
                        {
                            Debug.LogWarning($"[PurchaseService] Restore failed: {response?.error ?? "Unknown"}");
                        }
                        yield break;
                    }

                    Debug.LogWarning($"[PurchaseService] Restore request failed (attempt {attempt + 1}): {request.error}");
                }
            }
        }

        public bool HasEntitlement(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return false;
            return _cachedEntitlements.Exists(e => e.storyId == storyId);
        }

        public EntitlementData GetEntitlement(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return null;
            return _cachedEntitlements.Find(e => e.storyId == storyId);
        }

        public List<EntitlementData> GetAllEntitlements()
        {
            return new List<EntitlementData>(_cachedEntitlements);
        }

        private void AddEntitlement(EntitlementData entitlement)
        {
            if (entitlement == null) return;

            var existing = _cachedEntitlements.FindIndex(e => e.storyId == entitlement.storyId);
            if (existing >= 0)
                _cachedEntitlements[existing] = entitlement;
            else
                _cachedEntitlements.Add(entitlement);

            SaveEntitlementsToCache();
            OnPurchaseCompleted?.Invoke(entitlement);
            OnEntitlementsUpdated?.Invoke(new List<EntitlementData>(_cachedEntitlements));
        }

        private void LoadEntitlementsFromCache()
        {
            try
            {
                var path = System.IO.Path.Combine(Application.persistentDataPath, "purchase_entitlements.json");
                var encrypted = SecureStorage.LoadFromFile(path);
                if (!string.IsNullOrEmpty(encrypted))
                {
                    var wrapper = JsonUtility.FromJson<EntitlementListWrapper>(encrypted);
                    if (wrapper?.entitlements != null)
                    {
                        _cachedEntitlements = wrapper.entitlements;
                        Debug.Log($"[PurchaseService] Loaded {_cachedEntitlements.Count} entitlements from cache");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PurchaseService] Failed to load entitlements cache: {e.Message}");
            }
        }

        private void SaveEntitlementsToCache()
        {
            try
            {
                var wrapper = new EntitlementListWrapper { entitlements = _cachedEntitlements };
                var json = JsonUtility.ToJson(wrapper);
                var path = System.IO.Path.Combine(Application.persistentDataPath, "purchase_entitlements.json");
                SecureStorage.SaveToFile(path, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PurchaseService] Failed to save entitlements cache: {e.Message}");
            }
        }

        private static bool IsMockMode()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var auth = CloudRunAuthService.Instance;
            return auth == null || !auth.IsFirebaseReady;
#else
            return false;
#endif
        }

        [Serializable]
        private class EntitlementListWrapper
        {
            public List<EntitlementData> entitlements;
        }
    }
}
