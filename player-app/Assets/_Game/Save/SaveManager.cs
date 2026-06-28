using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using InteractiveFantasticTales.Models;

namespace InteractiveFantasticTales.Save
{
    [Serializable]
    public class SaveData
    {
        public string storyId;
        public int currentSection;
        public Character character;
        public List<int> navigationHistory;
        public List<int> visitedSections;
        public List<BookmarkData> bookmarks;
        public DateTime saveTime;
        public string saveName;
    }

    [Serializable]
    public class BookmarkData
    {
        public int sectionId;
        public string name;
        public DateTime createdAt;
    }

    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [SerializeField] private int _maxSaveSlots = 5;

        public List<int> VisitedSections { get; } = new();
        public List<BookmarkData> Bookmarks { get; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SaveGame(int slot, string name)
        {
            var engine = Core.GameEngine.Instance;
            if (engine?.CurrentStory == null) return;

            var data = new SaveData
            {
                storyId = engine.CurrentStory.metadata.id,
                currentSection = engine.CurrentSection?.id ?? 1,
                character = engine.PlayerCharacter,
                navigationHistory = new List<int>(engine.NavigationHistory),
                visitedSections = new List<int>(VisitedSections),
                bookmarks = new List<BookmarkData>(Bookmarks),
                saveTime = DateTime.Now,
                saveName = name,
            };

            var json = JsonUtility.ToJson(data);
            var path = GetSavePath(slot);
            File.WriteAllText(path, json);
            PlayerPrefs.SetString($"save_meta_{slot}", $"{name}|{DateTime.Now:g}");
        }

        public SaveData LoadGame(int slot)
        {
            var path = GetSavePath(slot);
            if (!File.Exists(path)) return null;

            try
            {
                var json = File.ReadAllText(path);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load save {slot}: {e.Message}");
                return null;
            }
        }

        public void DeleteSave(int slot)
        {
            var path = GetSavePath(slot);
            if (File.Exists(path)) File.Delete(path);
            PlayerPrefs.DeleteKey($"save_meta_{slot}");
        }

        public string GetSaveMeta(int slot)
        {
            return PlayerPrefs.GetString($"save_meta_{slot}", "Vazio");
        }

        public void AddBookmark(int sectionId, string name)
        {
            Bookmarks.Add(new BookmarkData
            {
                sectionId = sectionId,
                name = name,
                createdAt = DateTime.Now,
            });
        }

        public void RemoveBookmark(int index)
        {
            if (index >= 0 && index < Bookmarks.Count)
                Bookmarks.RemoveAt(index);
        }

        public void MarkVisited(int sectionId)
        {
            if (!VisitedSections.Contains(sectionId))
                VisitedSections.Add(sectionId);
        }

        private string GetSavePath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");
        }
    }
}
