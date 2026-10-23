using System.Collections.Generic;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Renders a single snake body via an anti-aliased, tapered LineRenderer and creates
    /// a distinctive organic viper head with tongue.
    /// Perfectly matches the sleek, delicate proportions of the production design.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class SnakeRenderer : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private float lineWidth = 0.13f;
        [SerializeField] private Color defaultBodyColor = new Color(0.04f, 0.06f, 0.12f); // Deep Crisp Navy-Black
        [SerializeField] private Color blockedColor = new Color(0.96f, 0.14f, 0.20f);      // Collision Red
        [SerializeField] private Color exitColor = new Color(0.12f, 0.42f, 0.98f);         // Royal Blue

        private LineRenderer lr;
        private GameObject headHolder;
        private SpriteRenderer headRenderer;

        private static Material sharedLineMaterial;
        private static Sprite cachedViperHeadSprite;

        private void Awake()
        {
            SetupLineRenderer();
        }

        public void SetupLineRenderer()
        {
            if (lr != null) return;
            lr = GetComponent<LineRenderer>();

            if (sharedLineMaterial == null)
            {
                sharedLineMaterial = new Material(Shader.Find("Sprites/Default"));
                sharedLineMaterial.mainTexture = GenerateAntiAliasedLineTexture();
            }

            lr.material = sharedLineMaterial;
            lr.textureMode = LineTextureMode.Stretch;
            lr.useWorldSpace = true;
            lr.numCapVertices = 12;
            lr.numCornerVertices = 12;
            lr.sortingOrder = 5;

            AnimationCurve taper = new AnimationCurve();
            taper.AddKey(0.00f, 0.76f);
            taper.AddKey(0.07f, 1.00f);
            taper.AddKey(0.75f, 0.96f);
            taper.AddKey(0.90f, 0.58f);
            taper.AddKey(0.97f, 0.25f);
            taper.AddKey(1.00f, 0.06f);

            lr.widthCurve = taper;
            lr.widthMultiplier = lineWidth;
            SetColor(defaultBodyColor);
        }

        public void InitializeVisuals(List<Vector2> worldPoints, GridCoord exitDir, Color color, float customWidth = 0.13f)
        {
            SetupLineRenderer();
            lineWidth = customWidth;
            lr.widthMultiplier = lineWidth;
            defaultBodyColor = color;
            SetColor(color);

            UpdateBodyPoints(worldPoints);
            UpdateHead(worldPoints, exitDir);
        }

        public void UpdateBodyPoints(List<Vector2> worldPoints)
        {
            if (worldPoints == null || worldPoints.Count < 2)
            {
                if (lr != null) lr.positionCount = 0;
                return;
            }

            List<Vector2> rounded = SmoothCorners(worldPoints);

            // Subdivide segments for silky smooth curvature
            List<Vector3> finalPts = new List<Vector3>();
            for (int i = 0; i < rounded.Count - 1; i++)
            {
                Vector2 a = rounded[i];
                Vector2 b = rounded[i + 1];
                float segLen = Vector2.Distance(a, b);

                int steps = Mathf.Max(2, Mathf.CeilToInt(segLen / 0.05f));
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps;
                    Vector2 pos = Vector2.Lerp(a, b, t);
                    finalPts.Add(new Vector3(pos.x, pos.y, 0f));
                }
            }

            Vector2 tail = rounded[rounded.Count - 1];
            finalPts.Add(new Vector3(tail.x, tail.y, 0f));

            lr.positionCount = finalPts.Count;
            lr.SetPositions(finalPts.ToArray());
        }

        public void UpdateHead(List<Vector2> worldPoints, GridCoord exitDir)
        {
            if (worldPoints == null || worldPoints.Count < 2)
            {
                if (headHolder != null) headHolder.SetActive(false);
                return;
            }

            if (headHolder == null)
            {
                headHolder = new GameObject("ViperHead");
                headHolder.transform.SetParent(transform, false);

                GameObject hSpriteObj = new GameObject("HeadWithTongueSprite");
                hSpriteObj.transform.SetParent(headHolder.transform, false);

                headRenderer = hSpriteObj.AddComponent<SpriteRenderer>();
                headRenderer.material = new Material(Shader.Find("Sprites/Default"));
                headRenderer.sprite = GetOrCreateViperHeadSprite();
                headRenderer.sortingOrder = lr.sortingOrder + 2;
            }

            headHolder.SetActive(true);

            Vector2 head = worldPoints[0];
            Vector2 neck = worldPoints[1];
            Vector2 forwardDir = exitDir.IsCardinalUnit
                ? new Vector2(exitDir.Col, exitDir.Row)
                : (head - neck).normalized;

            float angle = Mathf.Atan2(forwardDir.y, forwardDir.x) * Mathf.Rad2Deg;

            headHolder.transform.position = new Vector3(head.x, head.y, 0f);
            headHolder.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            // Precise production scale ratio (1.45x line width, perfectly matches sleek body)
            float scale = lineWidth * 1.45f;
            headHolder.transform.localScale = Vector3.one * scale;

            if (headRenderer != null)
            {
                headRenderer.color = lr.startColor;
            }
        }

        public void SetColor(Color c)
        {
            if (lr != null)
            {
                lr.startColor = c;
                lr.endColor = c;
            }
            if (headRenderer != null)
            {
                headRenderer.color = c;
            }
        }

        public void SetExitColor() => SetColor(exitColor);
        public void SetBlockedColor() => SetColor(blockedColor);
        public void ResetColor() => SetColor(defaultBodyColor);

        private List<Vector2> SmoothCorners(List<Vector2> pts)
        {
            if (pts.Count < 3) return new List<Vector2>(pts);

            List<Vector2> rounded = new List<Vector2> { pts[0] };
            for (int i = 1; i < pts.Count - 1; i++)
            {
                Vector2 prev = pts[i - 1];
                Vector2 curr = pts[i];
                Vector2 next = pts[i + 1];

                Vector2 d1 = curr - prev;
                Vector2 d2 = next - curr;
                float l1 = d1.magnitude;
                float l2 = d2.magnitude;

                if (l1 > 0.03f && l2 > 0.03f && Vector2.Angle(d1, d2) > 10f)
                {
                    float cut = Mathf.Min(l1 * 0.22f, l2 * 0.22f, 0.11f);
                    Vector2 pA = curr - d1.normalized * cut;
                    Vector2 pB = curr + d2.normalized * cut;

                    rounded.Add(pA);
                    for (int s = 1; s <= 3; s++)
                    {
                        float t = s / 4f;
                        Vector2 arc = (1f - t) * (1f - t) * pA + 2f * (1f - t) * t * curr + t * t * pB;
                        rounded.Add(arc);
                    }
                    rounded.Add(pB);
                }
                else
                {
                    rounded.Add(curr);
                }
            }
            rounded.Add(pts[pts.Count - 1]);
            return rounded;
        }

        private static Texture2D GenerateAntiAliasedLineTexture()
        {
            int w = 8, h = 64;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                float ny = Mathf.Abs((y / (float)(h - 1)) * 2f - 1f);
                float alpha = Mathf.Clamp01((0.92f - ny) / 0.16f);
                Color c = new Color(1f, 1f, 1f, alpha);
                for (int x = 0; x < w; x++) t.SetPixel(x, y, c);
            }
            t.Apply();
            return t;
        }

        private static Sprite GetOrCreateViperHeadSprite()
        {
            if (cachedViperHeadSprite != null) return cachedViperHeadSprite;

            int w = 160, h = 240;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x / (float)(w - 1)) * 2f - 1f;
                    float ny = y / (float)(h - 1);
                    float alpha = 0f;

                    if (ny <= 0.64f)
                    {
                        float hy = ny / 0.64f;
                        float maxW;
                        if (hy < 0.14f)
                            maxW = Mathf.Lerp(0.32f, 0.36f, hy / 0.14f);
                        else if (hy < 0.42f)
                            maxW = Mathf.Lerp(0.36f, 0.72f, Mathf.Sin(((hy - 0.14f) / 0.28f) * Mathf.PI * 0.5f));
                        else
                            maxW = 0.72f * Mathf.Cos(((hy - 0.42f) / 0.58f) * Mathf.PI * 0.5f * 0.88f);

                        alpha = Mathf.Clamp01((maxW - Mathf.Abs(nx)) / 0.035f);
                    }

                    if (ny >= 0.60f && ny <= 0.96f)
                    {
                        float ty = (ny - 0.60f) / 0.36f;
                        float tAlpha = 0f;
                        if (ty < 0.44f)
                            tAlpha = Mathf.Clamp01((0.055f - Mathf.Abs(nx)) / 0.028f);
                        else
                        {
                            float fk = (ty - 0.44f) / 0.56f;
                            float center = fk * 0.28f;
                            float halfW = Mathf.Lerp(0.048f, 0.015f, fk);
                            tAlpha = Mathf.Clamp01((halfW - Mathf.Abs(Mathf.Abs(nx) - center)) / 0.028f);
                        }
                        alpha = Mathf.Max(alpha, tAlpha);
                    }

                    t.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            t.Apply();
            cachedViperHeadSprite = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.10f), 120f);
            return cachedViperHeadSprite;
        }
    }
}
