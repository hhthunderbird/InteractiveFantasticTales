using UnityEngine;
using UnityEngine.UI;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales
{
    public class GameController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UI.StoryPanelUI _storyPanel;
        [SerializeField] private UI.ChoiceButtonsUI _choiceButtons;
        [SerializeField] private GameObject _characterSheet;
        [SerializeField] private GameObject _combatPanel;
        [SerializeField] private Text _combatEnemyText;
        [SerializeField] private Text _combatStatsText;
        [SerializeField] private Button _attackButton;
        [SerializeField] private Button _fleeButton;

        [Header("Prefabs")]
        [SerializeField] private GameObject _choiceButtonPrefab;

        private GameEngine _engine;

        private void Awake()
        {
            _engine = gameObject.AddComponent<GameEngine>();
            if (_choiceButtons == null) _choiceButtons = FindObjectOfType<UI.ChoiceButtonsUI>();
            if (_storyPanel == null) _storyPanel = FindObjectOfType<UI.StoryPanelUI>();
        }

        private void Start()
        {
            if (_choiceButtonPrefab != null && _choiceButtons != null)
                _choiceButtons.SendMessage("SetPrefab", _choiceButtonPrefab, SendMessageOptions.DontRequireReceiver);

            if (_attackButton != null) _attackButton.onClick.AddListener(() => _engine.FightRound());
            if (_fleeButton != null) _fleeButton.onClick.AddListener(() => _engine.FleeCombat());

            _engine.OnCombatUpdated += OnCombat;
            _engine.OnStateChanged += OnState;

            _characterSheet?.SetActive(false);
            _combatPanel?.SetActive(false);

            _engine.LoadDemoStory();
        }

        private void OnDestroy()
        {
            if (_engine != null)
            {
                _engine.OnCombatUpdated -= OnCombat;
                _engine.OnStateChanged -= OnState;
            }
        }

        private void OnCombat(CombatState cs)
        {
            _combatPanel?.SetActive(true);
            if (_combatEnemyText != null) _combatEnemyText.text = cs.enemyName;
            if (_combatStatsText != null) _combatStatsText.text = $"SKILL {cs.enemySkill}  STAM {cs.enemyStamina}/{cs.enemyMaxStamina}";
        }

        private void OnState(GameState state)
        {
            if (state == GameState.GameOver) _combatPanel?.SetActive(false);
            _characterSheet?.SetActive(state == GameState.Narrative);
        }

        public void ToggleCharacterSheet()
        {
            if (_characterSheet != null) _characterSheet.SetActive(!_characterSheet.activeSelf);
        }
    }
}