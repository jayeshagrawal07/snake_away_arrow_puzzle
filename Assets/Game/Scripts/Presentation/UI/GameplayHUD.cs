using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#pragma warning disable 0649

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Displays in-game HUD: level number, remaining snakes, undo and restart buttons.
    /// Automatically constructs a clean UI if no Canvas is pre-assigned.
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

        private void Awake()
        {
            EnsureUI();
        }

        public void Bind(Action onUndo, Action onRestart)
        {
            EnsureUI();

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
            EnsureUI();

            if (levelTitleText != null)
                levelTitleText.text = levelId;

            if (snakeCountText != null)
                snakeCountText.text = $"Snakes: {remainingSnakes}";
        }

        public void SetUndoInteractable(bool interactable)
        {
            if (undoButton != null)
                undoButton.interactable = interactable;
        }

        private void EnsureUI()
        {
            if (levelTitleText != null && undoButton != null) return;

            // Ensure EventSystem exists
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // Find or create Canvas
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("HUDCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            Transform root = canvas.transform;

            // Top Bar
            GameObject topBar = new GameObject("TopBar", typeof(RectTransform));
            topBar.transform.SetParent(root, false);
            RectTransform topRt = topBar.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0f, 1f);
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.anchoredPosition = new Vector2(0f, -60f);
            topRt.sizeDelta = new Vector2(-80f, 140f);

            // Level Title
            GameObject titleObj = new GameObject("LevelTitle", typeof(RectTransform));
            titleObj.transform.SetParent(topBar.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(400f, 100f);

            levelTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            levelTitleText.text = "Level 1";
            levelTitleText.fontSize = 54;
            levelTitleText.fontStyle = FontStyles.Bold;
            levelTitleText.color = new Color(0.12f, 0.15f, 0.22f);
            levelTitleText.alignment = TextAlignmentOptions.MidlineLeft;

            // Snake Count Badge
            GameObject countObj = new GameObject("SnakeCount", typeof(RectTransform));
            countObj.transform.SetParent(topBar.transform, false);
            RectTransform countRt = countObj.GetComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0.5f, 0.5f);
            countRt.anchorMax = new Vector2(1f, 0.5f);
            countRt.pivot = new Vector2(1f, 0.5f);
            countRt.anchoredPosition = Vector2.zero;
            countRt.sizeDelta = new Vector2(400f, 100f);

            snakeCountText = countObj.AddComponent<TextMeshProUGUI>();
            snakeCountText.text = "Snakes: 2";
            snakeCountText.fontSize = 42;
            snakeCountText.fontStyle = FontStyles.Normal;
            snakeCountText.color = new Color(0.35f, 0.40f, 0.50f);
            snakeCountText.alignment = TextAlignmentOptions.MidlineRight;

            // Bottom Bar
            GameObject bottomBar = new GameObject("BottomBar", typeof(RectTransform));
            bottomBar.transform.SetParent(root, false);
            RectTransform btmRt = bottomBar.GetComponent<RectTransform>();
            btmRt.anchorMin = new Vector2(0f, 0f);
            btmRt.anchorMax = new Vector2(1f, 0f);
            btmRt.pivot = new Vector2(0.5f, 0f);
            btmRt.anchoredPosition = new Vector2(0f, 80f);
            btmRt.sizeDelta = new Vector2(-80f, 140f);

            // Undo Button
            undoButton = CreatePillButton(bottomBar.transform, "UndoBtn", "↶ Undo", new Vector2(-180f, 0f), new Color(0.92f, 0.94f, 0.98f), new Color(0.18f, 0.24f, 0.36f));

            // Restart Button
            restartButton = CreatePillButton(bottomBar.transform, "RestartBtn", "↺ Restart", new Vector2(180f, 0f), new Color(0.92f, 0.94f, 0.98f), new Color(0.18f, 0.24f, 0.36f));
        }

        private static Button CreatePillButton(Transform parent, string name, string label, Vector2 pos, Color bgColor, Color textColor)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300f, 100f);

            Image img = btnObj.GetComponent<Image>();
            img.color = bgColor;

            GameObject textObj = new GameObject("Label", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 38;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;

            return btnObj.GetComponent<Button>();
        }
    }
}
