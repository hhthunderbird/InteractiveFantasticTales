using System;
using System.Collections.Generic;
using UnityEngine;

namespace InteractiveFantasticTales.Models
{
    [Serializable]
    public class Character : ISerializationCallbackReceiver
    {
        public int skill; public int stamina; public int maxStamina;
        public int luck; public int maxLuck; public int gold; public int provisions;
        public List<string> inventory = new();
        public Dictionary<string, object> flags = new();
        public Dictionary<string, int> counters = new();

        [SerializeField] private List<DictEntryObj> _flagsList = new();
        [SerializeField] private List<DictEntryInt> _countersList = new();

        public void OnBeforeSerialize()
        {
            _flagsList.Clear(); foreach (var kv in flags) _flagsList.Add(new DictEntryObj { key = kv.Key, value = kv.Value });
            _countersList.Clear(); foreach (var kv in counters) _countersList.Add(new DictEntryInt { key = kv.Key, value = kv.Value });
        }

        public void OnAfterDeserialize()
        {
            flags.Clear(); foreach (var e in _flagsList) flags[e.key] = e.value;
            counters.Clear(); foreach (var e in _countersList) counters[e.key] = e.value;
        }

        public void ModifyStamina(int delta) { stamina = Math.Clamp(stamina + delta, 0, maxStamina); }
        public void ModifyLuck(int delta) { luck = Math.Clamp(luck + delta, 0, maxLuck); }
        public void ModifyGold(int delta) { gold = Math.Max(0, gold + delta); }
        public bool HasItem(string item) => inventory.Contains(item);
        public void AddItem(string item) { if (!inventory.Contains(item)) inventory.Add(item); }
        public void RemoveItem(string item) { inventory.Remove(item); }
        public bool HasFlag(string flag) => flags.ContainsKey(flag) && flags[flag] is bool b && b;
        public void SetFlag(string flag, object value) { flags[flag] = value; }
        public int GetCounter(string key) => counters.TryGetValue(key, out var v) ? v : 0;
        public void IncrementCounter(string key, int amount = 1) { counters.TryGetValue(key, out var v); counters[key] = v + amount; }
        public bool TestSkill(int difficulty) => RollDice("2d6") + skill >= difficulty;
        public bool TestLuck() { int roll = RollDice("2d6"); if (roll <= luck) { luck = Math.Max(0, luck - 1); return true; } return false; }

        public static int RollDice(string formula)
        {
            var match = System.Text.RegularExpressions.Regex.Match(formula, @"(\d+)d(\d+)([+-]\d+)?");
            if (!match.Success) return 0;
            int count = int.Parse(match.Groups[1].Value), sides = int.Parse(match.Groups[2].Value);
            int mod = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            int total = 0; for (int i = 0; i < count; i++) total += UnityEngine.Random.Range(1, sides + 1);
            return total + mod;
        }
    }

    [Serializable] public class DictEntryInt { public string key; public int value; }
}