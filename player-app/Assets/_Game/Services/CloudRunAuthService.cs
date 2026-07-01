using System;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;
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

    public class CloudRunAuthService : MonoBehaviour
    {
        public static CloudRunAuthService Instance;

        private const string TokenCacheKey = "auth_cached_token";
        private const string UserIdCacheKey = "auth_user_id";
        private const string TokenExpiryCacheKey = "auth_token_expires";
        private const int TokenTtlMinutes = 55;
        private const int RefreshBeforeExpiryMinutes = 5;

        public event Action<bool> OnAuthStateChanged;

        private FirebaseAuth _firebaseAuth;
        private FirebaseUser _currentUser;
        private string _cachedToken;
        private string _cachedUserId;
        private long _tokenExpiresAt;
        private bool _isAuthenticated;
        private bool _firebaseReady;

        public bool Initialized { get; private set; }
        public bool IsAuthenticated => _isAuthenticated;
        public bool IsFirebaseReady => _firebaseReady;
        public string CachedUserId => _cachedUserId;

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
            Initialize();
        }

        public void Initialize()
        {
            if (Initialized) return;

            LoadTokenFromCache();
            Initialized = true;

            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiresAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                _isAuthenticated = true;
                Debug.Log("[CloudRunAuthService] Restored auth session from cache");
            }

            if (FirebaseBootstrap.Instance != null)
            {
                FirebaseBootstrap.Instance.OnFirebaseReady += OnFirebaseReadyHandler;
                if (FirebaseBootstrap.Instance.IsReady)
                    BindToFirebase();
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[CloudRunAuthService] FirebaseBootstrap not available — using mock mode");
                SetMockAuth();
#endif
            }
        }

        private void OnFirebaseReadyHandler(bool ready)
        {
            FirebaseBootstrap.Instance.OnFirebaseReady -= OnFirebaseReadyHandler;
            if (ready)
                BindToFirebase();
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[CloudRunAuthService] Firebase not ready — using mock mode");
                SetMockAuth();
#else
                Debug.LogError("[CloudRunAuthService] Firebase initialization failed — auth unavailable");
#endif
            }
        }

        private void BindToFirebase()
        {
            _firebaseAuth = FirebaseBootstrap.Instance.Auth;
            if (_firebaseAuth == null)
            {
                Debug.LogError("[CloudRunAuthService] FirebaseAuth is null after bootstrap");
                return;
            }

            _firebaseAuth.StateChanged += OnFirebaseStateChanged;
            _firebaseReady = true;
            _currentUser = _firebaseAuth.CurrentUser;

            if (_currentUser != null)
            {
                _cachedUserId = _currentUser.UserId;
                if (!_isAuthenticated)
                    SetAuthenticated(true);

                _ = RefreshTokenAsync();
            }
            else
            {
                _ = SignInAnonymouslyAsync();
            }

            Debug.Log("[CloudRunAuthService] Bound to Firebase Auth");
        }

        private void OnFirebaseStateChanged(object sender, EventArgs e)
        {
            if (_firebaseAuth == null) return;

            _currentUser = _firebaseAuth.CurrentUser;

            if (_currentUser != null)
            {
                _cachedUserId = _currentUser.UserId;
                if (!_isAuthenticated)
                    SetAuthenticated(true);
                _ = RefreshTokenAsync();
            }
            else
            {
                SetAuthenticated(false);
            }
        }

        public async Task<string> GetAuthTokenAsync()
        {
            var token = GetAuthTokenSync();
            if (!string.IsNullOrEmpty(token))
                return token;

            var refreshed = await RefreshTokenAsync();
            return refreshed ? _cachedToken : null;
        }

        public string GetAuthTokenSync()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiresAt > now + (RefreshBeforeExpiryMinutes * 60))
                return _cachedToken;

            if (_cachedToken != null && _tokenExpiresAt > now)
                return _cachedToken;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_firebaseReady)
            {
                SetMockAuth();
                return _cachedToken;
            }
#endif

            if (_firebaseReady && _currentUser != null)
            {
                _ = RefreshTokenAsync();
                return _cachedToken;
            }

            return null;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            if (!_firebaseReady || _currentUser == null)
                return false;

            try
            {
                var token = await _currentUser.TokenAsync(true);
                _cachedToken = token;
                _cachedUserId = _currentUser.UserId;
                _tokenExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (TokenTtlMinutes * 60);
                SaveTokenToCache();
                SetAuthenticated(true);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CloudRunAuthService] Token refresh failed: {e.Message}");
                return false;
            }
        }

        public async Task<bool> SignInAnonymouslyAsync()
        {
            if (!_firebaseReady)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                SetMockAuth();
                return true;
#else
                return false;
#endif
            }

            try
            {
                var signInResult = await _firebaseAuth.SignInAnonymouslyAsync();
                _currentUser = signInResult.User;
                _cachedUserId = _currentUser.UserId;

                await RefreshTokenAsync();
                Debug.Log($"[CloudRunAuthService] Anonymous sign-in successful: {_currentUser.UserId}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudRunAuthService] Anonymous sign-in failed: {e.Message}");
                SetAuthenticated(false);
                return false;
            }
        }

        public async Task<bool> LinkWithGoogleAsync(string idToken)
        {
            if (!_firebaseReady || _currentUser == null)
                return false;

            try
            {
                var credential = GoogleAuthProvider.GetCredential(idToken, null);
                var linkResult = await _currentUser.LinkWithCredentialAsync(credential);
                _currentUser = linkResult.User;
                await RefreshTokenAsync();
                Debug.Log($"[CloudRunAuthService] Google account linked: {_currentUser.UserId}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CloudRunAuthService] Google link failed: {e.Message}");
                return false;
            }
        }

        public async Task<bool> SignInWithGoogleAsync(string idToken)
        {
            if (!_firebaseReady)
                return false;

            try
            {
                var credential = GoogleAuthProvider.GetCredential(idToken, null);
                _currentUser = await _firebaseAuth.SignInWithCredentialAsync(credential);
                _cachedUserId = _currentUser.UserId;
                await RefreshTokenAsync();
                Debug.Log($"[CloudRunAuthService] Google sign-in: {_currentUser.UserId}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudRunAuthService] Google sign-in failed: {e.Message}");
                return false;
            }
        }

        public void AttachAuthHeader(UnityEngine.Networking.UnityWebRequest request)
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
            if (!_firebaseReady)
            {
                SetMockAuth();
                return _cachedUserId;
            }
#endif

            if (_firebaseReady && _currentUser != null)
            {
                _cachedUserId = _currentUser.UserId;
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
            _currentUser = null;

            if (_firebaseReady && _firebaseAuth != null)
            {
                _firebaseAuth.SignOut();
            }

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

        private void OnDestroy()
        {
            if (_firebaseAuth != null)
                _firebaseAuth.StateChanged -= OnFirebaseStateChanged;
        }
    }
}
