using System.Collections.Generic;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Renders the subtle background dot grid and optional grid boundaries.
    /// </summary>
    public class GridRenderer : MonoBehaviour
    {
        [SerializeField] private Color dotColor = new Color(0.82f, 0.85f, 0.90f, 0.85f);
        [SerializeField] private float dotScale = 0.09f;

        private static Sprite cachedDotSprite;
        private readonly List<GameObject> activeDotObjects = new List<GameObject>();
        private GameObject container;

        public void RenderGrid(BoardLayout layout)
        {
            Clear();

            if (container == null)
            {
                container = new GameObject("DotGridContainer");
                container.transform.SetParent(transform, false);
            }

            Sprite dotSprite = GetOrCreateDotSprite();

            for (int col = 0; col < layout.Cols; col++)
            {
                for (int row = 0; row < layout.Rows; row++)
                {
                    Vector2 worldPos = layout.GridToWorld(new GridCoord(col, row));

                    GameObject dot = new GameObject($"Dot_{col}_{row}");
                    dot.transform.SetParent(container.transform, false);
                    dot.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
                    dot.transform.localScale = Vector3.one * dotScale;

                    SpriteRenderer sr = dot.AddComponent<SpriteRenderer>();
                    sr.sprite = dotSprite;
                    sr.color = dotColor;
                    sr.sortingOrder = 0;

                    activeDotObjects.Add(dot);
                }
            }
        }

        public void Clear()
        {
            foreach (var dot in activeDotObjects)
            {
                if (dot != null) Destroy(dot);
            }
            activeDotObjects.Clear();
        }

        private static Sprite GetOrCreateDotSprite()
        {
            if (cachedDotSprite != null) return cachedDotSprite;

            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.44f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            cachedDotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return cachedDotSprite;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}

