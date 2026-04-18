using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Knife Hit — phone mini-game.
/// Throw knives at a spinning log. Don't hit other knives!
/// Click / Space / Tap to throw a knife. Survive all throws to progress.
/// </summary>
public class KnifeHitGame : MonoBehaviour
{
    private GameObject root;
    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform board;     // spinning log
    private RectTransform knifePool; // parent for stuck knives
    private RectTransform readyKnife;
    private TMP_Text scoreText;
    private TMP_Text stageText;
    private TMP_Text knivesLeftText;
    private TMP_Text resultText;
    private Image bgImage;

    private TMP_FontAsset font;

    private float boardRadius = 110f;
    private float spinSpeed = 120f;     // degrees per second
    private int knivesThrown = 0;
    private int knivesRequired = 6;
    private int score = 0;
    private int stage = 1;
    private bool isGameOver = false;
    private bool canThrow = true;
    private bool isThrowing = false;

    private List<float> stuckAngles = new List<float>(); // relative to board
    private float collisionAngle = 14f; // +/- degrees around stuck knife

    private System.Action onExit;

    public void Init(GameObject parentPanel, System.Action onExitCallback)
    {
        onExit = onExitCallback;
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Create a full-screen container bound to parent's rect
        root = new GameObject("KnifeHitRoot");
        root.transform.SetParent(parentPanel.transform, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Mask children so nothing escapes the phone screen
        var maskImg = root.AddComponent<Image>();
        maskImg.color = new Color(0, 0, 0, 0.01f); // nearly invisible, required for mask
        maskImg.raycastTarget = false;
        var mask = root.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        BuildUI();
        StartStage(1);
    }

    void BuildUI()
    {
        // Background
        var bgObj = new GameObject("BG");
        bgObj.transform.SetParent(root.transform, false);
        bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.18f, 0.28f, 1f);
        bgImage.raycastTarget = true;
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        // Click-anywhere to throw
        var bgBtn = bgObj.AddComponent<Button>();
        bgBtn.transition = Selectable.Transition.None;
        bgBtn.onClick.AddListener(TryThrow);

        // Top bar with score / stage / exit
        stageText = MakeText(root.transform, "Stage", "STAGE 1", 22,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -30),
            new Vector2(0, 40), new Color(1f, 0.9f, 0.3f), FontStyles.Bold);

        scoreText = MakeText(root.transform, "Score", "SCORE 0", 18,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -55),
            new Vector2(0, 28), Color.white, FontStyles.Normal);

        // Exit button (top-right X)
        var exitObj = new GameObject("ExitBtn");
        exitObj.transform.SetParent(root.transform, false);
        var exitImg = exitObj.AddComponent<Image>();
        exitImg.color = new Color(0.9f, 0.2f, 0.3f, 0.9f);
        var exitBtn = exitObj.AddComponent<Button>();
        exitBtn.onClick.AddListener(() => { Cleanup(); onExit?.Invoke(); });
        var ert = exitObj.GetComponent<RectTransform>();
        ert.anchorMin = new Vector2(1, 1); ert.anchorMax = new Vector2(1, 1);
        ert.pivot = new Vector2(1, 1);
        ert.sizeDelta = new Vector2(32, 32);
        ert.anchoredPosition = new Vector2(-8, -8);
        MakeText(exitObj.transform, "X", "✕", 16,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white, FontStyles.Bold);

        // Board (spinning log) at center
        var boardObj = new GameObject("Board");
        boardObj.transform.SetParent(root.transform, false);
        var boardImg = boardObj.AddComponent<Image>();
        boardImg.color = new Color(0.55f, 0.35f, 0.18f, 1f);
        boardImg.sprite = BuildCircleSprite();
        boardImg.raycastTarget = false;
        board = boardObj.GetComponent<RectTransform>();
        board.anchorMin = new Vector2(0.5f, 0.5f);
        board.anchorMax = new Vector2(0.5f, 0.5f);
        board.pivot = new Vector2(0.5f, 0.5f);
        board.sizeDelta = new Vector2(boardRadius * 2f, boardRadius * 2f);
        board.anchoredPosition = new Vector2(0f, 30f);

        // Inner ring for visual
        var innerObj = new GameObject("Inner");
        innerObj.transform.SetParent(board, false);
        var innerImg = innerObj.AddComponent<Image>();
        innerImg.color = new Color(0.7f, 0.5f, 0.25f, 1f);
        innerImg.sprite = boardImg.sprite;
        innerImg.raycastTarget = false;
        var irt = innerObj.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 0.5f);
        irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.pivot = new Vector2(0.5f, 0.5f);
        irt.sizeDelta = new Vector2(boardRadius * 1.5f, boardRadius * 1.5f);
        irt.anchoredPosition = Vector2.zero;

        // Bullseye
        var bullObj = new GameObject("Bullseye");
        bullObj.transform.SetParent(board, false);
        var bullImg = bullObj.AddComponent<Image>();
        bullImg.color = new Color(0.9f, 0.85f, 0.2f, 1f);
        bullImg.sprite = boardImg.sprite;
        bullImg.raycastTarget = false;
        var brt = bullObj.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(50, 50);
        brt.anchoredPosition = Vector2.zero;

        // Stuck knives container (child of board so they rotate with it)
        var poolObj = new GameObject("KnifePool");
        poolObj.transform.SetParent(board, false);
        knifePool = poolObj.AddComponent<RectTransform>();
        knifePool.anchorMin = new Vector2(0.5f, 0.5f);
        knifePool.anchorMax = new Vector2(0.5f, 0.5f);
        knifePool.sizeDelta = Vector2.zero;
        knifePool.anchoredPosition = Vector2.zero;

        // Knives-left indicator at bottom
        knivesLeftText = MakeText(root.transform, "KnivesLeft", "", 18,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 60),
            new Vector2(0, 30), new Color(0.8f, 0.8f, 0.9f), FontStyles.Normal);

        // Ready knife below board (the one you shoot)
        CreateReadyKnife();

        // Instruction
        MakeText(root.transform, "Hint", "TAP / SPACE to throw", 16,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 25),
            new Vector2(0, 24), new Color(1, 1, 1, 0.5f), FontStyles.Italic);

        // Result text (shown on game over)
        resultText = MakeText(root.transform, "Result", "", 38,
            new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero,
            new Vector2(0, 80), new Color(1f, 0.4f, 0.4f), FontStyles.Bold);
        resultText.gameObject.SetActive(false);
    }

    void CreateReadyKnife()
    {
        var obj = new GameObject("ReadyKnife");
        obj.transform.SetParent(root.transform, false);
        readyKnife = obj.AddComponent<RectTransform>();
        readyKnife.anchorMin = new Vector2(0.5f, 0.5f);
        readyKnife.anchorMax = new Vector2(0.5f, 0.5f);
        readyKnife.pivot = new Vector2(0.5f, 0f); // pivot at bottom (handle end)
        readyKnife.sizeDelta = new Vector2(10, 48);
        readyKnife.anchoredPosition = new Vector2(0f, -boardRadius - 24f);

        // Blade (silver)
        var blade = new GameObject("Blade");
        blade.transform.SetParent(obj.transform, false);
        var bi = blade.AddComponent<Image>();
        bi.color = new Color(0.85f, 0.88f, 0.92f);
        bi.raycastTarget = false;
        var brt = blade.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0, 0.3f); brt.anchorMax = new Vector2(1, 1f);
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

        // Handle (dark)
        var handle = new GameObject("Handle");
        handle.transform.SetParent(obj.transform, false);
        var hi = handle.AddComponent<Image>();
        hi.color = new Color(0.15f, 0.15f, 0.2f);
        hi.raycastTarget = false;
        var hrt = handle.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0, 0); hrt.anchorMax = new Vector2(1, 0.3f);
        hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
    }

    void StartStage(int stageNum)
    {
        stage = stageNum;
        knivesThrown = 0;
        knivesRequired = Mathf.Min(5 + stageNum, 12);
        spinSpeed = 110f + stageNum * 20f;
        if (stageNum % 2 == 0) spinSpeed = -spinSpeed; // alternate direction
        isGameOver = false;
        isThrowing = false;
        canThrow = true;

        stuckAngles.Clear();
        if (knifePool != null)
        {
            for (int i = knifePool.childCount - 1; i >= 0; i--)
                Destroy(knifePool.GetChild(i).gameObject);
        }

        // Add some pre-stuck knives as obstacles (more each stage)
        int preStuck = Mathf.Min(stageNum, 4);
        for (int i = 0; i < preStuck; i++)
        {
            float ang = (360f / preStuck) * i + Random.Range(-15f, 15f);
            AddStuckKnife(ang, false);
        }

        if (stageText != null) stageText.text = "STAGE " + stage;
        UpdateKnivesLeft();
        if (readyKnife != null) readyKnife.gameObject.SetActive(true);
    }

    void Update()
    {
        if (root == null) return;

        // Spin board
        if (!isGameOver && board != null)
            board.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        // Input (keyboard fallback)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            TryThrow();

        if (isGameOver)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                Restart();
        }
    }

    void TryThrow()
    {
        if (isGameOver || !canThrow || isThrowing) return;
        StartCoroutine(ThrowKnife());
    }

    IEnumerator ThrowKnife()
    {
        isThrowing = true;
        canThrow = false;

        var sm = SoundManager.Instance;
        if (sm != null) sm.PlayPhoneTap();

        Vector2 startPos = readyKnife.anchoredPosition;
        Vector2 endPos = new Vector2(0f, 30f - boardRadius); // bottom edge of board
        float duration = 0.12f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            readyKnife.anchoredPosition = Vector2.Lerp(startPos, endPos, k);
            yield return null;
        }
        readyKnife.anchoredPosition = endPos;

        // Check collision: angle at bottom of board (world angle where knife sticks)
        float worldAngle = -90f; // knife hits bottom
        float boardRotZ = board.localEulerAngles.z;
        // normalize
        boardRotZ = ((boardRotZ % 360) + 360) % 360;
        float relativeAngle = Mathf.Repeat(worldAngle - boardRotZ, 360f);

        bool hit = false;
        foreach (var a in stuckAngles)
        {
            float diff = Mathf.DeltaAngle(a, relativeAngle);
            if (Mathf.Abs(diff) < collisionAngle) { hit = true; break; }
        }

        if (hit)
        {
            yield return StartCoroutine(KnifeBounce());
            GameOver();
            yield break;
        }

        // Stick the knife!
        AddStuckKnife(relativeAngle, true);
        stuckAngles.Add(relativeAngle);
        knivesThrown++;
        score += 10;
        if (scoreText != null) scoreText.text = "SCORE " + score;
        UpdateKnivesLeft();

        // Reset ready knife
        readyKnife.anchoredPosition = startPos;

        if (knivesThrown >= knivesRequired)
        {
            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(StageCompleteFlash());
            StartStage(stage + 1);
        }

        isThrowing = false;
        canThrow = true;
    }

    IEnumerator KnifeBounce()
    {
        readyKnife.gameObject.SetActive(true);
        Vector2 from = readyKnife.anchoredPosition;
        Vector2 to = from + new Vector2(Random.Range(-180f, 180f), -200f);
        float rot0 = readyKnife.localEulerAngles.z;
        float rotTarget = rot0 + Random.Range(180f, 540f) * (Random.value > 0.5f ? 1 : -1);
        float dur = 0.6f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            readyKnife.anchoredPosition = Vector2.Lerp(from, to, k);
            readyKnife.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rot0, rotTarget, k));
            yield return null;
        }
        readyKnife.gameObject.SetActive(false);
    }

    IEnumerator StageCompleteFlash()
    {
        Color orig = bgImage.color;
        for (int i = 0; i < 3; i++)
        {
            bgImage.color = new Color(0.2f, 0.6f, 0.3f, 1f);
            yield return new WaitForSeconds(0.08f);
            bgImage.color = orig;
            yield return new WaitForSeconds(0.08f);
        }
    }

    void AddStuckKnife(float relativeAngle, bool playerThrown)
    {
        var obj = new GameObject("StuckKnife");
        obj.transform.SetParent(knifePool, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(9, 42);

        // Position: at `relativeAngle` around board, sticking OUT of edge
        float rad = relativeAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        rt.anchoredPosition = dir * boardRadius;
        // Rotate so knife points AWAY from center (handle pivot is at bottom)
        // The knife's "up" should point outward
        float knifeAngle = relativeAngle - 90f;
        rt.localEulerAngles = new Vector3(0, 0, knifeAngle);

        // Blade
        var blade = new GameObject("Blade");
        blade.transform.SetParent(obj.transform, false);
        var bi = blade.AddComponent<Image>();
        bi.color = playerThrown ? new Color(0.88f, 0.9f, 0.95f) : new Color(0.7f, 0.7f, 0.75f);
        bi.raycastTarget = false;
        var brt = blade.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0, 0.3f); brt.anchorMax = new Vector2(1, 1);
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

        // Handle
        var handle = new GameObject("Handle");
        handle.transform.SetParent(obj.transform, false);
        var hi = handle.AddComponent<Image>();
        hi.color = playerThrown ? new Color(0.15f, 0.15f, 0.2f) : new Color(0.25f, 0.2f, 0.15f);
        hi.raycastTarget = false;
        var hrt = handle.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0, 0); hrt.anchorMax = new Vector2(1, 0.3f);
        hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;

        // Register in list only if player-thrown or pre-stuck (both block future knives)
        if (!playerThrown) stuckAngles.Add(relativeAngle);
    }

    void UpdateKnivesLeft()
    {
        if (knivesLeftText == null) return;
        int left = knivesRequired - knivesThrown;
        string s = "";
        for (int i = 0; i < left; i++) s += "🔪 ";
        knivesLeftText.text = s.Trim();
    }

    void GameOver()
    {
        isGameOver = true;
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = $"💀 GAME OVER\nScore: {score}\nStage: {stage}\n(tap anywhere to restart)";
        }
        if (bgImage != null) bgImage.color = new Color(0.3f, 0.1f, 0.1f, 1f);

        // Swap BG onClick to restart
        var btn = bgImage.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(Restart);
    }

    void Restart()
    {
        score = 0;
        stage = 1;
        if (resultText != null) resultText.gameObject.SetActive(false);
        if (bgImage != null)
        {
            bgImage.color = new Color(0.15f, 0.18f, 0.28f, 1f);
            var btn = bgImage.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(TryThrow);
        }
        StartStage(1);
        if (scoreText != null) scoreText.text = "SCORE 0";
    }

    public void Cleanup()
    {
        if (root != null) Destroy(root);
        root = null;
    }

    // ---- Helpers ----

    static Sprite cachedCircle;
    Sprite BuildCircleSprite()
    {
        if (cachedCircle != null) return cachedCircle;
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = size * 0.5f, cy = size * 0.5f, r = size * 0.48f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = Mathf.Clamp01(r - d);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        cachedCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return cachedCircle;
    }

    TMP_Text MakeText(Transform parent, string name, string text, float size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
        Color color, FontStyles style)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = anchoredPos;
        r.sizeDelta = sizeDelta;
        return tmp;
    }
}
