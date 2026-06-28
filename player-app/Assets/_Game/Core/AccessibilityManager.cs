using System;
using UnityEngine;
using UnityEngine.UI;
using InteractiveFantasticTales.UI;

namespace InteractiveFantasticTales.Core
{
    public class AccessibilityManager : MonoBehaviour
    {
        public static AccessibilityManager Instance { get; private set; }

        public AccessibilityProfile CurrentProfile { get; private set; } = new();

        [Header("UI References")]
        [SerializeField] private StoryPanelUI _storyPanel;

        [Header("High Contrast")]
        [SerializeField] private Color _highContrastBg = Color.black;
        [SerializeField] private Color _highContrastText = new(1f, 0.84f, 0f); // gold
        [SerializeField] private Image _backgroundImage;
        private Color _originalBgColor;

        public event Action<AccessibilityProfile> OnProfileChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_backgroundImage != null)
                _originalBgColor = _backgroundImage.color;

            LoadProfile();
        }

        public void SetFontSize(string size)
        {
            CurrentProfile.fontSize = size;
            ApplyProfile();
        }

        public void SetDyslexicFont(bool use)
        {
            CurrentProfile.useDyslexicFont = use;
            ApplyProfile();
        }

        public void SetHighContrast(bool on)
        {
            CurrentProfile.highContrast = on;
            ApplyProfile();
        }

        public void SetAutoNarrate(bool on)
        {
            CurrentProfile.autoNarrate = on;
            Audio.NarrationManager.Instance?.SetAutoNarrate(on);
            ApplyProfile();
        }

        public void SetNarrationSpeed(float speed)
        {
            CurrentProfile.narrationSpeed = speed;
            if (Audio.NarrationManager.Instance != null)
                Audio.NarrationManager.Instance.Speed = speed;
            ApplyProfile();
        }

        public void SetNarrationVoice(string voice)
        {
            CurrentProfile.narrationVoice = voice;
            ApplyProfile();
        }

        private void ApplyProfile()
        {
            if (_storyPanel != null)
                _storyPanel.SetAccessibilityProfile(CurrentProfile);

            if (_backgroundImage != null)
            {
                _backgroundImage.color = CurrentProfile.highContrast
                    ? _highContrastBg
                    : _originalBgColor;
            }

            OnProfileChanged?.Invoke(CurrentProfile);
            SaveProfile();
        }

        private void SaveProfile()
        {
            var json = JsonUtility.ToJson(CurrentProfile);
            PlayerPrefs.SetString("accessibility_profile", json);
        }

        private void LoadProfile()
        {
            var json = PlayerPrefs.GetString("accessibility_profile", "");
            if (!string.IsNullOrEmpty(json))
            {
                try { CurrentProfile = JsonUtility.FromJson<AccessibilityProfile>(json) ?? new(); }
                catch { CurrentProfile = new(); }
            }
            ApplyProfile();
        }
    }
}
