using UnityEngine;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.Input
{
    public class SwipeHandler : MonoBehaviour
    {
        [SerializeField] private float _minSwipeDistance = 50f;
        [SerializeField] private float _maxSwipeTime = 0.5f;

        private Vector2 _startPos;
        private float _startTime;

        private void Update()
        {
            if (UnityEngine.Input.touchCount == 0) return;

            var touch = UnityEngine.Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _startPos = touch.position;
                    _startTime = Time.time;
                    break;

                case TouchPhase.Ended:
                    float swipeTime = Time.time - _startTime;
                    if (swipeTime > _maxSwipeTime) return;

                    Vector2 swipe = touch.position - _startPos;
                    if (swipe.magnitude < _minSwipeDistance) return;

                    if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
                    {
                        if (swipe.x > 0) OnSwipeRight();
                        else OnSwipeLeft();
                    }
                    else
                    {
                        if (swipe.y > 0) OnSwipeUp();
                        else OnSwipeDown();
                    }
                    break;
            }
        }

        private void OnSwipeRight() => GameEngine.Instance?.MakeChoice(0);
        private void OnSwipeLeft() => GameEngine.Instance?.MakeChoice(1);
        private void OnSwipeUp() => GameEngine.Instance?.MakeChoice(2);
        private void OnSwipeDown() => GameEngine.Instance?.MakeChoice(3);
    }
}
