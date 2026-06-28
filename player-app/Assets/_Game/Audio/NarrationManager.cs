using UnityEngine;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.Audio
{
    public class NarrationManager : MonoBehaviour
    {
        public static NarrationManager Instance { get; private set; }
        [SerializeField] private bool _autoNarrate = true;
        [SerializeField] private float _speed = 1f;

        public bool IsNarrating { get; private set; }

        private void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); }

        private void Start()
        {
            if (GameEngine.Instance != null)
                GameEngine.Instance.OnSectionChanged += OnSectionChanged;
        }

        private void OnDestroy()
        {
            if (GameEngine.Instance != null)
                GameEngine.Instance.OnSectionChanged -= OnSectionChanged;
        }

        private void OnSectionChanged(Models.SectionData section)
        {
            if (_autoNarrate && !string.IsNullOrEmpty(section.text))
                Speak(section.text);
        }

        public void Speak(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            IsNarrating = true;
            Debug.Log($"[NARRATOR] {text}");
            IsNarrating = false;
        }

        public void Stop() { IsNarrating = false; }
        public void Pause() { IsNarrating = false; }
        public void Resume() { IsNarrating = true; }
        public void SetAutoNarrate(bool on) { _autoNarrate = on; }
        public void SetSpeed(float s) { _speed = Mathf.Clamp(s, 0.5f, 2f); }
    }
}