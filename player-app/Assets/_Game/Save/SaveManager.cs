using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using InteractiveFantasticTales.Models;

namespace InteractiveFantasticTales.Save
{
    [Serializable]
    public class SaveFile
    {
        public string storyId; public int currentSection; public Character character;
        public List<int> history; public List<int> visited; public DateTime time; public string name;
    }

    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        public List<int> Visited { get; } = new();

        private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

        public void Save(int slot, string name)
        {
            var e = Core.GameEngine.Instance;
            if (e?.CurrentStory == null) return;
            var data = new SaveFile
            {
                storyId = e.CurrentStory.metadata.id, currentSection = e.CurrentSection?.id ?? 1,
                character = e.PlayerCharacter,
                history = new List<int>(e.NavigationHistory), visited = new List<int>(Visited),
                time = DateTime.Now, name = name,
            };
            File.WriteAllText(Path(slot), JsonUtility.ToJson(data));
        }

        public SaveFile Load(int slot)
        {
            var p = Path(slot);
            if (!File.Exists(p)) return null;
            try { return JsonUtility.FromJson<SaveFile>(File.ReadAllText(p)); }
            catch { return null; }
        }

        public void Delete(int slot) { var p = Path(slot); if (File.Exists(p)) File.Delete(p); }

        private static string Path(int s) => System.IO.Path.Combine(Application.persistentDataPath, $"save_{s}.json");
    }
}