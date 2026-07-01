using System;
using System.Collections;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

namespace InteractiveFantasticTales.Core
{
    public enum FirebaseInitStatus
    {
        NotStarted,
        Initializing,
        Ready,
        Failed
    }

    public class FirebaseBootstrap : MonoBehaviour
    {
        public static FirebaseBootstrap Instance { get; private set; }

        public event Action<bool> OnFirebaseReady;
        public event Action<FirebaseInitStatus> OnStatusChanged;

        public FirebaseInitStatus Status { get; private set; } = FirebaseInitStatus.NotStarted;
        public bool IsReady => Status == FirebaseInitStatus.Ready;
        public FirebaseApp App { get; private set; }
        public FirebaseAuth Auth { get; private set; }

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
            StartCoroutine(InitializeRoutine());
        }

        private IEnumerator InitializeRoutine()
        {
            SetStatus(FirebaseInitStatus.Initializing);

            bool initComplete = false;
            DependencyStatus depStatus = DependencyStatus.UnavailableOther;

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                depStatus = task.Result;
                initComplete = true;
            });

            yield return new WaitUntil(() => initComplete);

            if (depStatus == DependencyStatus.Available)
            {
                try
                {
                    App = FirebaseApp.DefaultInstance;
                    Auth = FirebaseAuth.DefaultInstance;

                    Auth.StateChanged += OnAuthStateChanged;

                    SetStatus(FirebaseInitStatus.Ready);
                    Debug.Log($"[FirebaseBootstrap] Firebase ready — project: {App.Name}");
                    OnFirebaseReady?.Invoke(true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[FirebaseBootstrap] Firebase config error: {e.Message}. " +
                        "Place google-services.json (Android) and GoogleService-Info.plist (iOS) in Assets/.");
                    SetStatus(FirebaseInitStatus.Failed);
                    OnFirebaseReady?.Invoke(false);
                }
            }
            else
            {
                Debug.LogError($"[FirebaseBootstrap] Firebase dependencies not resolved: {depStatus}");
                SetStatus(FirebaseInitStatus.Failed);
                OnFirebaseReady?.Invoke(false);
            }
        }

        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            if (Auth == null) return;

            var user = Auth.CurrentUser;
            if (user != null)
                Debug.Log($"[FirebaseBootstrap] Auth state changed — signed in: {user.UserId}");
            else
                Debug.Log("[FirebaseBootstrap] Auth state changed — signed out");
        }

        private void SetStatus(FirebaseInitStatus status)
        {
            Status = status;
            OnStatusChanged?.Invoke(status);
        }

        private void OnDestroy()
        {
            if (Auth != null)
                Auth.StateChanged -= OnAuthStateChanged;
        }
    }
}
