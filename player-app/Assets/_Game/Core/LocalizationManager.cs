using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace InteractiveFantasticTales.Core
{
    /// <summary>
    /// Simple key-based localization manager. Loads JSON locale files from Resources/Locales/.
    /// Usage: LocalizationManager.Instance.Get("key") or LocalizationManager.Instance["key"]
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        private Dictionary<string, Dictionary<string, string>> _locales = new();
        private string _currentLocale = "pt-BR";
        private Dictionary<string, string> _currentStrings;

        public event Action<string> OnLocaleChanged;
        public string CurrentLocale => _currentLocale;
        public List<string> AvailableLocales { get; private set; } = new();

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); return; }
            LoadAllLocales();
            LoadSavedLocale();
        }

        public string Get(string key)
        {
            if (_currentStrings != null && _currentStrings.TryGetValue(key, out var val))
                return val;
            return key;
        }

        public string this[string key] => Get(key);

        public void SetLocale(string localeCode)
        {
            if (!_locales.ContainsKey(localeCode))
            {
                Debug.LogWarning($"[Loc] Locale '{localeCode}' not found. Available: {string.Join(", ", _locales.Keys)}");
                return;
            }
            _currentLocale = localeCode;
            _currentStrings = _locales[localeCode];
            PlayerPrefs.SetString("locale", localeCode);
            PlayerPrefs.Save();
            OnLocaleChanged?.Invoke(localeCode);
            Debug.Log($"[Loc] Switched to {localeCode}");
        }

        public string GetLocaleNativeName(string code)
        {
            return code switch
            {
                "pt-BR" => "Português",
                "en" => "English",
                "es" => "Español",
                _ => code
            };
        }

        private void LoadAllLocales()
        {
            _locales.Clear();
            AvailableLocales.Clear();
            var assets = Resources.LoadAll<TextAsset>("Locales");
            foreach (var asset in assets)
            {
                try
                {
                    var localeCode = Path.GetFileNameWithoutExtension(asset.name);
                    var dict = ParseSimpleJson(asset.text);
                    _locales[localeCode] = dict;
                    AvailableLocales.Add(localeCode);
                    Debug.Log($"[Loc] Loaded '{localeCode}': {dict.Count} keys");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Loc] Failed to parse {asset.name}: {e.Message}");
                }
            }
        }

        private void LoadSavedLocale()
        {
            var saved = PlayerPrefs.GetString("locale", "pt-BR");
            if (_locales.ContainsKey(saved))
            {
                _currentLocale = saved;
                _currentStrings = _locales[saved];
            }
            else if (_locales.Count > 0)
            {
                // Fallback to first available
                var first = new List<string>(_locales.Keys)[0];
                _currentLocale = first;
                _currentStrings = _locales[first];
            }
        }

        /// <summary>
        /// Parses a flat JSON object: { "key": "value", ... }
        /// Handles escaped strings and nested quotes.
        /// </summary>
        private static Dictionary<string, string> ParseSimpleJson(string json)
        {
            var result = new Dictionary<string, string>();
            json = json.Trim();
            if (!json.StartsWith("{") || !json.EndsWith("}")) return result;

            var content = json.Substring(1, json.Length - 2).Trim();
            if (string.IsNullOrEmpty(content)) return result;

            int i = 0;
            while (i < content.Length)
            {
                // Skip whitespace and commas
                while (i < content.Length && (content[i] == ' ' || content[i] == '\n' || content[i] == '\r' || content[i] == '\t' || content[i] == ','))
                    i++;
                if (i >= content.Length) break;

                // Parse key (quoted string)
                if (content[i] != '"') break;
                var key = ReadQuotedString(content, ref i);
                if (key == null) break;

                // Skip colon
                while (i < content.Length && (content[i] == ' ' || content[i] == ':'))
                    i++;

                // Parse value (quoted string)
                if (i >= content.Length) break;
                if (content[i] != '"') break;
                var value = ReadQuotedString(content, ref i);
                if (value == null) break;

                result[key] = value;
            }
            return result;
        }

        private static string ReadQuotedString(string s, ref int i)
        {
            if (s[i] != '"') return null;
            i++; // skip opening quote
            var result = "";
            while (i < s.Length)
            {
                if (s[i] == '\\')
                {
                    i++;
                    if (i >= s.Length) break;
                    switch (s[i])
                    {
                        case '"': result += '"'; break;
                        case '\\': result += '\\'; break;
                        case 'n': result += '\n'; break;
                        case 'r': result += '\r'; break;
                        case 't': result += '\t'; break;
                        default: result += s[i]; break;
                    }
                }
                else if (s[i] == '"')
                {
                    i++; // skip closing quote
                    return result;
                }
                else
                {
                    result += s[i];
                }
                i++;
            }
            return result;
        }
    }
}
