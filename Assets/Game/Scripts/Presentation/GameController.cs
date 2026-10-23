using System.Collections.Generic;
using SnakeEscape.Application;
using SnakeEscape.Domain;
using UnityEngine;

#pragma warning disable 0649

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Master presentation controller that wires the GameSession to the BoardView,
    /// SnakeInputHandler, and UI views.
    /// </summary>
    [RequireComponent(typeof(BoardView), typeof(SnakeInputHandler))]
    public class GameController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private SnakeInputHandler inputHandler;
        [SerializeField] private GameplayHUD hud;
        [SerializeField] private LevelCompleteUI completeUI;

        [Header("Debug / Testing Level")]
        [SerializeField] private LevelDefinition initialLevelAsset;

        public GameSession Session { get; private set; }

        private void Awake()
        {
            if (boardView == null) boardView = GetComponent<BoardView>();
            if (inputHandler == null) inputHandler = GetComponent<SnakeInputHandler>();

            Session = new GameSession();
        }

        private void Start()
        {
            inputHandler.OnSnakeSelected += HandleSnakeSelected;

            if (hud != null)
            {
                hud.Bind(HandleUndoRequested, HandleRestartRequested);
            }

            if (completeUI != null)
            {
                completeUI.Bind(HandleNextLevelRequested, HandleRestartRequested);
            }

            Session.OnLevelComplete += HandleLevelComplete;

            // Load initial level (or create a test level if none assigned)
            LevelDefinition levelToLoad = initialLevelAsset != null ? initialLevelAsset : CreateSampleLevel();
            LoadLevel(levelToLoad);
        }

        private void Update()
        {
            if (Session != null && Session.CurrentState == GameSession.SessionState.Ready)
            {
                inputHandler.ProcessPointerInput(boardView);
            }
        }

        public void LoadLevel(LevelDefinition level)
        {
            Session.LoadLevel(level);
            boardView.SetupBoard(level);

            UpdateUI();
            if (completeUI != null) completeUI.Hide();
        }

        private void HandleSnakeSelected(string snakeId)
        {
            if (Session.CurrentState != GameSession.SessionState.Ready) return;

            // Record undo snapshot before move
            Session.UndoService.RecordSnapshot(Session.PuzzleState);

            var result = Session.RequestMove(snakeId);

            SnakeView view = boardView.GetSnakeView(snakeId);
            if (view == null) return;

            if (result.IsValid)
            {
                view.AnimateEscape(result.EscapeRoute, () =>
                {
                    boardView.RemoveSnakeView(snakeId);
                    Session.CommitMove(snakeId);
                    UpdateUI();
                });
            }
            else if (result.Status == MoveStatus.BlockedBySnake)
            {
                view.AnimateBlocked(result.BlockedAtCell, null);
            }
        }

        private void HandleUndoRequested()
        {
            if (Session.RequestUndo())
            {
                // Rebuild presentation board from restored state
                boardView.SetupBoard(Session.CurrentLevel);
                // Remove snakes that had already escaped in restored state
                foreach (var escapedId in Session.PuzzleState.EscapedSnakeIds)
                {
                    var view = boardView.GetSnakeView(escapedId);
                    if (view != null)
                    {
                        boardView.RemoveSnakeView(escapedId);
                        Destroy(view.gameObject);
                    }
                }
                UpdateUI();
            }
        }

        private void HandleRestartRequested()
        {
            if (Session.CurrentLevel == null) return;
            Session.RestartLevel();
            boardView.SetupBoard(Session.CurrentLevel);
            if (completeUI != null) completeUI.Hide();
            UpdateUI();
        }

        private void HandleLevelComplete()
        {
            if (completeUI != null)
            {
                completeUI.Show(Session.CurrentLevel.LevelId);
            }
        }

        private void HandleNextLevelRequested()
        {
            // Milestone 3: reload current level or restart
            HandleRestartRequested();
        }

        private void UpdateUI()
        {
            if (hud != null && Session.CurrentLevel != null)
            {
                int remaining = Session.CurrentLevel.Snakes.Count - Session.PuzzleState.EscapedSnakeIds.Count;
                hud.UpdateLevelDisplay(Session.CurrentLevel.LevelId, remaining);
                hud.SetUndoInteractable(Session.UndoService.CanUndo);
            }
        }

        /// <summary>
        /// Creates a sample 2-snake level satisfying Milestone 3:
        /// Snake A (Green) is blocked by Snake B (Blue).
        /// Escaping B first clears the route for A.
        /// </summary>
        public static LevelDefinition CreateSampleLevel()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "M3_Sample";
            level.Cols = 8;
            level.Rows = 8;

            // Snake A (Green): body at (2,3), (2,2), (2,1), head at (2,3) facing Right (exit (1,0)).
            // Path crosses col 4 at row 3.
            var snakeA = new SnakeDefinition
            {
                Id = "snake_A",
                BodyColor = new Color(0.18f, 0.72f, 0.44f), // Green
                ExitDirection = GridCoord.Right,
                BodyCells = new List<GridCoord>
                {
                    new GridCoord(2, 3),
                    new GridCoord(2, 2),
                    new GridCoord(2, 1)
                }
            };

            // Snake B (Blue): body at (4,3), (4,2), (4,1), head at (4,3) facing Up (exit (0,1)).
            // Blocks Snake A's escape route at (4,3).
            // When Snake B escapes Up, Snake A is free to escape Right!
            var snakeB = new SnakeDefinition
            {
                Id = "snake_B",
                BodyColor = new Color(0.20f, 0.50f, 0.95f), // Blue
                ExitDirection = GridCoord.Up,
                BodyCells = new List<GridCoord>
                {
                    new GridCoord(4, 3),
                    new GridCoord(4, 2),
                    new GridCoord(4, 1)
                }
            };

            level.Snakes = new List<SnakeDefinition> { snakeA, snakeB };
            return level;
        }

        private void OnDestroy()
        {
            if (inputHandler != null)
                inputHandler.OnSnakeSelected -= HandleSnakeSelected;
        }
    }
}
