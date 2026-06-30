using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using InteractiveFantasticTales.Core;
using InteractiveFantasticTales.Models;

namespace InteractiveFantasticTales.UI.UIToolkit
{
    public class MainUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private PanelSettings _panelSettings;

        private VisualElement _root;
        private VisualElement _appShell;
        private VisualElement _header;
        private Label _storyTitle, _sectionBadge;
        private Label _pillVigorVal, _pillLuckVal, _pillGoldVal;
        private VisualElement _illustration;
        private Label _illusIcon, _illusLabel;
        private ScrollView _scrollArea;
        private Label _narrativeText;
        private VisualElement _choicesContainer;
        private VisualElement _combatCard;
        private Label _combatEnemyName, _combatEnemyStats, _combatDiceResult;
        private VisualElement _hpBarFill;
        private Label _combatSkillVal, _combatStamVal;
        private Button _btnAttack, _btnFlee;
        private VisualElement _endingCard;
        private Label _endingIcon, _endingTitle, _endingStats;
        private VisualElement _toolbar;
        private VisualElement _charCreate;
        private Label _charCreateTitle, _startingGear;
        private VisualElement _rollGrid;
        private Button _btnStartAdventure;
        private Label _toast;

        private VisualElement _diceOverlay;
        private Label _diceLabel, _diceResult;
        private VisualElement _diceRow;

        private VisualElement _bridgeOverlay;
        private Label _bridgeIcon, _bridgeLabel;

        private VisualElement _helpOverlay;

        private Dictionary<string, VisualElement> _sheetOverlays = new();
        private string _openSheetId;

        private bool _narrationOn, _vibrationOn = true, _charShortcutsOn = true, _dysOn;
        private int _fsLevel = 1;

        private const string ILLUS_TYPE_NARRATIVE = "type-narrative";
        private const string ILLUS_TYPE_COMBAT = "type-combat";
        private const string ILLUS_TYPE_TEST = "type-test";
        private const string ILLUS_TYPE_ITEMGATE = "type-itemGate";
        private const string ILLUS_TYPE_RANDOM = "type-random";
        private const string ILLUS_TYPE_ENDING = "type-ending";

        private static readonly string[] ILLUS_CLASSES = {
            ILLUS_TYPE_NARRATIVE, ILLUS_TYPE_COMBAT, ILLUS_TYPE_TEST,
            ILLUS_TYPE_ITEMGATE, ILLUS_TYPE_RANDOM, ILLUS_TYPE_ENDING
        };

        private static readonly Dictionary<string, string> ICONS = new()
        {
            { "narrative", "\U0001F4D6" }, { "combat", "\u2694\uFE0F" },
            { "test", "\U0001F3B2" }, { "itemGate", "\U0001F5DD\uFE0F" },
            { "random", "\U0001F3B0" }, { "ending", "\U0001F3C1" }
        };

        private static readonly Dictionary<string, string> ICONS_LG = new()
        {
            { "narrative", "\U0001F3F0" }, { "combat", "\U0001F47A" },
            { "test", "\U0001F3B2" }, { "itemGate", "\U0001F6AA" },
            { "random", "\U0001F3B0" }, { "ending", "\u2B50" }
        };

        private static readonly Dictionary<string, string> TYPE_LABELS = new()
        {
            { "narrative", "Narrativa" }, { "combat", "Combate" },
            { "test", "Teste" }, { "itemGate", "Portao" },
            { "random", "Acaso" }, { "ending", "Final" }
        };

        private static readonly string[] ARROWS = { "\u2192", "\u2190", "\u2191", "\u2193" };

        [SerializeField] private StyleSheet _mainStyleSheet;

        private LocalizationManager L => LocalizationManager.Instance;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                Debug.LogError("MainUIController: No UIDocument found!");
                return;
            }

            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            if (_mainStyleSheet == null)
                _mainStyleSheet = Resources.Load<StyleSheet>("UIToolkit/MainUI");
            if (_mainStyleSheet != null && !_root.styleSheets.Contains(_mainStyleSheet))
                _root.styleSheets.Add(_mainStyleSheet);

            BindElements();
            BindEvents();
            BuildHelpContent();

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged += OnLocaleChanged;
        }

        private void Start()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnSectionChanged += ShowSection;
                GameEngine.Instance.OnCharacterUpdated += UpdateStats;
                GameEngine.Instance.OnCombatUpdated += ShowCombat;
                GameEngine.Instance.OnMessage += ShowToast;
                GameEngine.Instance.OnStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameEngine.Instance != null)
            {
                GameEngine.Instance.OnSectionChanged -= ShowSection;
                GameEngine.Instance.OnCharacterUpdated -= UpdateStats;
                GameEngine.Instance.OnCombatUpdated -= ShowCombat;
                GameEngine.Instance.OnMessage -= ShowToast;
                GameEngine.Instance.OnStateChanged -= OnGameStateChanged;
            }

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged -= OnLocaleChanged;
        }

        private void BindElements()
        {
            _appShell = _root.Q<VisualElement>("app-shell");
            _header = _root.Q<VisualElement>("header");
            _storyTitle = _root.Q<Label>("story-title");
            _sectionBadge = _root.Q<Label>("section-badge");
            _pillVigorVal = _root.Q<Label>("pill-vigor-val");
            _pillLuckVal = _root.Q<Label>("pill-luck-val");
            _pillGoldVal = _root.Q<Label>("pill-gold-val");
            _illustration = _root.Q<VisualElement>("illustration");
            _illusIcon = _root.Q<Label>("illus-icon");
            _illusLabel = _root.Q<Label>("illus-label");
            _scrollArea = _root.Q<ScrollView>("scroll-area");
            _narrativeText = _root.Q<Label>("narrative-text");
            _choicesContainer = _root.Q<VisualElement>("choices-container");
            _combatCard = _root.Q<VisualElement>("combat-card");
            _combatEnemyName = _root.Q<Label>("combat-enemy-name");
            _combatEnemyStats = _root.Q<Label>("combat-enemy-stats");
            _hpBarFill = _root.Q<VisualElement>("hp-bar-fill");
            _combatSkillVal = _root.Q<Label>("combat-stat-val-skill");
            _combatStamVal = _root.Q<Label>("combat-stat-val-stam");
            _combatDiceResult = _root.Q<Label>("combat-dice-result");
            _btnAttack = _root.Q<Button>("btn-attack");
            _btnFlee = _root.Q<Button>("btn-flee");
            _endingCard = _root.Q<VisualElement>("ending-card");
            _endingIcon = _root.Q<Label>("ending-icon");
            _endingTitle = _root.Q<Label>("ending-title");
            _endingStats = _root.Q<Label>("ending-stats");
            _toolbar = _root.Q<VisualElement>("toolbar");
            _charCreate = _root.Q<VisualElement>("char-create");
            _charCreateTitle = _root.Q<Label>("char-create-title");
            _startingGear = _root.Q<Label>("starting-gear");
            _rollGrid = _root.Q<VisualElement>("roll-grid");
            _btnStartAdventure = _root.Q<Button>("btn-start-adventure");
            _toast = _root.Q<Label>("toast");
            _diceOverlay = _root.Q<VisualElement>("dice-overlay");
            _diceLabel = _root.Q<Label>("dice-label");
            _diceResult = _root.Q<Label>("dice-result");
            _diceRow = _root.Q<VisualElement>("dice-row");
            _bridgeOverlay = _root.Q<VisualElement>("bridge-overlay");
            _bridgeIcon = _root.Q<Label>("bridge-icon");
            _bridgeLabel = _root.Q<Label>("bridge-label");
            _helpOverlay = _root.Q<VisualElement>("help-overlay");

            _sheetOverlays["char-sheet"] = _root.Q<VisualElement>("char-sheet-overlay");
            _sheetOverlays["inv-sheet"] = _root.Q<VisualElement>("inv-sheet-overlay");
            _sheetOverlays["settings-sheet"] = _root.Q<VisualElement>("settings-sheet-overlay");
            _sheetOverlays["save-sheet"] = _root.Q<VisualElement>("save-sheet-overlay");
            _sheetOverlays["rewind-sheet"] = _root.Q<VisualElement>("rewind-sheet-overlay");
        }

        private void BindEvents()
        {
            var hamburger = _root.Q<Button>("hamburger-btn");
            if (hamburger != null) hamburger.clicked += () => OpenSheet("settings-sheet");

            if (_btnAttack != null) _btnAttack.clicked += () => GameEngine.Instance?.FightRound();
            if (_btnFlee != null) _btnFlee.clicked += () => GameEngine.Instance?.FleeCombat();

            if (_btnStartAdventure != null) _btnStartAdventure.clicked += StartAdventure;

            var tbNarrate = _root.Q<Button>("tb-narrate");
            if (tbNarrate != null) tbNarrate.clicked += ToggleNarration;
            var tbChar = _root.Q<Button>("tb-char");
            if (tbChar != null) tbChar.clicked += () => ToggleSheet("char-sheet");
            var tbItems = _root.Q<Button>("tb-items");
            if (tbItems != null) tbItems.clicked += () => ToggleSheet("inv-sheet");
            var tbSave = _root.Q<Button>("tb-save");
            if (tbSave != null) tbSave.clicked += () => ToggleSheet("save-sheet");
            var tbRewind = _root.Q<Button>("tb-rewind");
            if (tbRewind != null) tbRewind.clicked += () => ToggleSheet("rewind-sheet");
            var tbSettings = _root.Q<Button>("tb-settings");
            if (tbSettings != null) tbSettings.clicked += () => ToggleSheet("settings-sheet");

            var charClose = _root.Q<Button>("char-sheet-close");
            if (charClose != null) charClose.clicked += () => CloseSheet("char-sheet");
            var invClose = _root.Q<Button>("inv-sheet-close");
            if (invClose != null) invClose.clicked += () => CloseSheet("inv-sheet");
            var settingsClose = _root.Q<Button>("settings-sheet-close");
            if (settingsClose != null) settingsClose.clicked += () => CloseSheet("settings-sheet");
            var saveClose = _root.Q<Button>("save-sheet-close");
            if (saveClose != null) saveClose.clicked += () => CloseSheet("save-sheet");
            var rewindClose = _root.Q<Button>("rewind-sheet-close");
            if (rewindClose != null) rewindClose.clicked += () => CloseSheet("rewind-sheet");

            var helpCloseBtn = _root.Q<Button>("help-close-btn");
            if (helpCloseBtn != null) helpCloseBtn.clicked += CloseHelp;

            var btnDys = _root.Q<Button>("btn-dys");
            if (btnDys != null) btnDys.clicked += ToggleDyslexic;
            var btnTts = _root.Q<Button>("btn-tts");
            if (btnTts != null) btnTts.clicked += ToggleNarration;
            var btnVib = _root.Q<Button>("btn-vib");
            if (btnVib != null) btnVib.clicked += ToggleVibration;
            var btnShortcuts = _root.Q<Button>("btn-shortcuts");
            if (btnShortcuts != null) btnShortcuts.clicked += ToggleCharShortcuts;

            for (int i = 0; i <= 3; i++)
            {
                int level = i;
                var fsBtn = _root.Q<Button>($"fs{level}");
                if (fsBtn != null) fsBtn.clicked += () => SetFontSize(level);
            }

            var thDark = _root.Q<Button>("th-dark");
            if (thDark != null) thDark.clicked += () => SetTheme("dark");
            var thSepia = _root.Q<Button>("th-sepia");
            if (thSepia != null) thSepia.clicked += () => SetTheme("sepia");
            var thContrast = _root.Q<Button>("th-contrast");
            if (thContrast != null) thContrast.clicked += () => SetTheme("contrast");

            var langPt = _root.Q<Button>("lang-pt"); if (langPt != null) langPt.clicked += () => SetLanguage("pt-BR");
            var langEn = _root.Q<Button>("lang-en"); if (langEn != null) langEn.clicked += () => SetLanguage("en");
            var langEs = _root.Q<Button>("lang-es"); if (langEs != null) langEs.clicked += () => SetLanguage("es");

            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);

            foreach (var kv in _sheetOverlays)
            {
                kv.Value?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target == kv.Value)
                        CloseSheet(kv.Key);
                });
            }

            _helpOverlay?.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == _helpOverlay)
                    CloseHelp();
            });
        }

        private void BuildHelpContent()
        {
            if (_helpOverlay == null) return;
            var nav = _helpOverlay.Q<VisualElement>("help-nav");
            AddHelpRow(nav, "Escolher opcao 1-4", "1 2 3 4");
            AddHelpRow(nav, "Ficha do personagem", "C");
            AddHelpRow(nav, "Inventario", "I");
            AddHelpRow(nav, "Salvar/Carregar", "S");
            AddHelpRow(nav, "Linha do tempo", "T");
            AddHelpRow(nav, "Ajustes", ",");
            AddHelpRow(nav, "Fechar painel / Voltar", "Esc");
            AddHelpRow(nav, "Esta ajuda", "?");

            var combat = _helpOverlay.Q<VisualElement>("help-combat");
            AddHelpRow(combat, "Atacar", "A");
            AddHelpRow(combat, "Fugir", "F");

            var narr = _helpOverlay.Q<VisualElement>("help-narr");
            AddHelpRow(narr, "Ligar/Desligar TTS", "N");
            AddHelpRow(narr, "Ler secao atual", "L");

            var gestures = _helpOverlay.Q<VisualElement>("help-gestures");
            AddHelpRow(gestures, "Cima/Baixo/Esq/Dir", "Escolhas 1-4");
            AddHelpRow(gestures, "Circulo horario", "Avancar");
            AddHelpRow(gestures, "Circulo anti-horario", "Voltar");
        }

        private void AddHelpRow(VisualElement parent, string action, string keys)
        {
            var row = new VisualElement();
            row.AddToClassList("help-row");
            var act = new Label(action);
            act.AddToClassList("help-act");
            var kbd = new Label(keys);
            row.Add(act);
            row.Add(kbd);
            parent.Add(row);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.target is TextField) return;

            if (evt.keyCode == KeyCode.Escape)
            {
                evt.StopPropagation();
                if (!string.IsNullOrEmpty(_openSheetId))
                {
                    CloseSheet(_openSheetId);
                    return;
                }
                if (_helpOverlay.ClassListContains("open"))
                {
                    CloseHelp();
                    return;
                }
                return;
            }

            if (evt.character == '?' || evt.keyCode == KeyCode.F1)
            {
                evt.StopPropagation();
                if (_helpOverlay.ClassListContains("open")) CloseHelp();
                else OpenHelp();
                return;
            }

            if (!_charShortcutsOn) return;

            if (evt.character >= '1' && evt.character <= '9')
            {
                int num = evt.character - '0';
                evt.StopPropagation();

                if (_openSheetId == "rewind-sheet")
                {
                    var hist = GameEngine.Instance?.NavigationHistory;
                    if (hist != null && num <= hist.Count)
                    {
                        RewindTo(num - 1);
                    }
                    return;
                }

                var sec = GameEngine.Instance?.CurrentSection;
                if (sec != null && sec.type == "narrative" && sec.choices != null && num <= sec.choices.Count)
                {
                    GameEngine.Instance?.MakeChoice(num - 1);
                }
                return;
            }

            if (evt.ctrlKey || evt.altKey) return;

            switch (evt.character)
            {
                case 'c': case 'C':
                    evt.StopPropagation(); ToggleSheet("char-sheet"); break;
                case 'i': case 'I':
                    evt.StopPropagation(); ToggleSheet("inv-sheet"); break;
                case 's': case 'S':
                    evt.StopPropagation(); ToggleSheet("save-sheet"); break;
                case 't': case 'T':
                    evt.StopPropagation(); ToggleSheet("rewind-sheet"); break;
                case ',':
                    evt.StopPropagation(); ToggleSheet("settings-sheet"); break;
                case 'n': case 'N':
                    evt.StopPropagation(); ToggleNarration(); break;
                case 'l': case 'L':
                    evt.StopPropagation(); ReadCurrentSection(); break;
                case 'a': case 'A':
                    if (_combatCard.style.display == DisplayStyle.Flex)
                    { evt.StopPropagation(); GameEngine.Instance?.FightRound(); }
                    break;
                case 'f': case 'F':
                    if (_combatCard.style.display == DisplayStyle.Flex)
                    { evt.StopPropagation(); GameEngine.Instance?.FleeCombat(); }
                    break;
            }
        }

        // ===== SECTION DISPLAY =====

        public void ShowSection(SectionData section)
        {
            if (section == null) return;

            StartCoroutine(SectionTransition(section));
        }

        private IEnumerator SectionTransition(SectionData section)
        {
            _scrollArea.AddToClassList("fade-out");
            yield return new WaitForSeconds(0.18f);

            _sectionBadge.text = $"Secao {section.id}";
            _storyTitle.text = GameEngine.Instance?.CurrentStory?.metadata?.title ?? "IFT";

            SetIllustration(section.type);

            _combatCard.style.display = DisplayStyle.None;
            _endingCard.style.display = DisplayStyle.None;
            _choicesContainer.Clear();

            switch (section.type)
            {
                case "narrative":
                    ShowNarrative(section);
                    break;
                case "combat":
                    ShowNarrative(section);
                    break;
                case "test":
                    ShowNarrative(section);
                    _choicesContainer.Clear();
                    break;
                case "itemGate":
                    ShowNarrative(section);
                    _choicesContainer.Clear();
                    break;
                case "random":
                    ShowNarrative(section);
                    _choicesContainer.Clear();
                    break;
                case "ending":
                    ShowEnding(section);
                    break;
            }

            _scrollArea.scrollOffset = Vector2.zero;
            _scrollArea.RemoveFromClassList("fade-out");
            yield return null;
        }

        private void ShowNarrative(SectionData section)
        {
            _narrativeText.text = section.text ?? "";
            _choicesContainer.Clear();

            if (section.choices != null && section.choices.Count > 0)
            {
                for (int i = 0; i < section.choices.Count; i++)
                {
                    var choice = section.choices[i];
                    var failed = GetFailedConditions(choice.conditions);
                    bool locked = failed.Count > 0;
                    int choiceIndex = i;

                    var btn = new Button();
                    btn.AddToClassList("choice-btn");
                    if (locked) btn.AddToClassList("locked");
                    btn.tabIndex = i + 2;
                    btn.focusable = true;

                    var keyBadge = new Label((i + 1).ToString());
                    keyBadge.AddToClassList("ch-key-badge");

                    string tally = new string('|', i + 1);
                    var tallyLabel = new Label(tally);
                    tallyLabel.AddToClassList("ch-tally");

                    var arrow = new Label(ARROWS[i % ARROWS.Length]);
                    arrow.AddToClassList("ch-arrow");

                    var textContainer = new VisualElement();
                    textContainer.style.flexGrow = 1;

                    var textLabel = new Label(choice.text);
                    textLabel.AddToClassList("ch-text");
                    textContainer.Add(textLabel);

                    if (locked)
                    {
                        var reason = new Label($"\U0001F512 {failed[0].reason}");
                        reason.AddToClassList("ch-lock-reason");
                        textContainer.Add(reason);
                        btn.tooltip = failed[0].reason;
                    }

                    btn.Add(keyBadge);
                    btn.Add(tallyLabel);
                    btn.Add(arrow);
                    btn.Add(textContainer);

                    if (!locked)
                    {
                        btn.clicked += () => GameEngine.Instance?.MakeChoice(choiceIndex);
                    }

                    btn.RegisterCallback<NavigationSubmitEvent>(evt =>
                    {
                        if (!locked) GameEngine.Instance?.MakeChoice(choiceIndex);
                    });

                    _choicesContainer.Add(btn);
                }

                StartCoroutine(FocusFirstChoice());
            }
        }

        private IEnumerator FocusFirstChoice()
        {
            yield return null;
            var first = _choicesContainer.Q<Button>(className: "choice-btn");
            if (first != null && !first.ClassListContains("locked"))
                first.Focus();
        }

        private List<FailedCondition> GetFailedConditions(List<ConditionData> conds)
        {
            var failed = new List<FailedCondition>();
            if (conds == null || conds.Count == 0) return failed;

            var player = GameEngine.Instance?.PlayerCharacter;
            if (player == null) return failed;

            foreach (var c in conds)
            {
                if (c.type == "hasItem" && !player.HasItem(c.key))
                    failed.Add(new FailedCondition { reason = $"Requer: {c.key}", type = "item" });
                else if (c.type == "hasFlag" && !player.HasFlag(c.key))
                    failed.Add(new FailedCondition { reason = $"Requer flag: {c.key}", type = "flag" });
            }
            return failed;
        }

        // ===== COMBAT =====

        public void ShowCombat(CombatState cs)
        {
            if (cs == null) return;

            _combatCard.style.display = DisplayStyle.Flex;
            _combatEnemyName.text = cs.enemyName;
            _combatEnemyStats.text = $"SKILL {cs.enemySkill}  STAM {Mathf.Max(0, cs.enemyStamina)}/{cs.enemyMaxStamina}";

            float pct = Mathf.Max(0, (float)cs.enemyStamina / cs.enemyMaxStamina * 100f);
            _hpBarFill.style.width = Length.Percent(pct);
            _hpBarFill.RemoveFromClassList("low");
            if (pct < 25) _hpBarFill.AddToClassList("low");

            var player = GameEngine.Instance?.PlayerCharacter;
            if (player != null)
            {
                _combatSkillVal.text = player.skill.ToString();
                _combatStamVal.text = $"{player.stamina}/{player.maxStamina}";
            }

            _combatDiceResult.text = "";
            _combatDiceResult.style.display = DisplayStyle.None;

            _btnAttack.SetEnabled(cs.phase == CombatPhase.Fighting);
            _btnFlee.SetEnabled(cs.phase == CombatPhase.Fighting && cs.allowFlee);

            if (cs.allowFlee)
                _btnFlee.style.display = DisplayStyle.Flex;
            else
                _btnFlee.style.display = DisplayStyle.None;
        }

        public void ShowCombatDiceResult(string text)
        {
            _combatDiceResult.text = text;
            _combatDiceResult.style.display = DisplayStyle.Flex;
            StartCoroutine(AnimateCombatResult());
        }

        private IEnumerator AnimateCombatResult()
        {
            _combatDiceResult.style.scale = new Scale(Vector2.one * 0.5f);
            _combatDiceResult.style.opacity = 0;

            float t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float p = t / 0.3f;
                _combatDiceResult.style.scale = new Scale(Vector2.one * Mathf.Lerp(0.5f, 1f, p));
                _combatDiceResult.style.opacity = Mathf.Lerp(0, 1, p);
                yield return null;
            }

            _combatDiceResult.style.scale = new Scale(Vector2.one);
            _combatDiceResult.style.opacity = 1;
        }

        public void PlayCombatShake()
        {
            StartCoroutine(CombatShakeRoutine());
        }

        private IEnumerator CombatShakeRoutine()
        {
            float duration = 0.35f;
            float elapsed = 0;
            float[] offsets = { -6, 6, -4, 4, -2, 2, 0 };
            int idx = 0;
            float interval = duration / offsets.Length;

            while (elapsed < duration && idx < offsets.Length)
            {
                _combatCard.style.translate = new Translate(offsets[idx], 0, 0);
                elapsed += interval;
                idx++;
                yield return new WaitForSeconds(interval);
            }
            _combatCard.style.translate = new Translate(0, 0, 0);
        }

        public void PlayEnemyHitEffect()
        {
            StartCoroutine(EnemyHitRoutine());
        }

        private IEnumerator EnemyHitRoutine()
        {
            var icon = _root.Q<Label>("combat-enemy-icon");
            if (icon == null) yield break;

            _combatCard.AddToClassList("enemy-hit");
            icon.AddToClassList("hit");

            yield return new WaitForSeconds(0.5f);

            _combatCard.RemoveFromClassList("enemy-hit");
            icon.RemoveFromClassList("hit");
        }

        public void PlayPlayerHitEffect()
        {
            StartCoroutine(PlayerHitRoutine());
        }

        private IEnumerator PlayerHitRoutine()
        {
            _combatCard.AddToClassList("player-hit");
            PlayCombatShake();
            yield return new WaitForSeconds(0.5f);
            _combatCard.RemoveFromClassList("player-hit");
        }

        // ===== ENDING =====

        private void ShowEnding(SectionData section)
        {
            _narrativeText.text = section.text ?? "";

            _endingCard.style.display = DisplayStyle.Flex;
            bool isVictory = section.ending?.type == "victory";
            _endingIcon.text = isVictory ? "\U0001F3C6" : "\u2620\uFE0F";
            _endingTitle.text = isVictory ? "Vitoria!" : "Derrota";
            _endingTitle.RemoveFromClassList("victory");
            _endingTitle.RemoveFromClassList("defeat");
            _endingTitle.AddToClassList(isVictory ? "victory" : "defeat");

            var player = GameEngine.Instance?.PlayerCharacter;
            if (player != null)
                _endingStats.text = $"Habilidade {player.skill}  Vigor {player.stamina}/{player.maxStamina}  Sorte {player.luck}  Ouro {player.gold}";
        }

        // ===== STATS =====

        public void UpdateStats(Character character)
        {
            if (character == null) return;
            _pillVigorVal.text = character.stamina.ToString();
            _pillLuckVal.text = character.luck.ToString();
            _pillGoldVal.text = character.gold.ToString();

            float ratio = character.maxStamina > 0 ? (float)character.stamina / character.maxStamina : 1f;
            _pillVigorVal.RemoveFromClassList("danger");
            if (ratio <= 0.5f) _pillVigorVal.AddToClassList("danger");
        }

        // ===== ILLUSTRATION =====

        private void SetIllustration(string type)
        {
            foreach (var cls in ILLUS_CLASSES)
                _illustration.RemoveFromClassList(cls);

            string safeType = type ?? "narrative";
            string clsName = $"type-{safeType}";
            if (System.Array.IndexOf(ILLUS_CLASSES, clsName) >= 0)
                _illustration.AddToClassList(clsName);
            else
                _illustration.AddToClassList(ILLUS_TYPE_NARRATIVE);

            _illusIcon.text = ICONS_LG.ContainsKey(safeType) ? ICONS_LG[safeType] : ICONS_LG["narrative"];
            _illusLabel.text = TYPE_LABELS.ContainsKey(safeType) ? TYPE_LABELS[safeType] : safeType;
        }

        // ===== DICE OVERLAY =====

        public void ShowDice(int d1, int d2, Action onComplete)
        {
            StartCoroutine(DiceRoutine("Rolando...", d1, d2, onComplete));
        }

        public void ShowDice(string label, int d1, int d2, Action onComplete)
        {
            StartCoroutine(DiceRoutine(label, d1, d2, onComplete));
        }

        private IEnumerator DiceRoutine(string label, int d1, int d2, Action onComplete)
        {
            _diceLabel.text = label;
            _diceResult.text = "";
            _diceResult.style.display = DisplayStyle.None;

            _diceOverlay.RemoveFromClassList("hidden");
            _diceOverlay.AddToClassList("show");

            float startTime = Time.time;
            float duration = 0.9f;
            bool singleDie = d2 == 0;

            while (Time.time - startTime < duration)
            {
                int f1 = UnityEngine.Random.Range(1, 7);
                int f2 = UnityEngine.Random.Range(1, 7);
                RenderDice(f1, singleDie ? 0 : f2, true);
                yield return new WaitForSeconds(0.08f);
            }

            RenderDice(d1, d2, false);
            _diceResult.text = singleDie ? d1.ToString() : (d1 + d2).ToString();
            _diceResult.style.display = DisplayStyle.Flex;
            StartCoroutine(AnimateDiceResult());

            yield return new WaitForSeconds(1f);

            _diceOverlay.RemoveFromClassList("show");
            _diceOverlay.AddToClassList("hidden");
            _diceResult.style.display = DisplayStyle.None;
            onComplete?.Invoke();
        }

        private void RenderDice(int val1, int val2, bool rolling)
        {
            _diceRow.Clear();
            _diceRow.Add(BuildDie(val1, rolling));

            if (val2 > 0)
                _diceRow.Add(BuildDie(val2, rolling));
        }

        private VisualElement BuildDie(int value, bool rolling)
        {
            var container = new VisualElement();
            container.AddToClassList("die-container");

            var dieEl = new VisualElement();
            dieEl.style.width = 72;
            dieEl.style.height = 72;
            dieEl.style.backgroundColor = new Color(0.96f, 0.94f, 0.88f);
            dieEl.style.borderTopLeftRadius = 14;
            dieEl.style.borderTopRightRadius = 14;
            dieEl.style.borderBottomLeftRadius = 14;
            dieEl.style.borderBottomRightRadius = 14;

            if (rolling)
                dieEl.AddToClassList("die-rolling");

            var dots = GetDiceDotPositions(value);
            foreach (var pos in dots)
            {
                var dot = new VisualElement();
                dot.AddToClassList("die-dot");
                dot.style.left = pos.x;
                dot.style.top = pos.y;
                dieEl.Add(dot);
            }

            container.Add(dieEl);
            return container;
        }

        private static readonly Dictionary<int, Vector2[]> DiceFaces = new()
        {
            { 1, new[] { new Vector2(29, 29) } },
            { 2, new[] { new Vector2(45, 12), new Vector2(12, 45) } },
            { 3, new[] { new Vector2(45, 12), new Vector2(29, 29), new Vector2(12, 45) } },
            { 4, new[] { new Vector2(12, 12), new Vector2(45, 12), new Vector2(12, 45), new Vector2(45, 45) } },
            { 5, new[] { new Vector2(12, 12), new Vector2(45, 12), new Vector2(29, 29), new Vector2(12, 45), new Vector2(45, 45) } },
            { 6, new[] { new Vector2(12, 12), new Vector2(45, 12), new Vector2(12, 29), new Vector2(45, 29), new Vector2(12, 45), new Vector2(45, 45) } }
        };

        private Vector2[] GetDiceDotPositions(int value)
        {
            if (DiceFaces.TryGetValue(value, out var positions))
                return positions;
            return DiceFaces[1];
        }

        private IEnumerator AnimateDiceResult()
        {
            _diceResult.style.scale = new Scale(Vector2.one * 0.5f);
            _diceResult.style.opacity = 0;

            float t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float p = t / 0.3f;
                _diceResult.style.scale = new Scale(Vector2.one * Mathf.Lerp(0.5f, 1f, p));
                _diceResult.style.opacity = Mathf.Lerp(0, 1, p);
                yield return null;
            }
        }

        // ===== BRIDGE OVERLAY =====

        public void ShowBridge(string type, string label, Action onComplete, float delay = 1.5f)
        {
            StartCoroutine(BridgeRoutine(type, label, onComplete, delay));
        }

        private IEnumerator BridgeRoutine(string type, string label, Action onComplete, float delay)
        {
            string icon = "\u26A1";
            var bridgeIcons = new Dictionary<string, string>
            {
                { "test", "\U0001F3B2" },
                { "itemGate", "\U0001F6AA" },
                { "random", "\U0001F3B0" }
            };
            if (bridgeIcons.TryGetValue(type, out var ico))
                icon = ico;

            _bridgeIcon.text = icon;
            _bridgeLabel.text = label;

            _bridgeOverlay.RemoveFromClassList("hidden");
            _bridgeOverlay.AddToClassList("show");

            yield return new WaitForSeconds(delay);

            _bridgeOverlay.RemoveFromClassList("show");
            _bridgeOverlay.AddToClassList("hidden");
            onComplete?.Invoke();
        }

        // ===== SHEETS =====

        public void OpenSheet(string id)
        {
            if (_openSheetId == id) return;
            if (!string.IsNullOrEmpty(_openSheetId))
                CloseSheet(_openSheetId);

            if (!_sheetOverlays.TryGetValue(id, out var overlay) || overlay == null) return;

            overlay.RemoveFromClassList("hidden");
            overlay.AddToClassList("open");
            _openSheetId = id;
            UpdateSheetContent(id);
        }

        public void CloseSheet(string id)
        {
            if (!_sheetOverlays.TryGetValue(id, out var overlay) || overlay == null) return;

            overlay.RemoveFromClassList("open");
            overlay.AddToClassList("hidden");

            if (_openSheetId == id)
                _openSheetId = null;
        }

        private void ToggleSheet(string id)
        {
            if (_openSheetId == id)
                CloseSheet(id);
            else
                OpenSheet(id);
        }

        private void UpdateSheetContent(string id)
        {
            switch (id)
            {
                case "char-sheet":
                    UpdateCharacterSheet();
                    break;
                case "inv-sheet":
                    UpdateInventorySheet();
                    break;
                case "rewind-sheet":
                    UpdateRewindSheet();
                    break;
                case "save-sheet":
                    UpdateSaveSheet();
                    break;
            }
        }

        private void UpdateCharacterSheet()
        {
            var body = _root.Q<ScrollView>("char-sheet-body");
            if (body == null) return;
            body.Clear();

            var player = GameEngine.Instance?.PlayerCharacter;
            if (player == null) return;

            AddStatRow(body, "Habilidade", player.skill.ToString());
            AddStatRow(body, "Vigor", $"{player.stamina}/{player.maxStamina}");
            AddStatRow(body, "Sorte", $"{player.luck}/{player.maxLuck}");
            AddStatRow(body, "Ouro", player.gold.ToString());
            AddStatRow(body, "Provisoes", player.provisions.ToString());

            var itemsTitle = new Label("Itens rapidos:");
            itemsTitle.style.marginTop = 16;
            itemsTitle.style.color = new StyleColor(new Color(0.54f, 0.52f, 0.47f));
            itemsTitle.style.fontSize = 14;
            body.Add(itemsTitle);

            if (player.inventory != null && player.inventory.Count > 0)
            {
                foreach (var item in player.inventory)
                {
                    var invItem = new Label($"\U0001F4E6 {item}");
                    invItem.AddToClassList("inv-item");
                    body.Add(invItem);
                }
            }
            else
            {
                var empty = new Label("Vazio");
                empty.AddToClassList("inv-empty");
                body.Add(empty);
            }

            var shortcut = new Label("Atalho: C abre/fecha ficha");
            shortcut.AddToClassList("sheet-shortcuts");
            body.Add(shortcut);
        }

        private void UpdateInventorySheet()
        {
            var body = _root.Q<ScrollView>("inv-sheet-body");
            if (body == null) return;
            body.Clear();

            var player = GameEngine.Instance?.PlayerCharacter;
            if (player == null) return;

            if (player.inventory != null && player.inventory.Count > 0)
            {
                foreach (var item in player.inventory)
                {
                    var invItem = new Label($"\U0001F4E6 {item}");
                    invItem.AddToClassList("inv-item");
                    body.Add(invItem);
                }
            }
            else
            {
                var empty = new Label("Inventario vazio");
                empty.AddToClassList("inv-empty");
                body.Add(empty);
            }

            var shortcut = new Label("Atalho: I abre/fecha inventario");
            shortcut.AddToClassList("sheet-shortcuts");
            body.Add(shortcut);
        }

        private void UpdateRewindSheet()
        {
            var body = _root.Q<ScrollView>("rewind-sheet-body");
            if (body == null) return;
            body.Clear();

            var hist = GameEngine.Instance?.NavigationHistory;
            if (hist == null || hist.Count == 0) return;

            var sections = GameEngine.Instance.CurrentStory?.sections;
            var histArr = hist.ToArray();

            for (int i = 0; i < histArr.Length; i++)
            {
                int idx = i;
                int sid = histArr[i];
                string icon = "";
                string label = $"Secao {sid}";

                if (sections != null && sections.TryGetValue(sid.ToString(), out var sec))
                {
                    string type = sec.type ?? "narrative";
                    icon = ICONS.ContainsKey(type) ? ICONS[type] + " " : "";
                    label = $"{icon}Secao {sid} \u2014 {(TYPE_LABELS.ContainsKey(type) ? TYPE_LABELS[type] : type)}";
                }

                var entry = new VisualElement();
                entry.AddToClassList("tl-entry");
                if (i == histArr.Length - 1) entry.AddToClassList("current");
                entry.focusable = true;
                entry.tabIndex = i <= 9 ? i + 1 : 0;

                if (i <= 9)
                {
                    var keyLabel = new Label((i + 1).ToString());
                    keyLabel.AddToClassList("tl-key");
                    entry.Add(keyLabel);
                }

                entry.Add(new Label(label));

                entry.RegisterCallback<ClickEvent>(evt => RewindTo(idx));
                entry.RegisterCallback<NavigationSubmitEvent>(evt => RewindTo(idx));

                body.Add(entry);
            }
        }

        private void UpdateSaveSheet()
        {
            var body = _root.Q<ScrollView>("save-sheet-body");
            if (body == null) return;
            body.Clear();

            for (int slot = 1; slot <= 3; slot++)
            {
                int s = slot;
                var slotRow = new VisualElement();
                slotRow.AddToClassList("save-slot");

                var info = new VisualElement();
                info.AddToClassList("slot-info");

                var nameLabel = new Label($"Slot {slot}");
                nameLabel.AddToClassList("slot-name");
                info.Add(nameLabel);

                string meta = "Vazio";
                string savedJson = PlayerPrefs.GetString($"ift_save_{slot}", "");
                if (!string.IsNullOrEmpty(savedJson))
                {
                    try
                    {
                        var data = JsonUtility.FromJson<SaveSlotData>(savedJson);
                        if (data != null)
                            meta = $"Secao {data.sectionId} \u2014 {data.typeLabel}";
                    }
                    catch { }
                }

                var metaLabel = new Label(meta);
                metaLabel.AddToClassList("slot-meta");
                info.Add(metaLabel);

                var btnRow = new VisualElement();
                btnRow.AddToClassList("slot-btns");
                btnRow.style.flexDirection = FlexDirection.Row;

                var saveBtn = new Button(() => SaveGame(s));
                saveBtn.AddToClassList("btn-sm");
                saveBtn.AddToClassList("primary");
                saveBtn.text = "Salvar";

                btnRow.Add(saveBtn);

                if (!string.IsNullOrEmpty(savedJson))
                {
                    var loadBtn = new Button(() => LoadGame(s));
                    loadBtn.AddToClassList("btn-sm");
                    loadBtn.AddToClassList("secondary");
                    loadBtn.text = "Carregar";
                    btnRow.Add(loadBtn);
                }

                slotRow.Add(info);
                slotRow.Add(btnRow);
                body.Add(slotRow);
            }
        }

        private void AddStatRow(VisualElement parent, string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("stat-row");

            var lbl = new Label(label);
            lbl.AddToClassList("s-label");
            var val = new Label(value);
            val.AddToClassList("s-value");

            row.Add(lbl);
            row.Add(val);
            parent.Add(row);
        }

        private void RewindTo(int idx)
        {
            CloseSheet("rewind-sheet");
            var player = GameEngine.Instance?.PlayerCharacter;
            var cc = GameEngine.Instance?.CurrentStory?.characterCreation;

            if (player != null && cc != null)
            {
                player.skill = Character.RollDice(cc.attributes.ContainsKey("skill") ? cc.attributes["skill"].dice : "1d6+6");
                player.stamina = Character.RollDice(cc.attributes.ContainsKey("stamina") ? cc.attributes["stamina"].dice : "2d6+12");
                player.maxStamina = player.stamina;
                player.luck = Character.RollDice(cc.attributes.ContainsKey("luck") ? cc.attributes["luck"].dice : "1d6+6");
                player.maxLuck = player.luck;
                player.gold = cc.startingGold;
                player.provisions = cc.startingProvisions;
                player.inventory.Clear();
                if (cc.startingItems != null) player.inventory.AddRange(cc.startingItems);
                player.flags.Clear();
                player.counters.Clear();
            }

            var hist = GameEngine.Instance?.NavigationHistory;
            var sections = GameEngine.Instance?.CurrentStory?.sections;
            if (hist != null && sections != null)
            {
                var histArr = hist.ToArray();
                var newHist = new Stack<int>();
                for (int i = 0; i <= idx && i < histArr.Length; i++)
                {
                    newHist.Push(histArr[i]);
                    if (sections.TryGetValue(histArr[i].ToString(), out var sec) && sec.onEnter != null)
                    {
                        EvalOnEnterLocal(sec.onEnter);
                    }
                }
            }

            if (hist != null && hist.Count > 0)
            {
                var arr = hist.ToArray();
                GameEngine.Instance.GoToSection(arr[arr.Length - 1]);
            }

            UpdateStats(GameEngine.Instance?.PlayerCharacter);
        }

        private void EvalOnEnterLocal(OnEnterData oe)
        {
            var player = GameEngine.Instance?.PlayerCharacter;
            if (player == null || oe == null) return;

            if (oe.addItems != null) foreach (var i in oe.addItems) player.AddItem(i);
            if (oe.removeItems != null) foreach (var i in oe.removeItems) player.RemoveItem(i);
            if (oe.setFlagsList != null) foreach (var kv in oe.setFlagsList) player.SetFlag(kv.key, kv.value);
            if (oe.modifyGold != 0) player.ModifyGold(oe.modifyGold);
            if (oe.modifyStamina != 0) player.ModifyStamina(oe.modifyStamina);
            if (oe.modifyLuck != 0) player.ModifyLuck(oe.modifyLuck);
        }

        private void SaveGame(int slot)
        {
            var sec = GameEngine.Instance?.CurrentSection;
            var player = GameEngine.Instance?.PlayerCharacter;
            var hist = GameEngine.Instance?.NavigationHistory;

            if (sec == null || player == null) return;

            var data = new SaveSlotData
            {
                sectionId = sec.id,
                typeLabel = TYPE_LABELS.ContainsKey(sec.type ?? "") ? TYPE_LABELS[sec.type] : sec.type ?? "",
                playerJson = JsonUtility.ToJson(player),
                historyJson = JsonUtility.ToJson(new SerializableList<int> { items = hist != null ? new List<int>(hist) : new List<int>() })
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString($"ift_save_{slot}", json);
            PlayerPrefs.Save();

            ShowToast($"Jogo salvo no slot {slot}!");
            UpdateSheetContent("save-sheet");
        }

        private void LoadGame(int slot)
        {
            string json = PlayerPrefs.GetString($"ift_save_{slot}", "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var data = JsonUtility.FromJson<SaveSlotData>(json);
                if (data == null) return;

                var player = GameEngine.Instance?.PlayerCharacter;
                if (player != null)
                    JsonUtility.FromJsonOverwrite(data.playerJson, player);

                CloseSheet("save-sheet");
                ShowToast($"Jogo carregado do slot {slot}!");
                GameEngine.Instance?.GoToSection(data.sectionId);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load save: {e.Message}");
            }
        }

        // ===== CHARACTER CREATION =====

        public void ShowCharacterCreation()
        {
            _header.style.display = DisplayStyle.None;
            _illustration.style.display = DisplayStyle.None;
            _scrollArea.style.display = DisplayStyle.None;
            _toolbar.style.display = DisplayStyle.None;
            _charCreate.style.display = DisplayStyle.Flex;

            var player = GameEngine.Instance?.PlayerCharacter;
            if (player == null) return;

            _rollGrid.Clear();

            AddRollResult(_rollGrid, "Habilidade", player.skill, "1d6+6");
            AddRollResult(_rollGrid, "Vigor", player.stamina, "2d6+12");
            AddRollResult(_rollGrid, "Sorte", player.luck, "1d6+6");

            string itemsStr = player.inventory != null ? string.Join(", ", player.inventory) : "";
            _startingGear.text = $"Itens: {itemsStr} | Ouro: {player.gold}";

            _btnStartAdventure.Focus();
        }

        private void AddRollResult(VisualElement parent, string label, int value, string dice)
        {
            var container = new VisualElement();
            container.AddToClassList("roll-result");

            var valLabel = new Label(value.ToString());
            valLabel.AddToClassList("roll-val");

            var descLabel = new Label($"{label}\n{dice}");
            descLabel.AddToClassList("roll-label");

            container.Add(valLabel);
            container.Add(descLabel);
            parent.Add(container);
        }

        private void StartAdventure()
        {
            _charCreate.style.display = DisplayStyle.None;
            _header.style.display = DisplayStyle.Flex;
            _illustration.style.display = DisplayStyle.Flex;
            _scrollArea.style.display = DisplayStyle.Flex;
            _toolbar.style.display = DisplayStyle.Flex;

            GameEngine.Instance?.GoToSection(GameEngine.Instance.CurrentStory?.metadata.startSection ?? 1);
        }

        // ===== TOAST =====

        public void ShowToast(string msg)
        {
            StartCoroutine(ToastRoutine(msg, ""));
        }

        public void ShowToast(string msg, string cls)
        {
            StartCoroutine(ToastRoutine(msg, cls));
        }

        private IEnumerator ToastRoutine(string msg, string cls)
        {
            _toast.text = msg;
            _toast.RemoveFromClassList("show");
            _toast.RemoveFromClassList("good");
            _toast.RemoveFromClassList("bad");

            if (cls == "good" || cls == "bad")
                _toast.AddToClassList(cls);

            _toast.AddToClassList("show");

            yield return new WaitForSeconds(2.5f);

            _toast.RemoveFromClassList("show");
        }

        // ===== HELP =====

        private void OpenHelp()
        {
            _helpOverlay.RemoveFromClassList("hidden");
            _helpOverlay.AddToClassList("open");
            _root.Q<Button>("help-close-btn")?.Focus();
        }

        private void CloseHelp()
        {
            _helpOverlay.RemoveFromClassList("open");
            _helpOverlay.AddToClassList("hidden");
        }

        // ===== SETTINGS =====

        private void SetFontSize(int level)
        {
            _fsLevel = level;
            _narrativeText.RemoveFromClassList("fs-lg");
            _narrativeText.RemoveFromClassList("fs-xl");

            if (level >= 2) _narrativeText.AddToClassList("fs-lg");
            if (level >= 3) _narrativeText.AddToClassList("fs-xl");

            for (int i = 0; i <= 3; i++)
            {
                var btn = _root.Q<Button>($"fs{i}");
                if (btn != null)
                {
                    if (i == level) btn.AddToClassList("selected");
                    else btn.RemoveFromClassList("selected");
                }
            }
        }

        private void SetTheme(string theme)
        {
            _appShell.RemoveFromClassList("theme-sepia");
            _appShell.RemoveFromClassList("theme-hc");

            switch (theme)
            {
                case "sepia":
                    OverrideThemeTokens("#f4ecd8", "#efe0c8", "#e8d5b0", "#3b2f1e", "#6b5e4e", "#8b6914");
                    break;
                case "contrast":
                    OverrideThemeTokens("#000000", "#000000", "#000000", "#FFD700", "#FFD700", "#FFD700");
                    break;
                default:
                    ResetThemeTokens();
                    break;
            }

            var themeBtns = new[] { "th-dark", "th-sepia", "th-contrast" };
            var themes = new[] { "dark", "sepia", "contrast" };
            for (int i = 0; i < themeBtns.Length; i++)
            {
                var btn = _root.Q<Button>(themeBtns[i]);
                if (btn != null)
                {
                    if (themes[i] == theme) btn.AddToClassList("selected");
                    else btn.RemoveFromClassList("selected");
                }
            }
        }

        private void OverrideThemeTokens(string bg, string surface, string surface2, string text, string textSec, string accent)
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;
            // Apply inline style overrides to root — cascades to children that inherit
            root.style.backgroundColor = new StyleColor(ParseColor(bg));
            root.style.color = new StyleColor(ParseColor(text));
            // Store for re-application after UI rebuilds
            _themeOverride = (bg, surface, surface2, text, textSec, accent);
            _hasThemeOverride = true;
        }

        private void ResetThemeTokens()
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;
            root.style.backgroundColor = StyleKeyword.None;
            root.style.color = StyleKeyword.None;
            _hasThemeOverride = false;
        }

        private (string bg, string surface, string surface2, string text, string textSec, string accent) _themeOverride;
        private bool _hasThemeOverride = false;

        private Color ParseColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.white;
        }

        private void ToggleDyslexic()
        {
            _dysOn = !_dysOn;
            var btn = _root.Q<Button>("btn-dys");
            if (btn != null)
            {
                btn.text = _dysOn ? "Ligado" : "Desligado";
                if (_dysOn) btn.AddToClassList("on");
                else btn.RemoveFromClassList("on");
            }
        }

        private void ToggleNarration()
        {
            _narrationOn = !_narrationOn;

            var tbBtn = _root.Q<Button>("tb-narrate");
            if (tbBtn != null)
            {
                if (_narrationOn) tbBtn.AddToClassList("on");
                else tbBtn.RemoveFromClassList("on");
            }

            var setBtn = _root.Q<Button>("btn-tts");
            if (setBtn != null)
            {
                setBtn.text = _narrationOn ? "Ligado" : "Desligado";
                if (_narrationOn) setBtn.AddToClassList("on");
                else setBtn.RemoveFromClassList("on");
            }
        }

        private void ToggleVibration()
        {
            _vibrationOn = !_vibrationOn;
            var btn = _root.Q<Button>("btn-vib");
            if (btn != null)
            {
                btn.text = _vibrationOn ? "Ligado" : "Desligado";
                if (_vibrationOn) btn.AddToClassList("on");
                else btn.RemoveFromClassList("on");
            }
        }

        private void ToggleCharShortcuts()
        {
            _charShortcutsOn = !_charShortcutsOn;
            var btn = _root.Q<Button>("btn-shortcuts");
            if (btn != null)
            {
                btn.text = _charShortcutsOn ? "Ligado" : "Desligado";
                if (_charShortcutsOn) btn.AddToClassList("on");
                else btn.RemoveFromClassList("on");
            }
        }

        private void ReadCurrentSection()
        {
            var text = GameEngine.Instance?.CurrentSection?.text;
            Debug.Log($"[TTS] Reading: {text}");
        }

        // ===== STATE =====

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Loading)
            {
                ShowCharacterCreation();
            }
        }

        private void OnLocaleChanged(string localeCode)
        {
            RestoreUIState();
        }

        private void RestoreUIState()
        {
            if (L == null) return;

            var narrateBtn = _root.Q<Button>("tb-narrate"); if (narrateBtn != null) narrateBtn.text = L["tb_narrate"];
            var charBtn = _root.Q<Button>("tb-char"); if (charBtn != null) charBtn.text = L["tb_char"];
            var itemsBtn = _root.Q<Button>("tb-items"); if (itemsBtn != null) itemsBtn.text = L["tb_items"];
            var saveBtn = _root.Q<Button>("tb-save"); if (saveBtn != null) saveBtn.text = L["tb_save"];
            var rewindBtn = _root.Q<Button>("tb-rewind"); if (rewindBtn != null) rewindBtn.text = L["tb_rewind"];
            var settingsBtn = _root.Q<Button>("tb-settings"); if (settingsBtn != null) settingsBtn.text = L["tb_settings"];

            if (_openSheetId == "char-sheet") UpdateCharacterSheet();
            if (_openSheetId == "inv-sheet") UpdateInventorySheet();
            if (_openSheetId == "rewind-sheet") UpdateRewindSheet();
            if (_openSheetId == "save-sheet") UpdateSaveSheet();
        }

        public void SetLanguage(string langCode)
        {
            if (L != null)
            {
                L.SetLocale(langCode);
            }

            var langBtns = new[] { "lang-pt", "lang-en", "lang-es" };
            var languages = new[] { "pt-BR", "en", "es" };
            for (int i = 0; i < langBtns.Length; i++)
            {
                var btn = _root.Q<Button>(langBtns[i]);
                if (btn != null)
                {
                    if (languages[i] == langCode) btn.AddToClassList("selected");
                    else btn.RemoveFromClassList("selected");
                }
            }
        }

        [Serializable]
        private class FailedCondition
        {
            public string reason;
            public string type;
        }

        [Serializable]
        private class SaveSlotData
        {
            public int sectionId;
            public string typeLabel;
            public string playerJson;
            public string historyJson;
        }

        [Serializable]
        private class SerializableList<T>
        {
            public List<T> items;
        }
    }
}
