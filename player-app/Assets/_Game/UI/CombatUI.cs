using UnityEngine;
using UnityEngine.UI;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.UI
{
    public class CombatUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _enemyNameText;
        [SerializeField] private Text _enemyStatsText;
        [SerializeField] private Image _enemyHealthBar;
        [SerializeField] private Text _playerStatsText;
        [SerializeField] private Button _attackButton;
        [SerializeField] private Button _fleeButton;

        private GameEngine _engine;

        private void Start()
        {
            _engine = GameEngine.Instance;
            if (_engine != null)
            {
                _engine.OnCombatUpdated += UpdateUI;
                _engine.OnStateChanged += OnStateChanged;
            }
            if (_attackButton != null) _attackButton.onClick.AddListener(() => _engine?.FightRound());
            if (_fleeButton != null) _fleeButton.onClick.AddListener(() => _engine?.FleeCombat());
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_engine != null)
            {
                _engine.OnCombatUpdated -= UpdateUI;
                _engine.OnStateChanged -= OnStateChanged;
            }
        }

        private void UpdateUI(CombatState cs)
        {
            if (_panel != null) _panel.SetActive(true);
            if (_enemyNameText != null) _enemyNameText.text = cs.enemyName;
            if (_enemyStatsText != null)
                _enemyStatsText.text = $"SKILL {cs.enemySkill}  STAM {Mathf.Max(0, cs.enemyStamina)}/{cs.enemyMaxStamina}";
            if (_enemyHealthBar != null && cs.enemyMaxStamina > 0)
                _enemyHealthBar.fillAmount = Mathf.Max(0, (float)cs.enemyStamina / cs.enemyMaxStamina);
            if (_playerStatsText != null && _engine?.PlayerCharacter != null)
            {
                var p = _engine.PlayerCharacter;
                _playerStatsText.text = $"SKILL {p.skill}  STAM {p.stamina}/{p.maxStamina}  SORTE {p.luck}";
            }
            if (_attackButton != null) _attackButton.gameObject.SetActive(cs.phase == CombatPhase.Fighting);
            if (_fleeButton != null) _fleeButton.gameObject.SetActive(cs.phase == CombatPhase.Fighting && cs.allowFlee);
        }

        private void OnStateChanged(GameState state)
        {
            if (state != GameState.CombatActive && _panel != null)
                _panel.SetActive(false);
        }
    }
}