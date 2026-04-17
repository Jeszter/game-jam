using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TvFlappyBird : MonoBehaviour
{
    [Header("Interaction")]
    public float interactionDistance = 5f;

    [Header("Game Settings")]
    public float pipeSpeed = 6f;
    public float spawnInterval = 1.8f;
    public float gapSize = 3.5f;
    public float pipeWidth = 1.2f;
    public float jumpForce = 6f;
    public float gravity = -15f;
    public float destroyDistance = 25f;

    private Camera playerCam;
    private Camera miniGameCam;
    private GameObject miniGameRoot;
    private GameObject playerCube;
    private TMP_Text scoreText;
    private TMP_Text infoText;

    private bool isPlaying = false;
    private bool isGameOver = false;
    private int score = 0;
    private float spawnTimer = 0f;
    private float playerVelocityY = 0f;
    private Vector3 playerStartPos;

    private GameObject playerObj;
    private MonoBehaviour[] disabledScripts;
    private float debugTimer = 0f;

    void Start()
    {
        // Find player camera
        playerObj = GameObject.Find("player");
        if (playerObj != null)
        {
            playerCam = playerObj.GetComponentInChildren<Camera>();
            Debug.Log("[TvFlappy] Found player camera: " + (playerCam != null ? playerCam.name : "NULL"));
        }
        else
        {
            playerCam = Camera.main;
            Debug.Log("[TvFlappy] No 'player' object, using Camera.main: " + (playerCam != null ? playerCam.name : "NULL"));
        }

        // Ensure colliders exist on TV children
        Collider[] cols = GetComponentsInChildren<Collider>();
        Debug.Log("[TvFlappy] TV colliders count: " + cols.Length);
        foreach (var c in cols)
            Debug.Log("[TvFlappy]   Collider on: " + c.gameObject.name + " type: " + c.GetType().Name);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (!isPlaying)
        {
            CheckInteraction();
        }
        else
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitGame();
                return;
            }

            if (isGameOver)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                    RestartGame();
                return;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                playerVelocityY = jumpForce;

            playerVelocityY += gravity * Time.deltaTime;
            Vector3 pos = playerCube.transform.localPosition;
            pos.y += playerVelocityY * Time.deltaTime;
            playerCube.transform.localPosition = pos;

            float tiltZ = Mathf.Clamp(playerVelocityY * 3f, -60f, 40f);
            playerCube.transform.localRotation = Quaternion.Euler(0, 0, tiltZ);

            if (pos.y < -5f || pos.y > 8f)
            {
                GameOver();
                return;
            }

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                SpawnPipe();
                spawnTimer = 0f;
            }

            MovePipesAndCheck();
        }
    }

    void CheckInteraction()
    {
        if (playerCam == null || !playerCam.gameObject.activeInHierarchy)
        {
            playerCam = Camera.main;
            if (playerCam == null) return;
        }

        // Periodic debug log
        debugTimer += Time.deltaTime;
        if (debugTimer > 3f)
        {
            debugTimer = 0f;
            float dist = Vector3.Distance(playerCam.transform.position, transform.position);
            Debug.Log("[TvFlappy] Distance to TV: " + dist.ToString("F1") +
                " | cam pos: " + playerCam.transform.position +
                " | tv pos: " + transform.position);

            // Also do a raycast test
            Ray testRay = new Ray(playerCam.transform.position, playerCam.transform.forward);
            RaycastHit testHit;
            if (Physics.Raycast(testRay, out testHit, 20f))
            {
                Debug.Log("[TvFlappy] Raycast hit: " + testHit.collider.gameObject.name +
                    " (parent: " + testHit.collider.transform.parent?.name + ")" +
                    " isChildOfTV: " + testHit.collider.transform.IsChildOf(transform));
            }
            else
            {
                Debug.Log("[TvFlappy] Raycast hit nothing");
            }
        }

        // Distance check
        float distance = Vector3.Distance(playerCam.transform.position, transform.position);
        if (distance > interactionDistance) return;

        // Raycast check
        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance + 2f))
        {
            bool hitTV = hit.collider.gameObject == gameObject ||
                         hit.collider.transform.IsChildOf(transform);

            if (hitTV && Keyboard.current.eKey.wasPressedThisFrame)
            {
                Debug.Log("[TvFlappy] STARTING GAME!");
                StartGame();
            }
        }
    }

    void StartGame()
    {
        if (isPlaying) return;
        isPlaying = true;

        DisablePlayerControls();

        miniGameRoot = new GameObject("FlappyBirdMiniGame");
        miniGameRoot.transform.position = new Vector3(1000f, 1000f, 1000f);

        // Camera
        GameObject camObj = new GameObject("FlappyCam");
        camObj.transform.SetParent(miniGameRoot.transform);
        camObj.transform.localPosition = new Vector3(0f, 2f, -12f);
        camObj.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
        miniGameCam = camObj.AddComponent<Camera>();
        miniGameCam.clearFlags = CameraClearFlags.SolidColor;
        miniGameCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        miniGameCam.fieldOfView = 60f;
        miniGameCam.nearClipPlane = 0.1f;
        miniGameCam.farClipPlane = 100f;

        // Disable player camera
        if (playerCam != null)
            playerCam.gameObject.SetActive(false);

        // Player cube
        playerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        playerCube.name = "FlappyPlayer";
        playerCube.transform.SetParent(miniGameRoot.transform);
        playerCube.transform.localPosition = new Vector3(-3f, 2f, 0f);
        playerCube.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        playerStartPos = playerCube.transform.localPosition;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            var playerMat = new Material(shader);
            playerMat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.2f));
            playerCube.GetComponent<Renderer>().material = playerMat;
        }
        Destroy(playerCube.GetComponent<Collider>());

        // Floor & ceiling
        CreateBox("FlappyFloor", new Vector3(5f, -5.5f, 0f), new Vector3(60f, 1f, 4f), new Color(0.2f, 0.6f, 0.2f));
        CreateBox("FlappyCeiling", new Vector3(5f, 8.5f, 0f), new Vector3(60f, 1f, 4f), new Color(0.3f, 0.3f, 0.35f));

        // Light
        GameObject lightObj = new GameObject("FlappyLight");
        lightObj.transform.SetParent(miniGameRoot.transform);
        lightObj.transform.localPosition = new Vector3(0f, 10f, -5f);
        lightObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        var lt = lightObj.AddComponent<Light>();
        lt.type = LightType.Directional;
        lt.intensity = 1.2f;

        // Canvas
        GameObject canvasObj = new GameObject("FlappyCanvas");
        canvasObj.transform.SetParent(miniGameRoot.transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = miniGameCam;
        canvas.planeDistance = 5f;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        scoreText = MakeText(canvasObj.transform, "ScoreText", "0", 72,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(300f, 100f), Color.white, TextAlignmentOptions.Top);

        infoText = MakeText(canvasObj.transform, "InfoText", "SPACE - jump\nE / ESC - exit", 28,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 30f), new Vector2(500f, 100f), new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.Center);

        score = 0;
        spawnTimer = 0f;
        playerVelocityY = 0f;
        isGameOver = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ExitGame()
    {
        if (!isPlaying) return;
        isPlaying = false;
        isGameOver = false;

        if (miniGameRoot != null) Destroy(miniGameRoot);
        if (playerCam != null) playerCam.gameObject.SetActive(true);

        EnablePlayerControls();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void RestartGame()
    {
        isGameOver = false;
        score = 0;
        playerVelocityY = 0f;
        spawnTimer = 0f;

        if (scoreText != null) scoreText.text = "0";
        if (infoText != null)
        {
            infoText.text = "SPACE - jump\nE / ESC - exit";
            infoText.color = new Color(1f, 1f, 1f, 0.7f);
            infoText.fontSize = 28;
        }

        if (playerCube != null)
        {
            playerCube.transform.localPosition = playerStartPos;
            playerCube.transform.localRotation = Quaternion.identity;
        }

        if (miniGameRoot != null)
        {
            for (int i = miniGameRoot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = miniGameRoot.transform.GetChild(i);
                if (child.name.StartsWith("PipePair"))
                    Destroy(child.gameObject);
            }
        }
    }

    void SpawnPipe()
    {
        float gapCenter = Random.Range(-1.5f, 5.5f);

        GameObject pipePair = new GameObject("PipePair");
        pipePair.transform.SetParent(miniGameRoot.transform);
        pipePair.transform.localPosition = new Vector3(20f, 0f, 0f);

        float bottomTop = gapCenter - gapSize / 2f;
        float bottomHeight = bottomTop + 5f;
        MakePipe(pipePair.transform, "BottomPipe",
            new Vector3(0f, (bottomTop + (-5f)) / 2f, 0f),
            new Vector3(pipeWidth, Mathf.Max(0.1f, bottomHeight), 2f));

        float topBottom = gapCenter + gapSize / 2f;
        float topHeight = 8f - topBottom;
        MakePipe(pipePair.transform, "TopPipe",
            new Vector3(0f, (topBottom + 8f) / 2f, 0f),
            new Vector3(pipeWidth, Mathf.Max(0.1f, topHeight), 2f));
    }

    void MovePipesAndCheck()
    {
        if (miniGameRoot == null || playerCube == null) return;

        float px = playerCube.transform.localPosition.x;
        float py = playerCube.transform.localPosition.y;
        float ph = 0.3f;

        for (int i = miniGameRoot.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = miniGameRoot.transform.GetChild(i);
            if (!child.name.StartsWith("PipePair")) continue;

            child.localPosition += Vector3.left * pipeSpeed * Time.deltaTime;

            if (child.localPosition.x < -destroyDistance)
            {
                Destroy(child.gameObject);
                continue;
            }

            float pipeX = child.localPosition.x;
            float prevPipeX = pipeX + pipeSpeed * Time.deltaTime;
            if (prevPipeX >= px && pipeX < px)
            {
                score++;
                if (scoreText != null) scoreText.text = score.ToString();
            }

            float phw = pipeWidth / 2f;
            if (px + ph > pipeX - phw && px - ph < pipeX + phw)
            {
                foreach (Transform pc in child)
                {
                    float pcy = pc.localPosition.y;
                    float pchh = pc.localScale.y / 2f;
                    if (py + ph > pcy - pchh && py - ph < pcy + pchh)
                    {
                        GameOver();
                        return;
                    }
                }
            }
        }
    }

    void GameOver()
    {
        isGameOver = true;
        playerVelocityY = 0f;
        if (infoText != null)
        {
            infoText.text = "GAME OVER!\nScore: " + score + "\n\nSPACE - restart\nE / ESC - exit";
            infoText.color = new Color(1f, 0.3f, 0.3f, 1f);
            infoText.fontSize = 36;
        }
    }

    void DisablePlayerControls()
    {
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj != null)
        {
            var allScripts = playerObj.GetComponents<MonoBehaviour>();
            disabledScripts = allScripts;
            foreach (var s in allScripts)
            {
                if (s != null && s.enabled) s.enabled = false;
            }
        }
    }

    void EnablePlayerControls()
    {
        if (disabledScripts != null)
        {
            foreach (var s in disabledScripts)
            {
                if (s != null) s.enabled = true;
            }
            disabledScripts = null;
        }
    }

    GameObject CreateBox(string name, Vector3 localPos, Vector3 localScale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(miniGameRoot.transform);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;
        Destroy(obj.GetComponent<Collider>());
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            obj.GetComponent<Renderer>().material = mat;
        }
        return obj;
    }

    void MakePipe(Transform parent, string name, Vector3 localPos, Vector3 localScale)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;
        Destroy(obj.GetComponent<Collider>());
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.2f, 0.75f, 0.3f));
            obj.GetComponent<Renderer>().material = mat;
        }
    }

    TMP_Text MakeText(Transform parent, string name, string text, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color, TextAlignmentOptions alignment)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return tmp;
    }

    void OnDestroy()
    {
        if (isPlaying) ExitGame();
    }
}
