using System;
using UnityEngine;

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Processes touch and mouse pointer input to detect taps on snakes.
    /// </summary>
    public class SnakeInputHandler : MonoBehaviour
    {
        public event Action<string> OnSnakeSelected;

        [SerializeField] private float tapRadiusThreshold = 0.42f;
        private Camera mainCamera;
        private bool isInputEnabled = true;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        public void SetInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;
        }

        public void ProcessPointerInput(BoardView boardView)
        {
            if (!isInputEnabled || boardView == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                Vector2 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                HandleTap(worldPos, boardView);
            }
        }

        private void HandleTap(Vector2 worldPos, BoardView boardView)
        {
            SnakeView bestCandidate = null;
            float bestDistance = tapRadiusThreshold;

            foreach (var snakeView in boardView.ActiveSnakes)
            {
                if (snakeView == null || snakeView.IsAnimating) continue;

                float dist = snakeView.GetDistanceToPoint(worldPos);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestCandidate = snakeView;
                }
            }

            if (bestCandidate != null)
            {
                OnSnakeSelected?.Invoke(bestCandidate.SnakeId);
            }
        }
    }
}
