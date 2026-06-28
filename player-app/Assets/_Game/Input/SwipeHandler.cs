using UnityEngine;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.Input
{
    public class SwipeHandler : MonoBehaviour
    {
        [SerializeField] private float _minDistance = 50f;
        [SerializeField] private float _maxTime = 0.5f;

        private Vector2 _start; private float _startTime;

        private void Update()
        {
            if (UnityEngine.Input.touchCount == 0) return;
            var engine = GameEngine.Instance;
            if (engine == null || engine.CurrentState != GameState.Narrative) return;
            var t = UnityEngine.Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { _start = t.position; _startTime = Time.time; }
            else if (t.phase == TouchPhase.Ended)
            {
                if (Time.time - _startTime > _maxTime) return;
                var d = t.position - _start;
                if (d.magnitude < _minDistance) return;
                if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) engine.MakeChoice(d.x > 0 ? 0 : 1);
                else engine.MakeChoice(d.y > 0 ? 2 : 3);
            }
        }
    }
}