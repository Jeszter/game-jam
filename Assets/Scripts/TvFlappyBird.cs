using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TvFlappyBird : MonoBehaviour
{
    [Header("Interaction")]
    public float interactionDistance = 8f;

    [Header("Game Settings")]
    public float pipeSpeed = 160f;           // pixels/sec
    public float spawnInterval = 1.6f;       // legacy, not used for spawning anymore
    public float pipeSpacing = 440f;         // horizontal distance between consecutive pipe pairs (px)
    public float gapSize = 220f;             // pixel gap between top/bottom pipe
    public float jumpForce = 520f;           // pixel/sec upward impulse
    public float gravity = -1400f;           // pixel/sec^2
    public float destroyDistance = 1200f;

    [Header("Layout (reference 1080p)")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    public float groundHeight = 180f;
    public float birdXPosition = -500f;      // relative to canvas center
    public float birdSize = 110f;

    // === Assets (assigned via editor; see SetupFlappyAssets) ===
    [Header("Sprites (auto-assigned by editor script)")]
    public Sprite sprBackground;
    public Sprite sprBase;
    public Sprite sprPipe;
    public Sprite sprMessage;
    public Sprite sprGameOver;
    public Sprite[] birdFrames = new Sprite[3];
    public Sprite[] numberSprites = new Sprite[10];

    [Header("Audio (auto-assigned by editor script)")]
    public AudioClip sfxWing;
    public AudioClip sfxPoint;
    public AudioClip sfxHit;
    public AudioClip sfxDie;

    // === Runtime ===
    private Camera playerCam;
    private Camera miniGameCam;
    private GameObject miniGameRoot;
    private Canvas canvas;
    private RectTransform canvasRoot;
    private RectTransform playField; // scroll area
    private RectTransform birdRT;
    private Image birdImage;
    private RectTransform groundRT1, groundRT2;
    private RectTransform scoreRoot;
    private RectTransform messagePanel;
    private RectTransform gameOverPanel;
    private AudioSource audioSrc;

    private bool isPlaying = false;
    private bool isGameOver = false;
    private bool gameStarted = false;   // after first SPACE press
    private int score = 0;
    private float spawnTimer = 0f;
    private float birdVelocityY = 0f;
    private float birdStartY;
    private float birdFrameTimer;
    private int birdFrameIdx;
    private float interactionCooldown = 0f;

    private List<RectTransform> pipes = new List<RectTransform>(); // each is a PipePair
    private HashSet<RectTransform> scoredPipes = new HashSet<RectTransform>();

    private GameObject playerObj;
    private MonoBehaviour[] disabledScripts;

    // Our own delta time that doesn't depend on Time.timeScale
    private float _lastRealtime;

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
            var cams = playerObj.GetComponentsInChildren<Camera>(false);
            foreach (var c in cams) if (c != null && c.gameObject.activeInHierarchy) return c;
            var anyCam = playerObj.GetComponentInChildren<Camera>(true);
            if (anyCam != null) return anyCam;
        }
        return Camera.main;
    }

    void Start()
    {
        playerCam = FindPlayerCam();

        var col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();
        if (col == null) gameObject.AddComponent<BoxCollider>();

        if (sprBackground == null || birdFrames == null || birdFrames.Length == 0 || birdFrames[0] == null)
            Debug.LogWarning("[Flappy] Sprites are not assigned on TvFlappyBird. Run menu: Tools > Flappy > Assign Assets To TV.");
    }

    void Update()
    {
        // Compute dt from realtime so it works even if Time.timeScale = 0
        float now = Time.realtimeSinceStartup;
        float dt = Mathf.Clamp(now - _lastRealtime, 0.0001f, 0.05f);
        _lastRealtime = now;

        if (interactionCooldown > 0f) interactionCooldown -= dt;

        if (!isPlaying)
        {
            CheckInteraction();
            return;
        }

        var kb = Keyboard.current;
        bool spacePressed = kb != null && kb.spaceKey.wasPressedThisFrame;
        bool exitPressed  = kb != null && (kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame);

        if (kb != null && kb.f1Key.wasPressedThisFrame)
        {
            Debug.Log($"[Flappy] isPlaying={isPlaying} gameStarted={gameStarted} isGameOver={isGameOver} " +
                      $"birdRT={birdRT != null} velY={birdVelocityY} pos={(birdRT!=null?birdRT.anchoredPosition.ToString():"null")} " +
                      $"timeScale={Time.timeScale} unscaledDT={dt:F4}");
        }

        // Exit
        if (exitPressed) { ExitGame(); return; }

        if (isGameOver)
        {
            if (spacePressed) RestartGame();
            return;
        }

        // First SPACE starts gameplay (before that: idle bob, message shown)
        if (!gameStarted)
        {
            AnimateBirdIdle(dt);
            if (spacePressed)
            {
                gameStarted = true;
                if (messagePanel != null) messagePanel.gameObject.SetActive(false);
                Flap();
            }
            ScrollGround(dt);
            return;
        }

        // Gameplay — step in small sub-steps so fast motion (strong flap)
        // at low frame rates doesn't tunnel through pipes/ground.
        if (spacePressed) Flap();

        float remaining = dt;
        const float maxStep = 1f / 120f; // at most ~8 ms per physics step
        while (remaining > 0f)
        {
            float step = Mathf.Min(remaining, maxStep);
            remaining -= step;

            birdVelocityY += gravity * step;
            Vector2 bpos = birdRT.anchoredPosition;
            bpos.y += birdVelocityY * step;
            birdRT.anchoredPosition = bpos;

            // Bounds
            float halfH = referenceResolution.y * 0.5f;
            float groundY = -halfH + groundHeight;
            if (bpos.y - birdSize * 0.36f < groundY) { GameOver(true); return; }
            if (bpos.y > halfH) { bpos.y = halfH; birdRT.anchoredPosition = bpos; birdVelocityY = Mathf.Min(birdVelocityY, 0f); }

            MovePipesAndCheck(step);
            if (isGameOver) return;
        }

        float tiltZ = Mathf.Clamp(birdVelocityY * 0.08f, -90f, 30f);
        birdRT.localRotation = Quaternion.Euler(0, 0, tiltZ);

        AnimateBirdFlap(dt);

        // Distance-based spawning: always keep the rightmost pipe ~pipeSpacing
        // away from the spawn X, so pipes are evenly distributed regardless of FPS.
        float spawnX = referenceResolution.x * 0.5f + 150f;
        if (pipes.Count == 0)
        {
            // First pipe appears a bit closer so the player doesn't wait too long.
            SpawnPipe(referenceResolution.x * 0.5f - 100f);
        }
        else
        {
            float rightmostX = float.MinValue;
            for (int i = 0; i < pipes.Count; i++)
            {
                if (pipes[i] == null) continue;
                if (pipes[i].anchoredPosition.x > rightmostX) rightmostX = pipes[i].anchoredPosition.x;
            }
            if (spawnX - rightmostX >= pipeSpacing)
                SpawnPipe(spawnX);
        }

        ScrollGround(dt);
    }

    void Flap()
    {
        birdVelocityY = jumpForce;
        if (audioSrc != null && sfxWing != null) audioSrc.PlayOneShot(sfxWing);
    }

    void AnimateBirdIdle(float dt)
    {
        // Gentle bob
        float t = Time.unscaledTime;
        Vector2 p = birdRT.anchoredPosition;
        p.y = birdStartY + Mathf.Sin(t * 4f) * 12f;
        birdRT.anchoredPosition = p;
        AnimateBirdFlap(dt);
    }

    void AnimateBirdFlap(float dt)
    {
        birdFrameTimer += dt;
        if (birdFrameTimer >= 0.09f)
        {
            birdFrameTimer = 0f;
            birdFrameIdx = (birdFrameIdx + 1) % birdFrames.Length;
            if (birdImage != null && birdFrames[birdFrameIdx] != null)
                birdImage.sprite = birdFrames[birdFrameIdx];
        }
    }

    void ScrollGround(float dt)
    {
        if (groundRT1 == null || groundRT2 == null) return;
        float dx = pipeSpeed * dt;
        groundRT1.anchoredPosition += Vector2.left * dx;
        groundRT2.anchoredPosition += Vector2.left * dx;
        float w = groundRT1.sizeDelta.x;
        if (groundRT1.anchoredPosition.x <= -w) groundRT1.anchoredPosition = new Vector2(groundRT2.anchoredPosition.x + w, groundRT1.anchoredPosition.y);
        if (groundRT2.anchoredPosition.x <= -w) groundRT2.anchoredPosition = new Vector2(groundRT1.anchoredPosition.x + w, groundRT2.anchoredPosition.y);
    }

    void CheckInteraction()
    {
        if (interactionCooldown > 0f) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        if (playerCam == null || !playerCam.gameObject.activeInHierarchy)
            playerCam = FindPlayerCam();
        if (playerCam == null || !playerCam.gameObject.activeInHierarchy) return;

        Vector3 origin = playerCam.transform.position;
        Vector3 dir = playerCam.transform.forward;

        bool hitTv = false;
        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, interactionDistance))
        {
            if (IsThisTv(hit.collider)) hitTv = true;
        }
        if (!hitTv)
        {
            var hits = Physics.SphereCastAll(origin, 0.4f, dir, interactionDistance);
            foreach (var h in hits)
            {
                if (h.collider != null && IsThisTv(h.collider))
                {
                    Vector3 toTv = (transform.position - origin).normalized;
                    if (Vector3.Dot(dir, toTv) > 0.5f) { hitTv = true; break; }
                }
            }
        }
        if (hitTv) StartGame();
    }

    bool IsThisTv(Collider col)
    {
        if (col == null) return false;
        return col.gameObject == gameObject ||
               col.transform.IsChildOf(transform) ||
               transform.IsChildOf(col.transform);
    }

    // ==========================================================
    // START / EXIT
    // ==========================================================
    void StartGame()
    {
        if (isPlaying) return;
        isPlaying = true;
        interactionCooldown = 0.5f;
        _lastRealtime = Time.realtimeSinceStartup;
        DisablePlayer();

        // TV switch-on SFX at the TV's world position.
        var sm = SoundManager.Instance;
        if (sm != null && sm.tvOn != null)
            sm.PlayAt(sm.tvOn, transform.position, 0.9f);

        miniGameRoot = new GameObject("FlappyBirdMiniGame");

        if (playerCam != null) playerCam.gameObject.SetActive(false);

        // Dedicated camera so nothing from 3D scene is rendered
        var camObj = new GameObject("FlappyCam");
        camObj.transform.SetParent(miniGameRoot.transform);
        miniGameCam = camObj.AddComponent<Camera>();
        miniGameCam.clearFlags = CameraClearFlags.SolidColor;
        miniGameCam.backgroundColor = Color.black;
        miniGameCam.cullingMask = 0; // render nothing
        miniGameCam.depth = 10;

        // AudioListener on this cam
        var existingListener = FindFirstObjectByType<AudioListener>();
        if (existingListener == null) camObj.AddComponent<AudioListener>();

        // Audio source
        audioSrc = miniGameRoot.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.volume = 0.8f;

        BuildUI();

        score = 0; spawnTimer = 0f; birdVelocityY = 0f;
        isGameOver = false; gameStarted = false;
        pipes.Clear(); scoredPipes.Clear();
        UpdateScoreUI();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }

    void ExitGame()
    {
        if (!isPlaying) return;
        isPlaying = false; isGameOver = false; gameStarted = false;
        interactionCooldown = 0.5f;

        // TV switch-off SFX.
        var sm = SoundManager.Instance;
        if (sm != null && sm.tvOff != null)
            sm.PlayAt(sm.tvOff, transform.position, 0.9f);

        if (miniGameRoot != null) Object.Destroy(miniGameRoot);

        playerCam = null;
        playerCam = FindPlayerCam();
        if (playerCam != null) playerCam.gameObject.SetActive(true);

        EnablePlayer();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void RestartGame()
    {
        // Clear pipes
        foreach (var p in pipes) if (p != null) Object.Destroy(p.gameObject);
        pipes.Clear(); scoredPipes.Clear();

        score = 0; birdVelocityY = 0f; spawnTimer = 0f;
        isGameOver = false; gameStarted = false;

        birdRT.anchoredPosition = new Vector2(birdXPosition, birdStartY);
        birdRT.localRotation = Quaternion.identity;
        UpdateScoreUI();

        if (gameOverPanel != null) gameOverPanel.gameObject.SetActive(false);
        if (messagePanel != null) messagePanel.gameObject.SetActive(true);
    }

    // ==========================================================
    // UI BUILD
    // ==========================================================
    void BuildUI()
    {
        var canvasObj = new GameObject("FlappyCanvas");
        canvasObj.transform.SetParent(miniGameRoot.transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 1f; // match height -> full height visible
        canvasObj.AddComponent<GraphicRaycaster>();

        canvasRoot = canvasObj.GetComponent<RectTransform>();

        // Background (stretched to full canvas, no tiling)
        var bg = CreateImage(canvasRoot, "Background", sprBackground);
        Stretch(bg);
        bg.preserveAspect = false;
        bg.type = Image.Type.Simple;

        // Playfield container (centered)
        var pfGO = new GameObject("PlayField", typeof(RectTransform));
        pfGO.transform.SetParent(canvasRoot, false);
        playField = pfGO.GetComponent<RectTransform>();
        playField.anchorMin = new Vector2(0.5f, 0.5f);
        playField.anchorMax = new Vector2(0.5f, 0.5f);
        playField.pivot = new Vector2(0.5f, 0.5f);
        playField.sizeDelta = referenceResolution;
        playField.anchoredPosition = Vector2.zero;

        // Bird
        var birdGO = new GameObject("Bird", typeof(RectTransform));
        birdGO.transform.SetParent(playField, false);
        birdRT = birdGO.GetComponent<RectTransform>();
        birdRT.sizeDelta = new Vector2(birdSize, birdSize * 0.72f);
        birdStartY = 0f;
        birdRT.anchoredPosition = new Vector2(birdXPosition, birdStartY);
        birdImage = birdGO.AddComponent<Image>();
        if (birdFrames != null && birdFrames.Length > 0 && birdFrames[1] != null)
            birdImage.sprite = birdFrames[1];
        birdImage.preserveAspect = true;

        // Ground (two tiles for scrolling). Parent to canvas root so they anchor to bottom of screen.
        groundRT1 = CreateImage(canvasRoot, "Ground1", sprBase).rectTransform;
        SetupGroundBottom(groundRT1, 0f);
        groundRT2 = CreateImage(canvasRoot, "Ground2", sprBase).rectTransform;
        SetupGroundBottom(groundRT2, referenceResolution.x);

        // Score
        scoreRoot = new GameObject("Score", typeof(RectTransform)).GetComponent<RectTransform>();
        scoreRoot.SetParent(canvasRoot, false);
        scoreRoot.anchorMin = new Vector2(0.5f, 1f);
        scoreRoot.anchorMax = new Vector2(0.5f, 1f);
        scoreRoot.pivot = new Vector2(0.5f, 1f);
        scoreRoot.anchoredPosition = new Vector2(0f, -50f);
        scoreRoot.sizeDelta = new Vector2(600f, 100f);

        // Message (tap to start)
        messagePanel = CreateImage(canvasRoot, "Message", sprMessage).rectTransform;
        messagePanel.anchorMin = new Vector2(0.5f, 0.5f);
        messagePanel.anchorMax = new Vector2(0.5f, 0.5f);
        messagePanel.pivot = new Vector2(0.5f, 0.5f);
        messagePanel.sizeDelta = new Vector2(480f, 700f);
        messagePanel.anchoredPosition = new Vector2(0f, 60f);

        // Game over (hidden)
        gameOverPanel = CreateImage(canvasRoot, "GameOver", sprGameOver).rectTransform;
        gameOverPanel.anchorMin = new Vector2(0.5f, 0.5f);
        gameOverPanel.anchorMax = new Vector2(0.5f, 0.5f);
        gameOverPanel.pivot = new Vector2(0.5f, 0.5f);
        gameOverPanel.sizeDelta = new Vector2(520f, 120f);
        gameOverPanel.anchoredPosition = new Vector2(0f, 180f);
        gameOverPanel.gameObject.SetActive(false);

    }

    void SetupGroundBottom(RectTransform rt, float x)
    {
        // Anchor to bottom of screen, full width
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(referenceResolution.x, groundHeight);
        rt.anchoredPosition = new Vector2(x, 0f);
        var img = rt.GetComponent<Image>();
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
    }

    Image CreateImage(Transform parent, string name, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    void Stretch(Image img)
    {
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ==========================================================
    // PIPES
    // ==========================================================
    void SpawnPipe(float atX)
    {
        float halfH = referenceResolution.y * 0.5f;
        float groundTop = -halfH + groundHeight;
        float ceiling = halfH;

        float minGapY = groundTop + gapSize * 0.5f + 80f;
        float maxGapY = ceiling - gapSize * 0.5f - 80f;
        float gapCenter = Random.Range(minGapY, maxGapY);

        var pairGO = new GameObject("PipePair", typeof(RectTransform));
        pairGO.transform.SetParent(playField, false);
        var pair = pairGO.GetComponent<RectTransform>();
        pair.anchorMin = new Vector2(0.5f, 0.5f);
        pair.anchorMax = new Vector2(0.5f, 0.5f);
        pair.pivot = new Vector2(0.5f, 0.5f);
        pair.anchoredPosition = new Vector2(atX, 0f);
        pair.sizeDelta = new Vector2(130f, referenceResolution.y);

        float pipeWidth = 130f;

        // Bottom pipe: rising from the ground, cap pointing up into the gap.
        var bot = CreateImage(pair, "PipeBot", sprPipe);
        var botRT = bot.rectTransform;
        float botTop = gapCenter - gapSize * 0.5f;      // y where the gap edge is (top of the bottom pipe)
        float botHeight = botTop - groundTop;           // pixel height from ground to gap edge
        botRT.anchorMin = new Vector2(0.5f, 0.5f);
        botRT.anchorMax = new Vector2(0.5f, 0.5f);
        botRT.pivot = new Vector2(0.5f, 0.5f);          // center pivot (uniform with top pipe)
        botRT.sizeDelta = new Vector2(pipeWidth, botHeight);
        // Center of the bottom pipe sits halfway between ground and the gap edge.
        botRT.anchoredPosition = new Vector2(0f, groundTop + botHeight * 0.5f);
        botRT.localRotation = Quaternion.identity;
        botRT.localScale = Vector3.one;
        bot.preserveAspect = false;

        // Top pipe: anchored at the ceiling with pivot at the top, extending downward.
        // We rotate 180° around Z so the pipe's cap visually points down (into the gap).
        var top = CreateImage(pair, "PipeTop", sprPipe);
        var topRT = top.rectTransform;
        float topBot = gapCenter + gapSize * 0.5f;      // y where the gap edge is
        float topHeight = ceiling - topBot;             // pixel height from gap edge to ceiling
        topRT.anchorMin = new Vector2(0.5f, 0.5f);
        topRT.anchorMax = new Vector2(0.5f, 0.5f);
        topRT.pivot = new Vector2(0.5f, 0.5f);          // center pivot keeps rotation symmetric
        topRT.sizeDelta = new Vector2(pipeWidth, topHeight);
        // Center of the top pipe sits halfway between the gap edge and the ceiling.
        topRT.anchoredPosition = new Vector2(0f, topBot + topHeight * 0.5f);
        topRT.localRotation = Quaternion.Euler(0f, 0f, 180f); // flip cap downward
        topRT.localScale = Vector3.one;
        top.preserveAspect = false;

        pipes.Add(pair);
    }

    void MovePipesAndCheck(float dt)
    {
        if (birdRT == null) return;
        float dx = pipeSpeed * dt;
        Vector2 birdPos = birdRT.anchoredPosition;

        // Tight hitbox: bird is visually ~110 wide x ~79 tall (aspect 0.72),
        // and the pixel-art sprite has a lot of transparency on the sides.
        // Use a small forgiving ellipse-ish box.
        float birdHalfX = birdSize * 0.28f;     // ≈31 px
        float birdHalfY = birdSize * 0.72f * 0.32f; // ≈25 px

        for (int i = pipes.Count - 1; i >= 0; i--)
        {
            var pair = pipes[i];
            if (pair == null) { pipes.RemoveAt(i); continue; }

            Vector2 p = pair.anchoredPosition;
            p.x -= dx;
            pair.anchoredPosition = p;

            if (p.x < -referenceResolution.x * 0.5f - 200f)
            {
                Object.Destroy(pair.gameObject);
                pipes.RemoveAt(i);
                continue;
            }

            // Score (trigger right after the bird's center passes the pair center)
            if (!scoredPipes.Contains(pair) && p.x < birdXPosition)
            {
                scoredPipes.Add(pair);
                score++;
                UpdateScoreUI();
                if (audioSrc != null && sfxPoint != null) audioSrc.PlayOneShot(sfxPoint);
                if (GameEconomy.Instance != null)
                    GameEconomy.Instance.AwardDopamine(GameEconomy.ActFlappy);
            }

            // Collision: use the real pipe visual width (130 px) minus a small
            // forgiveness margin so the player isn't killed at the very edge.
            const float pipeVisualHalfW = 130f * 0.5f;
            float hitHalfW = pipeVisualHalfW - 6f;

            bool xOverlap = birdPos.x + birdHalfX > p.x - hitHalfW &&
                            birdPos.x - birdHalfX < p.x + hitHalfW;
            if (!xOverlap) continue;

            // Only collide with Image children (actual pipe visuals), not the
            // container pair rect that spans full height.
            foreach (Transform ch in pair)
            {
                var chRT = ch as RectTransform;
                if (chRT == null) continue;

                // Both top and bottom pipes now use a centered pivot, so the
                // vertical range is symmetric around anchoredPosition.y.
                float halfSize = chRT.sizeDelta.y * 0.5f;
                float chTop = chRT.anchoredPosition.y + halfSize;
                float chBot = chRT.anchoredPosition.y - halfSize;

                // Shrink the pipe's vertical hitbox slightly so the player
                // can graze the gap edges without instantly dying.
                chBot += 4f;
                chTop -= 4f;

                if (birdPos.y + birdHalfY > chBot && birdPos.y - birdHalfY < chTop)
                {
                    GameOver(false);
                    return;
                }
            }
        }
    }

    // ==========================================================
    // SCORE DIGIT SPRITES
    // ==========================================================
    void UpdateScoreUI()
    {
        if (scoreRoot == null) return;
        // Clear old digits
        for (int i = scoreRoot.childCount - 1; i >= 0; i--)
            Object.Destroy(scoreRoot.GetChild(i).gameObject);

        string s = score.ToString();
        float digitW = 60f;
        float digitH = 90f;
        float spacing = 4f;
        float totalW = s.Length * digitW + (s.Length - 1) * spacing;
        float startX = -totalW * 0.5f + digitW * 0.5f;
        for (int i = 0; i < s.Length; i++)
        {
            int d = s[i] - '0';
            if (d < 0 || d > 9) continue;
            var img = CreateImage(scoreRoot, "D" + i, numberSprites[d]);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(digitW, digitH);
            rt.anchoredPosition = new Vector2(startX + i * (digitW + spacing), 0f);
            img.preserveAspect = true;
        }
    }

    // ==========================================================
    // GAME OVER
    // ==========================================================
    void GameOver(bool fromGround)
    {
        if (isGameOver) return;
        isGameOver = true;
        birdVelocityY = 0f;

        if (audioSrc != null)
        {
            if (sfxHit != null) audioSrc.PlayOneShot(sfxHit);
            if (sfxDie != null) audioSrc.PlayOneShot(sfxDie);
        }

        int bonusCoins = score * 5;
        if (bonusCoins > 0 && GameEconomy.Instance != null)
            GameEconomy.Instance.AddCoins(bonusCoins);

        if (gameOverPanel != null) gameOverPanel.gameObject.SetActive(true);
    }

    // ==========================================================
    // PLAYER
    // ==========================================================
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

    void OnDestroy() { if (isPlaying) ExitGame(); }
}
