using TMPro;
using UnityEngine;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.UI
{
    public class CharacterSheetUI : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI _skillText;
        [SerializeField] private TextMeshProUGUI _staminaText;
        [SerializeField] private TextMeshProUGUI _luckText;
        [SerializeField] private TextMeshProUGUI _goldText;

        [Header("Inventory")]
        [SerializeField] private TextMeshProUGUI _inventoryText;
        [SerializeField] private Transform _inventoryContainer;

        [Header("Panel")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _panelGroup;

        private bool _isOpen = false;

        private void Start()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnCharacterUpdated += UpdateSheet;
                if (GameEngine.Instance.PlayerCharacter != null)
                    UpdateSheet(GameEngine.Instance.PlayerCharacter);
            }

            if (_panel != null)
                _panel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.I))
            {
                Toggle();
            }
        }

        private void OnDestroy()
        {
            if (GameEngine.Instance != null)
                GameEngine.Instance.OnCharacterUpdated -= UpdateSheet;
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (_panel != null)
                _panel.SetActive(_isOpen);
        }

        private void UpdateSheet(Models.Character character)
        {
            if (_skillText != null)
                _skillText.text = $"Habilidade: {character.skill}";
            if (_staminaText != null)
                _staminaText.text = $"Vigor: {character.stamina}/{character.maxStamina}";
            if (_luckText != null)
                _luckText.text = $"Sorte: {character.luck}/{character.maxLuck}";
            if (_goldText != null)
                _goldText.text = $"💰 {character.gold}";

            if (_inventoryText != null)
            {
                if (character.inventory.Count == 0)
                    _inventoryText.text = "Inventário vazio";
                else
                    _inventoryText.text = string.Join("\n", character.inventory);
            }
        }
    }
}
