using UnityEngine;
using InteractiveFantasticTales.Core;
using InteractiveFantasticTales.Save;

namespace InteractiveFantasticTales
{
    public class GameController : MonoBehaviour
    {
        [Header("Core Systems")]
        [SerializeField] private GameEngine _gameEngine;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private AccessibilityManager _accessibilityManager;

        [Header("UI")]
        [SerializeField] private UI.StoryPanelUI _storyPanel;
        [SerializeField] private UI.ChoiceButtonsUI _choiceButtons;
        [SerializeField] private UI.CharacterSheetUI _characterSheet;
        [SerializeField] private UI.CombatUI _combatUI;

        [Header("Audio")]
        [SerializeField] private Audio.NarrationManager _narrationManager;

        [Header("Input")]
        [SerializeField] private Input.SwipeHandler _swipeHandler;
        [SerializeField] private Input.VoiceInputManager _voiceInputManager;

        [Header("Settings")]
        [SerializeField] private string _demoStoryFileName = "demo-labirinto-do-arquimago.json";

        private void Start()
        {
            FindOrCreateSystems();
            LoadInitialStory();
        }

        private void FindOrCreateSystems()
        {
            if (_gameEngine == null) _gameEngine = FindOrAdd<GameEngine>("GameEngine");
            if (_saveManager == null) _saveManager = FindOrAdd<SaveManager>("SaveManager");
            if (_accessibilityManager == null) _accessibilityManager = FindOrAdd<AccessibilityManager>("Accessibility");
            if (_narrationManager == null) _narrationManager = FindOrAdd<Audio.NarrationManager>("Narration");
            if (_swipeHandler == null) _swipeHandler = FindOrAdd<Input.SwipeHandler>("SwipeHandler");
            if (_voiceInputManager == null) _voiceInputManager = FindOrAdd<Input.VoiceInputManager>("VoiceInput");

            if (_storyPanel == null) _storyPanel = FindObjectOfType<UI.StoryPanelUI>();
            if (_choiceButtons == null) _choiceButtons = FindObjectOfType<UI.ChoiceButtonsUI>();
            if (_characterSheet == null) _characterSheet = FindObjectOfType<UI.CharacterSheetUI>();
            if (_combatUI == null) _combatUI = FindObjectOfType<UI.CombatUI>();
        }

        private T FindOrAdd<T>(string name) where T : MonoBehaviour
        {
            var existing = FindObjectOfType<T>();
            if (existing != null) return existing;

            var go = new GameObject(name);
            return go.AddComponent<T>();
        }

        private void LoadInitialStory()
        {
            if (!string.IsNullOrEmpty(_demoStoryFileName))
            {
                _gameEngine.LoadStoryFromStreamingAssets(_demoStoryFileName);
            }
        }
    }
}
