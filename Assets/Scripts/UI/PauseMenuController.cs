using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Простое меню паузы: Escape -> Pause / Resume.
/// Автоматически создаёт UI поверх основного Canvas.
/// Останавливает игру через Time.timeScale = 0, скрывает/показывает курсор,
/// отключает PlayerMovement look (phoneLock).
/// </summary>
[DefaultExecutionOrder(-50)]
public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas pauseCanvas;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Options")]
    [SerializeField] private bool blockWhenMiniGameOpen = true;

    private bool isPaused;
    public static PauseMenuController Instance { get; private set; }
    public bool IsPaused => isPaused;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        CreateUI();
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else
            {
                if (blockWhenMiniGameOpen && MiniGameActive()) return;
                Pause();
            }
        }
    }

    private bool MiniGameActive()
    {
        if (GameObject.Find("FlappyBirdMiniGame") != null) return true;
        if (GameObject.Find("LaptopMenuRoot") != null) return true;
        if (GameObject.Find("SubwayMiniGame") != null) return true;
        if (GameObject.Find("PoliceChaseGame") != null) return true;
        if (GameObject.Find("CasinoGameRoot") != null) return true;
        return false;
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPlayerLookEnabled(false);
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPlayerLookEnabled(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToDesktop()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPlayerLookEnabled(bool enabled)
    {
        GameObject player = GameObject.Find("player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null) pm.phoneLock = !enabled;
        }
    }

    // =================================================================
    // AUTO-CREATE UI
    // =================================================================
    void CreateUI()
    {
        if (pauseCanvas != null) return;

        var canvasGO = new GameObject("PauseMenuCanvas");
        canvasGO.transform.SetParent(transform, false);
        pauseCanvas = canvasGO.AddComponent<Canvas>();
        pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        pauseCanvas.sortingOrder = 9999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Full-screen panel (dim)
        pausePanel = new GameObject("PausePanel", typeof(RectTransform));
        pausePanel.transform.SetParent(pauseCanvas.transform, false);
        var prt = (RectTransform)pausePanel.transform;
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        var dim = pausePanel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.78f);

        // Menu box
        var box = new GameObject("MenuBox", typeof(RectTransform));
        box.transform.SetParent(pausePanel.transform, false);
        var brt = (RectTransform)box.transform;
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(520f, 520f);
        brt.anchoredPosition = Vector2.zero;
        var boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.08f, 0.05f, 0.12f, 0.98f);
        var outline = box.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.28f, 1f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Title
        var title = MakeText(box.transform, "Title", "PAUSED", 72, FontStyles.Bold,
            new Color(1f, 0.92f, 0.35f));
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(500f, 100f);
        trt.anchoredPosition = new Vector2(0f, -30f);
        title.alignment = TextAlignmentOptions.Center;

        // Buttons
        resumeButton  = MakeButton(box.transform, "ResumeBtn",  "RESUME",  0,
                                   new Color(0.22f, 0.68f, 0.35f), Resume);
        restartButton = MakeButton(box.transform, "RestartBtn", "RESTART", 1,
                                   new Color(0.85f, 0.55f, 0.15f), Restart);
        quitButton    = MakeButton(box.transform, "QuitBtn",    "QUIT",    2,
                                   new Color(0.78f, 0.2f, 0.25f), QuitToDesktop);

        // Hint
        var hint = MakeText(box.transform, "Hint", "Press ESC to resume", 22,
                            FontStyles.Italic, new Color(1f, 1f, 1f, 0.6f));
        var hrt = hint.rectTransform;
        hrt.anchorMin = new Vector2(0.5f, 0f);
        hrt.anchorMax = new Vector2(0.5f, 0f);
        hrt.pivot = new Vector2(0.5f, 0f);
        hrt.sizeDelta = new Vector2(500f, 50f);
        hrt.anchoredPosition = new Vector2(0f, 20f);
        hint.alignment = TextAlignmentOptions.Center;
    }

    Button MakeButton(Transform parent, string name, string text, int idx,
                      Color bg, System.Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(380f, 72f);
        rt.anchoredPosition = new Vector2(0f, 60f - idx * 90f);

        var img = go.AddComponent<Image>();
        img.color = bg;

        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = bg;
        cb.highlightedColor = new Color(
            Mathf.Min(1f, bg.r + 0.18f),
            Mathf.Min(1f, bg.g + 0.18f),
            Mathf.Min(1f, bg.b + 0.18f), 1f);
        cb.pressedColor = new Color(bg.r * 0.65f, bg.g * 0.65f, bg.b * 0.65f, 1f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        var label = MakeText(go.transform, "Text", text, 30, FontStyles.Bold, Color.white);
        var lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return btn;
    }

    TMP_Text MakeText(Transform parent, string name, string text, float size,
                      FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.alignment = TextAlignmentOptions.Center;
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) t.font = font;
        return t;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
