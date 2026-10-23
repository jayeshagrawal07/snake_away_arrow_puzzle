using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#pragma warning disable 0649

namespace SnakeEscape.Presentation
{
    /// <summary>
    /// Faithful recreation of the production HUD layout (matching Image 2):
    /// - Top Row: Blue Pause button (left), bold "Level X" (center), Blue Settings button (right)
    /// - Row 2: Snake counter pill (left), 3 Red Hearts (center), Difficulty pill (right)
    /// - Bottom Row: Circular Hint bulb button (left), Undo pill (center), Circular Ruler button (right)
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        public event Action OnUndoClicked;
        public event Action OnRestartClicked;
        public event Action OnPauseClicked;
        public event Action OnHintClicked;
        public event Action OnGuideClicked;

        [Header("UI References (Optional)")]
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI snakeCountText;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private Transform heartsContainer;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button guideButton;

        private readonly List<Image> heartImages = new List<Image>();

        private static Sprite cachedCircleSprite;
        private static Sprite cachedPillSprite;
        private static Sprite cachedHeartSprite;
        private static Sprite cachedPauseSprite;
        private static Sprite cachedGearSprite;
        private static Sprite cachedBulbSprite;
        private static Sprite cachedRulerSprite;

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

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(() => OnPauseClicked?.Invoke());
            }

            if (hintButton != null)
            {
                hintButton.onClick.RemoveAllListeners();
                hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
            }

            if (guideButton != null)
            {
                guideButton.onClick.RemoveAllListeners();
                guideButton.onClick.AddListener(() => OnGuideClicked?.Invoke());
            }
        }

        public void UpdateLevelDisplay(string levelId, int remainingSnakes, string difficulty = "Normal")
        {
            EnsureUI();

            if (levelTitleText != null)
                levelTitleText.text = levelId.StartsWith("Level") ? levelId : $"Level {levelId}";

            if (snakeCountText != null)
                snakeCountText.text = $"⤹ {remainingSnakes}";

            if (difficultyText != null)
                difficultyText.text = difficulty;
        }

        public void SetUndoInteractable(bool interactable)
        {
            if (undoButton != null)
                undoButton.interactable = interactable;
        }

        public void SetLives(int lives)
        {
            for (int i = 0; i < heartImages.Count; i++)
            {
                if (heartImages[i] != null)
                {
                    heartImages[i].color = (i < lives) ? new Color(0.92f, 0.18f, 0.22f) : new Color(0.80f, 0.83f, 0.88f);
                }
            }
        }

        private void EnsureUI()
        {
            if (levelTitleText != null && pauseButton != null) return;

            // Ensure EventSystem
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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

            // Sprites
            Sprite circle = GetOrCreateCircleSprite();
            Sprite pill = GetOrCreatePillSprite();
            Sprite heart = GetOrCreateHeartSprite();
            Sprite pause = GetOrCreatePauseSprite();
            Sprite gear = GetOrCreateGearSprite();
            Sprite bulb = GetOrCreateBulbSprite();
            Sprite ruler = GetOrCreateRulerSprite();

            // ── TOP ROW ──────────────────────────────────────────────────────────

            // Top-Left Pause Button (Blue Circle)
            pauseButton = CreateIconButton(root, "PauseBtn", new Vector2(96f, -110f), new Vector2(96f, 96f),
                                           new Color(0.18f, 0.52f, 1.0f), pause, Color.white);

            // Center Level Title ("Level 10")
            GameObject titleObj = new GameObject("LevelTitle", typeof(RectTransform));
            titleObj.transform.SetParent(root, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, -110f);
            titleRt.sizeDelta = new Vector2(400f, 80f);

            levelTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            levelTitleText.text = "Level 1";
            levelTitleText.fontSize = 58;
            levelTitleText.fontStyle = FontStyles.Bold;
            levelTitleText.color = new Color(0.06f, 0.10f, 0.18f); // Deep Crisp Navy
            levelTitleText.alignment = TextAlignmentOptions.Center;

            // Top-Right Settings Button (Blue Circle)
            settingsButton = CreateIconButton(root, "SettingsBtn", new Vector2(-96f, -110f), new Vector2(96f, 96f),
                                              new Color(0.18f, 0.52f, 1.0f), gear, Color.white);

            // ── ROW 2 (SUB-HEADER) ────────────────────────────────────────────────

            // Left Pill: Snake Counter
            GameObject countPill = CreatePillCard(root, "SnakeCountPill", new Vector2(165f, -220f), new Vector2(170f, 76f), pill);
            snakeCountText = CreateCardText(countPill.transform, "⤹ 37", 40f, new Color(0.08f, 0.12f, 0.20f), FontStyles.Bold);

            // Center: 3 Hearts
            GameObject heartsObj = new GameObject("HeartsContainer", typeof(RectTransform));
            heartsObj.transform.SetParent(root, false);
            RectTransform heartsRt = heartsObj.GetComponent<RectTransform>();
            heartsRt.anchorMin = new Vector2(0.5f, 1f);
            heartsRt.anchorMax = new Vector2(0.5f, 1f);
            heartsRt.pivot = new Vector2(0.5f, 0.5f);
            heartsRt.anchoredPosition = new Vector2(0f, -220f);
            heartsRt.sizeDelta = new Vector2(240f, 60f);
            heartsContainer = heartsObj.transform;

            heartImages.Clear();
            for (int i = 0; i < 3; i++)
            {
                GameObject hObj = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
                hObj.transform.SetParent(heartsObj.transform, false);
                RectTransform hRt = hObj.GetComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0.5f, 0.5f);
                hRt.anchorMax = new Vector2(0.5f, 0.5f);
                hRt.pivot = new Vector2(0.5f, 0.5f);
                hRt.anchoredPosition = new Vector2((i - 1) * 64f, 0f);
                hRt.sizeDelta = new Vector2(48f, 48f);

                Image hImg = hObj.GetComponent<Image>();
                hImg.sprite = heart;
                hImg.color = new Color(0.92f, 0.18f, 0.22f); // Vibrant Red
                hImg.preserveAspect = true;
                heartImages.Add(hImg);
            }

            // Right Pill: Difficulty Badge
            GameObject diffPill = CreatePillCard(root, "DifficultyPill", new Vector2(-165f, -220f), new Vector2(170f, 76f), pill);
            difficultyText = CreateCardText(diffPill.transform, "Hard", 36f, new Color(0.18f, 0.25f, 0.38f), FontStyles.Bold);

            // ── BOTTOM ROW ────────────────────────────────────────────────────────

            // Bottom Left: Circular Hint Bulb Button
            hintButton = CreatePowerupButton(root, "HintBulbBtn", new Vector2(-180f, 150f), circle, bulb);

            // Bottom Right: Circular Grid Guide Ruler Button
            guideButton = CreatePowerupButton(root, "GuideRulerBtn", new Vector2(180f, 150f), circle, ruler);

            // Bottom Center: Minimal Undo Button
            GameObject undoObj = new GameObject("UndoButton", typeof(RectTransform), typeof(Image), typeof(Button));
            undoObj.transform.SetParent(root, false);
            RectTransform undoRt = undoObj.GetComponent<RectTransform>();
            undoRt.anchorMin = new Vector2(0.5f, 0f);
            undoRt.anchorMax = new Vector2(0.5f, 0f);
            undoRt.pivot = new Vector2(0.5f, 0.5f);
            undoRt.anchoredPosition = new Vector2(0f, 150f);
            undoRt.sizeDelta = new Vector2(180f, 80f);

            Image undoImg = undoObj.GetComponent<Image>();
            undoImg.sprite = pill;
            undoImg.type = Image.Type.Sliced;
            undoImg.color = new Color(0.92f, 0.94f, 0.98f);

            GameObject undoTxtObj = new GameObject("Label", typeof(RectTransform));
            undoTxtObj.transform.SetParent(undoObj.transform, false);
            RectTransform utRt = undoTxtObj.GetComponent<RectTransform>();
            utRt.anchorMin = Vector2.zero;
            utRt.anchorMax = Vector2.one;
            utRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI utTmp = undoTxtObj.AddComponent<TextMeshProUGUI>();
            utTmp.text = "↶ Undo";
            utTmp.fontSize = 34;
            utTmp.fontStyle = FontStyles.Bold;
            utTmp.color = new Color(0.18f, 0.25f, 0.38f);
            utTmp.alignment = TextAlignmentOptions.Center;

            undoButton = undoObj.GetComponent<Button>();
        }

        private static Button CreateIconButton(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor, Sprite iconSprite, Color iconColor)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = pos.x > 0 ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image bgImg = btnObj.GetComponent<Image>();
            bgImg.sprite = GetOrCreateCircleSprite();
            bgImg.color = bgColor;

            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(btnObj.transform, false);
                RectTransform irt = iconObj.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.5f, 0.5f);
                irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.sizeDelta = size * 0.55f;

                Image img = iconObj.GetComponent<Image>();
                img.sprite = iconSprite;
                img.color = iconColor;
                img.preserveAspect = true;
            }

            return btnObj.GetComponent<Button>();
        }

        private static Button CreatePowerupButton(Transform parent, string name, Vector2 pos, Sprite circleSprite, Sprite iconSprite)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(160f, 160f);

            Image bgImg = btnObj.GetComponent<Image>();
            bgImg.sprite = circleSprite;
            bgImg.color = Color.white;

            // Icon
            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(btnObj.transform, false);
                RectTransform irt = iconObj.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.5f, 0.5f);
                irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.sizeDelta = new Vector2(100f, 100f);

                Image img = iconObj.GetComponent<Image>();
                img.sprite = iconSprite;
                img.preserveAspect = true;
            }

            // Blue Plus Badge on top right of button
            GameObject badgeObj = new GameObject("PlusBadge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(btnObj.transform, false);
            RectTransform brt = badgeObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(-15f, -15f);
            brt.sizeDelta = new Vector2(46f, 46f);

            Image bImg = badgeObj.GetComponent<Image>();
            bImg.sprite = circleSprite;
            bImg.color = new Color(0.18f, 0.52f, 1.0f);

            GameObject pTxtObj = new GameObject("PlusText", typeof(RectTransform));
            pTxtObj.transform.SetParent(badgeObj.transform, false);
            RectTransform ptRt = pTxtObj.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero;
            ptRt.anchorMax = Vector2.one;
            ptRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI pTmp = pTxtObj.AddComponent<TextMeshProUGUI>();
            pTmp.text = "+";
            pTmp.fontSize = 32;
            pTmp.fontStyle = FontStyles.Bold;
            pTmp.color = Color.white;
            pTmp.alignment = TextAlignmentOptions.Center;

            return btnObj.GetComponent<Button>();
        }

        private static GameObject CreatePillCard(Transform parent, string name, Vector2 pos, Vector2 size, Sprite pillSprite)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = pos.x > 0 ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.GetComponent<Image>();
            img.sprite = pillSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.93f, 0.95f, 0.98f); // Soft cool gray card

            return obj;
        }

        private static TextMeshProUGUI CreateCardText(Transform parent, string text, float fontSize, Color color, FontStyles style)
        {
            GameObject txtObj = new GameObject("Label", typeof(RectTransform));
            txtObj.transform.SetParent(parent, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        // ── PROCEDURAL SPRITE GENERATORS ──────────────────────────────────────

        private static Sprite GetOrCreateCircleSprite()
        {
            if (cachedCircleSprite != null) return cachedCircleSprite;
            int s = 128;
            Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            float r = s * 0.46f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float a = Mathf.Clamp01((r - d) / 1.5f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            cachedCircleSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return cachedCircleSprite;
        }

        private static Sprite GetOrCreatePillSprite()
        {
            if (cachedPillSprite != null) return cachedPillSprite;
            int w = 128, h = 64;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            float r = h * 0.46f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x, r, w - r);
                    float cy = h * 0.5f;
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float a = Mathf.Clamp01((r - d) / 1.5f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            cachedPillSprite = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(30, 25, 30, 25));
            return cachedPillSprite;
        }

        private static Sprite GetOrCreateHeartSprite()
        {
            if (cachedHeartSprite != null) return cachedHeartSprite;
            int s = 96;
            Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float nx = (x / (float)(s - 1)) * 2f - 1f;
                    float ny = (y / (float)(s - 1)) * 2f - 1.25f;
                    // Standard heart implicit formula: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                    float eq = Mathf.Pow(nx * nx + ny * ny - 1f, 3f) - nx * nx * Mathf.Pow(ny, 3f);
                    float a = Mathf.Clamp01(-eq * 3f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            cachedHeartSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return cachedHeartSprite;
        }

        private static Sprite GetOrCreatePauseSprite()
        {
            if (cachedPauseSprite != null) return cachedPauseSprite;
            int s = 64;
            Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    bool bar1 = (x >= 18 && x <= 26 && y >= 16 && y <= 48);
                    bool bar2 = (x >= 38 && x <= 46 && y >= 16 && y <= 48);
                    t.SetPixel(x, y, (bar1 || bar2) ? Color.white : Color.clear);
                }
            }
            t.Apply();
            cachedPauseSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return cachedPauseSprite;
        }

        private static Sprite GetOrCreateGearSprite()
        {
            if (cachedGearSprite != null) return cachedGearSprite;
            int s = 64;
            Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float angle = Mathf.Atan2(y - c.y, x - c.x);
                    float teeth = Mathf.Sin(angle * 6f) * 4f;
                    float r = 20f + teeth;
                    float a = (d >= 8f && d <= r) ? 1f : 0f;
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            cachedGearSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return cachedGearSprite;
        }

        private static Sprite GetOrCreateBulbSprite()
        {
            if (cachedBulbSprite != null) return cachedBulbSprite;
            int s = 80;
            Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            Vector2 c = new Vector2(s * 0.5f, s * 0.58f);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    bool glass = (d <= 20f);
                    bool basePlate = (x >= s * 0.38f && x <= s * 0.62f && y >= s * 0.22f && y <= s * 0.38f);
                    Color col = glass ? new Color(1.0f, 0.78f, 0.10f) : (basePlate ? new Color(0.40f, 0.48f, 0.62f) : Color.clear);
                    t.SetPixel(x, y, col);
                }
            }
            t.Apply();
            cachedBulbSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return cachedBulbSprite;
        }

        private static Sprite GetOrCreateRulerSprite()
        {
            if (cachedRulerSprite != null) return cachedRulerSprite;
            int s = 80;
            Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // Diagonal ruler strip
                    float diag = (x + y) * 0.7071f;
                    float width = Mathf.Abs((x - y) * 0.7071f);
                    bool onRuler = (diag >= 20f && diag <= 90f && width <= 14f);
                    Color col = onRuler ? new Color(0.35f, 0.45f, 0.95f) : Color.clear;
                    t.SetPixel(x, y, col);
                }
            }
            t.Apply();
            cachedRulerSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return cachedRulerSprite;
        }
    }
}
