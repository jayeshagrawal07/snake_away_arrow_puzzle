using System;
using System.Collections.Generic;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Visual representative of a single snake on the board.
    /// Manages its own renderer, animator, and tap target geometry.
    /// </summary>
    [RequireComponent(typeof(SnakeRenderer), typeof(SnakeAnimator))]
    public class SnakeView : MonoBehaviour
    {
        public SnakeDefinition Definition { get; private set; }
        public BoardLayout Layout { get; private set; }

        private SnakeRenderer snakeRenderer;
        private SnakeAnimator snakeAnimator;
        private List<Vector2> worldPoints = new List<Vector2>();

        public string SnakeId => Definition?.Id;
        public bool IsAnimating => snakeAnimator != null && snakeAnimator.IsAnimating;

        private void Awake()
        {
            snakeRenderer = GetComponent<SnakeRenderer>();
            snakeAnimator = GetComponent<SnakeAnimator>();
        }

        public void Initialize(SnakeDefinition definition, BoardLayout layout, float lineWidth = 0.13f)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));

            worldPoints.Clear();
            foreach (var cell in definition.BodyCells)
            {
                worldPoints.Add(layout.GridToWorld(cell));
            }

            snakeRenderer.InitializeVisuals(worldPoints, definition.ExitDirection, definition.BodyColor, lineWidth);
        }

        /// <summary>
        /// Calculates the minimum distance from a world-space point (e.g. touch/click) to any of this snake's body segments.
        /// </summary>
        public float GetDistanceToPoint(Vector2 testPoint)
        {
            if (worldPoints == null || worldPoints.Count == 0) return float.MaxValue;

            float minDist = float.MaxValue;
            for (int i = 0; i < worldPoints.Count - 1; i++)
            {
                float d = DistancePointToSegment(testPoint, worldPoints[i], worldPoints[i + 1]);
                if (d < minDist) minDist = d;
            }

            // Head proximity
            float dHead = Vector2.Distance(testPoint, worldPoints[0]);
            if (dHead < minDist) minDist = dHead;

            return minDist;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            return Vector2.Distance(p, a + t * ab);
        }

        /// <summary>
        /// Plays the forward slither escape animation. Destroys the visual game object upon completion.
        /// </summary>
        public void AnimateEscape(List<GridCoord> escapeRoute, Action onComplete)
        {
            List<Vector2> trajectory = new List<Vector2>();
            if (escapeRoute != null)
            {
                foreach (var cell in escapeRoute)
                {
                    trajectory.Add(Layout.GridToWorld(cell));
                }
            }

            snakeAnimator.AnimateEscape(worldPoints, trajectory, Definition.ExitDirection, () =>
            {
                onComplete?.Invoke();
                Destroy(gameObject);
            });
        }

        /// <summary>
        /// Plays the blocked bump animation.
        /// </summary>
        public void AnimateBlocked(GridCoord blockedAtCell, Action onComplete)
        {
            Vector2 impactPos = Layout.GridToWorld(blockedAtCell);
            snakeAnimator.AnimateBlocked(worldPoints, Definition.ExitDirection, impactPos, onComplete);
        }
    }
}

