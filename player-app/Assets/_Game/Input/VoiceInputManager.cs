using System.Collections.Generic;
using UnityEngine;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.Input
{
    public class VoiceInputManager : MonoBehaviour
    {
        private readonly Dictionary<string, System.Action> _commands = new();
        private bool _listening = false;

        private void Start()
        {
            _commands["escolher um"] = () => GameEngine.Instance?.MakeChoice(0);
            _commands["escolher dois"] = () => GameEngine.Instance?.MakeChoice(1);
            _commands["escolher tres"] = () => GameEngine.Instance?.MakeChoice(2);
            _commands["escolher quatro"] = () => GameEngine.Instance?.MakeChoice(3);
            _commands["opcao um"] = () => GameEngine.Instance?.MakeChoice(0);
            _commands["opcao dois"] = () => GameEngine.Instance?.MakeChoice(1);
            _commands["opcao tres"] = () => GameEngine.Instance?.MakeChoice(2);
            _commands["opcao quatro"] = () => GameEngine.Instance?.MakeChoice(3);
            _commands["primeira"] = () => GameEngine.Instance?.MakeChoice(0);
            _commands["segunda"] = () => GameEngine.Instance?.MakeChoice(1);
            _commands["terceira"] = () => GameEngine.Instance?.MakeChoice(2);
            _commands["quarta"] = () => GameEngine.Instance?.MakeChoice(3);
            _commands["repetir"] = () => Audio.NarrationManager.Instance?.Speak(GameEngine.Instance?.CurrentSection?.text ?? "");
            _commands["ler de novo"] = () => Audio.NarrationManager.Instance?.Speak(GameEngine.Instance?.CurrentSection?.text ?? "");
            _commands["ficha"] = () => FindObjectOfType<UI.CharacterSheetUI>()?.Toggle();
            _commands["status"] = () => FindObjectOfType<UI.CharacterSheetUI>()?.Toggle();
            _commands["inventario"] = () => FindObjectOfType<UI.CharacterSheetUI>()?.Toggle();
            _commands["pausar"] = () => Audio.NarrationManager.Instance?.Pause();
            _commands["continuar"] = () => Audio.NarrationManager.Instance?.Resume();
            _commands["atacar"] = () => GameEngine.Instance?.FightRound();
            _commands["fugir"] = () => GameEngine.Instance?.FleeCombat();
        }

        public void ProcessCommand(string text)
        {
            text = text.ToLower().Trim();
            foreach (var cmd in _commands)
            {
                if (text.Contains(cmd.Key))
                {
                    cmd.Value?.Invoke();
                    Debug.Log($"[VOICE] Comando: {cmd.Key}");
                    return;
                }
            }
        }

#if UNITY_ANDROID || UNITY_IOS
        // Stub for platform-specific speech recognition
        public void StartListening() => _listening = true;
        public void StopListening() => _listening = false;
#else
        public void StartListening() { _listening = true; Debug.Log("[VOICE] Escutando..."); }
        public void StopListening() { _listening = false; Debug.Log("[VOICE] Parou de escutar."); }
#endif
    }
}