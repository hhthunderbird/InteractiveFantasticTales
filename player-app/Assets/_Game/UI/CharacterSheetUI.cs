using UnityEngine;
using UnityEngine.UI;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.UI
{
    public class CharacterSheetUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _statsText;
        [SerializeField] private Text _inventoryText;

        private GameEngine _engine;

        private void Start()
        {
            _engine = GameEngine.Instance;
            if (_engine != null) _engine.OnCharacterUpdated += UpdateSheet;
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_engine != null) _engine.OnCharacterUpdated -= UpdateSheet;
        }

        public void Toggle()
        {
            if (_panel != null) _panel.SetActive(!_panel.activeSelf);
            if (_panel != null && _panel.activeSelf && _engine?.PlayerCharacter != null)
                UpdateSheet(_engine.PlayerCharacter);
        }

        private void UpdateSheet(Models.Character c)
        {
            if (_statsText != null)
                _statsText.text = $"Habilidade: {c.skill}\nVigor: {c.stamina}/{c.maxStamina}\nSorte: {c.luck}/{c.maxLuck}\nOuro: {c.gold}";
            if (_inventoryText != null)
                _inventoryText.text = c.inventory.Count > 0 ? string.Join("\n", c.inventory) : "Inventario vazio";
        }
    }
}