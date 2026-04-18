using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Условие победы: купить все ключевые апгрейды + накопить целевую сумму DoomCoin.
/// Иронический эндинг: "YOU ESCAPED THE DOOM LOOP".
///
/// Ключевые покупки по умолчанию: Laptop + TV + PS5.
/// Целевая сумма: 5000 DC.
/// </summary>
public class VictoryManager : MonoBehaviour
{
    public static VictoryManager Instance { get; private set; }

    [Header("Цели победы")]
    [SerializeField] private int coinGoal = 2000;
    [SerializeField] private List<string> requiredPurchases = new List<string> { "Laptop", "TV + PS5" };

    [Header("UI")]
    [SerializeField] private bool createProgressTicker = true;

    [Header("Debug")]
    [Tooltip("F9 — мгновенно выиграть (для тестирования)")]
    [SerializeField] private bool debugHotkeyEnabled = true;

    private HashSet<string> purchasedItems = new HashSet<string>();
    private bool victoryTriggered = false;

    private GameHUDController hud;
    private Canvas victoryCanvas;
    private CanvasGroup victoryCanvasGroup;
    private TMP_Text progressTicker;

    public int CoinGoal => coinGoal;
    public int PurchasedCount => purchasedItems.Count;
    public int RequiredCount => requiredPurchases.Count;
    public bool IsComplete => victoryTriggered;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        hud = FindFirstObjectByType<GameHUDController>();
        if (createProgressTicker) CreateProgressTicker();
    }

    void Update()
    {
        if (victoryTriggered) return;
        if (hud == null) hud = FindFirstObjectByType<GameHUDController>();

        // Debug хоткей: F9 — принудительная победа (только когда не в плеймоде мини-игр и т.д.)
        if (debugHotkeyEnabled &&
            UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.f9Key.wasPressedThisFrame)
        {
            Debug.Log("[Victory] Debug F9 pressed — forcing victory");
            victoryTriggered = true;
            StartCoroutine(VictorySequence());
            return;
        }

        UpdateTicker();

        if (hud == null) return;
        int coins = hud.GetCoins();
        bool allPurchased = purchasedItems.Count >= requiredPurchases.Count &&
                            AllRequiredPurchased();

        if (allPurchased && coins >= coinGoal)
        {
            victoryTriggered = true;
            StartCoroutine(VictorySequence());
        }
    }

    bool AllRequiredPurchased()
    {
        foreach (var r in requiredPurchases)
            if (!purchasedItems.Contains(r)) return false;
        return true;
    }

    /// <summary>Вызывается из ShopController при покупке ключевого предмета.</summary>
    public void ReportItemPurchased(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return;
        purchasedItems.Add(itemName);
        Debug.Log($"[Victory] Reported purchase: {itemName} ({purchasedItems.Count}/{requiredPurchases.Count})");
    }

    IEnumerator VictorySequence()
    {
        // Отключим управление игроком
        var player = GameObject.Find("player");
        if (player != null)
        {
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CreateVictoryScreen();

        // Fade-in
        float t = 0f;
        while (t < 2.2f)
        {
            t += Time.unscaledDeltaTime;
            if (victoryCanvasGroup != null)
                victoryCanvasGroup.alpha = Mathf.Clamp01(t / 2.2f);
            Time.timeScale = Mathf.Lerp(1f, 0f, t / 2.2f);
            yield return null;
        }
        Time.timeScale = 0f;
        if (victoryCanvasGroup != null) victoryCanvasGroup.alpha = 1f;
    }

    // =============================================================
    // PROGRESS TICKER (маленький прогресс в углу экрана)
    // =============================================================
    void CreateProgressTicker()
    {
        var canvas = GameObject.Find("GameUICanvas");
        if (canvas == null) return;

        var go = new GameObject("VictoryTicker", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(340f, 64f);
        rt.anchoredPosition = new Vector2(-20f, -160f);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = false;

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var trt = (RectTransform)textGO.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10f, 4f);
        trt.offsetMax = new Vector2(-10f, -4f);

        progressTicker = textGO.AddComponent<TextMeshProUGUI>();
        progressTicker.text = "";
        progressTicker.fontSize = 18f;
        progressTicker.fontStyle = FontStyles.Bold;
        progressTicker.color = new Color(1f, 0.9f, 0.4f);
        progressTicker.alignment = TextAlignmentOptions.MidlineLeft;
        progressTicker.enableWordWrapping = true;
        progressTicker.overflowMode = TextOverflowModes.Truncate;
        progressTicker.raycastTarget = false;
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) progressTicker.font = font;
    }

    void UpdateTicker()
    {
        if (progressTicker == null) return;
        int coins = hud != null ? hud.GetCoins() : 0;
        int bought = purchasedItems.Count;
        int total = requiredPurchases.Count;
        progressTicker.text =
            $"<color=#FFD54A>ESCAPE GOAL</color>\n" +
            $"Items: <b>{bought}/{total}</b>   Coins: <b>{coins}/{coinGoal} DC</b>";
    }

    // =============================================================
    // VICTORY SCREEN
    // =============================================================
    void CreateVictoryScreen()
    {
        var canvasGO = new GameObject("VictoryCanvas");
        victoryCanvas = canvasGO.AddComponent<Canvas>();
        victoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        victoryCanvas.sortingOrder = 10000;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        victoryCanvasGroup = canvasGO.AddComponent<CanvasGroup>();
        victoryCanvasGroup.alpha = 0f;

        // Dim overlay
        var overlay = new GameObject("Overlay", typeof(RectTransform));
        overlay.transform.SetParent(canvasGO.transform, false);
        var ort = (RectTransform)overlay.transform;
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;
        var oimg = overlay.AddComponent<Image>();
        oimg.color = new Color(0.05f, 0.03f, 0.1f, 0.88f);

        // Title
        var title = MakeText(canvasGO.transform, "Title", "YOU ESCAPED\nTHE DOOM LOOP",
            new Vector2(0.5f, 0.68f), 92f, new Color(1f, 0.92f, 0.3f), FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;
        title.characterSpacing = 6f;
        var trt = title.rectTransform;
        trt.sizeDelta = new Vector2(1600f, 320f);

        // Subtitle
        MakeText(canvasGO.transform, "Sub",
            "You bought the laptop, the TV, and enough coins to log off forever.\n" +
            "Congrats, you're finally free. Or are you?",
            new Vector2(0.5f, 0.5f), 30f, new Color(1f, 1f, 1f, 0.85f), FontStyles.Italic);

        // Stats
        string stats = hud != null
            ? $"Final DC:  <b>{hud.GetCoins()}</b>\nDopamine:  <b>{Mathf.RoundToInt(hud.GetDopamine())}%</b>"
            : "";
        var statText = MakeText(canvasGO.transform, "Stats", stats,
            new Vector2(0.5f, 0.38f), 28f, new Color(1f, 0.85f, 0.25f), FontStyles.Bold);
        statText.alignment = TextAlignmentOptions.Center;

        // Buttons
        MakeButton(canvasGO.transform, "RestartBtn", "PLAY AGAIN",
            new Vector2(0.5f, 0.22f), new Vector2(340, 68),
            new Color(0.22f, 0.68f, 0.35f), DoRestart);

        MakeButton(canvasGO.transform, "QuitBtn", "QUIT TO DESKTOP",
            new Vector2(0.5f, 0.13f), new Vector2(340, 68),
            new Color(0.3f, 0.3f, 0.35f), DoQuit);
    }

    void DoRestart()
    {
        Time.timeScale = 1f;
        victoryTriggered = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void DoQuit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    TMP_Text MakeText(Transform parent, string name, string text,
        Vector2 anchor, float fontSize, Color color, FontStyles style)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) tmp.font = font;

        var rt = (RectTransform)obj.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1400, 220);
        return tmp;
    }

    void MakeButton(Transform parent, string name, string label,
        Vector2 anchor, Vector2 size, Color bgColor, System.Action onClick)
    {
        var btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);

        var img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = new Color(
            Mathf.Min(1f, bgColor.r + 0.15f),
            Mathf.Min(1f, bgColor.g + 0.15f),
            Mathf.Min(1f, bgColor.b + 0.15f), 1f);
        colors.pressedColor = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        var rt = (RectTransform)btnObj.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) tmp.font = font;

        var trt = (RectTransform)textObj.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
