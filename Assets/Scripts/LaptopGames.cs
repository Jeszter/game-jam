using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to Laptop. Press E to open game selection menu.
/// E or Escape to exit back to room.
/// </summary>
public class LaptopGames : MonoBehaviour
{
    public float interactDistance = 3f;

    private Camera playerCam;
    private GameObject playerObj;
    private MonoBehaviour[] disabledScripts;

    private enum State { Idle, Menu, Playing }
    private State state = State.Idle;

    private GameObject menuRoot;
    private Camera menuCam;
    private SubwayGame subwayGame;
    private PoliceChaseGame policeGame;

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
                if (Keyboard.current.digit1Key.wasPressedThisFrame)
                    LaunchGame(1);
                if (Keyboard.current.digit2Key.wasPressedThisFrame)
                    LaunchGame(2);
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
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, interactDistance)) return;
        if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform)) return;

        OpenMenu();
    }

    void OpenMenu()
    {
        state = State.Menu;
        DisablePlayer();

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

        // Canvas — use ScreenSpaceOverlay for reliability
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
        MakeLabel(canvasObj.transform, "Title", "CHOOSE A GAME", 72,
            new Vector2(0.5f, 0.82f), Color.white);

        // Game 1
        MakeLabel(canvasObj.transform, "G1", "[1]  SUBWAY SURFER", 48,
            new Vector2(0.5f, 0.55f), new Color(0.2f, 1f, 0.4f));
        MakeLabel(canvasObj.transform, "G1d", "Endless runner — dodge obstacles", 26,
            new Vector2(0.5f, 0.48f), new Color(0.7f, 0.7f, 0.7f));

        // Game 2
        MakeLabel(canvasObj.transform, "G2", "[2]  POLICE CHASE", 48,
            new Vector2(0.5f, 0.35f), new Color(1f, 0.3f, 0.3f));
        MakeLabel(canvasObj.transform, "G2d", "Race through traffic — escape the cops", 26,
            new Vector2(0.5f, 0.28f), new Color(0.7f, 0.7f, 0.7f));

        // Exit
        MakeLabel(canvasObj.transform, "Exit", "E / ESC — back to room", 28,
            new Vector2(0.5f, 0.1f), new Color(1f, 1f, 1f, 0.4f));

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LaunchGame(int id)
    {
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
        }
        else
        {
            policeGame = menuRoot.AddComponent<PoliceChaseGame>();
            policeGame.Init(menuRoot, menuCam);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void CloseAll()
    {
        if (subwayGame != null) { subwayGame.Cleanup(); Destroy(subwayGame); subwayGame = null; }
        if (policeGame != null) { policeGame.Cleanup(); Destroy(policeGame); policeGame = null; }

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

    void MakeLabel(Transform parent, string name, string text, float size, Vector2 anchor, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(1200f, 80f);
    }

    void OnDestroy()
    {
        if (state != State.Idle) CloseAll();
    }
}
