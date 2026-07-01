using System;
using UnityEngine;
using InteractiveFantasticTales.Core;
using InteractiveFantasticTales.Models;

namespace InteractiveFantasticTales.Services
{
    /// <summary>
    /// Componente leve que conecta eventos do GameEngine ao LibraryManager para tracking automatico.
    /// Singleton (DontDestroyOnLoad). Gerencia playtime via Stopwatch e salva automaticamente.
    /// </summary>
    public class ProgressTracker : MonoBehaviour
    {
        public static ProgressTracker Instance;

        private System.Diagnostics.Stopwatch _sessionStopwatch;
        private string _currentStoryId;
        private int _currentSectionId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sessionStopwatch = new System.Diagnostics.Stopwatch();
        }

        private void Start()
        {
            SubscribeToEngine();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEngine();
            SaveCurrentProgress();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveCurrentProgress();
        }

        private void OnApplicationQuit()
        {
            SaveCurrentProgress();
        }

        private void SubscribeToEngine()
        {
            var engine = GameEngine.Instance;
            if (engine == null)
            {
                Debug.LogWarning("[ProgressTracker] GameEngine.Instance e null - assinatura adiada.");
                return;
            }

            engine.OnSectionChanged += HandleSectionChanged;
            engine.OnStateChanged += HandleStateChanged;
            engine.OnCombatUpdated += HandleCombatUpdated;
            engine.OnMessage += HandleMessage;

            Debug.Log("[ProgressTracker] Conectado aos eventos do GameEngine.");
        }

        private void UnsubscribeFromEngine()
        {
            var engine = GameEngine.Instance;
            if (engine == null) return;

            engine.OnSectionChanged -= HandleSectionChanged;
            engine.OnStateChanged -= HandleStateChanged;
            engine.OnCombatUpdated -= HandleCombatUpdated;
            engine.OnMessage -= HandleMessage;
        }

        // -- GameEngine Event Handlers ---------------------------------

        private void HandleSectionChanged(SectionData section)
        {
            if (section == null) return;

            _currentSectionId = section.id;

            var lib = LibraryManager.Instance;
            if (lib == null) return;

            lib.TrackSectionVisited(section.id);

            if (section.type == "ending")
            {
                string endingType = section.ending != null ? section.ending.type : "neutral";
                lib.TrackEndingFound($"ending_{section.id}", endingType);
            }
        }

        private void HandleStateChanged(GameState state)
        {
            var engine = GameEngine.Instance;
            if (engine?.CurrentStory == null) return;

            if (state == GameState.Narrative && _currentStoryId != engine.CurrentStory.metadata.id)
            {
                if (!string.IsNullOrEmpty(_currentStoryId))
                {
                    LibraryManager.Instance?.EndSession(_currentStoryId, _currentSectionId);
                }

                _currentStoryId = engine.CurrentStory.metadata.id;
                LibraryManager.Instance?.StartSession(_currentStoryId);
            }
        }

        private void HandleCombatUpdated(CombatState combat)
        {
            if (combat == null) return;

            if (combat.phase == CombatPhase.Result)
            {
                var engine = GameEngine.Instance;
                if (engine?.PlayerCharacter == null) return;

                string result;
                if (engine.PlayerCharacter.stamina > 0 && combat.enemyStamina <= 0)
                    result = "victory";
                else if (engine.PlayerCharacter.stamina <= 0)
                    result = "defeat";
                else
                    result = "fled";

                LibraryManager.Instance?.TrackCombatResult(result);
            }
        }

        private void HandleMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            var engine = GameEngine.Instance;
            if (engine == null) return;

            if (engine.PlayerCharacter == null) return;

            if (message.Contains("derrotado") || message.Contains("derrota"))
                LibraryManager.Instance?.TrackCharacterDeath();
        }

        // -- Progress Save ---------------------------------------------

        private void SaveCurrentProgress()
        {
            if (string.IsNullOrEmpty(_currentStoryId)) return;

            _sessionStopwatch?.Stop();

            var lib = LibraryManager.Instance;
            if (lib == null) return;

            lib.TrackSectionVisited(_currentSectionId);
            lib.SaveProgress();

            Debug.Log($"[ProgressTracker] Progresso salvo: {_currentStoryId}");
        }
    }
}
