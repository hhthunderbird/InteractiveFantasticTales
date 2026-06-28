using UnityEngine;
using InteractiveFantasticTales.Core;
using InteractiveFantasticTales.Models;

namespace InteractiveFantasticTales.Audio
{
    public class NarrationManager : MonoBehaviour
    {
        public static NarrationManager Instance { get; private set; }

        [SerializeField] private bool _autoNarrate = true;
        [SerializeField] private float _speed = 1f;
        [SerializeField] private AudioSource _audioSource;

        public bool IsNarrating { get; private set; }
        public float Speed
        {
            get => _speed;
            set => _speed = Mathf.Clamp(value, 0.5f, 2f);
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

            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnSectionChanged += OnSectionChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnSectionChanged -= OnSectionChanged;
            }
        }

        private void OnSectionChanged(SectionData section)
        {
            if (_autoNarrate && !string.IsNullOrEmpty(section.text))
            {
                Speak(section.text);
            }
        }

        public void Speak(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

#if UNITY_ANDROID || UNITY_IOS
            StartCoroutine(SpeakMobile(text));
#else
            SpeakWindows(text);
#endif
        }

        private void SpeakWindows(string text)
        {
            try
            {
                using var synth = new System.Speech.Synthesis.SpeechSynthesizer();
                synth.Rate = Mathf.RoundToInt((_speed - 1f) * 10);
                synth.SpeakAsync(text);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Narration failed: {e.Message}");
            }
        }

        private System.Collections.IEnumerator SpeakMobile(string text)
        {
            IsNarrating = true;
            Debug.Log($"[Narration] {text}");

#if UNITY_ANDROID
            // Android TTS would use AndroidJavaObject to call TextToSpeech
            yield return new WaitForSeconds(text.Length * 0.05f / _speed);
#elif UNITY_IOS
            // iOS TTS would use AVSpeechSynthesizer via plugin
            yield return new WaitForSeconds(text.Length * 0.05f / _speed);
#endif

            IsNarrating = false;
        }

        public void Stop()
        {
            IsNarrating = false;
        }

        public void Pause()
        {
            IsNarrating = false;
        }

        public void Resume()
        {
            IsNarrating = true;
        }

        public void SetAutoNarrate(bool auto)
        {
            _autoNarrate = auto;
        }
    }
}
