using System;
using System.Collections.Generic;
using UnityEngine;

namespace InteractiveFantasticTales.Models
{
    [Serializable]
    public class StoryData : ISerializationCallbackReceiver
    {
        public string formatVersion;
        public StoryMetadata metadata;
        public CharacterCreationData characterCreation;
        public Dictionary<string, FlagDefinition> flags = new();
        public Dictionary<string, ItemDefinition> items = new();
        public Dictionary<string, SectionData> sections = new();

        [SerializeField] private List<DictEntry<FlagDefinition>> _flagsList = new();
        [SerializeField] private List<DictEntry<ItemDefinition>> _itemsList = new();
        [SerializeField] private List<DictEntry<SectionData>> _sectionsList = new();

        public void OnBeforeSerialize()
        {
            _flagsList.Clear(); foreach (var kv in flags) _flagsList.Add(new DictEntry<FlagDefinition> { key = kv.Key, value = kv.Value });
            _itemsList.Clear(); foreach (var kv in items) _itemsList.Add(new DictEntry<ItemDefinition> { key = kv.Key, value = kv.Value });
            _sectionsList.Clear(); foreach (var kv in sections) _sectionsList.Add(new DictEntry<SectionData> { key = kv.Key, value = kv.Value });
        }

        public void OnAfterDeserialize()
        {
            flags.Clear(); foreach (var e in _flagsList) flags[e.key] = e.value;
            items.Clear(); foreach (var e in _itemsList) items[e.key] = e.value;
            sections.Clear(); foreach (var e in _sectionsList) sections[e.key] = e.value;
        }
    }

    [Serializable] public class DictEntry<T> { public string key; public T value; }

    [Serializable] public class StoryMetadata { public string id; public string title; public AuthorData author; public string version; public string language; public List<string> genre; public string description; public string coverImage; public int startSection; public string estimatedDuration; public List<string> tags; }
    [Serializable] public class AuthorData { public string name; public string email; }
    [Serializable] public class CharacterCreationData { public DictEntry<AttributeDef>[] attributesArray; public int startingGold; public List<string> startingItems; public int startingProvisions; public Dictionary<string, AttributeDef> attributes { get { var d = new Dictionary<string, AttributeDef>(); if (attributesArray != null) foreach (var e in attributesArray) d[e.key] = e.value; return d; } } }
    [Serializable] public class AttributeDef { public string label; public string dice; public int min; public int max; }
    [Serializable] public class FlagDefinition { public string type; public bool defaultBool; public int defaultCounter; }
    [Serializable] public class ItemDefinition { public string type; public string description; public int[] effectsKeys; public int[] effectsValues; }

    [Serializable]
    public class SectionData
    {
        public int id; public string type; public string text;
        public List<ChoiceData> choices; public CombatData combat; public TestData test;
        public ItemGateData itemGate; public RandomData random; public EndingData ending;
        public OnEnterData onEnter; public PresentationData presentation;
    }

    [Serializable] public class ChoiceData { public string id; public string text; public int targetSection; public List<ConditionData> conditions; }
    [Serializable] public class ConditionData { public string type; public string key; public string op; public string value; }
    [Serializable] public class CombatData { public string enemyName; public int enemySkill; public int enemyStamina; public int victoryTarget; public int defeatTarget; public int fleeTarget; public bool allowFlee; public List<string> lootOnVictory; }
    [Serializable] public class TestData { public string attribute; public int difficulty; public int successTarget; public int failTarget; }
    [Serializable] public class ItemGateData { public string item; public int hasItemTarget; public int noItemTarget; }
    [Serializable] public class RandomData { public List<RandomOutcome> outcomes; }
    [Serializable] public class RandomOutcome { public int targetSection; public float weight; }
    [Serializable] public class EndingData { public string type; }
    [Serializable] public class OnEnterData { public List<string> addItems; public List<string> removeItems; public List<DictEntryObj> setFlagsList; public int modifyGold; public int modifyStamina; public int modifyLuck; public Dictionary<string, object> setFlags { get { var d = new Dictionary<string, object>(); if (setFlagsList != null) foreach (var e in setFlagsList) d[e.key] = e.value; return d; } } }
    [Serializable] public class DictEntryObj { public string key; public object value; }
    [Serializable] public class PresentationData { public string illustration; public string ambientSound; public NarrationConfig narration; public HapticsConfig haptics; }
    [Serializable] public class NarrationConfig { public string voice; public float speed; public List<string> emphasis; }
    [Serializable] public class HapticPattern { public string type; public float intensity; public float duration; }
    [Serializable] public class HapticsConfig { public HapticPattern onEnter; public HapticPattern onCombat; public HapticPattern onDiscovery; public HapticPattern onDanger; }
}