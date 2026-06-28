using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using InteractiveFantasticTales.Models;

namespace InteractiveFantasticTales.Core
{
    public enum GameState
    {
        Loading,
        Narrative,
        ChoicePending,
        CombatActive,
        TestActive,
        ItemGateCheck,
        Transitioning,
        GameOver,
        Paused
    }

    public class GameEngine : MonoBehaviour
    {
        public static GameEngine Instance { get; private set; }

        [Header("Events")]
        public System.Action<SectionData> OnSectionChanged;
        public System.Action<Character> OnCharacterUpdated;
        public System.Action<GameState> OnStateChanged;
        public System.Action<string> OnMessage;
        public System.Action<CombatState> OnCombatUpdated;

        public StoryData CurrentStory { get; private set; }
        public Character PlayerCharacter { get; private set; }
        public SectionData CurrentSection { get; private set; }
        public GameState CurrentState { get; private set; }
        public CombatState ActiveCombat { get; private set; }
        public Stack<int> NavigationHistory { get; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadStoryFromJson(string json)
        {
            SetState(GameState.Loading);
            try
            {
                CurrentStory = JsonUtility.FromJson<StoryData>(json);
                if (CurrentStory?.sections == null)
                {
                    OnMessage?.Invoke("Erro: formato de história inválido.");
                    return;
                }
                CreateCharacter();
                GoToSection(CurrentStory.metadata.startSection);
            }
            catch (Exception e)
            {
                OnMessage?.Invoke($"Erro ao carregar história: {e.Message}");
            }
        }

        public void LoadStoryFromStreamingAssets(string fileName)
        {
            StartCoroutine(LoadJsonCoroutine(fileName));
        }

        private IEnumerator LoadJsonCoroutine(string fileName)
        {
            SetState(GameState.Loading);
            string filePath = Path.Combine(Application.streamingAssetsPath, "Stories", fileName);

            if (filePath.Contains("://") || filePath.Contains(":///"))
            {
                using var request = UnityWebRequest.Get(filePath);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    LoadStoryFromJson(request.downloadHandler.text);
                else
                    OnMessage?.Invoke($"Erro ao carregar arquivo: {request.error}");
            }
            else
            {
                if (File.Exists(filePath))
                    LoadStoryFromJson(File.ReadAllText(filePath));
                else
                    OnMessage?.Invoke($"Arquivo não encontrado: {fileName}");
            }
        }

        private void CreateCharacter()
        {
            var cc = CurrentStory.characterCreation;
            PlayerCharacter = new Character();

            if (cc?.attributes != null)
            {
                if (cc.attributes.TryGetValue("skill", out var skillAttr))
                    PlayerCharacter.skill = Character.RollDice(skillAttr.dice);
                else
                    PlayerCharacter.skill = Character.RollDice("1d6+6");

                if (cc.attributes.TryGetValue("stamina", out var staminaAttr))
                    PlayerCharacter.stamina = Character.RollDice(staminaAttr.dice);
                else
                    PlayerCharacter.stamina = Character.RollDice("2d6+12");

                PlayerCharacter.maxStamina = PlayerCharacter.stamina;

                if (cc.attributes.TryGetValue("luck", out var luckAttr))
                    PlayerCharacter.luck = Character.RollDice(luckAttr.dice);
                else
                    PlayerCharacter.luck = Character.RollDice("1d6+6");

                PlayerCharacter.maxLuck = PlayerCharacter.luck;
            }

            PlayerCharacter.gold = cc?.startingGold ?? 0;
            PlayerCharacter.provisions = cc?.startingProvisions ?? 10;

            if (cc?.startingItems != null)
                PlayerCharacter.inventory.AddRange(cc.startingItems);

            OnCharacterUpdated?.Invoke(PlayerCharacter);
        }

        public void GoToSection(int sectionId)
        {
            if (CurrentStory?.sections == null || !CurrentStory.sections.TryGetValue(sectionId.ToString(), out var section))
            {
                OnMessage?.Invoke($"Seção {sectionId} não encontrada.");
                return;
            }

            NavigationHistory.Push(sectionId);
            CurrentSection = section;

            if (section.onEnter != null)
                EvalOnEnter(section.onEnter);

            switch (section.type)
            {
                case "narrative":
                    SetState(GameState.Narrative);
                    break;
                case "combat":
                    if (section.combat != null)
                        StartCombat(section);
                    break;
                case "test":
                    SetState(GameState.TestActive);
                    break;
                case "itemGate":
                    SetState(GameState.ItemGateCheck);
                    break;
                case "random":
                    if (section.random?.outcomes?.Count > 0)
                        ResolveRandom(section);
                    else
                        SetState(GameState.Narrative);
                    break;
                case "ending":
                    SetState(GameState.GameOver);
                    break;
            }

            OnSectionChanged?.Invoke(section);
        }

        public void MakeChoice(int choiceIndex)
        {
            if (CurrentSection?.choices == null || choiceIndex >= CurrentSection.choices.Count) return;

            var choice = CurrentSection.choices[choiceIndex];

            if (!EvaluateConditions(choice.conditions)) return;

            var targetSection = choice.targetSection;
            if (CurrentStory.sections.TryGetValue(targetSection.ToString(), out var target))
            {
                if (target.type == "combat" && target.combat != null)
                    StartCombat(target);
                else
                    GoToSection(targetSection);
            }
        }

        public void FightRound()
        {
            if (ActiveCombat == null) return;

            var playerRoll = Character.RollDice("2d6") + PlayerCharacter.skill;
            var enemyRoll = Character.RollDice("2d6") + ActiveCombat.enemySkill;

            if (playerRoll > enemyRoll)
            {
                ActiveCombat.enemyStamina -= 2;
                OnMessage?.Invoke($"Você ataca! {ActiveCombat.enemyName} perde 2 STAMINA.");
            }
            else if (enemyRoll > playerRoll)
            {
                PlayerCharacter.stamina -= 2;
                OnMessage?.Invoke($"{ActiveCombat.enemyName} ataca! Você perde 2 STAMINA.");
            }
            else
            {
                OnMessage?.Invoke($"Empate! Ninguém se fere.");
            }

            OnCombatUpdated?.Invoke(ActiveCombat);

            if (ActiveCombat.enemyStamina <= 0)
            {
                ActiveCombat.phase = CombatPhase.Result;
                OnMessage?.Invoke($"🏆 {ActiveCombat.enemyName} foi derrotado!");
                if (ActiveCombat.lootOnVictory?.Count > 0)
                {
                    foreach (var item in ActiveCombat.lootOnVictory)
                        PlayerCharacter.AddItem(item);
                }
                GoToSection(ActiveCombat.victoryTarget);
            }
            else if (PlayerCharacter.stamina <= 0)
            {
                ActiveCombat.phase = CombatPhase.Result;
                OnMessage?.Invoke("💀 Você foi derrotado...");
                GoToSection(ActiveCombat.defeatTarget);
            }
        }

        public void FleeCombat()
        {
            if (ActiveCombat == null || !ActiveCombat.allowFlee) return;

            if (PlayerCharacter.TestLuck())
            {
                OnMessage?.Invoke("🏃 Fuga bem sucedida!");
                GoToSection(ActiveCombat.fleeTarget);
            }
            else
            {
                OnMessage?.Invoke("❌ Falha na fuga! Continue lutando.");
                OnCombatUpdated?.Invoke(ActiveCombat);
            }
        }

        public void ResolveTest()
        {
            if (CurrentSection?.test == null) return;
            var test = CurrentSection.test;

            int attrValue = test.attribute switch
            {
                "skill" => PlayerCharacter.skill,
                "luck" => PlayerCharacter.luck,
                _ => 7
            };

            int roll = Character.RollDice("2d6");
            bool success = roll + attrValue >= test.difficulty;

            OnMessage?.Invoke(success
                ? $"🎲 Sucesso! {roll + attrValue} ≥ {test.difficulty}"
                : $"🎲 Falha! {roll + attrValue} < {test.difficulty}");

            StartCoroutine(DelayedTransition(success ? test.successTarget : test.failTarget, 1.2f));
        }

        public void ResolveItemGate()
        {
            if (CurrentSection?.itemGate == null) return;
            var gate = CurrentSection.itemGate;

            bool hasItem = PlayerCharacter.HasItem(gate.item);
            OnMessage?.Invoke(hasItem ? $"✅ Você tem {gate.item}!" : $"❌ Você não tem {gate.item}.");
            StartCoroutine(DelayedTransition(hasItem ? gate.hasItemTarget : gate.noItemTarget, 1.0f));
        }

        private void ResolveRandom(SectionData section)
        {
            float totalWeight = 0;
            foreach (var o in section.random.outcomes)
                totalWeight += o.weight;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var outcome in section.random.outcomes)
            {
                cumulative += outcome.weight;
                if (roll <= cumulative)
                {
                    GoToSection(outcome.targetSection);
                    return;
                }
            }
        }

        private void StartCombat(SectionData section)
        {
            ActiveCombat = new CombatState
            {
                enemyName = section.combat.enemyName,
                enemySkill = section.combat.enemySkill,
                enemyStamina = section.combat.enemyStamina,
                enemyMaxStamina = section.combat.enemyStamina,
                victoryTarget = section.combat.victoryTarget,
                defeatTarget = section.combat.defeatTarget,
                fleeTarget = section.combat.fleeTarget,
                allowFlee = section.combat.allowFlee,
                lootOnVictory = section.combat.lootOnVictory,
                phase = CombatPhase.Fighting,
                combatSectionText = section.text,
            };

            SetState(GameState.CombatActive);
            OnSectionChanged?.Invoke(section);
            OnCombatUpdated?.Invoke(ActiveCombat);
        }

        private bool EvaluateConditions(List<ConditionData> conditions)
        {
            if (conditions == null || conditions.Count == 0) return true;

            foreach (var cond in conditions)
            {
                int actual = 0;
                switch (cond.type)
                {
                    case "hasItem":
                        if (!PlayerCharacter.HasItem(cond.key)) return false;
                        continue;
                    case "hasFlag":
                        if (!PlayerCharacter.HasFlag(cond.key)) return false;
                        continue;
                    case "skill": actual = PlayerCharacter.skill; break;
                    case "stamina": actual = PlayerCharacter.stamina; break;
                    case "luck": actual = PlayerCharacter.luck; break;
                    case "gold": actual = PlayerCharacter.gold; break;
                    case "counter": actual = PlayerCharacter.GetCounter(cond.key); break;
                }

                int val = int.TryParse(cond.value?.ToString(), out var parsed) ? parsed : 0;
                bool result = cond.op switch
                {
                    "==" => actual == val,
                    "!=" => actual != val,
                    ">=" => actual >= val,
                    "<=" => actual <= val,
                    ">" => actual > val,
                    "<" => actual < val,
                    _ => true
                };

                if (!result) return false;
            }

            return true;
        }

        private void EvalOnEnter(OnEnterData onEnter)
        {
            if (onEnter.addItems != null)
                foreach (var item in onEnter.addItems)
                    PlayerCharacter.AddItem(item);
            if (onEnter.removeItems != null)
                foreach (var item in onEnter.removeItems)
                    PlayerCharacter.RemoveItem(item);
            if (onEnter.setFlags != null)
                foreach (var kvp in onEnter.setFlags)
                    PlayerCharacter.SetFlag(kvp.Key, kvp.Value);
            if (onEnter.modifyGold != 0)
                PlayerCharacter.ModifyGold(onEnter.modifyGold);
            if (onEnter.modifyStamina != 0)
                PlayerCharacter.ModifyStamina(onEnter.modifyStamina);
            if (onEnter.modifyLuck != 0)
                PlayerCharacter.ModifyLuck(onEnter.modifyLuck);

            OnCharacterUpdated?.Invoke(PlayerCharacter);
        }

        private IEnumerator DelayedTransition(int sectionId, float delay)
        {
            yield return new WaitForSeconds(delay);
            GoToSection(sectionId);
        }

        private void SetState(GameState state)
        {
            CurrentState = state;
            OnStateChanged?.Invoke(state);
        }
    }

    public class CombatState
    {
        public string enemyName;
        public int enemySkill;
        public int enemyStamina;
        public int enemyMaxStamina;
        public int victoryTarget;
        public int defeatTarget;
        public int fleeTarget;
        public bool allowFlee;
        public List<string> lootOnVictory;
        public CombatPhase phase;
        public string combatSectionText;
    }

    public enum CombatPhase
    {
        Fighting,
        Result
    }
}
