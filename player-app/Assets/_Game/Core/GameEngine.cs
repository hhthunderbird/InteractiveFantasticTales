using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InteractiveFantasticTales.Models;

namespace InteractiveFantasticTales.Core
{
    public enum GameState { Loading, Narrative, ChoicePending, CombatActive, TestActive, ItemGateCheck, Transitioning, GameOver, Paused }
    public enum CombatPhase { Fighting, Result }

    public class CombatState
    {
        public string enemyName; public int enemySkill; public int enemyStamina; public int enemyMaxStamina;
        public int victoryTarget; public int defeatTarget; public int fleeTarget; public bool allowFlee;
        public List<string> lootOnVictory; public CombatPhase phase; public string combatSectionText;
    }

    public class GameEngine : MonoBehaviour
    {
        public static GameEngine Instance { get; private set; }
        public event Action<SectionData> OnSectionChanged;
        public event Action<Character> OnCharacterUpdated;
        public event Action<GameState> OnStateChanged;
        public event Action<string> OnMessage;
        public event Action<CombatState> OnCombatUpdated;

        public StoryData CurrentStory { get; private set; }
        public Character PlayerCharacter { get; private set; }
        public SectionData CurrentSection { get; private set; }
        public GameState CurrentState { get; private set; }
        public CombatState ActiveCombat { get; private set; }
        public Stack<int> NavigationHistory { get; } = new();

        private void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); }

        public void LoadStoryFromJson(string json)
        {
            SetState(GameState.Loading);
            try { CurrentStory = JsonUtility.FromJson<StoryData>(json); CreateCharacter(); GoToSection(CurrentStory.metadata.startSection); }
            catch (Exception e) { OnMessage?.Invoke($"Erro: {e.Message}"); }
        }

        public void LoadDemoStory()
        {
            var ta = Resources.Load<TextAsset>("demo-labirinto-do-arquimago");
            if (ta != null) LoadStoryFromJson(ta.text);
            else OnMessage?.Invoke("História demo não encontrada em Resources.");
        }

        private void CreateCharacter()
        {
            var cc = CurrentStory.characterCreation;
            PlayerCharacter = new Character();
            PlayerCharacter.skill = Character.RollDice(cc.attributes.ContainsKey("skill") ? cc.attributes["skill"].dice : "1d6+6");
            PlayerCharacter.stamina = Character.RollDice(cc.attributes.ContainsKey("stamina") ? cc.attributes["stamina"].dice : "2d6+12");
            PlayerCharacter.maxStamina = PlayerCharacter.stamina;
            PlayerCharacter.luck = Character.RollDice(cc.attributes.ContainsKey("luck") ? cc.attributes["luck"].dice : "1d6+6");
            PlayerCharacter.maxLuck = PlayerCharacter.luck;
            PlayerCharacter.gold = cc.startingGold;
            PlayerCharacter.provisions = cc.startingProvisions;
            if (cc.startingItems != null) PlayerCharacter.inventory.AddRange(cc.startingItems);
            OnCharacterUpdated?.Invoke(PlayerCharacter);
        }

        public void GoToSection(int sectionId)
        {
            string key = sectionId.ToString();
            if (!CurrentStory.sections.TryGetValue(key, out var section)) { OnMessage?.Invoke($"Seção {sectionId} não encontrada."); return; }
            NavigationHistory.Push(sectionId);
            CurrentSection = section;
            if (section.onEnter != null) EvalOnEnter(section.onEnter);
            switch (section.type)
            {
                case "narrative": SetState(GameState.Narrative); break;
                case "combat": if (section.combat != null) StartCombat(section); break;
                case "test": SetState(GameState.TestActive); break;
                case "itemGate": SetState(GameState.ItemGateCheck); break;
                case "random": SetState(GameState.Narrative); if (section.random?.outcomes?.Count > 0) ResolveRandom(section); break;
                case "ending": SetState(GameState.GameOver); break;
            }
            OnSectionChanged?.Invoke(section);
        }

        public void MakeChoice(int index)
        {
            if (CurrentSection?.choices == null || index >= CurrentSection.choices.Count) return;
            var choice = CurrentSection.choices[index];
            if (!EvaluateConditions(choice.conditions)) return;
            int target = choice.targetSection;
            if (CurrentStory.sections.TryGetValue(target.ToString(), out var t) && t.type == "combat" && t.combat != null)
                StartCombat(t);
            else
                GoToSection(target);
        }

        public void FightRound()
        {
            if (ActiveCombat == null) return;
            int pr = Character.RollDice("2d6") + PlayerCharacter.skill;
            int er = Character.RollDice("2d6") + ActiveCombat.enemySkill;
            if (pr > er) { ActiveCombat.enemyStamina -= 2; OnMessage?.Invoke($"Você ataca! {ActiveCombat.enemyName} perde 2 STAMINA."); }
            else if (er > pr) { PlayerCharacter.stamina -= 2; OnMessage?.Invoke($"{ActiveCombat.enemyName} ataca! Você perde 2 STAMINA."); }
            else { OnMessage?.Invoke("Empate! Ninguém se fere."); }
            OnCombatUpdated?.Invoke(ActiveCombat);
            if (ActiveCombat.enemyStamina <= 0) { ActiveCombat.phase = CombatPhase.Result; OnMessage?.Invoke($"Vitoria sobre {ActiveCombat.enemyName}!"); if (ActiveCombat.lootOnVictory != null) foreach (var item in ActiveCombat.lootOnVictory) PlayerCharacter.AddItem(item); GoToSection(ActiveCombat.victoryTarget); }
            else if (PlayerCharacter.stamina <= 0) { ActiveCombat.phase = CombatPhase.Result; OnMessage?.Invoke("Voce foi derrotado..."); GoToSection(ActiveCombat.defeatTarget); }
        }

        public void FleeCombat()
        {
            if (ActiveCombat == null || !ActiveCombat.allowFlee) return;
            if (PlayerCharacter.TestLuck()) { OnMessage?.Invoke("Fuga bem sucedida!"); GoToSection(ActiveCombat.fleeTarget); }
            else OnMessage?.Invoke("Falha na fuga!");
        }

        public void ResolveTest()
        {
            if (CurrentSection?.test == null) return;
            var t = CurrentSection.test;
            int val = t.attribute == "skill" ? PlayerCharacter.skill : t.attribute == "luck" ? PlayerCharacter.luck : 7;
            int roll = Character.RollDice("2d6"); bool ok = roll + val >= t.difficulty;
            OnMessage?.Invoke(ok ? $"Sucesso! {roll + val} >= {t.difficulty}" : $"Falha! {roll + val} < {t.difficulty}");
            StartCoroutine(DelayedGo(ok ? t.successTarget : t.failTarget, 1.2f));
        }

        public void ResolveItemGate()
        {
            if (CurrentSection?.itemGate == null) return;
            var g = CurrentSection.itemGate; bool has = PlayerCharacter.HasItem(g.item);
            OnMessage?.Invoke(has ? $"Voce tem {g.item}!" : $"Voce nao tem {g.item}.");
            StartCoroutine(DelayedGo(has ? g.hasItemTarget : g.noItemTarget, 1f));
        }

        private void ResolveRandom(SectionData s)
        {
            float total = 0; foreach (var o in s.random.outcomes) total += o.weight;
            float r = UnityEngine.Random.Range(0f, total); float c = 0;
            foreach (var o in s.random.outcomes) { c += o.weight; if (r <= c) { GoToSection(o.targetSection); return; } }
        }

        private void StartCombat(SectionData s)
        {
            ActiveCombat = new CombatState { enemyName = s.combat.enemyName, enemySkill = s.combat.enemySkill, enemyStamina = s.combat.enemyStamina, enemyMaxStamina = s.combat.enemyStamina, victoryTarget = s.combat.victoryTarget, defeatTarget = s.combat.defeatTarget, fleeTarget = s.combat.fleeTarget, allowFlee = s.combat.allowFlee, lootOnVictory = s.combat.lootOnVictory, phase = CombatPhase.Fighting, combatSectionText = s.text };
            SetState(GameState.CombatActive);
            OnSectionChanged?.Invoke(s); OnCombatUpdated?.Invoke(ActiveCombat);
        }

        private bool EvaluateConditions(List<ConditionData> conds)
        {
            if (conds == null || conds.Count == 0) return true;
            foreach (var c in conds) { if (c.type == "hasItem" && !PlayerCharacter.HasItem(c.key)) return false; if (c.type == "hasFlag" && !PlayerCharacter.HasFlag(c.key)) return false; }
            return true;
        }

        private void EvalOnEnter(OnEnterData oe)
        {
            if (oe.addItems != null) foreach (var i in oe.addItems) PlayerCharacter.AddItem(i);
            if (oe.removeItems != null) foreach (var i in oe.removeItems) PlayerCharacter.RemoveItem(i);
            if (oe.setFlags != null) foreach (var kv in oe.setFlags) PlayerCharacter.SetFlag(kv.Key, kv.Value);
            if (oe.modifyGold != 0) PlayerCharacter.ModifyGold(oe.modifyGold);
            if (oe.modifyStamina != 0) PlayerCharacter.ModifyStamina(oe.modifyStamina);
            if (oe.modifyLuck != 0) PlayerCharacter.ModifyLuck(oe.modifyLuck);
            OnCharacterUpdated?.Invoke(PlayerCharacter);
        }

        private IEnumerator DelayedGo(int id, float d) { yield return new WaitForSeconds(d); GoToSection(id); }
        private void SetState(GameState s) { CurrentState = s; OnStateChanged?.Invoke(s); }
    }
}