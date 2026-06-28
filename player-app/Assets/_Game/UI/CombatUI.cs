using TMPro;
using UnityEngine;
using UnityEngine.UI;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.UI
{
    public class CombatUI : MonoBehaviour
    {
        [Header("Enemy Info")]
        [SerializeField] private TextMeshProUGUI _enemyNameText;
        [SerializeField] private TextMeshProUGUI _enemyStatsText;
        [SerializeField] private Image _enemyHealthBar;

        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI _playerStatsText;

        [Header("Buttons")]
        [SerializeField] private Button _attackButton;
        [SerializeField] private Button _fleeButton;
        [SerializeField] private Button _resolveButton;

        [Header("Log")]
        [SerializeField] private TextMeshProUGUI _combatLog;

        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        private void Start()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnCombatUpdated += UpdateCombatUI;
                GameEngine.Instance.OnStateChanged += OnGameStateChanged;
            }

            if (_attackButton != null)
                _attackButton.onClick.AddListener(() => GameEngine.Instance?.FightRound());
            if (_fleeButton != null)
                _fleeButton.onClick.AddListener(() => GameEngine.Instance?.FleeCombat());
            if (_resolveButton != null)
                _resolveButton.onClick.AddListener(() => GameEngine.Instance?.GoToSection(GameEngine.Instance?.ActiveCombat?.victoryTarget ?? 0));

            if (_panel != null)
                _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnCombatUpdated -= UpdateCombatUI;
                GameEngine.Instance.OnStateChanged -= OnGameStateChanged;
            }
        }

        private void UpdateCombatUI(CombatState combat)
        {
            if (_panel != null)
                _panel.SetActive(true);

            if (_enemyNameText != null)
                _enemyNameText.text = combat.enemyName;

            if (_enemyStatsText != null)
                _enemyStatsText.text = $"SKILL {combat.enemySkill}  STAM {Mathf.Max(0, combat.enemyStamina)}/{combat.enemyMaxStamina}";

            if (_enemyHealthBar != null)
                _enemyHealthBar.fillAmount = Mathf.Max(0, (float)combat.enemyStamina / combat.enemyMaxStamina);

            if (_playerStatsText != null)
            {
                var p = GameEngine.Instance?.PlayerCharacter;
                if (p != null)
                    _playerStatsText.text = $"Você: SKILL {p.skill}  STAM {p.stamina}/{p.maxStamina}";
            }

            if (_attackButton != null)
                _attackButton.gameObject.SetActive(combat.phase == CombatPhase.Fighting);
            if (_fleeButton != null)
                _fleeButton.gameObject.SetActive(combat.phase == CombatPhase.Fighting && combat.allowFlee);
            if (_resolveButton != null)
                _resolveButton.gameObject.SetActive(combat.phase == CombatPhase.Result);
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.CombatActive && _panel != null)
                _panel.SetActive(false);
        }
    }
}
