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
    public class AssetUrlRequest
    {
        public string storyId;
        public string assetPath;
    }

    [Serializable]
    public class AssetUrlResponse
    {
        public bool success;
        public string signedUrl;
        public string assetPath;
        public long expiresAt;
        public string error;
    }

    [Serializable]
    public class PrefetchAssetUrlsRequest
    {
        public string storyId;
        public string[] paths;
    }

    [Serializable]
    public class PrefetchAssetUrlsResponse
    {
        public bool success;
        public List<AssetUrlResponse> urls;
        public string error;
    }

    [Serializable]
    public class EntitlementCheckResponse
    {
        public bool success;
        public bool hasEntitlement;
        public string error;
    }

    public class SignedUrlService
    {
        private static SignedUrlService _instance;
        public static SignedUrlService Instance => _instance ??= new SignedUrlService(null);

        private const string CloudRunBaseUrl = "https://ift-api-201008727431.us-central1.run.app";
        private const int DefaultUrlTtlSeconds = 900;
        private const int MaxRetries = 3;
        private const int RetryDelayBaseMs = 1000;

        private CloudRunAuthService _authService;
        private Dictionary<string, long> _urlCache = new Dictionary<string, long>();
        private bool _initialized;

        public SignedUrlService(CloudRunAuthService authService)
        {
            _authService = authService;
        }

        public void SetAuthService(CloudRunAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            Debug.Log("[SignedUrlService] Initialized");
        }

        public IEnumerator GetAssetUrl(string storyId, string assetPath, Action<AssetUrlResponse> callback)
        {
            if (string.IsNullOrEmpty(storyId)) throw new ArgumentNullException(nameof(storyId));
            if (string.IsNullOrEmpty(assetPath)) throw new ArgumentNullException(nameof(assetPath));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var cacheKey = $"{storyId}:{assetPath}";

            if (_urlCache.TryGetValue(cacheKey, out var expiresAt))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (now < expiresAt)
                {
                    var cachedResponse = new AssetUrlResponse
                    {
                        success = true,
                        signedUrl = cacheKey,
                        assetPath = assetPath,
                        expiresAt = expiresAt
                    };
                    callback(cachedResponse);
                    yield break;
                }
                _urlCache.Remove(cacheKey);
            }

            if (IsMockMode())
            {
                yield return new WaitForSeconds(0.1f);
                var mockUrl = $"https://storage.mock.local/ifts/{storyId}/{assetPath}?mock_sig=dev";
                var mockExpiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + DefaultUrlTtlSeconds;
                _urlCache[cacheKey] = mockExpiry;
                callback(new AssetUrlResponse
                {
                    success = true,
                    signedUrl = mockUrl,
                    assetPath = assetPath,
                    expiresAt = mockExpiry
                });
                yield break;
            }

            var requestBody = new AssetUrlRequest { storyId = storyId, assetPath = assetPath };
            var jsonBody = JsonUtility.ToJson(requestBody);

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    var delay = RetryDelayBaseMs * (1 << (attempt - 1));
                    yield return new WaitForSeconds(delay / 1000f);
                }

                using (var request = new UnityWebRequest($"{CloudRunBaseUrl}/api/stories/{storyId}/asset-url", "POST"))
                {
                    request.SetRequestHeader("Content-Type", "application/json");
                    _authService?.AttachAuthHeader(request);

                    var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<AssetUrlResponse>(request.downloadHandler.text);
                        if (response != null && response.success)
                        {
                            _urlCache[cacheKey] = response.expiresAt;
                            callback(response);
                            yield break;
                        }

                        callback(response ?? new AssetUrlResponse { success = false, error = "Empty response" });
                        yield break;
                    }

                    if (attempt == MaxRetries)
                    {
                        callback(new AssetUrlResponse { success = false, error = request.error });
                    }
                }
            }
        }

        public IEnumerator GetThumbnailUrl(string storyId, string size, Action<AssetUrlResponse> callback)
        {
            if (string.IsNullOrEmpty(storyId)) throw new ArgumentNullException(nameof(storyId));
            if (string.IsNullOrEmpty(size)) throw new ArgumentNullException(nameof(size));
            if (size != "cover_128" && size != "cover_512")
                size = "cover_128";

            var assetPath = $"thumbnails/{size}.jpg";
            yield return GetAssetUrl(storyId, assetPath, callback);
        }

        public IEnumerator GetStoryJsonUrl(string storyId, string version, Action<AssetUrlResponse> callback)
        {
            if (string.IsNullOrEmpty(storyId)) throw new ArgumentNullException(nameof(storyId));
            if (string.IsNullOrEmpty(version)) throw new ArgumentNullException(nameof(version));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var assetPath = $"data/story_{version}.json";
            yield return GetAssetUrl(storyId, assetPath, callback);
        }

        public IEnumerator VerifyEntitlement(string storyId, Action<EntitlementCheckResponse> callback)
        {
            if (string.IsNullOrEmpty(storyId)) throw new ArgumentNullException(nameof(storyId));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (IsMockMode())
            {
                yield return new WaitForSeconds(0.1f);
                callback(new EntitlementCheckResponse { success = true, hasEntitlement = true });
                yield break;
            }

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    var delay = RetryDelayBaseMs * (1 << (attempt - 1));
                    yield return new WaitForSeconds(delay / 1000f);
                }

                using (var request = UnityWebRequest.Get($"{CloudRunBaseUrl}/api/stories/{storyId}/entitlement"))
                {
                    _authService?.AttachAuthHeader(request);
                    request.downloadHandler = new DownloadHandlerBuffer();

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<EntitlementCheckResponse>(request.downloadHandler.text);
                        if (response != null)
                        {
                            callback(response);
                            yield break;
                        }
                    }

                    if (attempt == MaxRetries)
                    {
                        callback(new EntitlementCheckResponse { success = false, hasEntitlement = false, error = request.error });
                    }
                }
            }
        }

        public IEnumerator PrefetchAssetUrls(string storyId, string[] paths, Action<PrefetchAssetUrlsResponse> callback)
        {
            if (string.IsNullOrEmpty(storyId)) throw new ArgumentNullException(nameof(storyId));
            if (paths == null || paths.Length == 0) throw new ArgumentNullException(nameof(paths));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (IsMockMode())
            {
                yield return new WaitForSeconds(0.2f);
                var mockUrls = new List<AssetUrlResponse>();
                var mockExpiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + DefaultUrlTtlSeconds;
                foreach (var path in paths)
                {
                    var cacheKey = $"{storyId}:{path}";
                    var mockUrl = $"https://storage.mock.local/ifts/{storyId}/{path}?mock_sig=dev";
                    _urlCache[cacheKey] = mockExpiry;
                    mockUrls.Add(new AssetUrlResponse
                    {
                        success = true,
                        signedUrl = mockUrl,
                        assetPath = path,
                        expiresAt = mockExpiry
                    });
                }
                callback(new PrefetchAssetUrlsResponse { success = true, urls = mockUrls });
                yield break;
            }

            var requestBody = new PrefetchAssetUrlsRequest { storyId = storyId, paths = paths };
            var jsonBody = JsonUtility.ToJson(requestBody);

            using (var request = new UnityWebRequest($"{CloudRunBaseUrl}/api/stories/{storyId}/asset-urls/batch", "POST"))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                _authService?.AttachAuthHeader(request);

                var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<PrefetchAssetUrlsResponse>(request.downloadHandler.text);
                    if (response != null && response.success && response.urls != null)
                    {
                        foreach (var url in response.urls)
                        {
                            if (url.success)
                                _urlCache[$"{storyId}:{url.assetPath}"] = url.expiresAt;
                        }
                        callback(response);
                        yield break;
                    }
                }

                callback(new PrefetchAssetUrlsResponse { success = false, error = request.error });
            }
        }

        public void ClearUrlCache()
        {
            _urlCache.Clear();
        }

        public void ClearUrlCache(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;
            var prefix = $"{storyId}:";
            var keysToRemove = new List<string>();
            foreach (var key in _urlCache.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                    keysToRemove.Add(key);
            }
            foreach (var key in keysToRemove)
                _urlCache.Remove(key);
        }

        public int GetCachedUrlCount() => _urlCache.Count;

        private static bool IsMockMode()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var auth = CloudRunAuthService.Instance;
            return auth == null || !auth.IsFirebaseReady;
#else
            return false;
#endif
        }
    }
}
