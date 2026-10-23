using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0649

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Displays in-game HUD: level number, remaining snakes, undo and restart buttons.
    /// Can link to existing Canvas elements or generate minimal UI dynamically.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        public event Action OnUndoClicked;
        public event Action OnRestartClicked;

        [Header("UI References (Optional)")]
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI snakeCountText;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button restartButton;

        public void Bind(Action onUndo, Action onRestart)
        {
            OnUndoClicked = onUndo;
            OnRestartClicked = onRestart;

            if (undoButton != null)
            {
                undoButton.onClick.RemoveAllListeners();
                undoButton.onClick.AddListener(() => OnUndoClicked?.Invoke());
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
            }
        }

        public void UpdateLevelDisplay(string levelId, int remainingSnakes)
        {
            if (levelTitleText != null)
                levelTitleText.text = levelId;

            if (snakeCountText != null)
                snakeCountText.text = $"Remaining: {remainingSnakes}";
        }

        public void SetUndoInteractable(bool interactable)
        {
            if (undoButton != null)
                undoButton.interactable = interactable;
        }
    }
}
