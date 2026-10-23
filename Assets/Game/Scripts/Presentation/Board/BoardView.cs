using System.Collections.Generic;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Presentation manager for the game board: creates and manages SnakeViews and the GridRenderer.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private GridRenderer gridRenderer;

        public BoardLayout Layout { get; private set; }
        private readonly Dictionary<string, SnakeView> activeSnakeViews = new Dictionary<string, SnakeView>();
        private GameObject snakesContainer;

        private void Awake()
        {
            if (gridRenderer == null)
            {
                gridRenderer = gameObject.AddComponent<GridRenderer>();
            }
        }

        public void SetupBoard(LevelDefinition level, float cellSpacing = 0.55f)
        {
            Clear();

            Layout = new BoardLayout(level.Cols, level.Rows, cellSpacing);
            gridRenderer.RenderGrid(Layout);

            if (snakesContainer == null)
            {
                snakesContainer = new GameObject("SnakesContainer");
                snakesContainer.transform.SetParent(transform, false);
            }

            foreach (var snakeDef in level.Snakes)
            {
                SpawnSnake(snakeDef);
            }
        }

        private void SpawnSnake(SnakeDefinition def)
        {
            GameObject snakeObj = new GameObject($"Snake_{def.Id}");
            snakeObj.transform.SetParent(snakesContainer.transform, false);

            SnakeView view = snakeObj.AddComponent<SnakeView>();
            view.Initialize(def, Layout);

            activeSnakeViews[def.Id] = view;
        }

        public SnakeView GetSnakeView(string id)
        {
            activeSnakeViews.TryGetValue(id, out var view);
            return view;
        }

        public IReadOnlyCollection<SnakeView> ActiveSnakes => activeSnakeViews.Values;

        public void RemoveSnakeView(string id)
        {
            activeSnakeViews.Remove(id);
        }

        public void Clear()
        {
            foreach (var kv in activeSnakeViews)
            {
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            }
            activeSnakeViews.Clear();

            if (gridRenderer != null) gridRenderer.Clear();
        }
    }
}
