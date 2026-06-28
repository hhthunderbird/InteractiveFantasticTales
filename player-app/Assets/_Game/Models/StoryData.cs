using System;
using System.Collections.Generic;

namespace InteractiveFantasticTales.Models
{
    [Serializable]
    public class StoryData
    {
        public string formatVersion;
        public StoryMetadata metadata;
        public CharacterCreationData characterCreation;
        public Dictionary<string, FlagDefinition> flags;
        public Dictionary<string, ItemDefinition> items;
        public Dictionary<string, SectionData> sections;
    }

    [Serializable]
    public class StoryMetadata
    {
        public string id;
        public string title;
        public AuthorData author;
        public string version;
        public string language;
        public List<string> genre;
        public string description;
        public string coverImage;
        public int startSection;
        public string estimatedDuration;
        public List<string> tags;
    }

    [Serializable]
    public class AuthorData
    {
        public string name;
        public string email;
    }

    [Serializable]
    public class CharacterCreationData
    {
        public Dictionary<string, AttributeDef> attributes;
        public int startingGold;
        public List<string> startingItems;
        public int startingProvisions;
    }

    [Serializable]
    public class AttributeDef
    {
        public string label;
        public string dice;
        public int min;
        public int max;
    }

    [Serializable]
    public class FlagDefinition
    {
        public string type;
        public bool defaultBool;
        public int defaultCounter;
    }

    [Serializable]
    public class ItemDefinition
    {
        public string type;
        public string description;
        public Dictionary<string, int> effects;
    }

    [Serializable]
    public class SectionData
    {
        public int id;
        public string type;
        public string text;
        public List<ChoiceData> choices;
        public CombatData combat;
        public TestData test;
        public ItemGateData itemGate;
        public RandomData random;
        public EndingData ending;
        public OnEnterData onEnter;
        public PresentationData presentation;
    }

    [Serializable]
    public class ChoiceData
    {
        public string id;
        public string text;
        public int targetSection;
        public List<ConditionData> conditions;
    }

    [Serializable]
    public class ConditionData
    {
        public string type;
        public string key;
        public string op;
        public string value;
    }

    [Serializable]
    public class CombatData
    {
        public string enemyName;
        public int enemySkill;
        public int enemyStamina;
        public int victoryTarget;
        public int defeatTarget;
        public int fleeTarget;
        public bool allowFlee;
        public List<string> lootOnVictory;
    }

    [Serializable]
    public class TestData
    {
        public string attribute;
        public int difficulty;
        public int successTarget;
        public int failTarget;
    }

    [Serializable]
    public class ItemGateData
    {
        public string item;
        public int hasItemTarget;
        public int noItemTarget;
    }

    [Serializable]
    public class RandomData
    {
        public List<RandomOutcome> outcomes;
    }

    [Serializable]
    public class RandomOutcome
    {
        public int targetSection;
        public float weight;
    }

    [Serializable]
    public class EndingData
    {
        public string type;
    }

    [Serializable]
    public class OnEnterData
    {
        public List<string> addItems;
        public List<string> removeItems;
        public Dictionary<string, object> setFlags;
        public int modifyGold;
        public int modifyStamina;
        public int modifyLuck;
    }

    [Serializable]
    public class PresentationData
    {
        public string illustration;
        public string ambientSound;
        public NarrationConfig narration;
        public HapticsConfig haptics;
    }

    [Serializable]
    public class NarrationConfig
    {
        public string voice;
        public float speed;
        public List<string> emphasis;
    }

    [Serializable]
    public class HapticPattern
    {
        public string type;
        public float intensity;
        public float duration;
    }

    [Serializable]
    public class HapticsConfig
    {
        public HapticPattern onEnter;
        public HapticPattern onCombat;
        public HapticPattern onDiscovery;
        public HapticPattern onDanger;
    }
}
