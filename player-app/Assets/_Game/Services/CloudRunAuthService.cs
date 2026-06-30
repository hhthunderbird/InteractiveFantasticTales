using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.Services
{
    [Serializable]
    public class AuthTokenResponse
    {
        public string token;
        public string userId;
        public long expiresAt;
    }

    public class CloudRunAuthService
    {
        private const string CloudRunBaseUrl = "https://api-ift-cloudrun.example.com";
        private const string TokenCacheKey = "auth_cached_token";
        private const string UserIdCacheKey = "auth_user_id";
        private const string TokenExpiryCacheKey = "auth_token_expires";
        private const int TokenTtlMinutes = 55;
        private const int RefreshBeforeExpiryMinutes = 5;

        public event Action<bool> OnAuthStateChanged;

        private string _cachedToken;
        private string _cachedUserId;
        private long _tokenExpiresAt;
        private bool _isAuthenticated;

        public bool IsAuthenticated => _isAuthenticated;

        public void Initialize()
        {
            LoadTokenFromCache();

            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiresAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                _isAuthenticated = true;
                Debug.Log("[CloudRunAuthService] Restored auth session from cache");
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsMockMode() && !_isAuthenticated)
            {
                SetMockAuth();
            }
#endif

            if (!_isAuthenticated)
            {
                Debug.Log("[CloudRunAuthService] No valid cached token - user not authenticated");
            }
        }

        public IEnumerator GetAuthToken(Action<string> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiresAt > now + (RefreshBeforeExpiryMinutes * 60))
            {
                callback(_cachedToken);
                yield break;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsMockMode())
            {
                SetMockAuth();
                callback(_cachedToken);
                yield break;
            }
#endif

            var firebaseAuth = GetFirebaseAuthInstance();
            if (firebaseAuth == null)
            {
                Debug.LogWarning("[CloudRunAuthService] Firebase auth not available");
                callback(null);
                yield break;
            }

            var currentUser = GetFirebaseCurrentUser(firebaseAuth);
            if (currentUser == null)
            {
                Debug.Log("[CloudRunAuthService] No Firebase user signed in");
                SetAuthenticated(false);
                callback(null);
                yield break;
            }

            var taskResult = GetTokenAsync(currentUser);
            yield return new WaitUntil(() => IsTaskCompleted(taskResult));

            if (IsTaskFaulted(taskResult) || IsTaskCanceled(taskResult))
            {
                Debug.LogWarning("[CloudRunAuthService] Token async failed: " + GetTaskExceptionMessage(taskResult));
                callback(null);
                yield break;
            }

            _cachedToken = GetTaskResult(taskResult);
            _cachedUserId = GetFirebaseUserId(currentUser);
            _tokenExpiresAt = now + (TokenTtlMinutes * 60);

            SetAuthenticated(true);
            SaveTokenToCache();
            Debug.Log("[CloudRunAuthService] Token refreshed successfully");
            callback(_cachedToken);
        }

        public string GetAuthTokenSync()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiresAt > now + (RefreshBeforeExpiryMinutes * 60))
                return _cachedToken;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsMockMode())
            {
                SetMockAuth();
                return _cachedToken;
            }
#endif

            var firebaseAuth = GetFirebaseAuthInstance();
            if (firebaseAuth == null) return null;

            var currentUser = GetFirebaseCurrentUser(firebaseAuth);
            if (currentUser == null) return null;

            var taskResult = GetTokenAsync(currentUser);

            try { WaitTask(taskResult, 5000); }
            catch (Exception e) { Debug.LogWarning($"[CloudRunAuthService] WaitTask error: {e.Message}"); return null; }

            if (IsTaskCompleted(taskResult) && !IsTaskFaulted(taskResult) && !IsTaskCanceled(taskResult))
            {
                _cachedToken = GetTaskResult(taskResult);
                _cachedUserId = GetFirebaseUserId(currentUser);
                _tokenExpiresAt = now + (TokenTtlMinutes * 60);
                SetAuthenticated(true);
                SaveTokenToCache();
                return _cachedToken;
            }

            Debug.LogWarning("[CloudRunAuthService] Token task not completed successfully");
            return null;
        }

        public void AttachAuthHeader(UnityWebRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var token = GetAuthTokenSync();
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        public string GetUserId()
        {
            if (!string.IsNullOrEmpty(_cachedUserId))
                return _cachedUserId;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsMockMode())
            {
                SetMockAuth();
                return _cachedUserId;
            }
#endif

            var firebaseAuth = GetFirebaseAuthInstance();
            if (firebaseAuth == null) return null;

            var currentUser = GetFirebaseCurrentUser(firebaseAuth);
            if (currentUser != null)
            {
                _cachedUserId = GetFirebaseUserId(currentUser);
                return _cachedUserId;
            }

            return null;
        }

        public void SignOut()
        {
            _cachedToken = null;
            _cachedUserId = null;
            _tokenExpiresAt = 0;
            SetAuthenticated(false);
            ClearTokenCache();
            Debug.Log("[CloudRunAuthService] User signed out");
        }

        private void SetMockAuth()
        {
            _cachedToken = "mock_id_token_eyJhbGciOiJSUzI1NiI.dev_" + Guid.NewGuid().ToString("N");
            _cachedUserId = "mock_user_ftales_dev1";
            _tokenExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (TokenTtlMinutes * 60);
            SetAuthenticated(true);
        }

        private void SetAuthenticated(bool value)
        {
            if (_isAuthenticated == value) return;
            _isAuthenticated = value;
            OnAuthStateChanged?.Invoke(value);
        }

        private void LoadTokenFromCache()
        {
            try
            {
                _cachedToken = SecureStorage.ObfuscatedPrefs.GetString(TokenCacheKey);
                _cachedUserId = SecureStorage.ObfuscatedPrefs.GetString(UserIdCacheKey);
                var expiryStr = SecureStorage.ObfuscatedPrefs.GetString(TokenExpiryCacheKey);
                if (!string.IsNullOrEmpty(expiryStr) && long.TryParse(expiryStr, out var expiry))
                    _tokenExpiresAt = expiry;

                if (!string.IsNullOrEmpty(_cachedToken))
                    Debug.Log("[CloudRunAuthService] Token loaded from cache");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CloudRunAuthService] Failed to load token cache: {e.Message}");
            }
        }

        private void SaveTokenToCache()
        {
            try
            {
                SecureStorage.ObfuscatedPrefs.SetString(TokenCacheKey, _cachedToken ?? "");
                SecureStorage.ObfuscatedPrefs.SetString(UserIdCacheKey, _cachedUserId ?? "");
                SecureStorage.ObfuscatedPrefs.SetString(TokenExpiryCacheKey, _tokenExpiresAt.ToString());
                SecureStorage.ObfuscatedPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CloudRunAuthService] Failed to save token cache: {e.Message}");
            }
        }

        private void ClearTokenCache()
        {
            SecureStorage.ObfuscatedPrefs.DeleteKey(TokenCacheKey);
            SecureStorage.ObfuscatedPrefs.DeleteKey(UserIdCacheKey);
            SecureStorage.ObfuscatedPrefs.DeleteKey(TokenExpiryCacheKey);
            SecureStorage.ObfuscatedPrefs.Save();
        }

        #region Firebase Reflection Helpers

        private static object GetFirebaseAuthInstance()
        {
            try
            {
                var type = Type.GetType("Firebase.Auth.FirebaseAuth, Firebase.Auth");
                if (type == null) return null;
                var prop = type.GetProperty("DefaultInstance");
                return prop?.GetValue(null);
            }
            catch
            {
                return null;
            }
        }

        private static object GetFirebaseCurrentUser(object firebaseAuth)
        {
            if (firebaseAuth == null) return null;
            try
            {
                var prop = firebaseAuth.GetType().GetProperty("CurrentUser");
                return prop?.GetValue(firebaseAuth);
            }
            catch
            {
                return null;
            }
        }

        private static string GetFirebaseUserId(object firebaseUser)
        {
            if (firebaseUser == null) return null;
            try
            {
                var prop = firebaseUser.GetType().GetProperty("UserId");
                return prop?.GetValue(firebaseUser) as string;
            }
            catch
            {
                return null;
            }
        }

        private static object GetTokenAsync(object firebaseUser)
        {
            if (firebaseUser == null) return null;
            try
            {
                var method = firebaseUser.GetType().GetMethod("TokenAsync", new[] { typeof(bool) });
                return method?.Invoke(firebaseUser, new object[] { true });
            }
            catch
            {
                return null;
            }
        }

        private static bool IsTaskCompleted(object task)
        {
            if (task == null) return false;
            try
            {
                var prop = task.GetType().GetProperty("IsCompleted");
                return prop != null && (bool)prop.GetValue(task);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTaskFaulted(object task)
        {
            if (task == null) return false;
            try
            {
                var prop = task.GetType().GetProperty("IsFaulted");
                return prop != null && (bool)prop.GetValue(task);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTaskCanceled(object task)
        {
            if (task == null) return false;
            try
            {
                var prop = task.GetType().GetProperty("IsCanceled");
                return prop != null && (bool)prop.GetValue(task);
            }
            catch
            {
                return false;
            }
        }

        private static string GetTaskResult(object task)
        {
            if (task == null) return null;
            try
            {
                var prop = task.GetType().GetProperty("Result");
                return prop?.GetValue(task) as string;
            }
            catch
            {
                return null;
            }
        }

        private static string GetTaskExceptionMessage(object task)
        {
            if (task == null) return "null task";
            try
            {
                var prop = task.GetType().GetProperty("Exception");
                var ex = prop?.GetValue(task);
                if (ex == null) return "unknown error";
                var msgProp = ex.GetType().GetProperty("Message");
                return msgProp?.GetValue(ex) as string ?? "unknown error";
            }
            catch
            {
                return "unknown error";
            }
        }

        private static void WaitTask(object task, int millisecondsTimeout)
        {
            if (task == null) return;
            try
            {
                var method = task.GetType().GetMethod("Wait", new[] { typeof(int) });
                method?.Invoke(task, new object[] { millisecondsTimeout });
            }
            catch
            {
            }
        }

        #endregion

        private bool IsMockMode()
        {
            return true;
        }
    }
}
