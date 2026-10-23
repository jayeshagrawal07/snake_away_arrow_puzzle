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
    /// Manages multi-level progression, undo restoration, and input guards.
    /// </summary>
    [RequireComponent(typeof(BoardView), typeof(SnakeInputHandler))]
    public class GameController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private SnakeInputHandler inputHandler;
        [SerializeField] private GameplayHUD hud;
        [SerializeField] private LevelCompleteUI completeUI;

        [Header("Campaign Levels")]
        [SerializeField] private List<LevelDefinition> campaignLevels = new List<LevelDefinition>();
        private int currentLevelIndex = 0;

        public GameSession Session { get; private set; }

        private void Awake()
        {
            if (boardView == null) boardView = GetComponent<BoardView>();
            if (inputHandler == null) inputHandler = GetComponent<SnakeInputHandler>();
            if (hud == null) hud = gameObject.AddComponent<GameplayHUD>();
            if (completeUI == null) completeUI = gameObject.AddComponent<LevelCompleteUI>();

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

            // Initialize campaign levels if none assigned
            if (campaignLevels == null || campaignLevels.Count == 0)
            {
                campaignLevels = CreateSampleCampaign();
            }

            currentLevelIndex = 0;
            LoadCurrentLevel();
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

        private void LoadCurrentLevel()
        {
            if (campaignLevels == null || campaignLevels.Count == 0) return;
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, campaignLevels.Count - 1);
            LoadLevel(campaignLevels[currentLevelIndex]);
        }

        private void HandleSnakeSelected(string snakeId)
        {
            // Input guard: only accept moves when session is ready and idle
            if (Session.CurrentState != GameSession.SessionState.Ready) return;

            // Record undo snapshot before move begins
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
            // Input guard: cannot undo while animation in progress
            if (Session.CurrentState != GameSession.SessionState.Ready) return;

            if (Session.RequestUndo())
            {
                // Rebuild presentation board from restored snapshot
                boardView.SetupBoard(Session.CurrentLevel);

                // Remove snakes that had already escaped in restored snapshot
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
            if (campaignLevels == null || campaignLevels.Count == 0) return;

            currentLevelIndex = (currentLevelIndex + 1) % campaignLevels.Count;
            LoadCurrentLevel();
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
        /// Creates a mini campaign of 3 progressive levels for initial playtesting:
        /// Level 1: 2 snakes (A blocked by B)
        /// Level 2: 3 snakes (A blocked by B, B blocked by C)
        /// Level 3: 4 snakes (Curved and intersecting paths)
        /// </summary>
        public static List<LevelDefinition> CreateSampleCampaign()
        {
            var list = new List<LevelDefinition>();

            // --- Level 1 ---
            var lvl1 = ScriptableObject.CreateInstance<LevelDefinition>();
            lvl1.LevelId = "Level 1";
            lvl1.Cols = 8;
            lvl1.Rows = 8;
            var s1A = new SnakeDefinition
            {
                Id = "snake_A",
                BodyColor = new Color(0.18f, 0.72f, 0.44f), // Green
                ExitDirection = GridCoord.Right,
                BodyCells = new List<GridCoord> { new GridCoord(2, 3), new GridCoord(2, 2), new GridCoord(2, 1) }
            };
            var s1B = new SnakeDefinition
            {
                Id = "snake_B",
                BodyColor = new Color(0.20f, 0.50f, 0.95f), // Blue
                ExitDirection = GridCoord.Up,
                BodyCells = new List<GridCoord> { new GridCoord(4, 3), new GridCoord(4, 2), new GridCoord(4, 1) }
            };
            lvl1.Snakes = new List<SnakeDefinition> { s1A, s1B };
            list.Add(lvl1);

            // --- Level 2 ---
            var lvl2 = ScriptableObject.CreateInstance<LevelDefinition>();
            lvl2.LevelId = "Level 2";
            lvl2.Cols = 8;
            lvl2.Rows = 8;
            var s2A = new SnakeDefinition
            {
                Id = "snake_A",
                BodyColor = new Color(0.18f, 0.72f, 0.44f), // Green
                ExitDirection = GridCoord.Right,
                BodyCells = new List<GridCoord> { new GridCoord(1, 4), new GridCoord(1, 3), new GridCoord(1, 2) }
            };
            var s2B = new SnakeDefinition
            {
                Id = "snake_B",
                BodyColor = new Color(0.20f, 0.50f, 0.95f), // Blue
                ExitDirection = GridCoord.Up,
                BodyCells = new List<GridCoord> { new GridCoord(3, 4), new GridCoord(3, 3), new GridCoord(3, 2) }
            };
            var s2C = new SnakeDefinition
            {
                Id = "snake_C",
                BodyColor = new Color(0.95f, 0.45f, 0.20f), // Coral/Orange
                ExitDirection = GridCoord.Right,
                BodyCells = new List<GridCoord> { new GridCoord(3, 6), new GridCoord(2, 6), new GridCoord(1, 6) }
            };
            lvl2.Snakes = new List<SnakeDefinition> { s2A, s2B, s2C };
            list.Add(lvl2);

            // --- Level 3 ---
            var lvl3 = ScriptableObject.CreateInstance<LevelDefinition>();
            lvl3.LevelId = "Level 3";
            lvl3.Cols = 8;
            lvl3.Rows = 8;
            var s3A = new SnakeDefinition
            {
                Id = "snake_A",
                BodyColor = new Color(0.60f, 0.25f, 0.85f), // Purple
                ExitDirection = GridCoord.Up,
                BodyCells = new List<GridCoord> { new GridCoord(2, 5), new GridCoord(2, 4), new GridCoord(3, 4), new GridCoord(4, 4) }
            };
            var s3B = new SnakeDefinition
            {
                Id = "snake_B",
                BodyColor = new Color(0.15f, 0.75f, 0.75f), // Teal
                ExitDirection = GridCoord.Right,
                BodyCells = new List<GridCoord> { new GridCoord(2, 6), new GridCoord(1, 6), new GridCoord(0, 6) }
            };
            var s3C = new SnakeDefinition
            {
                Id = "snake_C",
                BodyColor = new Color(0.95f, 0.70f, 0.15f), // Gold
                ExitDirection = GridCoord.Down,
                BodyCells = new List<GridCoord> { new GridCoord(5, 3), new GridCoord(5, 4), new GridCoord(5, 5) }
            };
            lvl3.Snakes = new List<SnakeDefinition> { s3A, s3B, s3C };
            list.Add(lvl3);

            return list;
        }

        public static LevelDefinition CreateSampleLevel()
        {
            var campaign = CreateSampleCampaign();
            return campaign[0];
        }

        private void OnDestroy()
        {
            if (inputHandler != null)
                inputHandler.OnSnakeSelected -= HandleSnakeSelected;
        }
    }
}
