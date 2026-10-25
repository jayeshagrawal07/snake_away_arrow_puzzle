using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SnakeEscape.Domain;
using SnakeEscape.Application;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Level, Coins & Player State")]
    public bool resetToLevel1Now = false;
    public int currentLevel = 1;
    public int lives = 3;
    public int remainingHints = 1;
    public int remainingGridGuides = 1;
    public int playerCoins = 760;
    public bool hapticsEnabled = true;
    [HideInInspector] public bool isPaused = false;
    [HideInInspector] public bool isChallengeMode = false;

    [Header("Canvas References (Auto-Linked)")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI snakeCountText;
    public TextMeshProUGUI difficultyText;
    public Transform heartsContainer;
    public Button nextLevelButton;
    public Button hintButton;
    public Button pauseButton;

    [Header("Custom Assets (Auto-Loaded from Assets Folder)")]
    public Sprite winnerIconSprite;
    public Sprite pauseAssetSprite;
    public Sprite settingsAssetSprite;

    [HideInInspector] public float currentSpacing = 0.45f;

    private List<SnakeController> activeSnakes = new List<SnakeController>();
    private List<SnakeController> levelSnakes = new List<SnakeController>();
    private List<Image> heartImages = new List<Image>();
    private GameObject dotGridContainer;
    private GameObject gridGuideLinesContainer;
    private Dictionary<Vector2Int, Transform> gridDots = new Dictionary<Vector2Int, Transform>();
    private Vector2 currentOriginOffset;

    private int currentCols = 14;
    private int currentRows = 16;
    private int lockedLevelSeed = 0;
    private int lastGeneratedLevel = -1;
    private int challengeRunCount = 0;
    private int levelGeneration = 0;
    private bool isCurrentChallengeUnlocked = false;
    private PlayerSaveService playerSaveService;

    // Milestone & Timer State (3 Minutes = 180s)
    private int initialSnakeCountInLevel = 0;
    private bool halfLevelMilestoneShown = false;
    private bool isTimerActive = false;
    private float timeRemaining = 0f;
    private float totalTimerDuration = 180f;
    private bool halfTimePopupShown = false;
    private bool isGridGuideActiveThisLevel = false;
    private GameObject timerPillObj;
    private TextMeshProUGUI timerText;

    // Dynamic UI Text References
    private TextMeshProUGUI hintBadgeText;
    private TextMeshProUGUI gridBadgeText;
    private TextMeshProUGUI homeContinueBtnText;
    private TextMeshProUGUI homeCoinText;
    private TextMeshProUGUI challengeEntryCoinText;
    private TextMeshProUGUI hapticsToggleBtnText;

    // Pinch Zoom & Pan State
    private float defaultOrthoSize = 5.5f;
    private float minOrthoSize = 2.2f;
    private float maxOrthoSize = 7.2f;
    private Vector3 defaultCamPos = new Vector3(0f, -0.25f, -10f);
    private Vector3 panDragOrigin;
    private bool isPanning = false;
    private GameObject resetZoomBtnObj;

    // Overlays & Modals
    private Image damageVignette;
    private GameObject homeScreenObj;
    private GameObject settingsModalObj;
    private GameObject challengeEntryModalObj;
    private GameObject multiplayerModalObj;
    private GameObject pauseModalObj;
    private GameObject winModalObj;
    private GameObject gameOverModalObj;
    private GameObject hintAdModalObj;
    private GameObject gridGuideAdModalObj;
    private GameObject halfTimeAdModalObj;
    private GameObject halfLevelMilestoneModalObj;
    private GameObject adSimOverlayObj;
    private TextMeshProUGUI adSimTimerText;

    // Rotating Sunburst Transforms
    private List<RectTransform> activeSunburstRays = new List<RectTransform>();
    private TextMeshProUGUI winHeaderTitleText;
    private TextMeshProUGUI winCoinRewardText;
    private TextMeshProUGUI winCardPraiseText;
    private TextMeshProUGUI winNextBtnText;

    // 3-Second Board Celebration
    private GameObject praiseFloatingObj;
    private TextMeshProUGUI praiseFloatingText;
    private RectTransform confettiContainer;

    // HD Procedural Sprites
    private Sprite crispHeartSprite;
    private Sprite brokenHeart3DSprite;
    private Sprite goldCoinSprite;
    private Sprite pillSprite;
    private Sprite pillShadowSprite;
    private Sprite roundedCardSprite;
    private Sprite circleSprite;
    private Sprite yellowSunburstCardSprite;
    private Sprite sunburstRaysOverlaySprite;
    private Sprite videoAdIconSprite;
    private Sprite snakeCountIconSprite;
    private Sprite calendarBadgeSprite;
    private Sprite friendsTrophySprite;
    private Sprite gridGuideButtonSprite;
    private Sprite rulerBigIconSprite;
    private Sprite gearIconSprite;
    private Sprite yellowSunburstBackgroundSprite;

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoLoadCustomSpritesFromAssets();
    }

    void AutoLoadCustomSpritesFromAssets()
    {
        if (winnerIconSprite == null)
            winnerIconSprite = FindSpriteByKeyword("crown") ?? FindSpriteByKeyword("winner") ?? FindSpriteByKeyword("trophy");
        if (pauseAssetSprite == null)
            pauseAssetSprite = FindSpriteByKeyword("pause");
        if (settingsAssetSprite == null)
            settingsAssetSprite = FindSpriteByKeyword("settings");
    }

    Sprite FindSpriteByKeyword(string keyword)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite");
        foreach (string g in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null && sp.name.ToLower().Contains(keyword.ToLower()))
                return sp;
        }
        return null;
    }
#endif

    void Awake()
    {
        Instance = this;
        QualitySettings.antiAliasing = 8;
        Application.targetFrameRate = 60;

#if UNITY_EDITOR
        AutoLoadCustomSpritesFromAssets();
#endif

        playerSaveService = new PlayerSaveService(new UnityPlayerSaveStore());
        PlayerSaveData savedProgress = playerSaveService.LoadOrMigrate(resetToLevel1Now);
        resetToLevel1Now = false;
        currentLevel = savedProgress.currentLevel;
        remainingHints = savedProgress.hints;
        remainingGridGuides = savedProgress.gridGuides;
        playerCoins = savedProgress.coins;
        challengeRunCount = savedProgress.challengeRunCount;
        isCurrentChallengeUnlocked = savedProgress.challengeUnlocked;
        hapticsEnabled = savedProgress.hapticsEnabled;
    }

    void Start()
    {
        SetupCamera();
        AutoFixAndStyleCanvasUI();
        LoadLevel(currentLevel);
    }

    void SaveProgress()
    {
        SaveProgress(currentLevel);
    }

    void SaveProgress(int levelToSave)
    {
        currentLevel = Mathf.Max(1, currentLevel);
        remainingHints = Mathf.Max(0, remainingHints);
        remainingGridGuides = Mathf.Max(0, remainingGridGuides);
        playerCoins = Mathf.Max(0, playerCoins);
        challengeRunCount = Mathf.Max(0, challengeRunCount);

        playerSaveService.Save(new PlayerSaveData
        {
            currentLevel = Mathf.Max(1, levelToSave),
            hints = remainingHints,
            gridGuides = remainingGridGuides,
            coins = playerCoins,
            challengeRunCount = challengeRunCount,
            challengeUnlocked = isCurrentChallengeUnlocked,
            hapticsEnabled = hapticsEnabled
        });
    }

    // Point 3: Tactile Haptic Feedback
    public void TriggerHaptic(bool heavyImpact)
    {
        if (!hapticsEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    void AddCoins(int amount)
    {
        playerCoins = (int)System.Math.Min(int.MaxValue, (long)playerCoins + amount);
        SaveProgress();
        UpdateCoinAndHintUI();
    }

    void Update()
    {
        for (int i = 0; i < activeSunburstRays.Count; i++)
        {
            if (activeSunburstRays[i] != null && activeSunburstRays[i].gameObject.activeInHierarchy)
            {
                activeSunburstRays[i].Rotate(0f, 0f, -26f * Time.unscaledDeltaTime);
            }
        }

        if (isPaused) return;
        if (homeScreenObj != null && homeScreenObj.activeSelf) return;

        // Point 9: 3-Minute Timer + 50% Timer Completed Ad Offer Popup
        if (isTimerActive && activeSnakes.Count > 0)
        {
            timeRemaining -= Time.deltaTime;

            if (!halfTimePopupShown && timeRemaining <= (totalTimerDuration * 0.5f) && timeRemaining > 2f)
            {
                halfTimePopupShown = true;
                isPaused = true;
                if (halfTimeAdModalObj != null)
                {
                    halfTimeAdModalObj.SetActive(true);
                    halfTimeAdModalObj.transform.SetAsLastSibling();
                }
                return;
            }

            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                isTimerActive = false;
                UpdateTimerUI();
                StartCoroutine(ShowGameOverModalAfterDelay(levelGeneration));
                return;
            }
            UpdateTimerUI();
        }

        HandlePinchZoomAndPan();

        if (Input.touchCount <= 1 && Input.GetMouseButtonUp(0) && !isPanning)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 tapPt = new Vector2(mouseWorld.x, mouseWorld.y);

            SnakeController closestSnake = null;
            float bestDist = currentSpacing * 0.58f;

            for (int i = 0; i < activeSnakes.Count; i++)
            {
                SnakeController sc = activeSnakes[i];
                if (sc == null || sc.isMoving || sc.isCleared) continue;

                float d = sc.GetDistanceToPoint(tapPt);
                if (d < bestDist)
                {
                    bestDist = d;
                    closestSnake = sc;
                }
            }

            if (closestSnake != null) closestSnake.TriggerTap();
        }
    }

    void HandlePinchZoomAndPan()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float prevMag = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
            float currMag = (t0.position - t1.position).magnitude;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - (currMag - prevMag) * 0.0085f, minOrthoSize, maxOrthoSize);
            ClampCameraPan(cam);
            UpdateResetZoomButtonVisibility(cam);
            isPanning = true;
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * 2.2f, minOrthoSize, maxOrthoSize);
            ClampCameraPan(cam);
            UpdateResetZoomButtonVisibility(cam);
        }

        if (cam.orthographicSize < defaultOrthoSize - 0.15f)
        {
            if (Input.GetMouseButtonDown(0))
            {
                panDragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
                isPanning = false;
            }
            else if (Input.GetMouseButton(0))
            {
                Vector3 delta = panDragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
                if (delta.magnitude > 0.14f)
                {
                    isPanning = true;
                    cam.transform.position += new Vector3(delta.x, delta.y, 0f);
                    ClampCameraPan(cam);
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            isPanning = false;
        }
    }

    void ClampCameraPan(Camera cam)
    {
        float maxOffset = Mathf.Max(0f, (defaultOrthoSize - cam.orthographicSize) * 0.9f);
        float cx = Mathf.Clamp(cam.transform.position.x, defaultCamPos.x - maxOffset, defaultCamPos.x + maxOffset);
        float cy = Mathf.Clamp(cam.transform.position.y, defaultCamPos.y - maxOffset, defaultCamPos.y + maxOffset);
        cam.transform.position = new Vector3(cx, cy, defaultCamPos.z);
    }

    void UpdateResetZoomButtonVisibility(Camera cam)
    {
        if (resetZoomBtnObj != null)
            resetZoomBtnObj.SetActive(cam.orthographicSize < defaultOrthoSize - 0.15f);
    }

    public void ResetCameraZoom()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographicSize = defaultOrthoSize;
            cam.transform.position = defaultCamPos;
        }
        if (resetZoomBtnObj != null) resetZoomBtnObj.SetActive(false);
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = Color.white;
            cam.orthographic = true;
            cam.allowMSAA = true;
            cam.orthographicSize = defaultOrthoSize;
            cam.transform.position = defaultCamPos;
        }
    }

    void AutoFixAndStyleCanvasUI()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null) return;

        Canvas c = canvasObj.GetComponent<Canvas>();
        if (c != null)
        {
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.pixelPerfect = false;
        }

        CanvasScaler cs = canvasObj.GetComponent<CanvasScaler>();
        if (cs != null)
        {
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;
        }

        string[] unwantedHUD = { "TopRestartButton", "TopCoinWallet", "BottomChallengeBtn", "ChallengeHubModal" };
        foreach (string u in unwantedHUD)
        {
            Transform tr = canvasObj.transform.Find(u);
            if (tr != null) Destroy(tr.gameObject);
        }

        if (levelText == null) levelText = canvasObj.transform.Find("LevelTitleText")?.GetComponent<TextMeshProUGUI>();
        if (difficultyText == null) difficultyText = canvasObj.transform.Find("DifficultyText")?.GetComponent<TextMeshProUGUI>();
        if (snakeCountText == null) snakeCountText = canvasObj.transform.Find("SnakeCountText")?.GetComponent<TextMeshProUGUI>();
        if (heartsContainer == null) heartsContainer = canvasObj.transform.Find("HeartsContainer");
        if (nextLevelButton == null) nextLevelButton = canvasObj.transform.Find("NextLevelButton")?.GetComponent<Button>();
        if (hintButton == null) hintButton = canvasObj.transform.Find("HintButton")?.GetComponent<Button>();

        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);

        activeSunburstRays.Clear();
        pillSprite = Create9SlicePillSprite(256, 128, 62f);
        pillShadowSprite = Create9Slice3DPillSprite(256, 140, 64f);
        roundedCardSprite = Create9SliceRoundedCardSprite(256, 256, 54f);
        circleSprite = CreateSmoothCircleSprite(256);
        crispHeartSprite = CreatePlump3DHeartSprite(256, false);
        brokenHeart3DSprite = CreatePlump3DHeartSprite(256, true);
        goldCoinSprite = CreateGoldStarCoinSprite(128);
        yellowSunburstCardSprite = CreateYellowSunburstSprite(512, 512);
        yellowSunburstBackgroundSprite = CreateYellowSunburstSprite(512, 512, false);
        sunburstRaysOverlaySprite = CreateRotatingSunburstRaysSprite(512);
        videoAdIconSprite = CreateVideoAdClapperSprite(128);
        snakeCountIconSprite = CreateCrispSnakeBadgeIcon(256);
        calendarBadgeSprite = CreateCalendarIconSprite(128);
        friendsTrophySprite = CreateTrophyIconSprite(128);
        gridGuideButtonSprite = CreateGridGuideRulerButtonSprite(256);
        rulerBigIconSprite = CreateBigPurpleRulerSprite(256);
        gearIconSprite = settingsAssetSprite != null ? settingsAssetSprite : CreateGearIconSprite(128);

        if (winnerIconSprite == null) winnerIconSprite = friendsTrophySprite;

        // 1. Top Center: Level / Date Title
        if (levelText != null)
        {
            RectTransform rt = levelText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -105f);
            rt.sizeDelta = new Vector2(560f, 96f);
            StyleBoldTMP(levelText, 54f, new Color(0.04f, 0.07f, 0.14f));
        }

        // 2. Top Left Card: Snake Silhouette Icon + Crisp Count Text
        if (snakeCountText != null)
        {
            GameObject countBg = EnsureCardBackground(snakeCountText.transform, canvasObj.transform, "CountBoxBG", new Vector2(0f, 1f), new Vector2(142f, -225f), new Vector2(204f, 84f), pillSprite);

            Transform iconTr = countBg.transform.Find("MiniSnakeIcon");
            GameObject iconObj = iconTr != null ? iconTr.gameObject : new GameObject("MiniSnakeIcon");
            iconObj.transform.SetParent(countBg.transform, false);
            Image iconImg = iconObj.GetComponent<Image>();
            if (iconImg == null) iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = snakeCountIconSprite;
            iconImg.color = new Color(0.06f, 0.10f, 0.18f);
            iconImg.preserveAspect = true;
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(52f, 0f);
            iconRt.sizeDelta = new Vector2(46f, 46f);

            RectTransform rt = snakeCountText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(168f, -225f);
            rt.sizeDelta = new Vector2(124f, 84f);
            StyleBoldTMP(snakeCountText, 44f, new Color(0.06f, 0.10f, 0.18f));
        }

        // 3. Top Right Card: Difficulty Pill
        if (difficultyText != null)
        {
            EnsureCardBackground(difficultyText.transform, canvasObj.transform, "DiffBoxBG", new Vector2(1f, 1f), new Vector2(-142f, -225f), new Vector2(204f, 84f), pillSprite);
            RectTransform rt = difficultyText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-142f, -225f);
            rt.sizeDelta = new Vector2(196f, 84f);
            StyleBoldTMP(difficultyText, 38f, new Color(0.18f, 0.24f, 0.34f));
        }

        // 4. Center Spaced Hearts
        if (heartsContainer != null)
        {
            RectTransform rt = heartsContainer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -225f);
            rt.sizeDelta = new Vector2(280f, 80f);

            Image containerImg = heartsContainer.GetComponent<Image>();
            if (containerImg != null) containerImg.enabled = false;

            heartImages.Clear();
            while (heartsContainer.childCount < 3)
            {
                GameObject newH = new GameObject($"Heart_{heartsContainer.childCount}");
                newH.transform.SetParent(heartsContainer, false);
                newH.AddComponent<Image>();
            }

            for (int i = 0; i < 3; i++)
            {
                Transform child = heartsContainer.GetChild(i);
                RectTransform hRt = child.GetComponent<RectTransform>();
                if (hRt != null)
                {
                    hRt.anchorMin = new Vector2(0.5f, 0.5f);
                    hRt.anchorMax = new Vector2(0.5f, 0.5f);
                    hRt.pivot = new Vector2(0.5f, 0.5f);
                    hRt.anchoredPosition = new Vector2((i - 1) * 82f, 0f);
                    hRt.sizeDelta = new Vector2(64f, 64f);
                    hRt.localScale = Vector3.one;
                }
                Image hImg = child.GetComponent<Image>();
                if (hImg == null) hImg = child.gameObject.AddComponent<Image>();
                hImg.sprite = crispHeartSprite;
                hImg.type = Image.Type.Simple;
                hImg.preserveAspect = true;
                heartImages.Add(hImg);
            }
        }

        // 5. Clean Top-Left Pause, Top-Right Settings & 3-Min Timer Pill
        SetupCleanGameplayHUD(canvasObj.transform);

        // 6. Bottom Bar: Hint Button (Left) + Grid Guide Ruler Button (Right) (Point 6!)
        SetupBottomHintAndGridGuideButtons(canvasObj.transform);

        // 7. Build All Yellow-Sunburst Popups & Screens
        BuildPraiseAndConfettiUI(canvasObj.transform);
        BuildPauseModal(canvasObj.transform);
        BuildSettingsModal(canvasObj.transform);
        BuildWinCelebrationModal(canvasObj.transform);
        BuildOutOfLivesModal(canvasObj.transform);
        BuildGetHintAdModal(canvasObj.transform);
        BuildGetGridGuideAdModal(canvasObj.transform);
        BuildHalfTimeRemoveTimerModal(canvasObj.transform);
        BuildHalfLevelMilestoneModal(canvasObj.transform);
        BuildHomeScreen(canvasObj.transform);
        BuildDailyChallengeEntryModal(canvasObj.transform);
        BuildMultiplayerComingSoonModal(canvasObj.transform);
        BuildSimulatedAdOverlay(canvasObj.transform);

        Transform vigTr = canvasObj.transform.Find("DamageVignette");
        GameObject vigObj = vigTr != null ? vigTr.gameObject : new GameObject("DamageVignette");
        vigObj.transform.SetParent(canvasObj.transform, false);
        damageVignette = vigObj.GetComponent<Image>();
        if (damageVignette == null) damageVignette = vigObj.AddComponent<Image>();
        damageVignette.sprite = CreateVignetteSprite(128);
        damageVignette.color = new Color(0.95f, 0.15f, 0.20f, 0f);
        damageVignette.raycastTarget = false;
        RectTransform vigRt = vigObj.GetComponent<RectTransform>();
        vigRt.anchorMin = Vector2.zero;
        vigRt.anchorMax = Vector2.one;
        vigRt.sizeDelta = Vector2.zero;
    }

    void SetupCleanGameplayHUD(Transform canvasTr)
    {
        Transform pauseTr = canvasTr.Find("PauseButton");
        GameObject pauseObj = pauseTr != null ? pauseTr.gameObject : new GameObject("PauseButton");
        pauseObj.transform.SetParent(canvasTr, false);
        for (int i = pauseObj.transform.childCount - 1; i >= 0; i--)
            Destroy(pauseObj.transform.GetChild(i).gameObject);

        pauseButton = pauseObj.GetComponent<Button>();
        if (pauseButton == null) pauseButton = pauseObj.AddComponent<Button>();
        Image pauseImg = pauseObj.GetComponent<Image>();
        if (pauseImg == null) pauseImg = pauseObj.AddComponent<Image>();
        pauseImg.sprite = pauseAssetSprite != null ? pauseAssetSprite : CreateCrispPauseIconSprite(256);
        pauseImg.color = Color.white;
        pauseImg.preserveAspect = true;

        RectTransform pauseRt = pauseObj.GetComponent<RectTransform>();
        pauseRt.anchorMin = new Vector2(0f, 1f);
        pauseRt.anchorMax = new Vector2(0f, 1f);
        pauseRt.pivot = new Vector2(0.5f, 0.5f);
        pauseRt.anchoredPosition = new Vector2(92f, -105f);
        pauseRt.sizeDelta = new Vector2(84f, 84f);

        pauseButton.onClick.RemoveAllListeners();
        pauseButton.onClick.AddListener(() => { TriggerHaptic(false); TogglePause(); });

        // Top-Right Settings Gear Button (like Arrow Puzzle image (6).jpg!)
        Transform setTr = canvasTr.Find("TopSettingsButton");
        GameObject setObj = setTr != null ? setTr.gameObject : new GameObject("TopSettingsButton");
        setObj.transform.SetParent(canvasTr, false);
        Button setBtn = setObj.GetComponent<Button>();
        if (setBtn == null) setBtn = setObj.AddComponent<Button>();
        Image setImg = setObj.GetComponent<Image>();
        if (setImg == null) setImg = setObj.AddComponent<Image>();
        setImg.sprite = gearIconSprite;
        setImg.color = Color.white;
        setImg.preserveAspect = true;

        RectTransform setRt = setObj.GetComponent<RectTransform>();
        setRt.anchorMin = new Vector2(1f, 1f);
        setRt.anchorMax = new Vector2(1f, 1f);
        setRt.pivot = new Vector2(0.5f, 0.5f);
        setRt.anchoredPosition = new Vector2(-92f, -105f);
        setRt.sizeDelta = new Vector2(80f, 80f);

        setBtn.onClick.RemoveAllListeners();
        setBtn.onClick.AddListener(() => { TriggerHaptic(false); OpenSettingsModal(); });

        Transform oldTimer = canvasTr.Find("TopTimerPill");
        if (oldTimer != null) Destroy(oldTimer.gameObject);

        timerPillObj = new GameObject("TopTimerPill");
        timerPillObj.transform.SetParent(canvasTr, false);
        Image tBg = timerPillObj.AddComponent<Image>();
        tBg.sprite = pillSprite;
        tBg.type = Image.Type.Sliced;
        tBg.color = new Color(1.0f, 0.94f, 0.80f);
        RectTransform tRt = timerPillObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 1f);
        tRt.anchorMax = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -320f);
        tRt.sizeDelta = new Vector2(240f, 68f);

        GameObject tTxtObj = new GameObject("TimerText");
        tTxtObj.transform.SetParent(timerPillObj.transform, false);
        timerText = tTxtObj.AddComponent<TextMeshProUGUI>();
        StyleBoldTMP(timerText, 34f, new Color(0.85f, 0.25f, 0.05f));
        RectTransform ttRt = tTxtObj.GetComponent<RectTransform>();
        ttRt.anchorMin = Vector2.zero;
        ttRt.anchorMax = Vector2.one;
        ttRt.sizeDelta = Vector2.zero;
    }

    // Point 6: Bottom Hint Bulb Button (Left) + Grid Guide Ruler Button (Right) (matching image (6).jpg!)
    void SetupBottomHintAndGridGuideButtons(Transform canvasTr)
    {
        if (hintButton != null)
        {
            RectTransform rt = hintButton.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-135f, 185f);
            rt.sizeDelta = new Vector2(184f, 184f);

            for (int i = hintButton.transform.childCount - 1; i >= 0; i--)
                Destroy(hintButton.transform.GetChild(i).gameObject);

            Image btnImg = hintButton.GetComponent<Image>();
            if (btnImg == null) btnImg = hintButton.gameObject.AddComponent<Image>();
            btnImg.sprite = CreateArrowPuzzleHintButtonSprite(256);
            btnImg.color = Color.white;
            btnImg.preserveAspect = true;

            GameObject bTxtObj = new GameObject("HintBadgeText");
            bTxtObj.transform.SetParent(hintButton.transform, false);
            hintBadgeText = bTxtObj.AddComponent<TextMeshProUGUI>();
            StyleBoldTMP(hintBadgeText, 26f, Color.white);
            RectTransform bRt = bTxtObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0.5f);
            bRt.anchorMax = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(46f, 46f);
            bRt.sizeDelta = new Vector2(70f, 42f);

            hintButton.onClick.RemoveAllListeners();
            hintButton.onClick.AddListener(() => { TriggerHaptic(false); GiveHint(); });
        }

        // Right Side: Purple Ruler Grid Guide Button (exact match to 80789f93...jpg & image (6).jpg!)
        Transform oldGridBtn = canvasTr.Find("GridGuideButton");
        if (oldGridBtn != null) Destroy(oldGridBtn.gameObject);

        GameObject gridBtnObj = new GameObject("GridGuideButton");
        gridBtnObj.transform.SetParent(canvasTr, false);
        Button gridBtn = gridBtnObj.AddComponent<Button>();
        Image gImg = gridBtnObj.AddComponent<Image>();
        gImg.sprite = gridGuideButtonSprite;
        gImg.color = Color.white;
        gImg.preserveAspect = true;

        RectTransform gRt = gridBtnObj.GetComponent<RectTransform>();
        gRt.anchorMin = new Vector2(0.5f, 0f);
        gRt.anchorMax = new Vector2(0.5f, 0f);
        gRt.pivot = new Vector2(0.5f, 0.5f);
        gRt.anchoredPosition = new Vector2(135f, 185f);
        gRt.sizeDelta = new Vector2(184f, 184f);

        GameObject gTxtObj = new GameObject("GridBadgeText");
        gTxtObj.transform.SetParent(gridBtnObj.transform, false);
        gridBadgeText = gTxtObj.AddComponent<TextMeshProUGUI>();
        StyleBoldTMP(gridBadgeText, 26f, Color.white);
        RectTransform gbRt = gTxtObj.GetComponent<RectTransform>();
        gbRt.anchorMin = new Vector2(0.5f, 0.5f);
        gbRt.anchorMax = new Vector2(0.5f, 0.5f);
        gbRt.anchoredPosition = new Vector2(46f, 46f);
        gbRt.sizeDelta = new Vector2(70f, 42f);

        gridBtn.onClick.AddListener(() => { TriggerHaptic(false); UseGridGuide(); });

        Transform oldZoomBtn = canvasTr.Find("ResetZoomBtn");
        if (oldZoomBtn != null) Destroy(oldZoomBtn.gameObject);

        resetZoomBtnObj = CreatePillButton(canvasTr, "ResetZoomBtn", "Reset Zoom", new Color(0.18f, 0.24f, 0.34f, 0.9f), Color.white, new Vector2(0f, 330f), new Vector2(240f, 68f), 28f, false);
        RectTransform rzRt = resetZoomBtnObj.GetComponent<RectTransform>();
        rzRt.anchorMin = new Vector2(0.5f, 0f);
        rzRt.anchorMax = new Vector2(0.5f, 0f);
        rzRt.anchoredPosition = new Vector2(0f, 330f);
        resetZoomBtnObj.GetComponent<Button>().onClick.AddListener(ResetCameraZoom);
        resetZoomBtnObj.SetActive(false);

        UpdateCoinAndHintUI();
    }

    void UpdateCoinAndHintUI()
    {
        if (homeCoinText != null) homeCoinText.text = playerCoins.ToString();
        if (hintBadgeText != null)
            hintBadgeText.text = remainingHints > 0 ? remainingHints.ToString() : "+";
        if (gridBadgeText != null)
            gridBadgeText.text = remainingGridGuides > 0 ? remainingGridGuides.ToString() : "+";
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        int mins = Mathf.FloorToInt(timeRemaining / 60f);
        int secs = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = string.Format("⏱ {0:00}:{1:00}", mins, secs);
    }

    // Helper: Creates a Yellow & Light-Yellow Sunburst Popup Card (Point 11!)
    GameObject CreateYellowSunburstModalCard(Transform parent, string name, Vector2 size)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);
        Image img = card.AddComponent<Image>();
        img.sprite = yellowSunburstCardSprite;
        img.color = Color.white;

        Shadow sh = card.AddComponent<Shadow>();
        sh.effectColor = new Color(0.25f, 0.14f, 0.02f, 0.32f);
        sh.effectDistance = new Vector2(0f, -14f);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        // Add subtle rotating sunburst rays inside the card
        GameObject rays = new GameObject("SunburstRays");
        rays.transform.SetParent(card.transform, false);
        Image rImg = rays.AddComponent<Image>();
        rImg.sprite = sunburstRaysOverlaySprite;
        rImg.color = new Color(1f, 1f, 0.85f, 0.28f);
        rImg.raycastTarget = false;
        RectTransform rRt = rays.GetComponent<RectTransform>();
        rRt.sizeDelta = new Vector2(size.x * 0.92f, size.x * 0.92f);
        activeSunburstRays.Add(rRt);

        return card;
    }

    void BuildPraiseAndConfettiUI(Transform canvasTr)
    {
        Transform oldConf = canvasTr.Find("ConfettiContainer");
        if (oldConf != null) Destroy(oldConf.gameObject);

        GameObject confObj = new GameObject("ConfettiContainer");
        confObj.transform.SetParent(canvasTr, false);
        confettiContainer = confObj.AddComponent<RectTransform>();
        confettiContainer.anchorMin = Vector2.zero;
        confettiContainer.anchorMax = Vector2.one;
        confettiContainer.sizeDelta = Vector2.zero;

        Transform oldPraise = canvasTr.Find("PraiseFloatingBanner");
        if (oldPraise != null) Destroy(oldPraise.gameObject);

        praiseFloatingObj = new GameObject("PraiseFloatingBanner");
        praiseFloatingObj.transform.SetParent(canvasTr, false);
        Image bg = praiseFloatingObj.AddComponent<Image>();
        bg.sprite = roundedCardSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1.0f, 0.92f, 0.28f);
        Shadow sh = praiseFloatingObj.AddComponent<Shadow>();
        sh.effectColor = new Color(0.25f, 0.15f, 0.02f, 0.25f);
        sh.effectDistance = new Vector2(0f, -10f);

        RectTransform rt = praiseFloatingObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(580f, 150f);

        GameObject txtObj = new GameObject("PraiseText");
        txtObj.transform.SetParent(praiseFloatingObj.transform, false);
        praiseFloatingText = txtObj.AddComponent<TextMeshProUGUI>();
        StyleBoldTMP(praiseFloatingText, 54f, new Color(0.14f, 0.08f, 0.02f));
        RectTransform tRt = txtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        praiseFloatingObj.SetActive(false);
    }

    void BuildPauseModal(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("PauseModal");
        if (existing != null) Destroy(existing.gameObject);

        pauseModalObj = new GameObject("PauseModal");
        pauseModalObj.transform.SetParent(canvasTr, false);

        Image bg = pauseModalObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.16f, 0.65f);
        RectTransform rt = pauseModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject card = CreateYellowSunburstModalCard(pauseModalObj.transform, "PauseCard", new Vector2(820f, 680f));

        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI tt = tObj.AddComponent<TextMeshProUGUI>();
        tt.text = "Game Paused";
        StyleBoldTMP(tt, 56f, new Color(0.12f, 0.07f, 0.02f));
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 215f);
        tRt.sizeDelta = new Vector2(680f, 85f);

        GameObject contBtn = CreatePillButton(card.transform, "ContinueBtn", "Continue", new Color(0.14f, 0.52f, 0.98f), Color.white, new Vector2(0f, 55f), new Vector2(660f, 126f), 42f, false);
        contBtn.GetComponent<Button>().onClick.AddListener(() => { TriggerHaptic(false); TogglePause(); });

        GameObject restBtn = CreatePillButton(card.transform, "RestartBtn", "Restart Game", Color.white, new Color(0.14f, 0.52f, 0.98f), new Vector2(0f, -95f), new Vector2(660f, 126f), 42f, false);
        restBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            TriggerHaptic(false);
            TogglePause();
            if (isChallengeMode) StartChallengeGameplay(false);
            else LoadLevel(currentLevel);
        });

        GameObject homeBtn = CreateBorderlessTextButton(card.transform, "HomeBtn", "Main Menu", new Color(0.16f, 0.10f, 0.02f), new Vector2(0f, -230f), new Vector2(420f, 80f), 42f);
        homeBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            TriggerHaptic(false);
            isPaused = false;
            pauseModalObj.SetActive(false);
            OpenHomeScreen();
        });

        pauseModalObj.SetActive(false);
    }

    void BuildSettingsModal(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("SettingsModal");
        if (existing != null) Destroy(existing.gameObject);

        settingsModalObj = new GameObject("SettingsModal");
        settingsModalObj.transform.SetParent(canvasTr, false);

        Image bg = settingsModalObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.16f, 0.65f);
        RectTransform rt = settingsModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject card = CreateYellowSunburstModalCard(settingsModalObj.transform, "SettingsCard", new Vector2(820f, 640f));

        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI tt = tObj.AddComponent<TextMeshProUGUI>();
        tt.text = "Settings";
        StyleBoldTMP(tt, 56f, new Color(0.12f, 0.07f, 0.02f));
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 195f);
        tRt.sizeDelta = new Vector2(680f, 85f);

        GameObject hapBtn = CreatePillButton(card.transform, "HapticsBtn", hapticsEnabled ? "Haptics (Vibration): ON" : "Haptics (Vibration): OFF", new Color(0.14f, 0.76f, 0.38f), Color.white, new Vector2(0f, 35f), new Vector2(660f, 125f), 38f, false);
        hapticsToggleBtnText = hapBtn.GetComponentInChildren<TextMeshProUGUI>();
        hapBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            hapticsEnabled = !hapticsEnabled;
            SaveProgress();
            if (hapticsToggleBtnText != null)
                hapticsToggleBtnText.text = hapticsEnabled ? "Haptics (Vibration): ON" : "Haptics (Vibration): OFF";
            TriggerHaptic(false);
        });

        GameObject homeBtn = CreatePillButton(card.transform, "GoHomeBtn", "Main Menu", Color.white, new Color(0.14f, 0.50f, 0.98f), new Vector2(0f, -110f), new Vector2(660f, 120f), 40f, false);
        homeBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            TriggerHaptic(false);
            settingsModalObj.SetActive(false);
            OpenHomeScreen();
        });

        GameObject closeBtn = CreateBorderlessTextButton(card.transform, "CloseSetBtn", "Close", new Color(0.16f, 0.10f, 0.02f), new Vector2(0f, -235f), new Vector2(400f, 75f), 38f);
        closeBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            TriggerHaptic(false);
            settingsModalObj.SetActive(false);
            if (homeScreenObj == null || !homeScreenObj.activeSelf) isPaused = false;
        });

        settingsModalObj.SetActive(false);
    }

    public void OpenSettingsModal()
    {
        isPaused = true;
        if (settingsModalObj != null)
        {
            settingsModalObj.SetActive(true);
            settingsModalObj.transform.SetAsLastSibling();
        }
    }

    // Point 11: Yellow & Light-Yellow Sunburst Win Celebration Screen
    void BuildWinCelebrationModal(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("WinCelebrationModal");
        if (existing != null) Destroy(existing.gameObject);

        winModalObj = new GameObject("WinCelebrationModal");
        winModalObj.transform.SetParent(canvasTr, false);

        Image bg = winModalObj.AddComponent<Image>();
        bg.sprite = yellowSunburstBackgroundSprite;
        bg.color = Color.white;
        RectTransform rt = winModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject raysObj = new GameObject("RotatingSunburstRays");
        raysObj.transform.SetParent(winModalObj.transform, false);
        Image raysImg = raysObj.AddComponent<Image>();
        raysImg.sprite = sunburstRaysOverlaySprite;
        raysImg.color = new Color(1f, 1f, 0.88f, 0.38f);
        raysImg.raycastTarget = false;
        RectTransform raysRt = raysObj.GetComponent<RectTransform>();
        raysRt.anchoredPosition = new Vector2(0f, 110f);
        raysRt.sizeDelta = new Vector2(2200f, 2200f);
        activeSunburstRays.Add(raysRt);

        GameObject headerObj = new GameObject("HeaderTitle");
        headerObj.transform.SetParent(winModalObj.transform, false);
        winHeaderTitleText = headerObj.AddComponent<TextMeshProUGUI>();
        winHeaderTitleText.text = "Level Completed!";
        StyleBoldTMP(winHeaderTitleText, 68f, new Color(0.14f, 0.08f, 0.02f));
        RectTransform hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchoredPosition = new Vector2(0f, 520f);
        hRt.sizeDelta = new Vector2(960f, 110f);

        GameObject centerCard = CreateCleanWhiteModalCard(winModalObj.transform, "CenterWhiteCard", new Vector2(700f, 580f));
        centerCard.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 95f);

        GameObject emblemBg = new GameObject("EmblemCircle");
        emblemBg.transform.SetParent(centerCard.transform, false);
        Image ebImg = emblemBg.AddComponent<Image>();
        ebImg.sprite = circleSprite;
        ebImg.color = new Color(1.0f, 0.96f, 0.82f);
        RectTransform ebRt = emblemBg.GetComponent<RectTransform>();
        ebRt.anchoredPosition = new Vector2(0f, 95f);
        ebRt.sizeDelta = new Vector2(250f, 250f);

        GameObject winArt = new GameObject("WinnerIconArt");
        winArt.transform.SetParent(emblemBg.transform, false);
        Image waImg = winArt.AddComponent<Image>();
        waImg.sprite = winnerIconSprite;
        waImg.color = Color.white;
        waImg.preserveAspect = true;
        RectTransform waRt = winArt.GetComponent<RectTransform>();
        waRt.sizeDelta = new Vector2(175f, 175f);

        GameObject prObj = new GameObject("CardPraiseText");
        prObj.transform.SetParent(centerCard.transform, false);
        winCardPraiseText = prObj.AddComponent<TextMeshProUGUI>();
        StyleBoldTMP(winCardPraiseText, 58f, new Color(0.05f, 0.09f, 0.18f));
        RectTransform prRt = prObj.GetComponent<RectTransform>();
        prRt.anchoredPosition = new Vector2(0f, -85f);
        prRt.sizeDelta = new Vector2(620f, 85f);

        GameObject coinRewardPill = new GameObject("CoinRewardPill");
        coinRewardPill.transform.SetParent(centerCard.transform, false);
        Image crBg = coinRewardPill.AddComponent<Image>();
        crBg.sprite = pillSprite;
        crBg.type = Image.Type.Sliced;
        crBg.color = new Color(1.0f, 0.95f, 0.78f);
        RectTransform crRt = coinRewardPill.GetComponent<RectTransform>();
        crRt.anchoredPosition = new Vector2(0f, -195f);
        crRt.sizeDelta = new Vector2(460f, 84f);

        GameObject crTxtObj = new GameObject("CoinRewardText");
        crTxtObj.transform.SetParent(coinRewardPill.transform, false);
        winCoinRewardText = crTxtObj.AddComponent<TextMeshProUGUI>();
        StyleBoldTMP(winCoinRewardText, 38f, new Color(0.78f, 0.46f, 0.02f));
        RectTransform crtRt = crTxtObj.GetComponent<RectTransform>();
        crtRt.anchorMin = Vector2.zero;
        crtRt.anchorMax = Vector2.one;
        crtRt.sizeDelta = Vector2.zero;

        GameObject nextBtnObj = CreatePillButton(winModalObj.transform, "WinNextBtn", "Next Game", new Color(0.14f, 0.52f, 0.98f), Color.white, new Vector2(0f, -385f), new Vector2(680f, 145f), 44f, false);
        winNextBtnText = nextBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        nextBtnObj.GetComponent<Button>().onClick.AddListener(() =>
        {
            TriggerHaptic(false);
            winModalObj.SetActive(false);
            if (isChallengeMode) OpenDailyChallengeEntryModal();
            else NextLevel();
        });

        GameObject homeBtnObj = CreateBorderlessTextButton(winModalObj.transform, "WinHomeBtn", "Main Menu", new Color(0.14f, 0.08f, 0.02f), new Vector2(0f, -525f), new Vector2(340f, 80f), 42f);
        homeBtnObj.GetComponent<Button>().onClick.AddListener(() =>
        {
            TriggerHaptic(false);
            winModalObj.SetActive(false);
            if (!isChallengeMode)
            {
                currentLevel++;
                SaveProgress();
            }
            isChallengeMode = false;
            OpenHomeScreen();
        });

        winModalObj.SetActive(false);
    }

    void BuildOutOfLivesModal(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("GameOverModal");
        if (existing != null) Destroy(existing.gameObject);

        gameOverModalObj = new GameObject("GameOverModal");
        gameOverModalObj.transform.SetParent(canvasTr, false);

        Image bg = gameOverModalObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.16f, 0.65f);
        RectTransform rt = gameOverModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject card = CreateYellowSunburstModalCard(gameOverModalObj.transform, "OutOfLivesCard", new Vector2(840f, 840f));

        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI tt = tObj.AddComponent<TextMeshProUGUI>();
        tt.text = "Out of Lives!";
        StyleBoldTMP(tt, 58f, new Color(0.12f, 0.06f, 0.02f));
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 285f);
        tRt.sizeDelta = new Vector2(680f, 90f);

        GameObject heartCircle = new GameObject("HeartPlate");
        heartCircle.transform.SetParent(card.transform, false);
        Image hcImg = heartCircle.AddComponent<Image>();
        hcImg.sprite = circleSprite;
        hcImg.color = Color.white;
        RectTransform hcRt = heartCircle.GetComponent<RectTransform>();
        hcRt.anchoredPosition = new Vector2(0f, 65f);
        hcRt.sizeDelta = new Vector2(300f, 300f);

        GameObject bigBrokenHeart = new GameObject("BigBrokenHeart3D");
        bigBrokenHeart.transform.SetParent(heartCircle.transform, false);
        Image bhImg = bigBrokenHeart.AddComponent<Image>();
        bhImg.sprite = brokenHeart3DSprite;
        bhImg.color = Color.white;
        bhImg.preserveAspect = true;
        RectTransform bhRt = bigBrokenHeart.GetComponent<RectTransform>();
        bhRt.sizeDelta = new Vector2(180f, 180f);

        GameObject plusBadge = new GameObject("PlusOneBadge");
        plusBadge.transform.SetParent(heartCircle.transform, false);
        Image pbImg = plusBadge.AddComponent<Image>();
        pbImg.sprite = circleSprite;
        pbImg.color = new Color(0.16f, 0.52f, 0.98f);
        RectTransform pbRt = plusBadge.GetComponent<RectTransform>();
        pbRt.anchoredPosition = new Vector2(96f, 96f);
        pbRt.sizeDelta = new Vector2(92f, 92f);

        GameObject pbTxtObj = new GameObject("PlusOneText");
        pbTxtObj.transform.SetParent(plusBadge.transform, false);
        TextMeshProUGUI pbTmp = pbTxtObj.AddComponent<TextMeshProUGUI>();
        pbTmp.text = "+1";
        StyleBoldTMP(pbTmp, 38f, Color.white);
        pbTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(92f, 92f);

        GameObject reviveAdBtn = CreatePillButton(card.transform, "GetMoreLivesBtn", "Get More Lives", new Color(0.14f, 0.78f, 0.36f), Color.white, new Vector2(0f, -165f), new Vector2(700f, 130f), 44f, true);
        reviveAdBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            PlaySimulatedVideoAd(() =>
            {
                gameOverModalObj.SetActive(false);
                isPaused = false;
                lives = 1;
                if (isTimerActive && timeRemaining <= 5f) timeRemaining = 60f;
                UpdateHeartsUI();
            });
        });

        GameObject retryBtn = CreateBorderlessTextButton(card.transform, "RestartGameTextBtn", "Restart Game", new Color(0.14f, 0.08f, 0.02f), new Vector2(0f, -295f), new Vector2(480f, 80f), 42f);
        retryBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            gameOverModalObj.SetActive(false);
            isPaused = false;
            if (isChallengeMode) StartChallengeGameplay(false);
            else LoadLevel(currentLevel);
        });

        gameOverModalObj.SetActive(false);
    }

    // Point 11: Yellow Sunburst "Get a Hint" Popup
    void BuildGetHintAdModal(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("GetHintModal");
        if (existing != null) Destroy(existing.gameObject);

        hintAdModalObj = new GameObject("GetHintModal");
        hintAdModalObj.transform.SetParent(canvasTr, false);

        Image bg = hintAdModalObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.16f, 0.65f);
        RectTransform rt = hintAdModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject card = CreateYellowSunburstModalCard(hintAdModalObj.transform, "HintCard", new Vector2(820f, 780f));

        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI tt = tObj.AddComponent<TextMeshProUGUI>();
        tt.text = "Get a Hint";
        StyleBoldTMP(tt, 56f, new Color(0.12f, 0.07f, 0.02f));
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 260f);
        tRt.sizeDelta = new Vector2(640f, 85f);

        GameObject bulbIconObj = new GameObject("BulbPreview");
        bulbIconObj.transform.SetParent(card.transform, false);
        Image bImg = bulbIconObj.AddComponent<Image>();
        bImg.sprite = CreateArrowPuzzleHintButtonSprite(256);
        RectTransform bRt = bulbIconObj.GetComponent<RectTransform>();
        bRt.anchoredPosition = new Vector2(0f, 85f);
        bRt.sizeDelta = new Vector2(210f, 210f);

        GameObject adBtn = CreatePillButton(card.transform, "WatchAdHintBtn", "Get x1", new Color(0.18f, 0.78f, 0.18f), Color.white, new Vector2(0f, -85f), new Vector2(660f, 125f), 44f, true);
        adBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            PlaySimulatedVideoAd(() =>
            {
                remainingHints += 1;
                SaveProgress();
                UpdateCoinAndHintUI();
                hintAdModalObj.SetActive(false);
                isPaused = false;
                GiveHint();
            });
        });

        GameObject coinBtn = CreatePillButton(card.transform, "CoinHintBtn", "Get x3 (500 Coins)", Color.white, new Color(0.12f, 0.46f, 0.96f), new Vector2(0f, -225f), new Vector2(660f, 120f), 40f, false);
        coinBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (playerCoins >= 500)
            {
                playerCoins -= 500;
                remainingHints += 3;
                SaveProgress();
                UpdateCoinAndHintUI();
                hintAdModalObj.SetActive(false);
                isPaused = false;
                GiveHint();
            }
        });

        GameObject closeBtn = CreateBorderlessTextButton(card.transform, "CloseHintBtn", "Close", new Color(0.16f, 0.10f, 0.02f), new Vector2(0f, -330f), new Vector2(380f, 75f), 38f);
        closeBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            hintAdModalObj.SetActive(false);
            isPaused = false;
        });

        hintAdModalObj.SetActive(false);
    }

    // Point 6 & 11: Yellow Sunburst "Get a Grid Guide" Popup (matching 80789f93...jpg!)
    void BuildGetGridGuideAdModal(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("GetGridGuideModal");
        if (existing != null) Destroy(existing.gameObject);

        gridGuideAdModalObj = new GameObject("GetGridGuideModal");
        gridGuideAdModalObj.transform.SetParent(canvasTr, false);

        Image bg = gridGuideAdModalObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.16f, 0.65f);
        RectTransform rt = gridGuideAdModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject card = CreateYellowSunburstModalCard(gridGuideAdModalObj.transform, "GridGuideCard", new Vector2(820f, 780f));

        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI tt = tObj.AddComponent<TextMeshProUGUI>();
        tt.text = "Get a Grid Guide";
        StyleBoldTMP(tt, 54f, new Color(0.12f, 0.07f, 0.02f));
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 260f);
        tRt.sizeDelta = new Vector2(720f, 85f);

        GameObject rulerObj = new GameObject("RulerPreview");
        rulerObj.transform.SetParent(card.transform, false);
        Image rImg = rulerObj.AddComponent<Image>();
        rImg.sprite = rulerBigIconSprite;
        rImg.preserveAspect = true;
        RectTransform rRt = rulerObj.GetComponent<RectTransform>();
        rRt.anchoredPosition = new Vector2(0f, 85f);
        rRt.sizeDelta = new Vector2(220f, 220f);

        GameObject adBtn = CreatePillButton(card.transform, "WatchAdGridBtn", "Get x1", new Color(0.18f, 0.78f, 0.18f), Color.white, new Vector2(0f, -85f), new Vector2(660f, 125f), 44f, true);
        adBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            PlaySimulatedVideoAd(() =>
            {
                remainingGridGuides += 1;
                SaveProgress();
                UpdateCoinAndHintUI();
                gridGuideAdModalObj.SetActive(false);
                isPaused = false;
                UseGridGuide();
            });
        });

        GameObject coinBtn = CreatePillButton(card.transform, "CoinGridBtn", "Get x5 (150 Coins)", Color.white, new Color(0.12f, 0.46f, 0.96f), new Vector2(0f, -225f), new Vector2(660f, 120f), 40f, false);
        coinBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (playerCoins >= 150)
            {
                playerCoins -= 150;
                remainingGridGuides += 5;
                SaveProgress();
                UpdateCoinAndHintUI();
                gridGuideAdModalObj.SetActive(false);
                isPaused = false;
                UseGridGuide();
            }
        });

        GameObject closeBtn = CreateBorderlessTextButton(card.transform, "CloseGridBtn", "Close", new Color(0.16f, 0.10f, 0.02f), new Vector2(0f, -330f), new Vector2(380f, 75f), 38f);
        closeBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            gridGuideAdModalObj.SetActive(false);
            isPaused = false;
        });

        gridGuideAdModalObj.SetActive(false);
    }

    // Point 9: 50% Timer Completed Popup -> Watch Video Ad to Remove Timer Completely!
    void BuildHalfTimeRemoveTimerModal(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("HalfTimeModal");
        if (existing != null) Destroy(existing.gameObject);

        halfTimeAdModalObj = new GameObject("HalfTimeModal");
        halfTimeAdModalObj.transform.SetParent(canvasTr, false);

        Image bg = halfTimeAdModalObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.16f, 0.65f);
        RectTransform rt = halfTimeAdModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject card = CreateYellowSunburstModalCard(halfTimeAdModalObj.transform, "HalfTimeCard", new Vector2(840f, 680f));

        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI tt = tObj.AddComponent<TextMeshProUGUI>();
        tt.text = "50% Time Left!";
        StyleBoldTMP(tt, 56f, new Color(0.12f, 0.07f, 0.02f));
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 200f);
        tRt.sizeDelta = new Vector2(720f, 85f);

        GameObject subObj = new GameObject("Sub");
        subObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI st = subObj.AddComponent<TextMeshProUGUI>();
        st.text = "Watch a short video ad to remove\nthe timer for this level!";
        StyleBoldTMP(st, 34f, new Color(0.24f, 0.16f, 0.04f));
        RectTransform sRt = subObj.GetComponent<RectTransform>();
        sRt.anchoredPosition = new Vector2(0f, 75f);
        sRt.sizeDelta = new Vector2(740f, 100f);

        GameObject adBtn = CreatePillButton(card.transform, "RemoveTimerAdBtn", "Remove Timer", new Color(0.18f, 0.78f, 0.18f), Color.white, new Vector2(0f, -80f), new Vector2(680f, 128f), 42f, true);
        adBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            PlaySimulatedVideoAd(() =>
            {
                isTimerActive = false;
                if (timerPillObj != null) timerPillObj.SetActive(false);
                halfTimeAdModalObj.SetActive(false);
                isPaused = false;
            });
        });

        GameObject contBtn = CreateBorderlessTextButton(card.transform, "KeepTimerBtn", "Continue With Timer", new Color(0.16f, 0.10f, 0.02f), new Vector2(0f, -220f), new Vector2(520f, 80f), 38f);
        contBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            halfTimeAdModalObj.SetActive(false);
            isPaused = false;
        });

        halfTimeAdModalObj.SetActive(false);
    }

    // Point 10: 50% Level Completed Milestone Popup!
    void BuildHalfLevelMilestoneModal(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("HalfLevelMilestoneModal");
        if (existing != null) Destroy(existing.gameObject);

        halfLevelMilestoneModalObj = new GameObject("HalfLevelMilestoneModal");
        halfLevelMilestoneModalObj.transform.SetParent(canvasTr, false);

        Image bg = halfLevelMilestoneModalObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.16f, 0.55f);
        RectTransform rt = halfLevelMilestoneModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject card = CreateYellowSunburstModalCard(halfLevelMilestoneModalObj.transform, "MilestoneCard", new Vector2(800f, 580f));

        GameObject icObj = new GameObject("CrownIcon");
        icObj.transform.SetParent(card.transform, false);
        Image icImg = icObj.AddComponent<Image>();
        icImg.sprite = winnerIconSprite;
        icImg.preserveAspect = true;
        RectTransform icRt = icObj.GetComponent<RectTransform>();
        icRt.anchoredPosition = new Vector2(0f, 130f);
        icRt.sizeDelta = new Vector2(150f, 150f);

        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI tt = tObj.AddComponent<TextMeshProUGUI>();
        tt.text = "50% Completed!";
        StyleBoldTMP(tt, 54f, new Color(0.12f, 0.07f, 0.02f));
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, -10f);
        tRt.sizeDelta = new Vector2(720f, 80f);

        GameObject contBtn = CreatePillButton(card.transform, "KeepGoingBtn", "Keep Going!", new Color(0.14f, 0.52f, 0.98f), Color.white, new Vector2(0f, -155f), new Vector2(560f, 120f), 42f, false);
        contBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            TriggerHaptic(false);
            halfLevelMilestoneModalObj.SetActive(false);
            isPaused = false;
        });

        halfLevelMilestoneModalObj.SetActive(false);
    }

    // Point 2: Home Screen Layout Order:
    // Top Center Coins Wallet -> Top/Bottom Corner Settings Button -> "Snake Puzzle" Heading -> Center 2 Cards -> Continue Game -> Restart Game!
    void BuildHomeScreen(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("HomeScreen");
        if (existing != null) Destroy(existing.gameObject);

        homeScreenObj = new GameObject("HomeScreen");
        homeScreenObj.transform.SetParent(canvasTr, false);

        Image bg = homeScreenObj.AddComponent<Image>();
        bg.color = new Color(0.965f, 0.978f, 0.996f, 1.0f);
        RectTransform rt = homeScreenObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        // 1. Top Center Coins Wallet Pill
        GameObject walletObj = new GameObject("HomeCoinPill");
        walletObj.transform.SetParent(homeScreenObj.transform, false);
        Image wBg = walletObj.AddComponent<Image>();
        wBg.sprite = pillSprite;
        wBg.type = Image.Type.Sliced;
        wBg.color = Color.white;
        Shadow wSh = walletObj.AddComponent<Shadow>();
        wSh.effectColor = new Color(0.05f, 0.12f, 0.28f, 0.12f);
        wSh.effectDistance = new Vector2(0f, -6f);
        RectTransform wRt = walletObj.GetComponent<RectTransform>();
        wRt.anchorMin = new Vector2(0.5f, 1f);
        wRt.anchorMax = new Vector2(0.5f, 1f);
        wRt.anchoredPosition = new Vector2(0f, -125f);
        wRt.sizeDelta = new Vector2(310f, 92f);

        GameObject cIcon = new GameObject("CoinIcon");
        cIcon.transform.SetParent(walletObj.transform, false);
        Image ciImg = cIcon.AddComponent<Image>();
        ciImg.sprite = goldCoinSprite;
        RectTransform ciRt = cIcon.GetComponent<RectTransform>();
        ciRt.anchoredPosition = new Vector2(-95f, 0f);
        ciRt.sizeDelta = new Vector2(62f, 62f);

        GameObject cTxt = new GameObject("CoinText");
        cTxt.transform.SetParent(walletObj.transform, false);
        homeCoinText = cTxt.AddComponent<TextMeshProUGUI>();
        StyleBoldTMP(homeCoinText, 44f, new Color(0.05f, 0.08f, 0.15f));
        RectTransform ctRt = cTxt.GetComponent<RectTransform>();
        ctRt.anchoredPosition = Vector2.zero;
        ctRt.sizeDelta = new Vector2(160f, 80f);

        GameObject plusCircle = new GameObject("PlusBadge");
        plusCircle.transform.SetParent(walletObj.transform, false);
        Image pcImg = plusCircle.AddComponent<Image>();
        pcImg.sprite = circleSprite;
        pcImg.color = new Color(0.14f, 0.52f, 0.98f);
        RectTransform pcRt = plusCircle.GetComponent<RectTransform>();
        pcRt.anchoredPosition = new Vector2(102f, 0f);
        pcRt.sizeDelta = new Vector2(58f, 58f);

        GameObject pcTxt = new GameObject("PlusText");
        pcTxt.transform.SetParent(plusCircle.transform, false);
        TextMeshProUGUI pct = pcTxt.AddComponent<TextMeshProUGUI>();
        pct.text = "+";
        StyleBoldTMP(pct, 38f, Color.white);
        pcTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(58f, 58f);

        // Top-Right & Bottom-Right Settings Button on Home Page
        GameObject topSetBtn = new GameObject("HomeTopSettingsBtn");
        topSetBtn.transform.SetParent(homeScreenObj.transform, false);
        Button tsB = topSetBtn.AddComponent<Button>();
        Image tsI = topSetBtn.AddComponent<Image>();
        tsI.sprite = gearIconSprite;
        tsI.preserveAspect = true;
        RectTransform tsRt = topSetBtn.GetComponent<RectTransform>();
        tsRt.anchorMin = new Vector2(1f, 1f);
        tsRt.anchorMax = new Vector2(1f, 1f);
        tsRt.anchoredPosition = new Vector2(-92f, -125f);
        tsRt.sizeDelta = new Vector2(84f, 84f);
        tsB.onClick.AddListener(() => { TriggerHaptic(false); OpenSettingsModal(); });

        GameObject botSetBtn = new GameObject("HomeBotSettingsBtn");
        botSetBtn.transform.SetParent(homeScreenObj.transform, false);
        Button bsB = botSetBtn.AddComponent<Button>();
        Image bsI = botSetBtn.AddComponent<Image>();
        bsI.sprite = gearIconSprite;
        bsI.preserveAspect = true;
        RectTransform bsRt = botSetBtn.GetComponent<RectTransform>();
        bsRt.anchorMin = new Vector2(1f, 0f);
        bsRt.anchorMax = new Vector2(1f, 0f);
        bsRt.anchoredPosition = new Vector2(-92f, 125f);
        bsRt.sizeDelta = new Vector2(84f, 84f);
        bsB.onClick.AddListener(() => { TriggerHaptic(false); OpenSettingsModal(); });

        // 2. Below Coins Wallet: "Snake Puzzle" Main Heading!
        GameObject titleObj = new GameObject("GameTitle");
        titleObj.transform.SetParent(homeScreenObj.transform, false);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Snake Puzzle";
        StyleBoldTMP(titleTmp, 80f, new Color(0.10f, 0.16f, 0.26f));
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 560f);
        titleRt.sizeDelta = new Vector2(900f, 115f);

        // 3. Center: Two Feature Cards (Daily Challenge & Play With Friends)
        System.DateTime today = System.DateTime.Now;
        string realDateStr = today.ToString("MMMM d");

        GameObject leftCard = CreateHomeFeatureCard(
            homeScreenObj.transform,
            "DailyChallengeCard",
            new Vector2(-245f, 120f),
            new Vector2(450f, 560f),
            new Color(0.36f, 0.72f, 0.99f),
            new Color(0.06f, 0.48f, 0.96f),
            calendarBadgeSprite,
            "DAILY CHALLENGE",
            realDateStr,
            "Play"
        );
        leftCard.GetComponent<Button>().onClick.AddListener(() => { TriggerHaptic(false); OpenDailyChallengeEntryModal(); });

        GameObject rightCard = CreateHomeFeatureCard(
            homeScreenObj.transform,
            "MultiplayerFriendsCard",
            new Vector2(245f, 120f),
            new Vector2(450f, 560f),
            new Color(0.99f, 0.68f, 0.28f),
            new Color(0.86f, 0.38f, 0.06f),
            friendsTrophySprite,
            "ARENA CLASH",
            "Play With Friends",
            "Play"
        );
        rightCard.GetComponent<Button>().onClick.AddListener(() => { TriggerHaptic(false); OpenMultiplayerComingSoonModal(); });

        // 4. Below Cards: Continue Game & Restart Game Buttons
        GameObject contBtn = CreatePillButton(homeScreenObj.transform, "ContinueGameBtn", $"Continue Game\n<size=34>Level {currentLevel}</size>", new Color(0.18f, 0.56f, 0.98f), Color.white, new Vector2(0f, -330f), new Vector2(780f, 155f), 44f, false);
        homeContinueBtnText = contBtn.GetComponentInChildren<TextMeshProUGUI>();
        contBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            TriggerHaptic(false);
            homeScreenObj.SetActive(false);
            isChallengeMode = false;
            isPaused = false;
            LoadLevel(currentLevel);
        });

        GameObject restGameBtn = CreatePillButton(homeScreenObj.transform, "RestartGameBtn", "Restart Game", Color.white, new Color(0.18f, 0.56f, 0.98f), new Vector2(0f, -510f), new Vector2(780f, 130f), 42f, false);
        restGameBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            TriggerHaptic(false);
            homeScreenObj.SetActive(false);
            isChallengeMode = false;
            isPaused = false;
            LoadLevel(currentLevel);
        });

        homeScreenObj.SetActive(false);
    }

    GameObject CreateHomeFeatureCard(Transform parent, string name, Vector2 pos, Vector2 size, Color topCol, Color botCol, Sprite iconSp, string subtitle, string mainTitle, string btnText)
    {
        GameObject cardObj = new GameObject(name);
        cardObj.transform.SetParent(parent, false);
        cardObj.AddComponent<Button>();
        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.sprite = CreateVerticalGradientCardSprite(256, 320, 44f, topCol, botCol);
        cardImg.color = Color.white;

        Shadow sh = cardObj.AddComponent<Shadow>();
        sh.effectColor = new Color(0.04f, 0.10f, 0.24f, 0.18f);
        sh.effectDistance = new Vector2(0f, -12f);

        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject icObj = new GameObject("CardIcon");
        icObj.transform.SetParent(cardObj.transform, false);
        Image icImg = icObj.AddComponent<Image>();
        icImg.sprite = iconSp;
        icImg.preserveAspect = true;
        RectTransform icRt = icObj.GetComponent<RectTransform>();
        icRt.anchoredPosition = new Vector2(0f, 135f);
        icRt.sizeDelta = new Vector2(130f, 130f);

        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(cardObj.transform, false);
        TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = subtitle;
        StyleBoldTMP(subTmp, 26f, new Color(1f, 1f, 1f, 0.88f));
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, 20f);
        subRt.sizeDelta = new Vector2(410f, 45f);

        GameObject mainObj = new GameObject("MainTitle");
        mainObj.transform.SetParent(cardObj.transform, false);
        TextMeshProUGUI mainTmp = mainObj.AddComponent<TextMeshProUGUI>();
        mainTmp.text = mainTitle;
        StyleBoldTMP(mainTmp, 42f, Color.white);
        RectTransform mainRt = mainObj.GetComponent<RectTransform>();
        mainRt.anchoredPosition = new Vector2(0f, -45f);
        mainRt.sizeDelta = new Vector2(420f, 65f);

        GameObject playPill = new GameObject("PlayPill");
        playPill.transform.SetParent(cardObj.transform, false);
        Image ppImg = playPill.AddComponent<Image>();
        ppImg.sprite = pillSprite;
        ppImg.type = Image.Type.Sliced;
        ppImg.color = new Color(1f, 1f, 1f, 0.28f);
        RectTransform ppRt = playPill.GetComponent<RectTransform>();
        ppRt.anchoredPosition = new Vector2(0f, -165f);
        ppRt.sizeDelta = new Vector2(260f, 86f);

        GameObject ppTxt = new GameObject("PlayText");
        ppTxt.transform.SetParent(playPill.transform, false);
        TextMeshProUGUI ppt = ppTxt.AddComponent<TextMeshProUGUI>();
        ppt.text = btnText;
        StyleBoldTMP(ppt, 38f, Color.white);
        RectTransform pptRt = ppTxt.GetComponent<RectTransform>();
        pptRt.anchorMin = Vector2.zero;
        pptRt.anchorMax = Vector2.one;
        pptRt.sizeDelta = Vector2.zero;

        return cardObj;
    }

    // Point 8 & 11: Simple Yellow Sunburst "Challenge" Popup (500 Coins OR Watch Video Ad)
    // Plus: If already unlocked (`isCurrentChallengeUnlocked == true`), clicking Daily Challenge resumes immediately without asking for coins/ad again!
    void BuildDailyChallengeEntryModal(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("DailyChallengeEntryModal");
        if (existing != null) Destroy(existing.gameObject);

        challengeEntryModalObj = new GameObject("DailyChallengeEntryModal");
        challengeEntryModalObj.transform.SetParent(canvasTr, false);

        Image bg = challengeEntryModalObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.18f, 0.68f);
        RectTransform rt = challengeEntryModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject card = CreateYellowSunburstModalCard(challengeEntryModalObj.transform, "ChallengeEntryCard", new Vector2(820f, 760f));

        GameObject icObj = new GameObject("CalIcon");
        icObj.transform.SetParent(card.transform, false);
        Image icImg = icObj.AddComponent<Image>();
        icImg.sprite = calendarBadgeSprite;
        RectTransform icRt = icObj.GetComponent<RectTransform>();
        icRt.anchoredPosition = new Vector2(0f, 220f);
        icRt.sizeDelta = new Vector2(140f, 140f);

        // Simple Title: "Challenge" (Point 8)
        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI tt = tObj.AddComponent<TextMeshProUGUI>();
        tt.text = "Challenge";
        StyleBoldTMP(tt, 60f, new Color(0.12f, 0.07f, 0.02f));
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 80f);
        tRt.sizeDelta = new Vector2(700f, 85f);

        GameObject coinBtn = CreatePillButton(card.transform, "Play500CoinsBtn", "Pay 500 Coins", new Color(0.14f, 0.50f, 0.98f), Color.white, new Vector2(0f, -65f), new Vector2(680f, 126f), 42f, false);
        challengeEntryCoinText = coinBtn.GetComponentInChildren<TextMeshProUGUI>();
        coinBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (playerCoins >= 500)
            {
                playerCoins -= 500;
                isCurrentChallengeUnlocked = true;
                challengeRunCount++;
                SaveProgress();
                UpdateCoinAndHintUI();
                challengeEntryModalObj.SetActive(false);
                if (homeScreenObj != null) homeScreenObj.SetActive(false);
                StartChallengeGameplay(false);
            }
            else
            {
                if (challengeEntryCoinText != null)
                    challengeEntryCoinText.text = $"Need 500 Coins ({playerCoins})";
            }
        });

        GameObject adBtn = CreatePillButton(card.transform, "PlayFreeAdBtn", "Watch Video Ad", new Color(0.16f, 0.78f, 0.22f), Color.white, new Vector2(0f, -215f), new Vector2(680f, 126f), 42f, true);
        adBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            PlaySimulatedVideoAd(() =>
            {
                isCurrentChallengeUnlocked = true;
                challengeRunCount++;
                SaveProgress();
                challengeEntryModalObj.SetActive(false);
                if (homeScreenObj != null) homeScreenObj.SetActive(false);
                StartChallengeGameplay(false);
            });
        });

        GameObject closeBtn = CreateBorderlessTextButton(card.transform, "CloseBtn", "Back", new Color(0.16f, 0.10f, 0.02f), new Vector2(0f, -330f), new Vector2(380f, 75f), 38f);
        closeBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            challengeEntryModalObj.SetActive(false);
        });

        challengeEntryModalObj.SetActive(false);
    }

    public void OpenDailyChallengeEntryModal()
    {
        // Point 8: If the user already paid 500 coins or watched an Ad and hasn't finished that challenge yet,
        // resume it immediately without asking for coins or an Ad again!
        if (isCurrentChallengeUnlocked)
        {
            if (homeScreenObj != null) homeScreenObj.SetActive(false);
            if (challengeEntryModalObj != null) challengeEntryModalObj.SetActive(false);
            StartChallengeGameplay(false);
            return;
        }

        isPaused = true;
        if (challengeEntryCoinText != null)
            challengeEntryCoinText.text = "Pay 500 Coins";
        if (challengeEntryModalObj != null)
        {
            challengeEntryModalObj.SetActive(true);
            challengeEntryModalObj.transform.SetAsLastSibling();
        }
    }

    void BuildMultiplayerComingSoonModal(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("MultiplayerModal");
        if (existing != null) Destroy(existing.gameObject);

        multiplayerModalObj = new GameObject("MultiplayerModal");
        multiplayerModalObj.transform.SetParent(canvasTr, false);

        Image bg = multiplayerModalObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.04f, 0.16f, 0.72f);
        RectTransform rt = multiplayerModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject card = CreateYellowSunburstModalCard(multiplayerModalObj.transform, "MultiplayerCard", new Vector2(880f, 880f));

        GameObject badge = new GameObject("ComingSoonBadge");
        badge.transform.SetParent(card.transform, false);
        Image bImg = badge.AddComponent<Image>();
        bImg.sprite = pillSprite;
        bImg.type = Image.Type.Sliced;
        bImg.color = new Color(0.14f, 0.08f, 0.02f);
        RectTransform bRt = badge.GetComponent<RectTransform>();
        bRt.anchoredPosition = new Vector2(0f, 330f);
        bRt.sizeDelta = new Vector2(380f, 68f);

        GameObject bTxt = new GameObject("BadgeText");
        bTxt.transform.SetParent(badge.transform, false);
        TextMeshProUGUI bt = bTxt.AddComponent<TextMeshProUGUI>();
        bt.text = "COMING SOON";
        StyleBoldTMP(bt, 30f, new Color(1.0f, 0.88f, 0.25f));
        bTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 68f);

        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(card.transform, false);
        TextMeshProUGUI tt = tObj.AddComponent<TextMeshProUGUI>();
        tt.text = "Play With Friends";
        StyleBoldTMP(tt, 58f, new Color(0.12f, 0.06f, 0.02f));
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 225f);
        tRt.sizeDelta = new Vector2(780f, 85f);

        CreatePillButton(card.transform, "InviteFriendsBtn", "Invite Friends (WhatsApp / Link)\n<size=25>Send Room Link or Friend Request • Coming Soon</size>", new Color(0.14f, 0.74f, 0.36f), Color.white, new Vector2(0f, 45f), new Vector2(740f, 145f), 34f, false);
        CreatePillButton(card.transform, "FindOnlineBtn", "Find Online Opponent (Live 1v1)\n<size=25>Global Real-Time Matchmaking • Coming Soon</size>", new Color(0.14f, 0.50f, 0.98f), Color.white, new Vector2(0f, -130f), new Vector2(740f, 145f), 34f, false);

        GameObject closeBtn = CreateBorderlessTextButton(card.transform, "CloseMpBtn", "Back to Home", new Color(0.14f, 0.08f, 0.02f), new Vector2(0f, -310f), new Vector2(460f, 95f), 40f);
        closeBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            multiplayerModalObj.SetActive(false);
        });

        multiplayerModalObj.SetActive(false);
    }

    public void OpenMultiplayerComingSoonModal()
    {
        if (multiplayerModalObj != null)
        {
            multiplayerModalObj.SetActive(true);
            multiplayerModalObj.transform.SetAsLastSibling();
        }
    }

    // Point 9: Daily Challenge with 150-200 Snakes & 3-Minute Timer (180s)!
    public void StartChallengeGameplay(bool advancePatternSeed)
    {
        ResetLevelTransitionEffects();

        if (advancePatternSeed)
        {
            challengeRunCount++;
            SaveProgress();
        }

        isChallengeMode = true;
        isPaused = false;
        isGridGuideActiveThisLevel = false;
        halfLevelMilestoneShown = false;
        halfTimePopupShown = false;
        ResetCameraZoom();

        if (pauseModalObj != null) pauseModalObj.SetActive(false);
        if (winModalObj != null) winModalObj.SetActive(false);
        if (gameOverModalObj != null) gameOverModalObj.SetActive(false);
        if (praiseFloatingObj != null) praiseFloatingObj.SetActive(false);

        DestroyLevelSnakes();
        if (gridGuideLinesContainer != null) Destroy(gridGuideLinesContainer);

        lives = 3;
        UpdateHeartsUI();
        UpdateCoinAndHintUI();

        System.DateTime now = System.DateTime.Now;
        if (levelText != null) levelText.text = now.ToString("MMMM d");
        if (difficultyText != null) difficultyText.text = "Extreme";

        // 3-Minute Timer (180s) for Daily Challenge!
        totalTimerDuration = 180f;
        timeRemaining = 180f;
        isTimerActive = true;
        if (timerPillObj != null) timerPillObj.SetActive(true);
        UpdateTimerUI();

        int challengeSeed = now.Year * 10000 + now.Month * 100 + now.Day + (Mathf.Max(1, challengeRunCount) * 919);
        GenerateAddictiveLongSpineMaze(100 + challengeRunCount, challengeSeed, true);
        initialSnakeCountInLevel = activeSnakes.Count;
        UpdateSnakeCounter();
    }

    public void OpenHomeScreen()
    {
        isPaused = true;
        UpdateCoinAndHintUI();
        if (homeContinueBtnText != null)
            homeContinueBtnText.text = $"Continue Game\n<size=34>Level {currentLevel}</size>";
        if (homeScreenObj != null)
        {
            homeScreenObj.SetActive(true);
            homeScreenObj.transform.SetAsLastSibling();
        }
    }

    void BuildSimulatedAdOverlay(Transform canvasTr)
    {
        Transform existing = canvasTr.Find("SimulatedAdOverlay");
        if (existing != null) Destroy(existing.gameObject);

        adSimOverlayObj = new GameObject("SimulatedAdOverlay");
        adSimOverlayObj.transform.SetParent(canvasTr, false);
        Image bg = adSimOverlayObj.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.05f, 0.10f, 0.96f);
        RectTransform rt = adSimOverlayObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        GameObject txtObj = new GameObject("AdText");
        txtObj.transform.SetParent(adSimOverlayObj.transform, false);
        adSimTimerText = txtObj.AddComponent<TextMeshProUGUI>();
        StyleBoldTMP(adSimTimerText, 46f, Color.white);
        RectTransform tRt = txtObj.GetComponent<RectTransform>();
        tRt.sizeDelta = new Vector2(800f, 200f);

        adSimOverlayObj.SetActive(false);
    }

    void PlaySimulatedVideoAd(System.Action onRewardGranted)
    {
        StartCoroutine(SimulateVideoAdRoutine(onRewardGranted, levelGeneration));
    }

    IEnumerator SimulateVideoAdRoutine(System.Action onRewardGranted, int generation)
    {
        if (generation != levelGeneration) yield break;

        if (adSimOverlayObj != null)
        {
            adSimOverlayObj.SetActive(true);
            adSimOverlayObj.transform.SetAsLastSibling();
        }

        float remaining = 1.3f;
        while (remaining > 0f)
        {
            if (generation != levelGeneration) yield break;
            if (adSimTimerText != null)
                adSimTimerText.text = $"Video Ad Playing...\n\n<size=34>Reward in {Mathf.CeilToInt(remaining)}s</size>";
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (generation != levelGeneration) yield break;
        if (adSimOverlayObj != null) adSimOverlayObj.SetActive(false);
        onRewardGranted?.Invoke();
    }

    GameObject CreateCleanWhiteModalCard(Transform parent, string name, Vector2 size)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);
        Image img = card.AddComponent<Image>();
        img.sprite = roundedCardSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        Shadow sh = card.AddComponent<Shadow>();
        sh.effectColor = new Color(0.02f, 0.06f, 0.15f, 0.24f);
        sh.effectDistance = new Vector2(0f, -14f);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        return card;
    }

    GameObject CreatePillButton(Transform parent, string name, string label, Color bgCol, Color txtCol, Vector2 pos, Vector2 size, float fontSize, bool showVideoAdIcon)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        btnObj.AddComponent<Button>();
        Image img = btnObj.AddComponent<Image>();
        img.sprite = pillSprite;
        img.type = Image.Type.Sliced;
        img.color = bgCol;

        Shadow sh = btnObj.AddComponent<Shadow>();
        sh.effectColor = new Color(0.05f, 0.15f, 0.35f, 0.22f);
        sh.effectDistance = new Vector2(0f, -8f);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        StyleBoldTMP(tmp, fontSize, txtCol);
        RectTransform tRt = tmp.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;
        tRt.anchoredPosition = showVideoAdIcon ? new Vector2(-28f, 0f) : Vector2.zero;

        if (showVideoAdIcon && videoAdIconSprite != null)
        {
            GameObject iconObj = new GameObject("VideoAdIcon");
            iconObj.transform.SetParent(btnObj.transform, false);
            Image icImg = iconObj.AddComponent<Image>();
            icImg.sprite = videoAdIconSprite;
            icImg.color = txtCol;
            icImg.preserveAspect = true;
            RectTransform icRt = iconObj.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0.5f, 0.5f);
            icRt.anchorMax = new Vector2(0.5f, 0.5f);
            icRt.anchoredPosition = new Vector2(size.x * 0.30f, 0f);
            icRt.sizeDelta = new Vector2(52f, 42f);
        }

        return btnObj;
    }

    GameObject CreateBorderlessTextButton(Transform parent, string name, string label, Color txtCol, Vector2 pos, Vector2 size, float fontSize)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        btnObj.AddComponent<Button>();
        Image img = btnObj.AddComponent<Image>();
        img.color = Color.clear;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        StyleBoldTMP(tmp, fontSize, txtCol);
        RectTransform tRt = tmp.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        return btnObj;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (pauseModalObj != null)
        {
            pauseModalObj.SetActive(isPaused);
            pauseModalObj.transform.SetAsLastSibling();
        }
    }

    void StyleBoldTMP(TextMeshProUGUI tmp, float size, Color col)
    {
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = col;
        tmp.outlineWidth = 0f;
        tmp.extraPadding = true;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
    }

    GameObject EnsureCardBackground(Transform targetText, Transform canvasTransform, string bgName, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sp)
    {
        Transform existing = canvasTransform.Find(bgName);
        GameObject bgObj = existing != null ? existing.gameObject : new GameObject(bgName);
        bgObj.transform.SetParent(canvasTransform, false);
        bgObj.transform.SetSiblingIndex(targetText.GetSiblingIndex());

        Image img = bgObj.GetComponent<Image>();
        if (img == null) img = bgObj.AddComponent<Image>();
        img.sprite = sp;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.925f, 0.942f, 0.968f);

        RectTransform rt = bgObj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return bgObj;
    }

    public bool CheckPathClearAndDistance(SnakeController snake, out int freeSteps)
    {
        return EvaluateEscapeMove(snake, out freeSteps).IsValid;
    }

    public MoveResult EvaluateEscapeMove(SnakeController snake, out int freeSteps)
    {
        freeSteps = 0;
        Vector2Int head = snake.GetGridHead();
        Vector2Int direction = snake.GetGridForwardDir();
        return EscapeRouteGeometry.EvaluateMove(
            snake.gameObject.name,
            new GridCoord(head.x, head.y),
            new GridCoord(direction.x, direction.y),
            currentCols,
            currentRows,
            cell =>
            {
                for (int i = 0; i < activeSnakes.Count; i++)
                {
                    SnakeController other = activeSnakes[i];
                    if (other == null || other == snake || other.isCleared) continue;
                    if (other.gridCells.Contains(new Vector2Int(cell.Col, cell.Row)))
                        return other.gameObject.name;
                }
                return null;
            },
            out freeSteps);
    }

    public void LoadLevel(int level)
    {
        ResetLevelTransitionEffects();

        isChallengeMode = false;
        isPaused = false;
        isGridGuideActiveThisLevel = false;
        halfLevelMilestoneShown = false;
        halfTimePopupShown = false;
        ResetCameraZoom();
        currentLevel = Mathf.Max(1, level);
        SaveProgress();

        if (pauseModalObj != null) pauseModalObj.SetActive(false);
        if (winModalObj != null) winModalObj.SetActive(false);
        if (gameOverModalObj != null) gameOverModalObj.SetActive(false);
        if (praiseFloatingObj != null) praiseFloatingObj.SetActive(false);
        if (gridGuideLinesContainer != null) Destroy(gridGuideLinesContainer);

        DestroyLevelSnakes();

        lives = 3;
        UpdateHeartsUI();
        UpdateCoinAndHintUI();

        if (levelText != null) levelText.text = $"Level {currentLevel}";
        if (difficultyText != null)
        {
            if (currentLevel <= 5) difficultyText.text = "Easy";
            else if (currentLevel <= 10) difficultyText.text = "Hard";
            else difficultyText.text = "Master";
        }

        // Point 9: 3-Minute Timer (180s) after Level 10 (Level 11+)!
        if (currentLevel > 10)
        {
            totalTimerDuration = 180f;
            timeRemaining = 180f;
            isTimerActive = true;
            if (timerPillObj != null) timerPillObj.SetActive(true);
            UpdateTimerUI();
        }
        else
        {
            isTimerActive = false;
            if (timerPillObj != null) timerPillObj.SetActive(false);
        }

        if (lastGeneratedLevel != currentLevel)
        {
            lastGeneratedLevel = currentLevel;
            lockedLevelSeed = currentLevel * 81919 + 53;
        }

        if (currentLevel == 1)
            BuildTutorialLevel1();
        else
            GenerateAddictiveLongSpineMaze(currentLevel, lockedLevelSeed, false);

        initialSnakeCountInLevel = activeSnakes.Count;
        UpdateSnakeCounter();
    }

    void ResetLevelTransitionEffects()
    {
        levelGeneration++;
        if (adSimOverlayObj != null)
            adSimOverlayObj.SetActive(false);
        if (confettiContainer == null) return;

        for (int i = confettiContainer.childCount - 1; i >= 0; i--)
        {
            Transform piece = confettiContainer.GetChild(i);
            piece.gameObject.SetActive(false);
            Destroy(piece.gameObject);
        }
    }

    void DestroyLevelSnakes()
    {
        foreach (SnakeController snake in levelSnakes)
        {
            if (snake == null) continue;
            snake.gameObject.SetActive(false);
            Destroy(snake.gameObject);
        }

        levelSnakes.Clear();
        activeSnakes.Clear();
    }

    void BuildTutorialLevel1()
    {
        currentCols = 5;
        currentRows = 6;
        currentSpacing = 0.85f;
        float snakeWidth = currentSpacing * 0.28f;
        currentOriginOffset = new Vector2((currentCols - 1) * currentSpacing * 0.5f, (currentRows - 1) * currentSpacing * 0.5f + 0.22f);

        bool[,] occupied = new bool[currentCols, currentRows];
        List<Vector2Int> allCells = new List<Vector2Int>();

        SpawnAndRegisterSnake(new List<Vector2Int> {
            new Vector2Int(1, 4), new Vector2Int(1, 3), new Vector2Int(1, 2), new Vector2Int(1, 1)
        }, occupied, allCells, snakeWidth);

        SpawnAndRegisterSnake(new List<Vector2Int> {
            new Vector2Int(2, 4), new Vector2Int(2, 3), new Vector2Int(2, 2), new Vector2Int(2, 1)
        }, occupied, allCells, snakeWidth);

        SpawnAndRegisterSnake(new List<Vector2Int> {
            new Vector2Int(3, 1), new Vector2Int(3, 2), new Vector2Int(3, 3), new Vector2Int(3, 4)
        }, occupied, allCells, snakeWidth);

        BuildDotsUnderSnakesOnly(allCells, currentSpacing, currentOriginOffset);
    }

    // POINTS 1 & 5: ADDICTIVE LONG-BACKBONE + INTERLOCKING SERPENTINE GENERATOR
    // Pass 1 carves LONG SPINE/BACKBONE SNAKES (8 to 18 blocks) that span across the board (just like Arrow Puzzle image (6).jpg!),
    // Pass 2 & 3 wrap medium and compact interlocking snakes around those long backbones so zero space is wasted!
    void GenerateAddictiveLongSpineMaze(int level, int seed, bool isDailyChallenge150Plus)
    {
        Random.InitState(seed);

        int spineMinLen, spineMaxLen, targetSnakeCap;
        int shapeType = 0;

        if (isDailyChallenge150Plus)
        {
            // Daily Challenge: 25x33 Organic Island -> 150 to 200 Snakes with Long Spines!
            currentCols = 25 + Mathf.Clamp(challengeRunCount % 3, 0, 2);
            currentRows = 32 + Mathf.Clamp(challengeRunCount % 3, 0, 2);
            spineMinLen = 7;
            spineMaxLen = 15;
            targetSnakeCap = Random.Range(154, 198);
            shapeType = 5;
        }
        else if (level <= 5)
        {
            // Levels 2 to 5: Easy, fast-paced addictive intro
            currentCols = 4 + level;
            currentRows = 5 + level;
            spineMinLen = 5;
            spineMaxLen = 10;
            targetSnakeCap = 30;
            shapeType = 0;
        }
        else if (level == 6)
        {
            currentCols = 16;
            currentRows = 18;
            spineMinLen = 7;
            spineMaxLen = 16;
            targetSnakeCap = 62;
            shapeType = 1; // Heart Maze
        }
        else
        {
            // Levels 7+: 55 to 95+ Snakes with Long Winding Spines!
            currentCols = Mathf.Clamp(15 + (level - 6) / 2, 16, 20);
            currentRows = Mathf.Clamp(19 + (level - 6) / 2, 20, 25);
            spineMinLen = 8;
            spineMaxLen = 18;
            targetSnakeCap = Mathf.Clamp(56 + (level - 6) * 3, 56, 98);
            int[] patterns = { 5, 1, 2, 3, 4, 0 };
            shapeType = patterns[(level - 7) % patterns.Length];
        }

        currentSpacing = 5.5f / Mathf.Max(currentCols, currentRows - 1);
        float snakeWidth = currentSpacing * 0.28f;
        currentOriginOffset = new Vector2((currentCols - 1) * currentSpacing * 0.5f, (currentRows - 1) * currentSpacing * 0.5f + 0.20f);

        bool[,] unassigned = new bool[currentCols, currentRows];
        for (int x = 0; x < currentCols; x++)
            for (int y = 0; y < currentRows; y++)
                unassigned[x, y] = IsCellInsideShape(x, y, currentCols, currentRows, shapeType, seed);

        int[] dx = { 0, 1, 0, -1 };
        int[] dy = { 1, 0, -1, 0 };

        List<List<Vector2Int>> carvedSnakes = new List<List<Vector2Int>>();
        HashSet<Vector2Int> allVacatedCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> lastSnakeCells = new HashSet<Vector2Int>();

        // PASS 1: Carve Long Backbone/Spine Snakes (8 to 18 blocks) that stretch across the board!
        int safety = 0;
        while (safety < 850 && carvedSnakes.Count < targetSnakeCap)
        {
            safety++;
            bool requireBlocker = (level >= 6 || isDailyChallenge150Plus) && (carvedSnakes.Count >= 1);

            // Alternate between Long Spine Snakes and Medium Interlocking Snakes (exact Arrow Puzzle rhythm!)
            bool buildLongSpine = (carvedSnakes.Count % 3 != 2);
            int minL = buildLongSpine ? spineMinLen : 4;
            int maxL = buildLongSpine ? spineMaxLen : 7;

            List<Vector2Int> snake = CarveNextInterlockingSnake(unassigned, allVacatedCells, lastSnakeCells, currentCols, currentRows, dx, dy, minL, maxL, requireBlocker, buildLongSpine);
            if (snake == null)
                snake = CarveNextInterlockingSnake(unassigned, allVacatedCells, lastSnakeCells, currentCols, currentRows, dx, dy, 3, 6, false, false);
            if (snake == null)
                snake = CarveNextInterlockingSnake(unassigned, allVacatedCells, lastSnakeCells, currentCols, currentRows, dx, dy, 2, 4, false, false);

            if (snake != null)
            {
                carvedSnakes.Add(snake);
                lastSnakeCells.Clear();
                foreach (var c in snake)
                {
                    unassigned[c.x, c.y] = false;
                    allVacatedCells.Add(c);
                    lastSnakeCells.Add(c);
                }
            }
            else break;
        }

        bool[,] occupied = new bool[currentCols, currentRows];
        List<Vector2Int> allSnakeOccupiedCells = new List<Vector2Int>();

        foreach (var path in carvedSnakes)
            SpawnAndRegisterSnake(path, occupied, allSnakeOccupiedCells, snakeWidth);

        BuildDotsUnderSnakesOnly(allSnakeOccupiedCells, currentSpacing, currentOriginOffset);
    }

    List<Vector2Int> CarveNextInterlockingSnake(bool[,] unassigned, HashSet<Vector2Int> allVacated, HashSet<Vector2Int> lastVacated, int cols, int rows, int[] dx, int[] dy, int minLen, int maxLen, bool strictLock, bool preferLongCorridors)
    {
        List<Vector3Int> validHeads = new List<Vector3Int>();

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!unassigned[x, y]) continue;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x - dx[d], ny = y - dy[d];
                    if (nx < 0 || nx >= cols || ny < 0 || ny >= rows || !unassigned[nx, ny]) continue;
                    if (IsRayClearOfUnassigned(unassigned, cols, rows, x, y, dx[d], dy[d]))
                    {
                        if (strictLock && !DoesRayCrossAnyCell(x, y, dx[d], dy[d], cols, rows, allVacated))
                            continue;
                        validHeads.Add(new Vector3Int(x, y, d));
                    }
                }
            }
        }

        if (validHeads.Count == 0) return null;

        for (int i = 0; i < validHeads.Count; i++)
        {
            int sw = Random.Range(i, validHeads.Count);
            Vector3Int tmp = validHeads[i]; validHeads[i] = validHeads[sw]; validHeads[sw] = tmp;
        }

        validHeads.Sort((a, b) =>
        {
            int scoreA = ScoreInterlockHead(a.x, a.y, dx[a.z], dy[a.z], cols, rows, allVacated, lastVacated);
            int scoreB = ScoreInterlockHead(b.x, b.y, dx[b.z], dy[b.z], cols, rows, allVacated, lastVacated);
            return scoreA.CompareTo(scoreB);
        });

        int maxTry = Mathf.Min(validHeads.Count, 45);
        for (int i = 0; i < maxTry; i++)
        {
            Vector3Int h = validHeads[i];
            int hx = h.x, hy = h.y, dir = h.z;
            int nx = hx - dx[dir], ny = hy - dy[dir];

            List<Vector2Int> cells = new List<Vector2Int> { new Vector2Int(hx, hy), new Vector2Int(nx, ny) };
            int targetLen = Random.Range(minLen, maxLen + 1);
            int cx = nx, cy = ny, lastDir = dir, straightRun = 1;
            int desiredRunBeforeTurn = preferLongCorridors ? Random.Range(3, 6) : Random.Range(1, 3);

            for (int step = 2; step < targetLen; step++)
            {
                List<int> dirs = new List<int> { 0, 1, 2, 3 };
                for (int k = 0; k < 4; k++)
                {
                    int sw = Random.Range(k, 4);
                    int t = dirs[k]; dirs[k] = dirs[sw]; dirs[sw] = t;
                }

                dirs.Sort((dA, dB) =>
                {
                    Vector2Int pA = new Vector2Int(cx - dx[dA], cy - dy[dA]);
                    Vector2Int pB = new Vector2Int(cx - dx[dB], cy - dy[dB]);
                    int scA = -CountExposedSides(pA.x, pA.y, cols, rows, unassigned, cells) * 2;
                    int scB = -CountExposedSides(pB.x, pB.y, cols, rows, unassigned, cells) * 2;

                    if (straightRun < desiredRunBeforeTurn)
                    {
                        if (dA == lastDir) scA -= 5;
                        if (dB == lastDir) scB -= 5;
                    }
                    else
                    {
                        if (dA != lastDir) scA -= 6;
                        if (dB != lastDir) scB -= 6;
                    }
                    return scA.CompareTo(scB);
                });

                bool added = false;
                foreach (int nextD in dirs)
                {
                    int bx = cx - dx[nextD], by = cy - dy[nextD];
                    if (bx < 0 || bx >= cols || by < 0 || by >= rows) continue;
                    if (!unassigned[bx, by] || cells.Contains(new Vector2Int(bx, by))) continue;
                    if (IsCellOnRay(hx, hy, dx[dir], dy[dir], bx, by)) continue;

                    cells.Add(new Vector2Int(bx, by));
                    cx = bx; cy = by;
                    if (nextD == lastDir)
                    {
                        straightRun++;
                    }
                    else
                    {
                        straightRun = 1;
                        desiredRunBeforeTurn = preferLongCorridors ? Random.Range(2, 5) : Random.Range(1, 3);
                    }
                    lastDir = nextD;
                    added = true;
                    break;
                }
                if (!added) break;
            }

            if (cells.Count >= minLen) return cells;
        }
        return null;
    }

    bool DoesRayCrossAnyCell(int hx, int hy, int rdx, int rdy, int cols, int rows, HashSet<Vector2Int> targetCells)
    {
        int x = hx + rdx, y = hy + rdy;
        while (x >= 0 && x < cols && y >= 0 && y < rows)
        {
            if (targetCells.Contains(new Vector2Int(x, y))) return true;
            x += rdx; y += rdy;
        }
        return false;
    }

    int ScoreInterlockHead(int hx, int hy, int rdx, int rdy, int cols, int rows, HashSet<Vector2Int> allVacated, HashSet<Vector2Int> lastVacated)
    {
        int score = 0, x = hx + rdx, y = hy + rdy, blockersCrossed = 0;
        bool hitsLast = false;
        while (x >= 0 && x < cols && y >= 0 && y < rows)
        {
            Vector2Int pt = new Vector2Int(x, y);
            if (allVacated.Contains(pt)) blockersCrossed++;
            if (lastVacated.Contains(pt)) hitsLast = true;
            x += rdx; y += rdy;
        }
        if (hitsLast) score -= 45;
        score -= blockersCrossed * 9;
        if (blockersCrossed == 0 && allVacated.Count > 0) score += 120;
        return score;
    }

    int CountExposedSides(int x, int y, int cols, int rows, bool[,] unassigned, List<Vector2Int> currentPath)
    {
        if (x < 0 || x >= cols || y < 0 || y >= rows) return 0;
        int exposed = 0;
        int[] nx = { x + 1, x - 1, x, x };
        int[] ny = { y, y, y + 1, y - 1 };
        for (int i = 0; i < 4; i++)
        {
            if (nx[i] < 0 || nx[i] >= cols || ny[i] < 0 || ny[i] >= rows || !unassigned[nx[i], ny[i]] || currentPath.Contains(new Vector2Int(nx[i], ny[i])))
                exposed++;
        }
        return exposed;
    }

    bool IsRayClearOfUnassigned(bool[,] unassigned, int cols, int rows, int hx, int hy, int rdx, int rdy)
    {
        int x = hx + rdx, y = hy + rdy;
        while (x >= 0 && x < cols && y >= 0 && y < rows)
        {
            if (unassigned[x, y]) return false;
            x += rdx; y += rdy;
        }
        return true;
    }

    void SpawnAndRegisterSnake(List<Vector2Int> chosenSnake, bool[,] occupied, List<Vector2Int> allCells, float snakeWidth)
    {
        foreach (var c in chosenSnake)
        {
            occupied[c.x, c.y] = true;
            allCells.Add(c);
        }

        List<Vector2> worldPts = new List<Vector2>();
        foreach (var c in chosenSnake)
            worldPts.Add(new Vector2(c.x * currentSpacing - currentOriginOffset.x, c.y * currentSpacing - currentOriginOffset.y));

        GameObject sObj = new GameObject($"Snake_{activeSnakes.Count}");
        SnakeController sc = sObj.AddComponent<SnakeController>();
        sc.InitSnake(chosenSnake, worldPts, snakeWidth);
        activeSnakes.Add(sc);
        levelSnakes.Add(sc);
    }

    bool IsCellInsideShape(int x, int y, int cols, int rows, int shape, int seed)
    {
        float nx = ((x / (float)(cols - 1)) * 2f) - 1f;
        float ny = ((y / (float)(rows - 1)) * 2f) - 1f;

        if (shape == 1)
        {
            float hy = ny * 1.12f + 0.16f, hx = nx * 1.12f;
            float a = hx * hx + hy * hy - 1f;
            return (a * a * a - hx * hx * hy * hy * hy) <= 0.14f;
        }
        else if (shape == 2)
        {
            return (Mathf.Abs(nx) + Mathf.Abs(ny)) <= 1.38f;
        }
        else if (shape == 3)
        {
            return (Mathf.Abs(nx) <= 0.56f || Mathf.Abs(ny) <= 0.56f);
        }
        else if (shape == 4)
        {
            float pinch = 0.52f + 0.42f * ny * ny;
            return Mathf.Abs(nx) <= pinch && Mathf.Abs(ny) <= 0.95f;
        }
        else if (shape == 5)
        {
            float angle = Mathf.Atan2(ny, nx);
            float s1 = Mathf.Sin(angle * 3f + seed * 0.13f) * 0.16f;
            float s2 = Mathf.Cos(angle * 5f - seed * 0.29f) * 0.11f;
            float s3 = Mathf.Sin((nx + ny) * 4.5f + seed) * 0.08f;
            float maxR = 0.84f + s1 + s2 + s3;
            float dist = Mathf.Sqrt(nx * nx * 1.08f + ny * ny * 0.92f);
            return dist <= maxR;
        }
        return true;
    }

    bool IsCellOnRay(int hx, int hy, int rdx, int rdy, int tx, int ty)
    {
        int x = hx + rdx, y = hy + rdy;
        while (x >= 0 && x < 45 && y >= 0 && y < 45)
        {
            if (x == tx && y == ty) return true;
            x += rdx; y += rdy;
        }
        return false;
    }

    void BuildDotsUnderSnakesOnly(List<Vector2Int> snakeCells, float spacing, Vector2 originOffset)
    {
        if (dotGridContainer != null) Destroy(dotGridContainer);
        dotGridContainer = new GameObject("DotMatrixGrid");
        gridDots.Clear();

        Sprite dotSp = CreateSmoothCircleSprite(64);
        Color dotCol = new Color(0.75f, 0.80f, 0.86f);

        foreach (Vector2Int cell in snakeCells)
        {
            if (gridDots.ContainsKey(cell)) continue;
            GameObject dot = new GameObject($"Dot_{cell.x}_{cell.y}");
            dot.transform.SetParent(dotGridContainer.transform);
            dot.transform.position = new Vector3(cell.x * spacing - originOffset.x, cell.y * spacing - originOffset.y, 0f);

            SpriteRenderer sr = dot.AddComponent<SpriteRenderer>();
            sr.sprite = dotSp;
            sr.color = dotCol;
            sr.sortingOrder = 1;

            dot.transform.localScale = Vector3.zero;
            gridDots[cell] = dot.transform;
        }
    }

    // Point 6: Draws Fine Grid Guide Lines across the board (like Arrow Puzzle image (6).jpg!)
    public void UseGridGuide()
    {
        if (isPaused) return;
        if (isGridGuideActiveThisLevel) return;

        if (remainingGridGuides <= 0)
        {
            if (gridGuideAdModalObj != null)
            {
                isPaused = true;
                gridGuideAdModalObj.transform.SetAsLastSibling();
                gridGuideAdModalObj.SetActive(true);
            }
            return;
        }

        remainingGridGuides--;
        isGridGuideActiveThisLevel = true;
        SaveProgress();
        UpdateCoinAndHintUI();
        RenderGridGuideLinesOnBoard();
    }

    void RenderGridGuideLinesOnBoard()
    {
        if (gridGuideLinesContainer != null) Destroy(gridGuideLinesContainer);
        gridGuideLinesContainer = new GameObject("GridGuideLinesContainer");

        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        Color gridLineCol = new Color(0.82f, 0.86f, 0.93f, 0.65f);
        float lineThickness = currentSpacing * 0.045f;

        float minX = -currentOriginOffset.x - currentSpacing;
        float maxX = (currentCols - 1) * currentSpacing - currentOriginOffset.x + currentSpacing;
        float minY = -currentOriginOffset.y - currentSpacing;
        float maxY = (currentRows - 1) * currentSpacing - currentOriginOffset.y + currentSpacing;

        for (int x = 0; x < currentCols; x++)
        {
            float wx = x * currentSpacing - currentOriginOffset.x;
            CreateSingleGridLine(gridGuideLinesContainer.transform, new Vector3(wx, minY, 0f), new Vector3(wx, maxY, 0f), lineMat, gridLineCol, lineThickness);
        }

        for (int y = 0; y < currentRows; y++)
        {
            float wy = y * currentSpacing - currentOriginOffset.y;
            CreateSingleGridLine(gridGuideLinesContainer.transform, new Vector3(minX, wy, 0f), new Vector3(maxX, wy, 0f), lineMat, gridLineCol, lineThickness);
        }
    }

    void CreateSingleGridLine(Transform parent, Vector3 start, Vector3 end, Material mat, Color col, float width)
    {
        GameObject go = new GameObject("GridLine");
        go.transform.SetParent(parent, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.useWorldSpace = true;
        lr.sortingOrder = 0;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = col;
        lr.endColor = col;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    public void RevealDotsAlongSnake(List<Vector2> snakeWorldPts)
    {
        StartCoroutine(PopDotsRoutine(snakeWorldPts));
    }

    IEnumerator PopDotsRoutine(List<Vector2> worldPts)
    {
        float targetScale = currentSpacing * 0.16f;
        for (int i = worldPts.Count - 1; i >= 0; i--)
        {
            Vector2 wp = worldPts[i];
            int gx = Mathf.RoundToInt((wp.x + currentOriginOffset.x) / currentSpacing);
            int gy = Mathf.RoundToInt((wp.y + currentOriginOffset.y) / currentSpacing);
            Vector2Int key = new Vector2Int(gx, gy);

            if (gridDots.TryGetValue(key, out Transform dotTr) && dotTr != null)
                StartCoroutine(AnimateSingleDotPop(dotTr, targetScale));
            yield return new WaitForSeconds(0.018f);
        }
    }

    IEnumerator AnimateSingleDotPop(Transform dotTr, float targetScale)
    {
        float t = 0f;
        while (t < 0.15f)
        {
            if (dotTr == null) yield break;
            t += Time.deltaTime;
            float s = Mathf.Sin((t / 0.15f) * Mathf.PI * 0.75f) * (targetScale * 1.25f);
            dotTr.localScale = Vector3.one * s;
            yield return null;
        }
        if (dotTr != null) dotTr.localScale = Vector3.one * targetScale;
    }

    public void OnSnakeCleared(SnakeController s)
    {
        activeSnakes.Remove(s);
        UpdateSnakeCounter();

        // Point 10: 50% Level Completed Milestone Popup!
        if (!halfLevelMilestoneShown && initialSnakeCountInLevel >= 6 && activeSnakes.Count == (initialSnakeCountInLevel / 2))
        {
            halfLevelMilestoneShown = true;
            isPaused = true;
            if (halfLevelMilestoneModalObj != null)
            {
                halfLevelMilestoneModalObj.SetActive(true);
                halfLevelMilestoneModalObj.transform.SetAsLastSibling();
            }
        }

        if (activeSnakes.Count == 0)
        {
            isTimerActive = false;
            int heartCount = Mathf.Clamp(lives, 1, 3);
            int coinsEarned = isChallengeMode ? 250 : (heartCount * 10);
            AddCoins(coinsEarned);

            if (isChallengeMode)
            {
                // Challenge finished! Next challenge will require unlock again
                isCurrentChallengeUnlocked = false;
                SaveProgress();
            }
            else
            {
                SaveProgress(currentLevel + 1);
            }

            StartCoroutine(Celebrate3SecondsAndShowWinScreen(heartCount, coinsEarned, levelGeneration));
        }
    }

    public void OnSnakeEscapeAnimationFinished(SnakeController snake)
    {
        levelSnakes.Remove(snake);
    }

    IEnumerator Celebrate3SecondsAndShowWinScreen(int heartCount, int coinsEarned, int generation)
    {
        string praiseMsg = (heartCount == 3) ? "Awesome!" : (heartCount == 2) ? "Fabulous!" : "Well Done!";

        StartCoroutine(SpawnConfettiBurstRoutine(3.0f, generation));

        if (praiseFloatingObj != null && praiseFloatingText != null)
        {
            praiseFloatingText.text = praiseMsg;
            praiseFloatingObj.transform.SetAsLastSibling();
            praiseFloatingObj.SetActive(true);

            float elapsed = 0f;
            while (elapsed < 3.0f)
            {
                if (generation != levelGeneration) yield break;
                elapsed += Time.deltaTime;
                float pop = (elapsed < 0.25f)
                    ? Mathf.SmoothStep(0f, 1.08f, elapsed / 0.25f)
                    : 1.0f + Mathf.Sin(elapsed * 4.5f) * 0.03f;
                praiseFloatingObj.transform.localScale = Vector3.one * pop;
                yield return null;
            }
            praiseFloatingObj.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(3.0f);
        }

        if (generation != levelGeneration) yield break;

        if (winModalObj != null)
        {
            if (winCardPraiseText != null) winCardPraiseText.text = praiseMsg;
            if (winCoinRewardText != null) winCoinRewardText.text = $"+{coinsEarned} Coins";

            if (isChallengeMode)
            {
                if (winHeaderTitleText != null) winHeaderTitleText.text = "Challenge Completed!";
                if (winNextBtnText != null) winNextBtnText.text = "Next Challenge";
            }
            else
            {
                if (winHeaderTitleText != null) winHeaderTitleText.text = "Level Completed!";
                if (winNextBtnText != null) winNextBtnText.text = $"Next Game\n<size=32>Level {currentLevel + 1}</size>";
            }

            winModalObj.transform.SetAsLastSibling();
            winModalObj.SetActive(true);
            StartCoroutine(SpawnConfettiBurstRoutine(2.5f, generation));
        }
        else
        {
            NextLevel();
        }
    }

    IEnumerator SpawnConfettiBurstRoutine(float duration, int generation)
    {
        if (confettiContainer == null || generation != levelGeneration) yield break;
        confettiContainer.SetAsLastSibling();

        Color[] palette = new Color[] {
            new Color(1f, 0.28f, 0.38f),
            new Color(0.18f, 0.62f, 1f),
            new Color(1f, 0.85f, 0.18f),
            new Color(0.22f, 0.88f, 0.48f),
            new Color(0.75f, 0.38f, 1f)
        };

        List<RectTransform> pieces = new List<RectTransform>();
        List<Vector2> velocities = new List<Vector2>();
        List<float> spinSpeeds = new List<float>();

        for (int i = 0; i < 60; i++)
        {
            GameObject p = new GameObject($"Confetti_{i}");
            p.transform.SetParent(confettiContainer, false);
            Image img = p.AddComponent<Image>();
            img.color = palette[i % palette.Length];
            img.raycastTarget = false;

            RectTransform rt = p.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(Random.Range(-520f, 520f), Random.Range(350f, 1250f));
            rt.sizeDelta = new Vector2(Random.Range(18f, 30f), Random.Range(12f, 20f));
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            pieces.Add(rt);
            velocities.Add(new Vector2(Random.Range(-110f, 110f), Random.Range(-420f, -780f)));
            spinSpeeds.Add(Random.Range(-320f, 320f));
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (generation != levelGeneration) yield break;
            elapsed += Time.deltaTime;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] == null) continue;
                pieces[i].anchoredPosition += velocities[i] * Time.deltaTime;
                pieces[i].Rotate(0f, 0f, spinSpeeds[i] * Time.deltaTime);
            }
            yield return null;
        }

        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i] != null) Destroy(pieces[i].gameObject);
    }

    public void NextLevel()
    {
        currentLevel++;
        LoadLevel(currentLevel);
    }

    public void OnMistake()
    {
        TriggerHaptic(true);
        if (lives > 0)
        {
            lives--;
            UpdateHeartsUI();
            StartCoroutine(AnimateBrokenHeartAndFlash(lives));

            if (lives <= 0)
                StartCoroutine(ShowGameOverModalAfterDelay(levelGeneration));
        }
    }

    IEnumerator ShowGameOverModalAfterDelay(int generation)
    {
        isPaused = true;
        yield return new WaitForSeconds(0.55f);
        if (generation != levelGeneration) yield break;

        if (gameOverModalObj != null)
        {
            gameOverModalObj.transform.SetAsLastSibling();
            gameOverModalObj.SetActive(true);
        }
        else LoadLevel(currentLevel);
    }

    IEnumerator AnimateBrokenHeartAndFlash(int brokenHeartIndex)
    {
        if (damageVignette != null)
        {
            damageVignette.transform.SetAsLastSibling();
            damageVignette.color = new Color(0.95f, 0.15f, 0.20f, 0.34f);
        }

        Transform heartTr = (brokenHeartIndex >= 0 && brokenHeartIndex < heartImages.Count)
            ? heartImages[brokenHeartIndex].transform : null;

        float elapsed = 0f;
        while (elapsed < 0.28f)
        {
            elapsed += Time.deltaTime;
            if (damageVignette != null)
                damageVignette.color = new Color(0.95f, 0.15f, 0.20f, Mathf.Lerp(0.34f, 0f, elapsed / 0.28f));
            if (heartTr != null)
                heartTr.localScale = Vector3.one * (1f + Mathf.Sin((elapsed / 0.28f) * Mathf.PI) * 0.35f);
            yield return null;
        }

        if (heartTr != null) heartTr.localScale = Vector3.one;
        if (damageVignette != null) damageVignette.color = new Color(0.95f, 0.15f, 0.20f, 0f);
    }

    void UpdateSnakeCounter()
    {
        if (snakeCountText != null)
            snakeCountText.text = activeSnakes.Count.ToString();
    }

    void UpdateHeartsUI()
    {
        for (int i = 0; i < heartImages.Count; i++)
        {
            Image h = heartImages[i];
            if (h != null)
            {
                if (crispHeartSprite != null) h.sprite = crispHeartSprite;
                h.color = (i < lives) ? Color.white : new Color(0.82f, 0.85f, 0.90f, 0.55f);
            }
        }
    }

    public void GiveHint()
    {
        if (isPaused) return;

        if (remainingHints <= 0)
        {
            if (hintAdModalObj != null)
            {
                isPaused = true;
                hintAdModalObj.transform.SetAsLastSibling();
                hintAdModalObj.SetActive(true);
            }
            return;
        }

        foreach (var sc in activeSnakes)
        {
            if (sc == null || sc.isMoving || sc.isCleared) continue;
            int freeSteps;
            if (CheckPathClearAndDistance(sc, out freeSteps))
            {
                remainingHints--;
                SaveProgress();
                UpdateCoinAndHintUI();
                sc.SetColor(sc.hintColor);
                break;
            }
        }
    }

    // --- STUDIO-GRADE PROCEDURAL SPRITE GENERATORS ---

    // Point 11: Yellow & Light-Yellow Radial Sunburst Card Sprite!
    Sprite CreateYellowSunburstSprite(int w, int h, bool roundedCorners = true)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(w * 0.5f, h * 0.54f);
        float cornerRadius = 46f;
        Color goldenYellow = new Color(1.0f, 0.74f, 0.08f, 1f);
        Color warmYellow = new Color(1.0f, 0.86f, 0.28f, 1f);
        Color lightRayYellow = new Color(1.0f, 0.96f, 0.65f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float alpha = 1f;
                if (roundedCorners)
                {
                    float cx = Mathf.Clamp(x, cornerRadius, w - cornerRadius);
                    float cy = Mathf.Clamp(y, cornerRadius, h - cornerRadius);
                    float edgeDist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    alpha = Mathf.Clamp01((cornerRadius - edgeDist) / 1.8f);
                }

                if (alpha <= 0f)
                {
                    t.SetPixel(x, y, Color.clear);
                    continue;
                }

                Vector2 diff = new Vector2(x, y) - center;
                float dist = diff.magnitude / (w * 0.72f);
                float beam = Mathf.SmoothStep(0.25f, 0.75f, Mathf.Sin(Mathf.Atan2(diff.y, diff.x) * 16f) * 0.5f + 0.5f);

                Color baseCol = Color.Lerp(warmYellow, goldenYellow, Mathf.Clamp01(dist));
                Color finalCol = Color.Lerp(baseCol, lightRayYellow, beam * 0.42f * Mathf.Clamp01(1f - dist * 0.45f));
                finalCol.a = alpha;
                t.SetPixel(x, y, finalCol);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    // Point 6: Floating Grid Guide Button Sprite (White 3D Circle + Tilted Purple Ruler + Blue Badge!)
    Sprite CreateGridGuideRulerButtonSprite(int s)
    {
        Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        Vector2 mainCenter = new Vector2(-0.04f, -0.04f);
        float mainRadius = 0.76f;
        Vector2 shadowCenter = new Vector2(-0.04f, -0.10f);
        Vector2 badgeCenter = new Vector2(0.50f, 0.50f);
        float badgeRadius = 0.28f;

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x / (float)(s - 1)) * 2f - 1f;
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                Vector2 p = new Vector2(nx, ny);
                Color finalCol = Color.clear;

                float dShadow = Vector2.Distance(p, shadowCenter);
                if (dShadow <= mainRadius + 0.08f)
                {
                    float shA = Mathf.Clamp01(1f - Mathf.Max(0f, dShadow - (mainRadius - 0.04f)) / 0.12f) * 0.25f;
                    finalCol = new Color(0.40f, 0.48f, 0.62f, shA);
                }

                float dMain = Vector2.Distance(p, mainCenter);
                if (dMain <= mainRadius)
                {
                    float edgeA = Mathf.Clamp01((mainRadius - dMain) / 0.025f);
                    bool isRim = dMain >= (mainRadius - 0.055f);
                    Color plateCol = isRim ? new Color(0.84f, 0.88f, 0.94f) : Color.white;
                    finalCol = Color.Lerp(finalCol, new Color(plateCol.r, plateCol.g, plateCol.b, 1f), edgeA);
                }

                // Tilted Purple Ruler inside Circle (matching image (6).jpg!)
                Vector2 rp = (p - mainCenter) * 0.92f;
                float rx = rp.x * 0.866f + rp.y * 0.50f;
                float ry = -rp.x * 0.50f + rp.y * 0.866f;

                if (Mathf.Abs(rx) <= 0.18f && Mathf.Abs(ry) <= 0.38f)
                {
                    Color rulerCol = Color.Lerp(new Color(0.38f, 0.32f, 0.92f), new Color(0.52f, 0.46f, 0.98f), (ry + 0.38f) / 0.76f);
                    bool isTick = (rx < -0.05f && Mathf.Abs(Mathf.Sin(ry * 38f)) > 0.75f);
                    finalCol = isTick ? new Color(0.22f, 0.16f, 0.62f, 1f) : rulerCol;
                }

                float dBadge = Vector2.Distance(p, badgeCenter);
                if (dBadge <= badgeRadius)
                {
                    float bEdge = Mathf.Clamp01((badgeRadius - dBadge) / 0.025f);
                    finalCol = Color.Lerp(finalCol, new Color(0.14f, 0.48f, 0.96f, 1f), bEdge);
                }

                t.SetPixel(x, y, finalCol);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateBigPurpleRulerSprite(int s)
    {
        Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x / (float)(s - 1)) * 2f - 1f;
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                float rx = nx * 0.866f + ny * 0.50f;
                float ry = -nx * 0.50f + ny * 0.866f;

                if (Mathf.Abs(rx) <= 0.32f && Mathf.Abs(ry) <= 0.72f)
                {
                    bool isBorder = Mathf.Abs(rx) > 0.28f || Mathf.Abs(ry) > 0.68f;
                    if (isBorder) { t.SetPixel(x, y, Color.white); continue; }
                    Color rulerCol = Color.Lerp(new Color(0.38f, 0.32f, 0.92f), new Color(0.54f, 0.48f, 0.98f), (ry + 0.72f) / 1.44f);
                    bool isTick = (rx < -0.08f && Mathf.Abs(Mathf.Sin(ry * 26f)) > 0.72f);
                    t.SetPixel(x, y, isTick ? new Color(0.22f, 0.16f, 0.62f, 1f) : rulerCol);
                }
                else t.SetPixel(x, y, Color.clear);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateGearIconSprite(int s)
    {
        Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x / (float)(s - 1)) * 2f - 1f;
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float ang = Mathf.Atan2(ny, nx);
                float outerR = 0.62f + Mathf.Cos(ang * 8f) * 0.10f;
                bool inGear = (r <= outerR && r >= 0.24f);
                t.SetPixel(x, y, inGear ? new Color(0.14f, 0.48f, 0.96f, 1f) : Color.clear);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateVerticalGradientCardSprite(int w, int h, float radius, Color topCol, Color botCol)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            Color rowCol = Color.Lerp(botCol, topCol, y / (float)(h - 1));
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x, radius, w - radius);
                float cy = Mathf.Clamp(y, radius, h - radius);
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float a = Mathf.Clamp01((radius - d) / 1.5f);
                t.SetPixel(x, y, new Color(rowCol.r, rowCol.g, rowCol.b, a));
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateCalendarIconSprite(int s)
    {
        Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x / (float)(s - 1)) * 2f - 1f;
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                if (Mathf.Abs(nx) > 0.72f || Mathf.Abs(ny) > 0.72f) { t.SetPixel(x, y, Color.clear); continue; }
                Color c = (ny > 0.35f) ? new Color(1.0f, 0.68f, 0.12f, 1f) : Color.white;
                float dCenter = Vector2.Distance(new Vector2(nx, ny), new Vector2(0f, -0.12f));
                if (dCenter < 0.28f && dCenter > 0.18f) c = new Color(0.82f, 0.86f, 0.92f, 1f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateTrophyIconSprite(int s)
    {
        Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x / (float)(s - 1)) * 2f - 1f;
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                bool inCup = (ny >= -0.05f && ny <= 0.65f && Mathf.Abs(nx) <= Mathf.Lerp(0.22f, 0.52f, (ny + 0.05f) / 0.70f));
                bool inStem = (ny >= -0.42f && ny < -0.05f && Mathf.Abs(nx) <= 0.12f);
                bool inBase = (ny >= -0.62f && ny < -0.42f && Mathf.Abs(nx) <= 0.42f);

                if (inCup || inStem || inBase)
                {
                    Color gold = Color.Lerp(new Color(0.96f, 0.62f, 0.04f, 1f), new Color(1.0f, 0.88f, 0.24f, 1f), (ny + 0.65f) / 1.3f);
                    t.SetPixel(x, y, gold);
                }
                else t.SetPixel(x, y, Color.clear);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateCrispSnakeBadgeIcon(int s)
    {
        Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x / (float)(s - 1)) * 2f - 1f;
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                float rx = nx * 0.766f - ny * 0.643f;
                float ry = nx * 0.643f + ny * 0.766f;

                float alpha = 0f;
                if (ry >= -0.74f && ry <= 0.36f)
                {
                    float curve = Mathf.Sin(ry * 4.8f) * 0.20f;
                    float w = Mathf.Lerp(0.05f, 0.15f, (ry + 0.74f) / 1.10f);
                    alpha = Mathf.Clamp01((w - Mathf.Abs(rx - curve)) / 0.035f);
                }
                else if (ry > 0.36f && ry <= 0.76f)
                {
                    float hk = (ry - 0.36f) / 0.40f;
                    float maxW = Mathf.Sin(hk * Mathf.PI) * 0.24f + 0.05f;
                    alpha = Mathf.Clamp01((maxW - Mathf.Abs(rx)) / 0.035f);
                }

                t.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }

    Sprite CreatePlump3DHeartSprite(int size, bool isBroken)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float rawNx = (x / (float)(size - 1)) * 2f - 1f;
                float rawNy = (y / (float)(size - 1)) * 2f - 1f;

                if (isBroken)
                {
                    float zig = Mathf.Sin(rawNy * 12f) * 0.055f + (rawNy * 0.03f);
                    float crackWidth = 0.055f * Mathf.Clamp01(1.1f - Mathf.Abs(rawNy) * 0.4f);
                    if (Mathf.Abs(rawNx - zig) < crackWidth)
                    {
                        t.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    rawNx += (rawNx > zig) ? -0.025f : 0.025f;
                }

                float px = Mathf.Abs(rawNx) * 1.10f;
                float py = rawNy * 1.10f + 0.50f;

                float sd;
                if (py + px > 1.0f)
                {
                    float dx = px - 0.25f, dy = py - 0.75f;
                    sd = Mathf.Sqrt(dx * dx + dy * dy) - (Mathf.Sqrt(2.0f) / 4.0f);
                }
                else
                {
                    float dx1 = px, dy1 = py - 1.0f;
                    float d1 = dx1 * dx1 + dy1 * dy1;
                    float m = 0.5f * Mathf.Max(px + py, 0.0f);
                    float d2 = (px - m) * (px - m) + (py - m) * (py - m);
                    sd = Mathf.Sqrt(Mathf.Min(d1, d2)) * Mathf.Sign(px - py);
                }

                float alpha = Mathf.Clamp01((-sd) / 0.022f);
                float vGrad = y / (float)size;
                Color baseRed = Color.Lerp(new Color(0.82f, 0.10f, 0.18f), new Color(1.0f, 0.28f, 0.36f), Mathf.SmoothStep(0.12f, 0.88f, vGrad));
                t.SetPixel(x, y, new Color(baseRed.r, baseRed.g, baseRed.b, alpha));
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateGoldStarCoinSprite(int s)
    {
        Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
        float r = s * 0.45f;

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01((r - d) / 1.6f);
                if (a <= 0f) { t.SetPixel(x, y, Color.clear); continue; }
                bool isRim = d > r * 0.76f;
                Color gold = isRim ? new Color(0.96f, 0.62f, 0.05f, a) : new Color(1.0f, 0.84f, 0.18f, a);
                t.SetPixel(x, y, gold);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateRotatingSunburstRaysSprite(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 diff = new Vector2(x, y) - center;
                float dist = diff.magnitude / (size * 0.5f);
                float angle = Mathf.Atan2(diff.y, diff.x);
                float ray = Mathf.SmoothStep(-0.2f, 0.2f, Mathf.Sin(angle * 14f));
                float fade = Mathf.Clamp01(1f - dist * 0.85f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, ray * fade));
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    Sprite Create9SlicePillSprite(int w, int h, float radius)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x, radius, w - radius);
                float cy = Mathf.Clamp(y, radius, h - radius);
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float a = Mathf.Clamp01((radius - d) / 1.5f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        t.Apply();
        Vector4 border = new Vector4(radius + 2f, radius + 2f, radius + 2f, radius + 2f);
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    Sprite Create9Slice3DPillSprite(int w, int h, float radius)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x, radius, w - radius);
                float cy = Mathf.Clamp(y, radius, h - radius);
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float a = Mathf.Clamp01((radius - d) / 1.5f);
                bool isLip = (y < h * 0.13f);
                Color col = isLip ? new Color(0.82f, 0.90f, 0.99f, a) : new Color(1f, 1f, 1f, a);
                t.SetPixel(x, y, col);
            }
        }
        t.Apply();
        Vector4 border = new Vector4(radius + 2f, radius + 2f, radius + 2f, radius + 2f);
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    Sprite Create9SliceRoundedCardSprite(int w, int h, float radius)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x, radius, w - radius);
                float cy = Mathf.Clamp(y, radius, h - radius);
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float a = Mathf.Clamp01((radius - d) / 1.5f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        t.Apply();
        Vector4 border = new Vector4(radius + 4f, radius + 4f, radius + 4f, radius + 4f);
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    Sprite CreateVideoAdClapperSprite(int s)
    {
        Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x / (float)(s - 1)) * 2f - 1f;
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                bool inBox = Mathf.Abs(nx) <= 0.82f && Mathf.Abs(ny) <= 0.65f;
                if (!inBox) { t.SetPixel(x, y, Color.clear); continue; }
                float py = ny + 0.05f, px = nx - 0.05f;
                bool inTriangle = (px >= -0.22f && px <= 0.30f && Mathf.Abs(py) <= (0.30f - px) * 0.62f);
                t.SetPixel(x, y, inTriangle ? Color.clear : Color.white);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateCrispPauseIconSprite(int s)
    {
        Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(s * 0.5f, s * 0.5f);
        float radius = s * 0.45f;
        Color circleBg = new Color(0.92f, 0.95f, 0.99f, 1f);
        Color iconBlue = new Color(0.12f, 0.46f, 0.96f, 1f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float bgA = Mathf.Clamp01((radius - d) / 1.8f);
                if (bgA <= 0f) { t.SetPixel(x, y, Color.clear); continue; }
                float nx = (x / (float)(s - 1)) * 2f - 1f;
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                bool fg = (Mathf.Abs(Mathf.Abs(nx) - 0.18f) <= 0.08f && Mathf.Abs(ny) <= 0.34f);
                Color c = fg ? iconBlue : circleBg;
                c.a = bgA;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateSmoothCircleSprite(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        float r = size * 0.46f;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float a = Mathf.Clamp01((r - Vector2.Distance(new Vector2(x, y), center)) / 1.8f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateArrowPuzzleHintButtonSprite(int s)
    {
        Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        Vector2 mainCenter = new Vector2(-0.04f, -0.04f);
        float mainRadius = 0.76f;
        Vector2 shadowCenter = new Vector2(-0.04f, -0.10f);
        Vector2 badgeCenter = new Vector2(0.50f, 0.50f);
        float badgeRadius = 0.28f;

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x / (float)(s - 1)) * 2f - 1f;
                float ny = (y / (float)(s - 1)) * 2f - 1f;
                Vector2 p = new Vector2(nx, ny);
                Color finalCol = Color.clear;

                float dShadow = Vector2.Distance(p, shadowCenter);
                if (dShadow <= mainRadius + 0.08f)
                {
                    float shA = Mathf.Clamp01(1f - Mathf.Max(0f, dShadow - (mainRadius - 0.04f)) / 0.12f) * 0.25f;
                    finalCol = new Color(0.40f, 0.48f, 0.62f, shA);
                }

                float dMain = Vector2.Distance(p, mainCenter);
                if (dMain <= mainRadius)
                {
                    float edgeA = Mathf.Clamp01((mainRadius - dMain) / 0.025f);
                    bool isRim = dMain >= (mainRadius - 0.055f);
                    Color plateCol = isRim ? new Color(0.84f, 0.88f, 0.94f) : Color.white;
                    finalCol = Color.Lerp(finalCol, new Color(plateCol.r, plateCol.g, plateCol.b, 1f), edgeA);
                }

                Vector2 bp = (p - mainCenter) * 0.85f;
                float bx = bp.x * 0.8829f + bp.y * 0.4695f;
                float by = -bp.x * 0.4695f + bp.y * 0.8829f;

                float dGlass = Vector2.Distance(new Vector2(bx, by), new Vector2(0f, 0.10f));
                bool inNeck = (by >= -0.18f && by <= 0.05f && Mathf.Abs(bx) <= Mathf.Lerp(0.14f, 0.25f, (by + 0.18f) / 0.23f));
                if (dGlass <= 0.26f || inNeck)
                {
                    Color bulbCol = Color.Lerp(new Color(0.98f, 0.62f, 0.02f), new Color(1.0f, 0.88f, 0.22f), Mathf.Clamp01((by + 0.15f) / 0.50f));
                    float highlightDist = Vector2.Distance(new Vector2(bx, by), new Vector2(-0.09f, 0.17f));
                    if (highlightDist < 0.08f)
                        bulbCol = Color.Lerp(bulbCol, Color.white, Mathf.Clamp01((0.08f - highlightDist) / 0.04f) * 0.8f);
                    finalCol = bulbCol;
                }

                if (by >= -0.32f && by < -0.17f && Mathf.Abs(bx) <= 0.13f)
                    finalCol = new Color(0.55f, 0.65f, 0.80f, 1f);
                if (by >= -0.37f && by < -0.32f && Mathf.Abs(bx) <= 0.08f)
                    finalCol = new Color(0.32f, 0.38f, 0.50f, 1f);

                float dBadge = Vector2.Distance(p, badgeCenter);
                if (dBadge <= badgeRadius)
                {
                    float bEdge = Mathf.Clamp01((badgeRadius - dBadge) / 0.025f);
                    finalCol = Color.Lerp(finalCol, new Color(0.14f, 0.44f, 0.88f, 1f), bEdge);
                }

                t.SetPixel(x, y, finalCol);
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateVignetteSprite(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)size) * 2f - 1f, ny = (y / (float)size) * 2f - 1f;
                float a = Mathf.Clamp01((Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny)) - 0.70f) / 0.30f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}