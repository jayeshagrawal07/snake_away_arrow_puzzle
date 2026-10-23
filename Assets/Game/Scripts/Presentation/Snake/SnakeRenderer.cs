using System.Collections.Generic;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Renders a single snake body via an anti-aliased, tapered LineRenderer and creates
    /// a distinctive organic viper head with tongue.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class SnakeRenderer : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private float lineWidth = 0.14f;
        [SerializeField] private Color defaultBodyColor = new Color(0.04f, 0.06f, 0.12f);
        [SerializeField] private Color blockedColor = new Color(0.96f, 0.14f, 0.20f);
        [SerializeField] private Color exitColor = new Color(0.12f, 0.42f, 0.98f);

        private LineRenderer lr;
        private GameObject headObj;
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

        public void InitializeVisuals(List<Vector2> worldPoints, GridCoord exitDir, Color color, float customWidth = 0.14f)
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

            List<Vector2> smoothed = SmoothCorners(worldPoints);
            lr.positionCount = smoothed.Count;
            for (int i = 0; i < smoothed.Count; i++)
            {
                lr.SetPosition(i, new Vector3(smoothed[i].x, smoothed[i].y, 0f));
            }
        }

        public void UpdateHead(List<Vector2> worldPoints, GridCoord exitDir)
        {
            if (worldPoints == null || worldPoints.Count == 0)
            {
                if (headObj != null) headObj.SetActive(false);
                return;
            }

            if (headObj == null)
            {
                headObj = new GameObject("ViperHead");
                headObj.transform.SetParent(transform, false);
                headRenderer = headObj.AddComponent<SpriteRenderer>();
                headRenderer.sprite = GetOrCreateViperHeadSprite();
                headRenderer.sortingOrder = 6;
            }

            headObj.SetActive(true);
            headObj.transform.position = new Vector3(worldPoints[0].x, worldPoints[0].y, 0f);

            // Orientation
            Vector2 forwardDir = exitDir.IsCardinalUnit 
                ? new Vector2(exitDir.Col, exitDir.Row)
                : (worldPoints.Count > 1 ? (worldPoints[0] - worldPoints[1]).normalized : Vector2.up);

            float angle = Mathf.Atan2(forwardDir.y, forwardDir.x) * Mathf.Rad2Deg - 90f;
            headObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            float headScale = lineWidth * 5.2f;
            headObj.transform.localScale = new Vector3(headScale, headScale, 1f);
            headRenderer.color = lr.startColor;
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

            List<Vector2> result = new List<Vector2> { pts[0] };
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
                    float cut = Mathf.Min(l1 * 0.22f, l2 * 0.22f, 0.12f);
                    Vector2 pA = curr - d1.normalized * cut;
                    Vector2 pB = curr + d2.normalized * cut;
                    result.Add(pA);
                    result.Add(Vector2.Lerp(pA, pB, 0.5f));
                    result.Add(pB);
                }
                else
                {
                    result.Add(curr);
                }
            }
            result.Add(pts[pts.Count - 1]);
            return result;
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
