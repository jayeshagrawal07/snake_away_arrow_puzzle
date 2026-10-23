using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0649

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Displays completion screen when all snakes have successfully escaped.
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        public event Action OnNextLevelClicked;
        public event Action OnReplayClicked;

        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button replayButton;

        public void Bind(Action onNext, Action onReplay)
        {
            OnNextLevelClicked = onNext;
            OnReplayClicked = onReplay;

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());
            }

            if (replayButton != null)
            {
                replayButton.onClick.RemoveAllListeners();
                replayButton.onClick.AddListener(() => OnReplayClicked?.Invoke());
            }

            Hide();
        }

        public void Show(string levelId)
        {
            if (modalRoot != null)
                modalRoot.SetActive(true);
            else
                gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = $"Level {levelId} Cleared!";
        }

        public void Hide()
        {
            if (modalRoot != null)
                modalRoot.SetActive(false);
            else
                gameObject.SetActive(false);
        }
    }
}
