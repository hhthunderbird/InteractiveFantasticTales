using UnityEngine;
using UnityEngine.UI;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.UI
{
    public class ChoiceButtonsUI : MonoBehaviour
    {
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private GameObject _choiceButtonPrefab;

        private void Start()
        {
            if (GameEngine.Instance != null) GameEngine.Instance.OnSectionChanged += ShowChoices;
        }
        private void OnDestroy() { if (GameEngine.Instance != null) GameEngine.Instance.OnSectionChanged -= ShowChoices; }

        private void ShowChoices(Models.SectionData section)
        {
            foreach (Transform child in _choicesContainer) Destroy(child.gameObject);
            if (section.choices == null) return;
            for (int i = 0; i < section.choices.Count; i++)
            {
                var choice = section.choices[i];
                var btn = Instantiate(_choiceButtonPrefab, _choicesContainer);
                var text = btn.GetComponentInChildren<Text>();
                if (text != null) text.text = $"▶ {choice.text}";
                var button = btn.GetComponent<Button>();
                if (button != null) { int idx = i; button.onClick.AddListener(() => GameEngine.Instance.MakeChoice(idx)); }
            }
        }
    }
}