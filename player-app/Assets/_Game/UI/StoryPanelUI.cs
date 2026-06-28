using TMPro;
using UnityEngine;
using UnityEngine.UI;
using InteractiveFantasticTales.Core;
using InteractiveFantasticTales.Models;

namespace InteractiveFantasticTales.UI
{
    public class StoryPanelUI : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _sectionText;
        [SerializeField] private TextMeshProUGUI _sectionHeader;

        [Header("Illustration")]
        [SerializeField] private Image _illustration;

        [Header("Message")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private CanvasGroup _messageGroup;

        [Header("Accessibility")]
        [SerializeField] private TMP_FontAsset _defaultFont;
        [SerializeField] private TMP_FontAsset _dyslexicFont;
        [SerializeField] private float _fontSizeSmall = 16f;
        [SerializeField] private float _fontSizeMedium = 22f;
        [SerializeField] private float _fontSizeLarge = 30f;
        [SerializeField] private float _fontSizeHuge = 40f;

        private void Start()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnSectionChanged += ShowSection;
                GameEngine.Instance.OnMessage += ShowMessage;
                GameEngine.Instance.OnStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnSectionChanged -= ShowSection;
                GameEngine.Instance.OnMessage -= ShowMessage;
                GameEngine.Instance.OnStateChanged -= OnGameStateChanged;
            }
        }

        private void ShowSection(SectionData section)
        {
            if (_sectionHeader != null)
                _sectionHeader.text = $"Seção #{section.id}";

            if (_sectionText != null)
                _sectionText.text = section.text ?? "(sem texto)";

            if (_illustration != null)
            {
                _illustration.enabled = !string.IsNullOrEmpty(section.presentation?.illustration);
            }

            HideMessage();
        }

        private void ShowMessage(string message)
        {
            if (_messageText != null)
                _messageText.text = message;
            if (_messageGroup != null)
            {
                _messageGroup.alpha = 1;
                StopAllCoroutines();
                StartCoroutine(FadeMessage());
            }
        }

        private System.Collections.IEnumerator FadeMessage()
        {
            yield return new WaitForSeconds(3f);
            float t = 0;
            while (t < 1 && _messageGroup != null)
            {
                t += Time.deltaTime;
                _messageGroup.alpha = Mathf.Lerp(1, 0, t);
                yield return null;
            }
        }

        private void HideMessage()
        {
            if (_messageGroup != null)
                _messageGroup.alpha = 0;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                if (_sectionText != null)
                    _sectionText.text += "\n\n━━━━━━━━━━━━━━━━\nFim da Aventura";
            }
        }

        public void SetAccessibilityProfile(AccessibilityProfile profile)
        {
            if (_sectionText == null) return;

            _sectionText.fontSize = profile.fontSize switch
            {
                "small" => _fontSizeSmall,
                "medium" => _fontSizeMedium,
                "large" => _fontSizeLarge,
                "huge" => _fontSizeHuge,
                _ => _fontSizeMedium
            };

            _sectionText.font = profile.useDyslexicFont ? _dyslexicFont : _defaultFont;
        }
    }

    public class AccessibilityProfile
    {
        public string fontSize = "medium";
        public bool useDyslexicFont = false;
        public bool highContrast = false;
        public bool autoNarrate = true;
        public float narrationSpeed = 1f;
        public string narrationVoice = "default";
    }
}
