using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TvFlappyBird : MonoBehaviour
{
    [Header("Interaction")]
    public float interactionDistance = 8f;

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

    // Кулдаун на нажатие E (чтобы при выходе не триггерилась снова активация)
    private float interactionCooldown = 0f;

    private GameObject playerObj;
    private MonoBehaviour[] disabledScripts;

    Camera FindPlayerCam()
    {
        if (playerObj == null)
        {
            playerObj = GameObject.Find("player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null) playerObj = tagged;
            }
        }
        if (playerObj != null)
        {
            // Ищем только АКТИВНУЮ камеру внутри player
            var cams = playerObj.GetComponentsInChildren<Camera>(false);
            foreach (var c in cams)
            {
                if (c != null && c.gameObject.activeInHierarchy) return c;
            }
            // Если активной нет, берём любую (даже неактивную)
            var anyCam = playerObj.GetComponentInChildren<Camera>(true);
            if (anyCam != null) return anyCam;
        }
        return Camera.main;
    }

    void Start()
    {
        playerCam = FindPlayerCam();

        // Проверка коллайдера на TV
        var col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[TvFlappyBird] На TV не было Collider — добавляю BoxCollider автоматически. Настрой его размер!", gameObject);
            gameObject.AddComponent<BoxCollider>();
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Уменьшаем кулдаун
        if (interactionCooldown > 0f) interactionCooldown -= Time.deltaTime;

        // F1 — отладка
        if (Keyboard.current.f1Key != null && Keyboard.current.f1Key.wasPressedThisFrame)
            DebugState();

        if (!isPlaying)
        {
            CheckInteraction();
        }
        else
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                Keyboard.current.eKey.wasPressedThisFrame)
            { ExitGame(); return; }

            if (isGameOver)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame) RestartGame();
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

            if (pos.y < -5f || pos.y > 8f) { GameOver(); return; }

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval) { SpawnPipe(); spawnTimer = 0f; }

            MovePipesAndCheck();
        }
    }

    void DebugState()
    {
        Debug.Log("=== TvFlappyBird Debug ===");
        Debug.Log("playerCam: " + (playerCam != null ? playerCam.name + " (active=" + playerCam.gameObject.activeInHierarchy + ")" : "NULL"));
        Debug.Log("playerObj: " + (playerObj != null ? playerObj.name : "NULL"));
        var col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        Debug.Log("TV Collider: " + (col != null ? col.name : "NULL"));
        if (playerCam != null)
        {
            float dist = Vector3.Distance(playerCam.transform.position, transform.position);
            Vector3 toTv = (transform.position - playerCam.transform.position).normalized;
            float dot = Vector3.Dot(playerCam.transform.forward, toTv);
            Debug.Log("Distance: " + dist + " | Dot: " + dot);
        }
    }

    void CheckInteraction()
    {
        // Кулдаун блокирует E
        if (interactionCooldown > 0f) return;

        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        // ВАЖНО: всегда заново ищем активную камеру игрока
        if (playerCam == null || !playerCam.gameObject.activeInHierarchy)
            playerCam = FindPlayerCam();
        if (playerCam == null || !playerCam.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[TvFlappyBird] Не найдена активная камера игрока");
            return;
        }

        Vector3 origin = playerCam.transform.position;
        Vector3 dir = playerCam.transform.forward;

        Debug.DrawRay(origin, dir * interactionDistance, Color.red, 2f);

        bool hitTv = false;

        // 1) Обычный Raycast
        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, interactionDistance))
        {
            if (IsThisTv(hit.collider)) hitTv = true;
            else Debug.Log("[TvFlappyBird] Raycast попал в: " + hit.collider.name + " (не TV)");
        }

        // 2) SphereCast — только если Raycast не попал прямо в TV
        if (!hitTv)
        {
            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.4f, dir, interactionDistance);
            foreach (var h in hits)
            {
                if (h.collider != null && IsThisTv(h.collider))
                {
                    // Дополнительно проверим что игрок действительно смотрит на TV
                    Vector3 toTv = (transform.position - origin).normalized;
                    float dot = Vector3.Dot(dir, toTv);
                    if (dot > 0.5f) { hitTv = true; break; }
                }
            }
        }

        if (hitTv)
        {
            Debug.Log("[TvFlappyBird] TV активирован!");
            StartGame();
        }
    }

    // Проверка что коллайдер принадлежит этому TV
    bool IsThisTv(Collider col)
    {
        if (col == null) return false;
        return col.gameObject == gameObject ||
               col.transform.IsChildOf(transform) ||
               transform.IsChildOf(col.transform);
    }

    void StartGame()
    {
        if (isPlaying) return;
        isPlaying = true;
        interactionCooldown = 0.5f; // защита от двойного срабатывания
        DisablePlayer();

        miniGameRoot = new GameObject("FlappyBirdMiniGame");
        miniGameRoot.transform.position = new Vector3(1000f, 1000f, 1000f);

        var camObj = new GameObject("FlappyCam");
        camObj.transform.SetParent(miniGameRoot.transform);
        camObj.transform.localPosition = new Vector3(0f, 2f, -12f);
        camObj.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
        miniGameCam = camObj.AddComponent<Camera>();
        miniGameCam.clearFlags = CameraClearFlags.SolidColor;
        miniGameCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        miniGameCam.fieldOfView = 60f;
        miniGameCam.depth = 10;

        if (playerCam != null) playerCam.gameObject.SetActive(false);

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
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.2f));
            playerCube.GetComponent<Renderer>().material = mat;
        }
        Object.Destroy(playerCube.GetComponent<Collider>());

        // Floor & ceiling
        CreateBox("Floor", new Vector3(5f, -5.5f, 0f), new Vector3(60f, 1f, 4f), new Color(0.2f, 0.6f, 0.2f));
        CreateBox("Ceiling", new Vector3(5f, 8.5f, 0f), new Vector3(60f, 1f, 4f), new Color(0.3f, 0.3f, 0.35f));

        // Light
        var lightObj = new GameObject("FlappyLight");
        lightObj.transform.SetParent(miniGameRoot.transform);
        lightObj.transform.localPosition = new Vector3(0f, 10f, -5f);
        lightObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        var lt = lightObj.AddComponent<Light>();
        lt.type = LightType.Directional; lt.intensity = 1.2f;

        // UI
        var canvasObj = new GameObject("FlappyCanvas");
        canvasObj.transform.SetParent(miniGameRoot.transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        scoreText = MakeText(canvasObj.transform, "Score", "0", 72,
            new Vector2(0.5f, 1f), new Vector2(0f, -30f));
        scoreText.color = Color.white;

        infoText = MakeText(canvasObj.transform, "Info", "SPACE — jump  |  E/ESC — exit", 28,
            new Vector2(0.5f, 0f), new Vector2(0f, 25f));
        infoText.color = new Color(1f, 1f, 1f, 0.6f);

        score = 0; spawnTimer = 0f; playerVelocityY = 0f; isGameOver = false;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    void ExitGame()
    {
        if (!isPlaying) return;
        isPlaying = false; isGameOver = false;
        interactionCooldown = 0.5f; // ВАЖНО: блокируем E после выхода

        if (miniGameRoot != null) Object.Destroy(miniGameRoot);

        // Сбрасываем кэш камеры и ищем её заново
        playerCam = null;
        playerCam = FindPlayerCam();
        if (playerCam != null) playerCam.gameObject.SetActive(true);

        EnablePlayer();
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }

    void RestartGame()
    {
        isGameOver = false; score = 0; playerVelocityY = 0f; spawnTimer = 0f;
        if (scoreText != null) scoreText.text = "0";
        if (infoText != null) { infoText.text = "SPACE — jump  |  E/ESC — exit"; infoText.color = new Color(1, 1, 1, 0.6f); infoText.fontSize = 28; }
        if (playerCube != null) { playerCube.transform.localPosition = playerStartPos; playerCube.transform.localRotation = Quaternion.identity; }
        if (miniGameRoot != null)
            for (int i = miniGameRoot.transform.childCount - 1; i >= 0; i--)
            { var c = miniGameRoot.transform.GetChild(i); if (c.name.StartsWith("PipePair")) Object.Destroy(c.gameObject); }
    }

    void SpawnPipe()
    {
        float gapCenter = Random.Range(-1.5f, 5.5f);
        var pair = new GameObject("PipePair");
        pair.transform.SetParent(miniGameRoot.transform);
        pair.transform.localPosition = new Vector3(20f, 0f, 0f);

        float bTop = gapCenter - gapSize / 2f;
        float bH = bTop + 5f;
        MakePipe(pair.transform, "Bot", new Vector3(0f, (bTop + (-5f)) / 2f, 0f), new Vector3(pipeWidth, Mathf.Max(0.1f, bH), 2f));

        float tBot = gapCenter + gapSize / 2f;
        float tH = 8f - tBot;
        MakePipe(pair.transform, "Top", new Vector3(0f, (tBot + 8f) / 2f, 0f), new Vector3(pipeWidth, Mathf.Max(0.1f, tH), 2f));
    }

    void MovePipesAndCheck()
    {
        if (miniGameRoot == null || playerCube == null) return;
        float px = playerCube.transform.localPosition.x, py = playerCube.transform.localPosition.y, ph = 0.3f;

        for (int i = miniGameRoot.transform.childCount - 1; i >= 0; i--)
        {
            var child = miniGameRoot.transform.GetChild(i);
            if (!child.name.StartsWith("PipePair")) continue;
            child.localPosition += Vector3.left * pipeSpeed * Time.deltaTime;
            if (child.localPosition.x < -destroyDistance) { Object.Destroy(child.gameObject); continue; }

            float pipeX = child.localPosition.x;
            float prevX = pipeX + pipeSpeed * Time.deltaTime;
            if (prevX >= px && pipeX < px)
            {
                score++;
                if (scoreText != null) scoreText.text = score.ToString();

                // Award dopamine & coins for passing a pipe
                if (GameEconomy.Instance != null)
                    GameEconomy.Instance.AwardDopamine(GameEconomy.ActFlappy);
            }

            float phw = pipeWidth / 2f;
            if (px + ph > pipeX - phw && px - ph < pipeX + phw)
            {
                foreach (Transform pc in child)
                {
                    float pcy = pc.localPosition.y, pchh = pc.localScale.y / 2f;
                    if (py + ph > pcy - pchh && py - ph < pcy + pchh) { GameOver(); return; }
                }
            }
        }
    }

    void GameOver()
    {
        isGameOver = true; playerVelocityY = 0f;

        // Bonus coins based on final score
        int bonusCoins = score * 5;
        if (bonusCoins > 0 && GameEconomy.Instance != null)
            GameEconomy.Instance.AddCoins(bonusCoins);

        string bonusMsg = bonusCoins > 0 ? "\n+" + bonusCoins + " DC bonus!" : "";
        if (infoText != null) { infoText.text = "GAME OVER! Score: " + score + bonusMsg + "\nSPACE — restart  |  E/ESC — exit"; infoText.color = new Color(1f, 0.3f, 0.3f, 1f); infoText.fontSize = 36; }
    }

    void DisablePlayer()
    {
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            disabledScripts = playerObj.GetComponents<MonoBehaviour>();
            foreach (var s in disabledScripts) if (s != null && s.enabled) s.enabled = false;
        }
    }

    void EnablePlayer()
    {
        if (disabledScripts != null) { foreach (var s in disabledScripts) if (s != null) s.enabled = true; disabledScripts = null; }
    }

    void CreateBox(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name; obj.transform.SetParent(miniGameRoot.transform);
        obj.transform.localPosition = pos; obj.transform.localScale = scale;
        Object.Destroy(obj.GetComponent<Collider>());
        var s = Shader.Find("Universal Render Pipeline/Lit");
        if (s != null) { var m = new Material(s); m.SetColor("_BaseColor", color); obj.GetComponent<Renderer>().material = m; }
    }

    void MakePipe(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name; obj.transform.SetParent(parent);
        obj.transform.localPosition = pos; obj.transform.localScale = scale;
        Object.Destroy(obj.GetComponent<Collider>());
        var s = Shader.Find("Universal Render Pipeline/Lit");
        if (s != null) { var m = new Material(s); m.SetColor("_BaseColor", new Color(0.2f, 0.75f, 0.3f)); obj.GetComponent<Renderer>().material = m; }
    }

    TMP_Text MakeText(Transform parent, string name, string text, float size, Vector2 anchor, Vector2 pos)
    {
        var obj = new GameObject(name); obj.transform.SetParent(parent);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, anchor.y > 0.5f ? 1f : 0f);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(600f, 100f);
        return tmp;
    }

    void OnDestroy() { if (isPlaying) ExitGame(); }
}

