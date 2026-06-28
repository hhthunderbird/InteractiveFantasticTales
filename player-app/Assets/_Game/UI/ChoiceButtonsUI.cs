using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using InteractiveFantasticTales.Core;
using InteractiveFantasticTales.Models;

namespace InteractiveFantasticTales.UI
{
    public class ChoiceButtonsUI : MonoBehaviour
    {
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private GameObject _choiceButtonPrefab;
        [SerializeField] private int _maxChoices = 4;

        private readonly List<GameObject> _activeButtons = new();

        private void Start()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnSectionChanged += ShowChoices;
                GameEngine.Instance.OnStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnSectionChanged -= ShowChoices;
                GameEngine.Instance.OnStateChanged -= OnGameStateChanged;
            }
        }

        private void ShowChoices(SectionData section)
        {
            ClearButtons();

            if (section.choices == null || section.choices.Count == 0) return;

            for (int i = 0; i < Mathf.Min(section.choices.Count, _maxChoices); i++)
            {
                var choice = section.choices[i];
                var btn = Instantiate(_choiceButtonPrefab, _choicesContainer);
                var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    bool canChoose = EvaluateConditions(choice.conditions);
                    text.text = $"{(canChoose ? "▶" : "🔒")} {choice.text}";
                    text.color = canChoose ? Color.white : Color.gray;
                }

                var button = btn.GetComponent<Button>();
                if (button != null)
                {
                    int idx = i;
                    bool canChoose = EvaluateConditions(choice.conditions);
                    button.interactable = canChoose;
                    button.onClick.AddListener(() => GameEngine.Instance.MakeChoice(idx));
                }

                _activeButtons.Add(btn);
            }
        }

        private void ClearButtons()
        {
            foreach (var btn in _activeButtons)
                Destroy(btn);
            _activeButtons.Clear();
        }

        private bool EvaluateConditions(List<ConditionData> conditions)
        {
            if (conditions == null || conditions.Count == 0) return true;
            var player = GameEngine.Instance?.PlayerCharacter;
            if (player == null) return false;

            foreach (var cond in conditions)
            {
                switch (cond.type)
                {
                    case "hasItem":
                        if (!player.HasItem(cond.key)) return false;
                        break;
                    case "hasFlag":
                        if (!player.HasFlag(cond.key)) return false;
                        break;
                }
            }
            return true;
        }

        private void OnGameStateChanged(GameState state)
        {
            bool show = state == GameState.Narrative;
            if (_choicesContainer != null)
                _choicesContainer.gameObject.SetActive(show);
        }
    }
}
