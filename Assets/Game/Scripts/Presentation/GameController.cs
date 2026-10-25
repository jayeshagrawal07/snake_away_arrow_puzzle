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
    /// Supports both procedural long-spine levels and authored LevelDefinition assets.
    /// </summary>
    [RequireComponent(typeof(BoardView), typeof(SnakeInputHandler))]
    public class GameController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private SnakeInputHandler inputHandler;
        [SerializeField] private GameplayHUD hud;
        [SerializeField] private LevelCompleteUI completeUI;

        [Header("Level Configuration")]
        [Tooltip("Starting level number (e.g. 1, 5, 10).")]
        [SerializeField] private int currentLevelNumber = 1;
        [SerializeField] private int playerLives = 3;

        public GameSession Session { get; private set; }

        private void Awake()
        {
            if (boardView == null) boardView = GetComponent<BoardView>();
            if (inputHandler == null) inputHandler = GetComponent<SnakeInputHandler>();
            if (hud == null) hud = gameObject.AddComponent<GameplayHUD>();
            if (completeUI == null) completeUI = gameObject.AddComponent<LevelCompleteUI>();

            // Ensure Camera has pure white background like Image 2
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.white;
            }

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

            LoadLevelByNumber(currentLevelNumber);
        }

        private void Update()
        {
            if (Session != null && Session.CurrentState == GameSession.SessionState.Ready)
            {
                inputHandler.ProcessPointerInput(boardView);
            }
        }

        public void LoadLevelByNumber(int levelNum)
        {
            currentLevelNumber = Mathf.Max(1, levelNum);

            // Generate dense long-spine interlocking level matching production quality
            LevelDefinition generatedLevel = LongSpineMazeGenerator.Generate(currentLevelNumber);
            LoadLevel(generatedLevel);
        }

        public void LoadLevel(LevelDefinition level)
        {
            playerLives = 3;
            Session.LoadLevel(level);
            boardView.SetupBoard(level);

            UpdateUI();
            if (completeUI != null) completeUI.Hide();
        }

        private void HandleSnakeSelected(string snakeId)
        {
            if (Session.CurrentState != GameSession.SessionState.Ready) return;

            var result = Session.RequestMove(snakeId);

            SnakeView view = boardView.GetSnakeView(snakeId);
            if (view == null) return;

            if (result.IsValid)
            {
                Session.UndoService.RecordSnapshot(Session.PuzzleState);
                view.AnimateEscape(result.EscapeRoute, () =>
                {
                    boardView.RemoveSnakeView(snakeId);
                    Session.CommitMove(snakeId);
                    UpdateUI();
                });
            }
            else if (result.Status == MoveStatus.BlockedBySnake)
            {
                // Collision penalty & visual feedback
                playerLives = Mathf.Max(0, playerLives - 1);
                if (hud != null) hud.SetLives(playerLives);

                view.AnimateBlocked(result.BlockedAtCell, () =>
                {
                    // Snap back
                });
            }
        }

        private void HandleUndoRequested()
        {
            if (Session.CurrentState != GameSession.SessionState.Ready &&
                Session.CurrentState != GameSession.SessionState.LevelComplete) return;

            if (Session.RequestUndo())
            {
                if (completeUI != null) completeUI.Hide();

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
            LoadLevelByNumber(currentLevelNumber);
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
            currentLevelNumber++;
            LoadLevelByNumber(currentLevelNumber);
        }

        private void UpdateUI()
        {
            if (hud != null && Session.CurrentLevel != null)
            {
                int remaining = Session.CurrentLevel.Snakes.Count - Session.PuzzleState.EscapedSnakeIds.Count;
                string difficulty = Session.CurrentLevel.DifficultyCategory ?? "Normal";
                hud.UpdateLevelDisplay(Session.CurrentLevel.LevelId, remaining, difficulty);
                hud.SetLives(playerLives);
                hud.SetUndoInteractable(Session.UndoService.CanUndo);
            }
        }

        /// <summary>
        /// Compatibility helper for unit tests.
        /// </summary>
        public static LevelDefinition CreateSampleLevel()
        {
            return LongSpineMazeGenerator.Generate(1);
        }

        /// <summary>
        /// Compatibility helper for unit tests.
        /// </summary>
        public static List<LevelDefinition> CreateSampleCampaign()
        {
            return new List<LevelDefinition>
            {
                LongSpineMazeGenerator.Generate(1),
                LongSpineMazeGenerator.Generate(5),
                LongSpineMazeGenerator.Generate(10)
            };
        }

        private void OnDestroy()
        {
            if (inputHandler != null)
                inputHandler.OnSnakeSelected -= HandleSnakeSelected;
        }
    }
}
