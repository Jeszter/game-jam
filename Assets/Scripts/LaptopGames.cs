using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to Laptop. Press E to open game selection menu.
/// Select game by clicking its icon OR pressing 1/2/3. E or Escape to exit.
/// </summary>
public class LaptopGames : MonoBehaviour
{
    public float interactDistance = 3f;

    [Header("Game Icons (assign in Inspector)")]
    public Sprite iconSubwaySurf;
    public Sprite iconPoliceChase;
    public Sprite iconCasino;

    private Camera playerCam;
    private GameObject playerObj;
    private MonoBehaviour[] disabledScripts;

    private enum State { Idle, Menu, Playing }
    private State state = State.Idle;

    private GameObject menuRoot;
    private Camera menuCam;
    private SubwayGame subwayGame;
    private PoliceChaseGame policeGame;
    private CasinoGame casinoGame;

    private TMP_FontAsset font;

    void Start()
    {
        playerObj = GameObject.Find("player");
        if (playerObj != null)
            playerCam = playerObj.GetComponentInChildren<Camera>();
        if (playerCam == null)
            playerCam = Camera.main;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        switch (state)
        {
            case State.Idle:
                CheckInteraction();
                break;
            case State.Menu:
                if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                    Keyboard.current.eKey.wasPressedThisFrame)
                { CloseAll(); return; }
                if (Keyboard.current.digit1Key.wasPressedThisFrame) LaunchGame(1);
                if (Keyboard.current.digit2Key.wasPressedThisFrame) LaunchGame(2);
                if (Keyboard.current.digit3Key.wasPressedThisFrame) LaunchGame(3);
                break;
            case State.Playing:
                if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                    Keyboard.current.eKey.wasPressedThisFrame)
                { CloseAll(); return; }
                break;
        }
    }

    void CheckInteraction()
    {
        if (playerCam == null || !playerCam.gameObject.activeInHierarchy)
        {
            playerObj = GameObject.Find("player");
            if (playerObj != null) playerCam = playerObj.GetComponentInChildren<Camera>();
            if (playerCam == null) return;
        }
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        var hits = Physics.SphereCastAll(ray.origin, 0.3f, ray.direction, interactDistance);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
            {
                OpenMenu();
                return;
            }
        }
    }

    void OpenMenu()
    {
        state = State.Menu;
        DisablePlayer();

        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        menuRoot = new GameObject("LaptopMenuRoot");
        menuRoot.transform.position = new Vector3(2000f, 2000f, 2000f);

        // Camera
        var camObj = new GameObject("MenuCam");
        camObj.transform.SetParent(menuRoot.transform);
        camObj.transform.localPosition = Vector3.zero;
        menuCam = camObj.AddComponent<Camera>();
        menuCam.clearFlags = CameraClearFlags.SolidColor;
        menuCam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        menuCam.depth = 10;

        if (playerCam != null) playerCam.gameObject.SetActive(false);

        // Canvas
        var canvasObj = new GameObject("MenuCanvas");
        canvasObj.transform.SetParent(menuRoot.transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background panel
        var bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.12f, 1f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Title
        MakeLabel(canvasObj.transform, "Title", "💻  CHOOSE A GAME  💻", 80,
            new Vector2(0.5f, 0.88f), Color.white, FontStyles.Bold);

        MakeLabel(canvasObj.transform, "Sub", "Click an icon or press 1 / 2 / 3", 24,
            new Vector2(0.5f, 0.81f), new Color(0.7f, 0.7f, 0.8f), FontStyles.Italic);

        // App grid — 3 icon buttons
        MakeAppIcon(canvasObj.transform, "App1", "🏃", "[1]  SUBWAY SURFER",
            "Endless runner — dodge obstacles",
            new Vector2(0.2f, 0.45f), new Color(0.2f, 1f, 0.4f),
            () => LaunchGame(1), iconSubwaySurf);

        MakeAppIcon(canvasObj.transform, "App2", "🚓", "[2]  POLICE CHASE",
            "Race through traffic — escape the cops",
            new Vector2(0.5f, 0.45f), new Color(1f, 0.3f, 0.3f),
            () => LaunchGame(2), iconPoliceChase);

        MakeAppIcon(canvasObj.transform, "App3", "🎰", "[3]  DOOM CASINO",
            "Slot machine — bet your DC coins",
            new Vector2(0.8f, 0.45f), new Color(1f, 0.85f, 0.2f),
            () => LaunchGame(3), iconCasino);

        // Exit hint
        MakeLabel(canvasObj.transform, "Exit", "E / ESC — back to room", 26,
            new Vector2(0.5f, 0.08f), new Color(1f, 1f, 1f, 0.45f), FontStyles.Normal);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LaunchGame(int id)
    {
        if (state == State.Playing) return;
        state = State.Playing;

        // Destroy menu canvas (keep root + camera)
        foreach (Transform child in menuRoot.transform)
        {
            if (child.GetComponent<Camera>() == null)
                Destroy(child.gameObject);
        }

        if (id == 1)
        {
            subwayGame = menuRoot.AddComponent<SubwayGame>();
            subwayGame.Init(menuRoot, menuCam);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (id == 2)
        {
            policeGame = menuRoot.AddComponent<PoliceChaseGame>();
            policeGame.Init(menuRoot, menuCam);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (id == 3)
        {
            casinoGame = menuRoot.AddComponent<CasinoGame>();
            casinoGame.Init(menuRoot, menuCam);
            // Casino uses mouse — keep cursor visible
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void CloseAll()
    {
        if (subwayGame != null) { subwayGame.Cleanup(); Destroy(subwayGame); subwayGame = null; }
        if (policeGame != null) { policeGame.Cleanup(); Destroy(policeGame); policeGame = null; }
        if (casinoGame != null) { casinoGame.Cleanup(); Destroy(casinoGame); casinoGame = null; }

        state = State.Idle;
        if (menuRoot != null) Destroy(menuRoot);
        if (playerCam != null) playerCam.gameObject.SetActive(true);

        EnablePlayer();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void DisablePlayer()
    {
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj != null)
        {
            disabledScripts = playerObj.GetComponents<MonoBehaviour>();
            foreach (var s in disabledScripts)
                if (s != null && s.enabled) s.enabled = false;
        }
    }

    void EnablePlayer()
    {
        if (disabledScripts != null)
        {
            foreach (var s in disabledScripts)
                if (s != null) s.enabled = true;
            disabledScripts = null;
        }
    }

    void MakeLabel(Transform parent, string name, string text, float size, Vector2 anchor, Color color, FontStyles style)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = style;
        if (font != null) tmp.font = font;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(1600, 120);
    }

    void MakeAppIcon(Transform parent, string name, string emoji, string label, string desc,
        Vector2 anchor, Color accent, System.Action onClick, Sprite iconSprite = null)
    {
        // Button panel
        var panel = new GameObject(name);
        panel.transform.SetParent(parent);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.16f, 1f);
        var btn = panel.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = new Color(0.1f, 0.1f, 0.16f, 1f);
        cb.highlightedColor = new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 1f);
        cb.pressedColor = new Color(accent.r * 0.6f, accent.g * 0.6f, accent.b * 0.6f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => {
            var sm = SoundManager.Instance;
            if (sm != null) sm.PlayMenuClick();
            onClick();
        });

        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = anchor; prt.anchorMax = anchor;
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(380, 480);
        prt.anchoredPosition = Vector2.zero;

        // Colored border strip at top
        var strip = new GameObject("Strip");
        strip.transform.SetParent(panel.transform);
        var stripImg = strip.AddComponent<Image>();
        stripImg.color = accent;
        var srt = strip.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1);
        srt.pivot = new Vector2(0.5f, 1);
        srt.sizeDelta = new Vector2(0, 8);
        srt.anchoredPosition = Vector2.zero;

        // Icon — use sprite image if assigned, otherwise fallback to emoji text
        if (iconSprite != null)
        {
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(panel.transform);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            var irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.05f, 0.38f);
            irt.anchorMax = new Vector2(0.95f, 0.95f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
        }
        else
        {
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(panel.transform);
            var iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
            iconTmp.text = emoji;
            iconTmp.fontSize = 180;
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.color = Color.white;
            if (font != null) iconTmp.font = font;
            var irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0.4f);
            irt.anchorMax = new Vector2(1, 0.95f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
        }

        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(panel.transform);
        var ltmp = labelObj.AddComponent<TextMeshProUGUI>();
        ltmp.text = label;
        ltmp.fontSize = 30;
        ltmp.color = accent;
        ltmp.fontStyle = FontStyles.Bold;
        ltmp.alignment = TextAlignmentOptions.Center;
        if (font != null) ltmp.font = font;
        var lrt = labelObj.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0, 0.22f); lrt.anchorMax = new Vector2(1, 0.38f);
        lrt.offsetMin = new Vector2(10, 0); lrt.offsetMax = new Vector2(-10, 0);

        // Description
        var descObj = new GameObject("Desc");
        descObj.transform.SetParent(panel.transform);
        var dtmp = descObj.AddComponent<TextMeshProUGUI>();
        dtmp.text = desc;
        dtmp.fontSize = 20;
        dtmp.color = new Color(0.75f, 0.75f, 0.8f);
        dtmp.alignment = TextAlignmentOptions.Center;
        dtmp.textWrappingMode = TextWrappingModes.Normal;
        if (font != null) dtmp.font = font;
        var drt = descObj.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0, 0.04f); drt.anchorMax = new Vector2(1, 0.22f);
        drt.offsetMin = new Vector2(15, 0); drt.offsetMax = new Vector2(-15, 0);
    }

    void OnDestroy()
    {
        if (state != State.Idle) CloseAll();
    }
}
