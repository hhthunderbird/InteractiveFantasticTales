using System.Collections.Generic;
using UnityEngine;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.Input
{
    public class VoiceInputManager : MonoBehaviour
    {
        [SerializeField] private bool _enableVoice = true;
        [SerializeField] private float _silenceTimeout = 2f;

        private readonly Dictionary<string, System.Action> _commands = new();

        private void Start()
        {
            if (!_enableVoice) return;

            _commands["escolher um"] = () => GameEngine.Instance?.MakeChoice(0);
            _commands["escolher dois"] = () => GameEngine.Instance?.MakeChoice(1);
            _commands["escolher três"] = () => GameEngine.Instance?.MakeChoice(2);
            _commands["escolher quatro"] = () => GameEngine.Instance?.MakeChoice(3);
            _commands["opção um"] = () => GameEngine.Instance?.MakeChoice(0);
            _commands["opção dois"] = () => GameEngine.Instance?.MakeChoice(1);
            _commands["opção três"] = () => GameEngine.Instance?.MakeChoice(2);
            _commands["opção quatro"] = () => GameEngine.Instance?.MakeChoice(3);
            _commands["primeira"] = () => GameEngine.Instance?.MakeChoice(0);
            _commands["segunda"] = () => GameEngine.Instance?.MakeChoice(1);
            _commands["terceira"] = () => GameEngine.Instance?.MakeChoice(2);
            _commands["quarta"] = () => GameEngine.Instance?.MakeChoice(3);
            _commands["repetir"] = RepeatNarration;
            _commands["ler de novo"] = RepeatNarration;
            _commands["voltar"] = GoBack;
            _commands["retornar"] = GoBack;
            _commands["ficha"] = () => FindObjectOfType<UI.CharacterSheetUI>()?.Toggle();
            _commands["status"] = () => FindObjectOfType<UI.CharacterSheetUI>()?.Toggle();
            _commands["personagem"] = () => FindObjectOfType<UI.CharacterSheetUI>()?.Toggle();
            _commands["inventário"] = () => FindObjectOfType<UI.CharacterSheetUI>()?.Toggle();
            _commands["itens"] = () => FindObjectOfType<UI.CharacterSheetUI>()?.Toggle();
            _commands["mochila"] = () => FindObjectOfType<UI.CharacterSheetUI>()?.Toggle();
            _commands["pausar"] = () => Audio.NarrationManager.Instance?.Pause();
            _commands["parar"] = () => Audio.NarrationManager.Instance?.Stop();
            _commands["continuar"] = () => Audio.NarrationManager.Instance?.Resume();
            _commands["seguir"] = () => Audio.NarrationManager.Instance?.Resume();
            _commands["atacar"] = () => GameEngine.Instance?.FightRound();
            _commands["fugir"] = () => GameEngine.Instance?.FleeCombat();
        }

        public void OnVoiceCommand(string command)
        {
            command = command.ToLower().Trim();
            foreach (var kvp in _commands)
            {
                if (command.Contains(kvp.Key))
                {
                    kvp.Value?.Invoke();
                    return;
                }
            }
        }

        private void RepeatNarration()
        {
            Audio.NarrationManager.Instance?.Speak(GameEngine.Instance?.CurrentSection?.text ?? "");
        }

        private void GoBack()
        {
            var history = GameEngine.Instance?.NavigationHistory;
            if (history == null || history.Count <= 1) return;
            history.Pop();
            int prevId = history.Peek();
            GameEngine.Instance?.GoToSection(prevId);
        }
    }
}
