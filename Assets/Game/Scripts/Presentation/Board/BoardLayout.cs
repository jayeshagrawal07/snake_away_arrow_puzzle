using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Calculates world-space positions for grid coordinates and manages board coordinate transforms.
    /// Centers the board at world origin (0, yOffset) by default.
    /// </summary>
    public class BoardLayout
    {
        public int Cols { get; private set; } = 8;
        public int Rows { get; private set; } = 8;
        public float CellSpacing { get; set; } = 0.55f;
        public Vector2 WorldCenterOffset { get; set; } = new Vector2(0f, -0.2f);

        public BoardLayout(int cols, int rows, float cellSpacing = 0.55f)
        {
            UpdateDimensions(cols, rows, cellSpacing);
        }

        public void UpdateDimensions(int cols, int rows, float cellSpacing = 0.55f)
        {
            Cols = Mathf.Max(2, cols);
            Rows = Mathf.Max(2, rows);
            CellSpacing = cellSpacing;
        }

        /// <summary>
        /// Converts a discrete GridCoord into world-space Vector2 position.
        /// </summary>
        public Vector2 GridToWorld(GridCoord coord)
        {
            float halfWidth = (Cols - 1) * 0.5f * CellSpacing;
            float halfHeight = (Rows - 1) * 0.5f * CellSpacing;

            float x = (coord.Col * CellSpacing) - halfWidth + WorldCenterOffset.x;
            float y = (coord.Row * CellSpacing) - halfHeight + WorldCenterOffset.y;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Converts a world-space point to the nearest discrete GridCoord.
        /// </summary>
        public GridCoord WorldToGrid(Vector2 worldPos)
        {
            float halfWidth = (Cols - 1) * 0.5f * CellSpacing;
            float halfHeight = (Rows - 1) * 0.5f * CellSpacing;

            float relativeX = worldPos.x - WorldCenterOffset.x + halfWidth;
            float relativeY = worldPos.y - WorldCenterOffset.y + halfHeight;

            int col = Mathf.RoundToInt(relativeX / CellSpacing);
            int row = Mathf.RoundToInt(relativeY / CellSpacing);
            return new GridCoord(col, row);
        }

        /// <summary>
        /// Total pixel/world width of the board.
        /// </summary>
        public float BoardWidth => (Cols - 1) * CellSpacing;

        /// <summary>
        /// Total pixel/world height of the board.
        /// </summary>
        public float BoardHeight => (Rows - 1) * CellSpacing;
    }
}

