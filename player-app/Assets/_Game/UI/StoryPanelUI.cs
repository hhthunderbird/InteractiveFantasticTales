using UnityEngine;
using UnityEngine.UI;
using InteractiveFantasticTales.Core;
using InteractiveFantasticTales.Models;

namespace InteractiveFantasticTales.UI
{
    public class StoryPanelUI : MonoBehaviour
    {
        [SerializeField] private Text _sectionText;
        [SerializeField] private Text _sectionHeader;
        [SerializeField] private Image _illustration;
        [SerializeField] private Text _messageText;
        [SerializeField] private CanvasGroup _messageGroup;
        [SerializeField] private Font _defaultFont;
        [SerializeField] private Font _dyslexicFont;
        [SerializeField] private int _fontSizeSmall = 16, _fontSizeMedium = 22, _fontSizeLarge = 30, _fontSizeHuge = 40;

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
            if (_sectionHeader != null) _sectionHeader.text = $"Secao #{section.id}";
            if (_sectionText != null) _sectionText.text = section.text ?? "(sem texto)";
            if (_illustration != null) _illustration.enabled = !string.IsNullOrEmpty(section.presentation?.illustration);
            HideMessage();
        }

        private void ShowMessage(string msg)
        {
            if (_messageText != null) _messageText.text = msg;
            if (_messageGroup != null) { _messageGroup.alpha = 1; StopAllCoroutines(); StartCoroutine(FadeMessage()); }
        }

        private System.Collections.IEnumerator FadeMessage() { yield return new WaitForSeconds(3f); float t = 0; while (t < 1 && _messageGroup != null) { t += Time.deltaTime; _messageGroup.alpha = Mathf.Lerp(1, 0, t); yield return null; } }
        private void HideMessage() { if (_messageGroup != null) _messageGroup.alpha = 0; }
        private void OnGameStateChanged(GameState state) { if (state == GameState.GameOver && _sectionText != null) _sectionText.text += "\n\n--- Fim da Aventura ---"; }

        public void SetAccessibility(string fontSize, bool dyslexic)
        {
            if (_sectionText == null) return;
            _sectionText.fontSize = fontSize == "small" ? _fontSizeSmall : fontSize == "medium" ? _fontSizeMedium : fontSize == "large" ? _fontSizeLarge : _fontSizeHuge;
            if (_dyslexicFont != null && _defaultFont != null) _sectionText.font = dyslexic ? _dyslexicFont : _defaultFont;
        }
    }
}