using System.Collections;
using System.Collections.Generic;
using SnakeEscape.Domain;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SnakeController : MonoBehaviour
{
    public Color bodyColor = new Color(0.04f, 0.06f, 0.12f);     // Deep Crisp Navy-Black
    public Color exitBlueColor = new Color(0.12f, 0.42f, 0.98f); // Royal Blue when exiting!
    public Color hitColor = new Color(0.96f, 0.14f, 0.20f);      // Collision Red
    public Color hintColor = new Color(0.08f, 0.78f, 0.36f);     // Hint Green

    public float lineWidth = 0.14f;
    public float slitherSpeed = 17f;

    [HideInInspector] public bool isMoving = false;
    [HideInInspector] public bool isCleared = false;
    [HideInInspector] public List<Vector2Int> gridCells = new List<Vector2Int>();

    private LineRenderer lr;
    private List<Vector2> baseWaypoints = new List<Vector2>();
    private List<Vector2> currentWaypoints = new List<Vector2>();

    private GameObject headHolder;
    private SpriteRenderer headRenderer;

    private static Material sharedSmoothLineMat;
    private static Sprite cachedHeadWithTongueSprite;
    private static Sprite cachedImpactCircleSprite;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        SetupLineRenderer();
    }

    void SetupLineRenderer()
    {
        if (sharedSmoothLineMat == null)
        {
            sharedSmoothLineMat = new Material(Shader.Find("Sprites/Default"));
            sharedSmoothLineMat.mainTexture = GenerateAntiAliasedLineTexture();
        }

        lr.material = sharedSmoothLineMat;
        lr.textureMode = LineTextureMode.Stretch;
        lr.useWorldSpace = true;
        lr.numCapVertices = 12;
        lr.numCornerVertices = 12;
        lr.sortingOrder = 5;

        AnimationCurve taper = new AnimationCurve();
        taper.AddKey(0.00f, 0.76f);
        taper.AddKey(0.07f, 1.00f);
        taper.AddKey(0.75f, 0.96f);
        taper.AddKey(0.90f, 0.58f);
        taper.AddKey(0.97f, 0.25f);
        taper.AddKey(1.00f, 0.06f);

        lr.widthCurve = taper;
        lr.widthMultiplier = lineWidth;
        SetColor(bodyColor);
    }

    static Texture2D GenerateAntiAliasedLineTexture()
    {
        int w = 8, h = 64;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            float ny = Mathf.Abs((y / (float)(h - 1)) * 2f - 1f);
            float alpha = Mathf.Clamp01((0.92f - ny) / 0.16f);
            Color c = new Color(1f, 1f, 1f, alpha);
            for (int x = 0; x < w; x++) t.SetPixel(x, y, c);
        }
        t.Apply();
        return t;
    }

    public void InitSnake(List<Vector2Int> cells, List<Vector2> worldPts, float customWidth)
    {
        gridCells = new List<Vector2Int>(cells);
        baseWaypoints = new List<Vector2>(worldPts);
        currentWaypoints = new List<Vector2>(worldPts);

        lineWidth = customWidth;
        lr.widthMultiplier = lineWidth;

        RenderCleanSnakeBody(currentWaypoints);
        CreateOrUpdateHead(currentWaypoints);
    }

    public float GetDistanceToPoint(Vector2 worldPt)
    {
        float minDist = float.MaxValue;
        for (int i = 0; i < currentWaypoints.Count - 1; i++)
        {
            float d = DistancePointToSegment(worldPt, currentWaypoints[i], currentWaypoints[i + 1]);
            if (d < minDist) minDist = d;
        }
        if (currentWaypoints.Count > 0)
        {
            float dHead = Vector2.Distance(worldPt, currentWaypoints[0]);
            if (dHead < minDist) minDist = dHead;
        }
        return minDist;
    }

    float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
        return Vector2.Distance(p, a + t * ab);
    }

    void RenderCleanSnakeBody(List<Vector2> pts)
    {
        if (pts == null || pts.Count < 2) return;

        List<Vector2> rounded = new List<Vector2>();
        rounded.Add(pts[0]);

        for (int i = 1; i < pts.Count - 1; i++)
        {
            Vector2 prev = pts[i - 1];
            Vector2 curr = pts[i];
            Vector2 next = pts[i + 1];

            Vector2 d1 = curr - prev;
            Vector2 d2 = next - curr;
            float l1 = d1.magnitude;
            float l2 = d2.magnitude;

            if (l1 > 0.03f && l2 > 0.03f && Vector2.Angle(d1, d2) > 10f)
            {
                float cut = Mathf.Min(l1 * 0.22f, l2 * 0.22f, lineWidth * 0.80f);
                Vector2 pA = curr - d1.normalized * cut;
                Vector2 pB = curr + d2.normalized * cut;

                for (int s = 0; s <= 4; s++)
                {
                    float t = s / 4f;
                    Vector2 arc = (1f - t) * (1f - t) * pA + 2f * (1f - t) * t * curr + t * t * pB;
                    rounded.Add(arc);
                }
            }
            else
            {
                rounded.Add(curr);
            }
        }
        rounded.Add(pts[pts.Count - 1]);

        List<Vector3> finalPts = new List<Vector3>();
        for (int i = 0; i < rounded.Count - 1; i++)
        {
            Vector2 a = rounded[i];
            Vector2 b = rounded[i + 1];
            float segLen = Vector2.Distance(a, b);

            int steps = Mathf.Max(2, Mathf.CeilToInt(segLen / 0.06f));
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps;
                Vector2 pos = Vector2.Lerp(a, b, t);
                finalPts.Add(new Vector3(pos.x, pos.y, 0f));
            }
        }

        Vector2 tail = rounded[rounded.Count - 1];
        finalPts.Add(new Vector3(tail.x, tail.y, 0f));

        lr.positionCount = finalPts.Count;
        lr.SetPositions(finalPts.ToArray());
    }

    void CreateOrUpdateHead(List<Vector2> pts)
    {
        if (pts.Count < 2) return;

        if (headHolder == null)
        {
            headHolder = new GameObject("ViperHead");
            headHolder.transform.SetParent(transform);

            GameObject hObj = new GameObject("HeadWithTongueSprite");
            hObj.transform.SetParent(headHolder.transform, false);
            headRenderer = hObj.AddComponent<SpriteRenderer>();
            headRenderer.material = new Material(Shader.Find("Sprites/Default"));
            headRenderer.sortingOrder = lr.sortingOrder + 2;

            if (cachedHeadWithTongueSprite == null)
                cachedHeadWithTongueSprite = GenerateOrganicViperHeadWithTongue();

            headRenderer.sprite = cachedHeadWithTongueSprite;
        }

        AlignHead(pts);
        SetColor(bodyColor);
    }

    void AlignHead(List<Vector2> pts)
    {
        if (pts.Count < 2 || headHolder == null) return;
        Vector2 head = pts[0];
        Vector2 neck = pts[1];
        Vector2 dir = (head - neck).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        headHolder.transform.position = new Vector3(head.x, head.y, 0f);
        headHolder.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        headHolder.transform.localScale = Vector3.one * (lineWidth * 1.45f);
    }

    public Vector2Int GetGridHead() => gridCells[0];

    public Vector2Int GetGridForwardDir()
    {
        if (gridCells.Count < 2) return Vector2Int.up;
        Vector2Int d = gridCells[0] - gridCells[1];
        return new Vector2Int(Mathf.Clamp(d.x, -1, 1), Mathf.Clamp(d.y, -1, 1));
    }

    public Vector2 GetWorldForwardDir()
    {
        if (currentWaypoints.Count < 2) return Vector2.up;
        return (currentWaypoints[0] - currentWaypoints[1]).normalized;
    }

    public void TriggerTap()
    {
        if (isMoving || isCleared || GameManager.Instance.isPaused) return;
        StartCoroutine(ExecuteTapRoutine());
    }

    IEnumerator ExecuteTapRoutine()
    {
        isMoving = true;

        int freeCellsAhead;
        MoveResult result = GameManager.Instance.EvaluateEscapeMove(this, out freeCellsAhead);
        bool clear = result.IsValid;

        if (!clear)
        {
            float spacing = GameManager.Instance.currentSpacing;
            float maxMoveDist = (freeCellsAhead * spacing) + (spacing * 0.34f);
            Vector2 fwd = GetWorldForwardDir();

            float moved = 0f;
            float hitSpeed = slitherSpeed * 0.9f;
            while (moved < maxMoveDist)
            {
                if (GameManager.Instance.isPaused) { yield return null; continue; }
                float step = Mathf.Min(hitSpeed * Time.deltaTime, maxMoveDist - moved);
                moved += step;
                AdvanceListAlongPath(currentWaypoints, fwd, step);
                RenderCleanSnakeBody(currentWaypoints);
                AlignHead(currentWaypoints);
                yield return null;
            }

            SetColor(hitColor);
            if (GameManager.Instance != null) GameManager.Instance.OnMistake();

            GameObject impactDot = SpawnImpactCircle(currentWaypoints[0] + fwd * (lineWidth * 0.45f));

            List<Vector2> impactPts = new List<Vector2>(currentWaypoints);
            float shakeTimer = 0.24f;
            while (shakeTimer > 0f)
            {
                if (GameManager.Instance.isPaused) { yield return null; continue; }
                shakeTimer -= Time.deltaTime;
                Vector2 jitter = Random.insideUnitCircle * (lineWidth * 0.16f);
                for (int i = 0; i < currentWaypoints.Count; i++)
                    currentWaypoints[i] = impactPts[i] + jitter;
                RenderCleanSnakeBody(currentWaypoints);
                AlignHead(currentWaypoints);
                yield return null;
            }

            if (impactDot != null) Destroy(impactDot);

            float returnDuration = Mathf.Clamp(maxMoveDist / hitSpeed, 0.10f, 0.25f);
            float elapsed = 0f;
            while (elapsed < returnDuration)
            {
                if (GameManager.Instance.isPaused) { yield return null; continue; }
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
                currentWaypoints = LerpPointLists(impactPts, baseWaypoints, t);
                RenderCleanSnakeBody(currentWaypoints);
                AlignHead(currentWaypoints);
                yield return null;
            }

            currentWaypoints = new List<Vector2>(baseWaypoints);
            RenderCleanSnakeBody(currentWaypoints);
            AlignHead(currentWaypoints);
            SetColor(bodyColor);
            isMoving = false;
        }
        else
        {
            // Point 4: Turn Vibrant Royal Blue when exiting!
            isCleared = true;
            SetColor(exitBlueColor);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerHaptic(false);
                GameManager.Instance.OnSnakeCleared(this);
                GameManager.Instance.RevealDotsAlongSnake(baseWaypoints);
            }

            Vector2 fwd = GetWorldForwardDir();

            while (currentWaypoints.Count > 1)
            {
                if (GameManager.Instance.isPaused) { yield return null; continue; }
                float step = slitherSpeed * Time.deltaTime;
                AdvanceListAlongPath(currentWaypoints, fwd, step);

                RenderCleanSnakeBody(currentWaypoints);
                AlignHead(currentWaypoints);

                Vector2 tail = currentWaypoints[currentWaypoints.Count - 1];
                if (Mathf.Abs(tail.x) > 9.5f || Mathf.Abs(tail.y) > 11.5f)
                {
                    break;
                }
                yield return null;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnSnakeEscapeAnimationFinished(this);
            Destroy(gameObject);
        }
    }

    GameObject SpawnImpactCircle(Vector2 pos)
    {
        if (cachedImpactCircleSprite == null)
        {
            int s = 64;
            Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(s * 0.5f, s * 0.5f));
                    float a = Mathf.Clamp01((s * 0.45f - d) / 2.0f);
                    t.SetPixel(x, y, new Color(0.75f, 0.78f, 0.82f, a * 0.75f));
                }
            }
            t.Apply();
            cachedImpactCircleSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }

        GameObject go = new GameObject("ImpactCircle");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one * (lineWidth * 2.2f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = cachedImpactCircleSprite;
        sr.sortingOrder = 4;
        return go;
    }

    void AdvanceListAlongPath(List<Vector2> pts, Vector2 headDir, float step)
    {
        if (pts.Count < 2) return;
        pts[0] += headDir * step;

        float rem = step;
        while (rem > 0f && pts.Count > 1)
        {
            int tailIdx = pts.Count - 1;
            Vector2 tail = pts[tailIdx];
            Vector2 prev = pts[tailIdx - 1];
            float segLen = Vector2.Distance(tail, prev);

            if (pts.Count > 2)
            {
                if (segLen <= rem)
                {
                    rem -= segLen;
                    pts.RemoveAt(tailIdx);
                }
                else
                {
                    pts[tailIdx] = Vector2.MoveTowards(tail, prev, rem);
                    rem = 0f;
                }
            }
            else
            {
                pts[tailIdx] += headDir * rem;
                rem = 0f;
            }
        }
    }

    List<Vector2> LerpPointLists(List<Vector2> from, List<Vector2> to, float t)
    {
        List<Vector2> res = new List<Vector2>();
        int count = to.Count;
        for (int i = 0; i < count; i++)
        {
            Vector2 a = (i < from.Count) ? from[i] : from[from.Count - 1];
            Vector2 b = to[i];
            res.Add(Vector2.Lerp(a, b, t));
        }
        return res;
    }

    public void SetColor(Color c)
    {
        if (lr != null) { lr.startColor = c; lr.endColor = c; }
        if (headRenderer != null) headRenderer.color = c;
    }

    static Sprite GenerateOrganicViperHeadWithTongue()
    {
        int w = 160, h = 240;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x / (float)(w - 1)) * 2f - 1f;
                float ny = y / (float)(h - 1);
                float alpha = 0f;

                if (ny <= 0.64f)
                {
                    float hy = ny / 0.64f;
                    float maxW;
                    if (hy < 0.14f)
                        maxW = Mathf.Lerp(0.32f, 0.36f, hy / 0.14f);
                    else if (hy < 0.42f)
                        maxW = Mathf.Lerp(0.36f, 0.72f, Mathf.Sin(((hy - 0.14f) / 0.28f) * Mathf.PI * 0.5f));
                    else
                        maxW = 0.72f * Mathf.Cos(((hy - 0.42f) / 0.58f) * Mathf.PI * 0.5f * 0.88f);

                    alpha = Mathf.Clamp01((maxW - Mathf.Abs(nx)) / 0.035f);
                }

                if (ny >= 0.60f && ny <= 0.96f)
                {
                    float ty = (ny - 0.60f) / 0.36f;
                    float tAlpha = 0f;
                    if (ty < 0.44f)
                        tAlpha = Mathf.Clamp01((0.055f - Mathf.Abs(nx)) / 0.028f);
                    else
                    {
                        float fk = (ty - 0.44f) / 0.56f;
                        float center = fk * 0.28f;
                        float halfW = Mathf.Lerp(0.048f, 0.015f, fk);
                        tAlpha = Mathf.Clamp01((halfW - Mathf.Abs(Mathf.Abs(nx) - center)) / 0.028f);
                    }
                    alpha = Mathf.Max(alpha, tAlpha);
                }

                t.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.10f), 120f);
    }
}