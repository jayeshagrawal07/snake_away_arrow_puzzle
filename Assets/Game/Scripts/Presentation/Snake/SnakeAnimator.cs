using System;
using System.Collections;
using System.Collections.Generic;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Handles deterministic presentation animations for snake escape motion and blocked feedback.
    /// Does not mutate logical gameplay state.
    /// </summary>
    public class SnakeAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float slitherSpeed = 16f;
        [SerializeField] private float blockedBumpDistance = 0.09f;
        [SerializeField] private float blockedBumpDuration = 0.18f;

        private SnakeRenderer snakeRenderer;
        private Coroutine activeAnimation;
        private static Sprite cachedImpactSprite;

        private void Awake()
        {
            snakeRenderer = GetComponent<SnakeRenderer>();
        }

        public bool IsAnimating => activeAnimation != null;

        /// <summary>
        /// Animates the snake slithering forward along its escape trajectory off the board.
        /// </summary>
        public void AnimateEscape(List<Vector2> bodyPoints, List<Vector2> escapeTrajectory, GridCoord exitDir, Action onComplete)
        {
            if (activeAnimation != null) StopCoroutine(activeAnimation);
            activeAnimation = StartCoroutine(EscapeRoutine(bodyPoints, escapeTrajectory, exitDir, onComplete));
        }

        private IEnumerator EscapeRoutine(List<Vector2> originalBody, List<Vector2> escapeTrajectory, GridCoord exitDir, Action onComplete)
        {
            snakeRenderer.SetExitColor();

            List<Vector2> currentPts = new List<Vector2>(originalBody);
            Vector2 headForward = new Vector2(exitDir.Col, exitDir.Row).normalized;
            if (headForward.sqrMagnitude < 0.01f) headForward = Vector2.up;

            // Step along escape trajectory
            int targetIdx = 0;
            while (targetIdx < escapeTrajectory.Count)
            {
                Vector2 target = escapeTrajectory[targetIdx];
                while (Vector2.Distance(currentPts[0], target) > 0.04f)
                {
                    float step = slitherSpeed * Time.deltaTime;
                    AdvanceSnake(currentPts, target, step);
                    snakeRenderer.UpdateBodyPoints(currentPts);
                    snakeRenderer.UpdateHead(currentPts, exitDir);
                    yield return null;
                }
                targetIdx++;
            }

            // Slither off-board until head and tail are beyond view
            float distanceLeft = 6.0f;
            while (distanceLeft > 0f)
            {
                float step = slitherSpeed * Time.deltaTime;
                distanceLeft -= step;
                AdvanceSnake(currentPts, currentPts[0] + headForward * 10f, step);
                snakeRenderer.UpdateBodyPoints(currentPts);
                snakeRenderer.UpdateHead(currentPts, exitDir);
                yield return null;
            }

            activeAnimation = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Advances points along target direction and pulls tail along body waypoints.
        /// </summary>
        private void AdvanceSnake(List<Vector2> pts, Vector2 forwardTarget, float step)
        {
            if (pts.Count < 2) return;

            Vector2 dir = (forwardTarget - pts[0]).normalized;
            pts[0] += dir * step;

            float rem = step;
            while (rem > 0f && pts.Count > 1)
            {
                int tailIdx = pts.Count - 1;
                Vector2 tail = pts[tailIdx];
                Vector2 prev = pts[tailIdx - 1];
                float segLen = Vector2.Distance(tail, prev);

                if (pts.Count > 2)
                {
                    if (segLen <= rem)
                    {
                        rem -= segLen;
                        pts.RemoveAt(tailIdx);
                    }
                    else
                    {
                        pts[tailIdx] = Vector2.MoveTowards(tail, prev, rem);
                        rem = 0f;
                    }
                }
                else
                {
                    pts[tailIdx] += dir * rem;
                    rem = 0f;
                }
            }
        }

        /// <summary>
        /// Animates blocked feedback: bump forward, flash red, spawn impact circle, return.
        /// </summary>
        public void AnimateBlocked(List<Vector2> bodyPoints, GridCoord exitDir, Vector2 impactWorldPos, Action onComplete)
        {
            if (activeAnimation != null) StopCoroutine(activeAnimation);
            activeAnimation = StartCoroutine(BlockedRoutine(bodyPoints, exitDir, impactWorldPos, onComplete));
        }

        private IEnumerator BlockedRoutine(List<Vector2> bodyPoints, GridCoord exitDir, Vector2 impactWorldPos, Action onComplete)
        {
            snakeRenderer.SetBlockedColor();
            GameObject impactObj = SpawnImpactCircle(impactWorldPos);

            Vector2 bumpDir = new Vector2(exitDir.Col, exitDir.Row).normalized * blockedBumpDistance;

            float elapsed = 0f;
            float halfDuration = blockedBumpDuration * 0.5f;

            // Bump forward
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                var offsetPts = OffsetPoints(bodyPoints, bumpDir * Mathf.Sin(t * Mathf.PI * 0.5f));
                snakeRenderer.UpdateBodyPoints(offsetPts);
                snakeRenderer.UpdateHead(offsetPts, exitDir);
                yield return null;
            }

            // Return to origin
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                var offsetPts = OffsetPoints(bodyPoints, bumpDir * (1f - Mathf.Sin(t * Mathf.PI * 0.5f)));
                snakeRenderer.UpdateBodyPoints(offsetPts);
                snakeRenderer.UpdateHead(offsetPts, exitDir);
                yield return null;
            }

            snakeRenderer.UpdateBodyPoints(bodyPoints);
            snakeRenderer.UpdateHead(bodyPoints, exitDir);
            snakeRenderer.ResetColor();

            if (impactObj != null) Destroy(impactObj);

            activeAnimation = null;
            onComplete?.Invoke();
        }

        private List<Vector2> OffsetPoints(List<Vector2> pts, Vector2 offset)
        {
            var res = new List<Vector2>(pts.Count);
            for (int i = 0; i < pts.Count; i++) res.Add(pts[i] + offset);
            return res;
        }

        private GameObject SpawnImpactCircle(Vector2 pos)
        {
            if (cachedImpactSprite == null)
            {
                int s = 64;
                Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
                t.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(s * 0.5f, s * 0.5f));
                        float a = Mathf.Clamp01((s * 0.45f - d) / 2.0f);
                        t.SetPixel(x, y, new Color(0.96f, 0.20f, 0.24f, a * 0.75f));
                    }
                }
                t.Apply();
                cachedImpactSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            }

            GameObject go = new GameObject("ImpactCircle");
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 0.35f;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = cachedImpactSprite;
            sr.sortingOrder = 7;
            return go;
        }

        public void Stop()
        {
            if (activeAnimation != null)
            {
                StopCoroutine(activeAnimation);
                activeAnimation = null;
            }
        }
    }
}
