using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Handles game over (death) when dopamine or hunger reaches 0,
/// and pause menu on ESC.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("Death Settings")]
    public float deathDelay = 1.5f;
    public float deathFadeDuration = 2f;

    private bool isDead = false;
    private bool isPaused = false;
    private bool deathTriggered = false;

    private GameObject deathCanvas;
    private CanvasGroup deathCanvasGroup;
    private GameObject pauseCanvas;

    private GameHUDController hud;
    private GameEconomy economy;

    // Grace period: don't trigger death during the opening cutscene / very first seconds.
    [Header("Safety")]
    [Tooltip("Seconds after load during which death checks are disabled (prevents false game-over during intro cutscene when HUD/economy are still initializing).")]
    public float startupGracePeriod = 4f;
    private float startupTimer = 0f;

    void Start()
    {
        hud = FindFirstObjectByType<GameHUDController>();
        economy = GameEconomy.Instance;
    }

    void Update()
    {
        if (economy == null) economy = GameEconomy.Instance;
        if (hud == null) hud = FindFirstObjectByType<GameHUDController>();

        // Grace period: skip death checks until managers are initialized.
        if (startupTimer < startupGracePeriod)
        {
            startupTimer += Time.unscaledDeltaTime;
            return;
        }

        // Also skip while the wake-up cutscene is running.
        var cutscene = FindFirstObjectByType<BedWakeCutscene>();
        if (cutscene != null && cutscene.isActiveAndEnabled) return;

        if (!isDead && !deathTriggered)
        {
            bool dopamineDead = hud != null && hud.GetDopamine() <= 0f;
            bool hungerDead = economy != null && economy.CurrentHunger <= 0f;

            if (dopamineDead || hungerDead)
            {
                deathTriggered = true;
                string cause = dopamineDead ? "DOPAMINE DEPLETED" : "STARVED TO DEATH";
                StartCoroutine(DeathSequence(cause));
            }
        }

        // Пауза вынесена в PauseMenuController — здесь ничего не делаем.
    }

    private bool CheckMiniGameActive()
    {
        if (GameObject.Find("FlappyBirdMiniGame") != null) return true;
        if (GameObject.Find("LaptopMenuRoot") != null) return true;
        var phone = FindFirstObjectByType<GamePhoneController>();
        if (phone != null)
        {
            var cg = phone.GetComponentInChildren<CanvasGroup>();
            if (cg != null && cg.alpha > 0.5f && cg.interactable) return true;
        }
        return false;
    }

    private IEnumerator DeathSequence(string causeOfDeath)
    {
        yield return new WaitForSeconds(deathDelay);

        isDead = true;
        DisablePlayer();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CreateDeathScreen(causeOfDeath);

        float elapsed = 0f;
        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / deathFadeDuration);
            float alpha = t * t;
            if (deathCanvasGroup != null)
                deathCanvasGroup.alpha = alpha;
            Time.timeScale = Mathf.Lerp(1f, 0f, t * t);
            yield return null;
        }

        Time.timeScale = 0f;
        if (deathCanvasGroup != null)
            deathCanvasGroup.alpha = 1f;
    }

    private void CreateDeathScreen(string causeOfDeath)
    {
        deathCanvas = new GameObject("DeathCanvas");
        var canvas = deathCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var scaler = deathCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        deathCanvas.AddComponent<GraphicRaycaster>();

        deathCanvasGroup = deathCanvas.AddComponent<CanvasGroup>();
        deathCanvasGroup.alpha = 0f;

        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(deathCanvas.transform, false);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0.05f, 0f, 0f, 0.85f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;

        var diedText = MakeText(deathCanvas.transform, "DiedText", "YOU DIED",
            new Vector2(0.5f, 0.55f), 120f, new Color(0.8f, 0.1f, 0.1f, 1f), FontStyles.Bold);
        diedText.characterSpacing = 12f;

        MakeText(deathCanvas.transform, "CauseText", causeOfDeath,
            new Vector2(0.5f, 0.44f), 36f, new Color(1f, 0.4f, 0.3f, 0.9f), FontStyles.Normal);

        string scoreInfo = "";
        if (hud != null)
            scoreInfo = "Final DC: " + hud.GetCoins();
        MakeText(deathCanvas.transform, "ScoreText", scoreInfo,
            new Vector2(0.5f, 0.36f), 28f, new Color(1f, 0.85f, 0.2f, 0.8f), FontStyles.Bold);

        MakeButton(deathCanvas.transform, "RestartBtn", "RESTART",
            new Vector2(0.5f, 0.22f), new Vector2(300, 60),
            new Color(0.8f, 0.15f, 0.15f, 1f), DoRestart);

        MakeButton(deathCanvas.transform, "QuitBtn", "QUIT",
            new Vector2(0.5f, 0.13f), new Vector2(300, 60),
            new Color(0.3f, 0.3f, 0.35f, 1f), DoQuit);
    }

    private void DoPause()
    {
        if (isDead) return;
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CreatePauseScreen();
    }

    private void DoResume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (pauseCanvas != null)
            Destroy(pauseCanvas);
    }

    private void CreatePauseScreen()
    {
        pauseCanvas = new GameObject("PauseCanvas");
        var canvas = pauseCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998;
        var scaler = pauseCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        pauseCanvas.AddComponent<GraphicRaycaster>();

        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(pauseCanvas.transform, false);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.7f);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;

        MakeText(pauseCanvas.transform, "PausedText", "PAUSED",
            new Vector2(0.5f, 0.6f), 90f, Color.white, FontStyles.Bold);

        MakeButton(pauseCanvas.transform, "ResumeBtn", "RESUME",
            new Vector2(0.5f, 0.45f), new Vector2(300, 60),
            new Color(0.2f, 0.8f, 0.4f, 1f), DoResume);

        MakeButton(pauseCanvas.transform, "RestartBtn", "RESTART",
            new Vector2(0.5f, 0.35f), new Vector2(300, 60),
            new Color(0.9f, 0.6f, 0.1f, 1f), DoRestart);

        MakeButton(pauseCanvas.transform, "QuitBtn", "QUIT",
            new Vector2(0.5f, 0.25f), new Vector2(300, 60),
            new Color(0.8f, 0.2f, 0.2f, 1f), DoQuit);
    }

    private void DoRestart()
    {
        Time.timeScale = 1f;
        isDead = false;
        deathTriggered = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void DoQuit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void DisablePlayer()
    {
        var player = GameObject.Find("player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            var scripts = player.GetComponents<MonoBehaviour>();
            foreach (var s in scripts)
                if (s != null && s.enabled) s.enabled = false;
        }
    }

    private TMP_Text MakeText(Transform parent, string name, string text,
        Vector2 anchor, float fontSize, Color color, FontStyles style)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) tmp.font = font;

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1200, 150);
        return tmp;
    }

    private void MakeButton(Transform parent, string name, string label,
        Vector2 anchor, Vector2 size, Color bgColor, System.Action onClick)
    {
        var btnObj = new GameObject(name);
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

        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) tmp.font = font;

        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
