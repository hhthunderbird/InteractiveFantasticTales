using UnityEngine;
using UnityEngine.UI;

namespace InteractiveFantasticTales.Core
{
    public class AccessibilityManager : MonoBehaviour
    {
        public static AccessibilityManager Instance { get; private set; }

        [SerializeField] private UI.StoryPanelUI _storyPanel;
        [SerializeField] private Image _background;

        private int _fontSizeLevel = 1;
        private bool _highContrast = false;
        private bool _dyslexic = false;

        private readonly string[] _sizes = { "small", "medium", "large", "huge" };
        private Color _origBg = Color.black;

        private void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); }

        private void Start()
        {
            if (_background != null) _origBg = _background.color;
            LoadPrefs();
        }

        public void CycleFontSize()
        {
            _fontSizeLevel = (_fontSizeLevel + 1) % _sizes.Length;
            Apply();
        }

        public void ToggleHighContrast() { _highContrast = !_highContrast; Apply(); }
        public void ToggleDyslexic() { _dyslexic = !_dyslexic; Apply(); }

        private void Apply()
        {
            if (_storyPanel != null) _storyPanel.SetAccessibility(_sizes[_fontSizeLevel], _dyslexic);
            if (_background != null) _background.color = _highContrast ? Color.black : _origBg;
            SavePrefs();
        }

        private void SavePrefs()
        {
            PlayerPrefs.SetInt("a11y_font", _fontSizeLevel);
            PlayerPrefs.SetInt("a11y_contrast", _highContrast ? 1 : 0);
            PlayerPrefs.SetInt("a11y_dyslexic", _dyslexic ? 1 : 0);
        }

        private void LoadPrefs()
        {
            _fontSizeLevel = PlayerPrefs.GetInt("a11y_font", 1);
            _highContrast = PlayerPrefs.GetInt("a11y_contrast", 0) == 1;
            _dyslexic = PlayerPrefs.GetInt("a11y_dyslexic", 0) == 1;
            Apply();
        }
    }
}