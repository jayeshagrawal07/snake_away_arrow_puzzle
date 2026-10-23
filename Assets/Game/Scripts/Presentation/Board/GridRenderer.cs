using System.Collections.Generic;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Renders subtle background dot grid under snakes, matching the clean focused aesthetic.
    /// </summary>
    public class GridRenderer : MonoBehaviour
    {
        [SerializeField] private Color dotColor = new Color(0.80f, 0.84f, 0.90f, 0.70f);
        [SerializeField] private float dotScale = 0.08f;

        private static Sprite cachedDotSprite;
        private readonly List<GameObject> activeDotObjects = new List<GameObject>();
        private GameObject container;

        public void RenderGrid(BoardLayout layout)
        {
            Clear();
            EnsureContainer();
            Sprite dotSprite = GetOrCreateDotSprite();

            for (int col = 0; col < layout.Cols; col++)
            {
                for (int row = 0; row < layout.Rows; row++)
                {
                    SpawnDot(layout.GridToWorld(new GridCoord(col, row)), dotSprite);
                }
            }
        }

        public void RenderGridUnderSnakes(BoardLayout layout, IEnumerable<GridCoord> snakeCells)
        {
            Clear();
            EnsureContainer();
            Sprite dotSprite = GetOrCreateDotSprite();

            HashSet<GridCoord> cellsWithDots = new HashSet<GridCoord>();
            foreach (var cell in snakeCells)
            {
                cellsWithDots.Add(cell);
                // Also optionally add adjacent cells for subtle buffer
                for (int d = 0; d < 4; d++)
                {
                    var adj = cell + GridCoord.CardinalDirections[d];
                    if (layout.Cols > adj.Col && adj.Col >= 0 && layout.Rows > adj.Row && adj.Row >= 0)
                    {
                        cellsWithDots.Add(adj);
                    }
                }
            }

            foreach (var cell in cellsWithDots)
            {
                SpawnDot(layout.GridToWorld(cell), dotSprite);
            }
        }

        private void SpawnDot(Vector2 worldPos, Sprite dotSprite)
        {
            GameObject dot = new GameObject("Dot");
            dot.transform.SetParent(container.transform, false);
            dot.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            dot.transform.localScale = Vector3.one * dotScale;

            SpriteRenderer sr = dot.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.color = dotColor;
            sr.sortingOrder = 0;

            activeDotObjects.Add(dot);
        }

        private void EnsureContainer()
        {
            if (container == null)
            {
                container = new GameObject("DotGridContainer");
                container.transform.SetParent(transform, false);
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
