using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class SubwayGame : MonoBehaviour
{
    private GameObject root;
    private Camera cam;
    private GameObject player;
    private TMP_Text scoreText;
    private TMP_Text infoText;
    private Canvas uiCanvas;

    private float laneWidth = 2.5f;
    private float speed = 10f;
    private float speedIncrease = 0.3f;
    private float spawnInterval = 1.2f;
    private float jumpForce = 9f;
    private float gravityVal = -22f;

    private int targetLane = 1;
    private float playerX = 0f;
    private float playerY = 0f;
    private float velocityY = 0f;
    private bool isJumping = false;
    private float spawnTimer = 0f;
    private int score = 0;
    private float distanceTraveled = 0f;
    private bool gameOver = false;
    private List<GameObject> obstacles = new List<GameObject>();
    private List<GameObject> coins = new List<GameObject>();
    private int coinCount = 0;

    // Ground — single large plane, no gaps
    private GameObject ground;
    private Material obsMat;
    private Material coinMat;

    public void Init(GameObject gameRoot, Camera gameCam)
    {
        root = gameRoot;
        cam = gameCam;

        cam.transform.localPosition = new Vector3(0f, 7f, -9f);
        cam.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        cam.backgroundColor = new Color(0.35f, 0.55f, 0.85f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.depth = 10;

        var shader = Shader.Find("Universal Render Pipeline/Lit");

        // Player
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "RunnerPlayer";
        player.transform.SetParent(root.transform);
        player.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        player.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);
        var pMat = new Material(shader);
        pMat.SetColor("_BaseColor", new Color(0f, 0.85f, 1f));
        player.GetComponent<Renderer>().material = pMat;
        Object.Destroy(player.GetComponent<Collider>());

        // Ground — one big plane, always under player
        ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.localScale = new Vector3(10f, 0.2f, 500f);
        ground.transform.localPosition = new Vector3(0f, -0.1f, 200f);
        var gMat = new Material(shader);
        gMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.35f));
        ground.GetComponent<Renderer>().material = gMat;
        Object.Destroy(ground.GetComponent<Collider>());

        // Lane lines
        for (int i = -1; i <= 1; i += 2)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "Line";
            line.transform.SetParent(root.transform);
            line.transform.localScale = new Vector3(0.08f, 0.22f, 500f);
            line.transform.localPosition = new Vector3(i * laneWidth * 0.5f, -0.01f, 200f);
            var lMat = new Material(shader);
            lMat.SetColor("_BaseColor", Color.white);
            line.GetComponent<Renderer>().material = lMat;
            Object.Destroy(line.GetComponent<Collider>());
        }

        // Side walls
        for (int s = -1; s <= 1; s += 2)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(root.transform);
            wall.transform.localScale = new Vector3(0.5f, 3f, 500f);
            wall.transform.localPosition = new Vector3(s * 4.5f, 1.5f, 200f);
            var wMat = new Material(shader);
            wMat.SetColor("_BaseColor", new Color(0.45f, 0.45f, 0.5f));
            wall.GetComponent<Renderer>().material = wMat;
            Object.Destroy(wall.GetComponent<Collider>());
        }

        // Materials for obstacles/coins
        obsMat = new Material(shader);
        obsMat.SetColor("_BaseColor", new Color(0.9f, 0.15f, 0.15f));
        coinMat = new Material(shader);
        coinMat.SetColor("_BaseColor", new Color(1f, 0.85f, 0f));
        coinMat.SetFloat("_Metallic", 1f);

        // Light
        var lightObj = new GameObject("Light");
        lightObj.transform.SetParent(root.transform);
        lightObj.transform.localPosition = new Vector3(0f, 15f, 5f);
        lightObj.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
        var lt = lightObj.AddComponent<Light>();
        lt.type = LightType.Directional;
        lt.intensity = 1.3f;

        // UI — ScreenSpaceOverlay so it always shows on top
        var canvasObj = new GameObject("SubwayCanvas");
        canvasObj.transform.SetParent(root.transform);
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        scoreText = MakeText(canvasObj.transform, "Score", "0", 64,
            new Vector2(0.5f, 1f), new Vector2(0f, -40f));
        scoreText.color = Color.white;

        var coinText = MakeText(canvasObj.transform, "Coins", "Coins: 0", 32,
            new Vector2(0.9f, 1f), new Vector2(0f, -40f));
        coinText.color = new Color(1f, 0.85f, 0f);

        infoText = MakeText(canvasObj.transform, "Info",
            "A/D — lanes  |  SPACE — jump  |  E/ESC — exit", 24,
            new Vector2(0.5f, 0f), new Vector2(0f, 25f));
        infoText.color = new Color(1, 1, 1, 0.5f);

        // Init
        targetLane = 1;
        playerX = 0f;
        playerY = 0f;
        score = 0;
        coinCount = 0;
        gameOver = false;
        speed = 10f;
        spawnInterval = 1.2f;
    }

    void Update()
    {
        if (root == null || player == null) return;
        if (Keyboard.current == null) return;

        if (gameOver)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                Restart();
            return;
        }

        // Input
        if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            targetLane = Mathf.Max(0, targetLane - 1);
        if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            targetLane = Mathf.Min(2, targetLane + 1);
        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isJumping)
        {
            velocityY = jumpForce;
            isJumping = true;
        }

        // Lane movement
        float targetX = (targetLane - 1) * laneWidth;
        playerX = Mathf.MoveTowards(playerX, targetX, 18f * Time.deltaTime);

        // Jump physics
        velocityY += gravityVal * Time.deltaTime;
        playerY += velocityY * Time.deltaTime;
        if (playerY <= 0f)
        {
            playerY = 0f;
            velocityY = 0f;
            isJumping = false;
        }

        player.transform.localPosition = new Vector3(playerX, 0.7f + playerY, 0f);

        // Speed & score
        speed += speedIncrease * Time.deltaTime;
        distanceTraveled += speed * Time.deltaTime;
        score = (int)(distanceTraveled * 2f);
        if (scoreText != null) scoreText.text = score.ToString();

        // Spawn
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnRow();
            spawnTimer = 0f;
            spawnInterval = Mathf.Max(0.45f, spawnInterval - 0.008f);
        }

        // Move & collide obstacles
        MoveAndCollide();

        // Move & collect coins
        MoveCoins();
    }

    void SpawnRow()
    {
        // Spawn 1-2 obstacles in random lanes
        int obsCount = Random.value > 0.6f ? 2 : 1;
        List<int> usedLanes = new List<int>();

        for (int i = 0; i < obsCount; i++)
        {
            int lane;
            do { lane = Random.Range(0, 3); } while (usedLanes.Contains(lane));
            usedLanes.Add(lane);

            float x = (lane - 1) * laneWidth;
            bool tall = Random.value > 0.5f;
            float h = tall ? 1.8f : 0.7f;
            float w = Random.value > 0.5f ? 2f : 1.2f;

            var obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obs.name = "Obs";
            obs.transform.SetParent(root.transform);
            obs.transform.localScale = new Vector3(w, h, 1f);
            obs.transform.localPosition = new Vector3(x, h / 2f, 35f);
            obs.GetComponent<Renderer>().material = obsMat;
            Object.Destroy(obs.GetComponent<Collider>());
            obs.AddComponent<ObstacleTag>().height = h;
            obstacles.Add(obs);
        }

        // Spawn coins in free lane
        for (int lane = 0; lane < 3; lane++)
        {
            if (!usedLanes.Contains(lane) && Random.value > 0.4f)
            {
                float x = (lane - 1) * laneWidth;
                for (int c = 0; c < 3; c++)
                {
                    var coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    coin.name = "Coin";
                    coin.transform.SetParent(root.transform);
                    coin.transform.localScale = new Vector3(0.4f, 0.4f, 0.1f);
                    coin.transform.localPosition = new Vector3(x, 1f, 35f + c * 1.5f);
                    coin.GetComponent<Renderer>().material = coinMat;
                    Object.Destroy(coin.GetComponent<Collider>());
                    coins.Add(coin);
                }
            }
        }
    }

    void MoveAndCollide()
    {
        float ph = 0.4f; // player half width

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            var obs = obstacles[i];
            if (obs == null) { obstacles.RemoveAt(i); continue; }

            obs.transform.localPosition += Vector3.back * speed * Time.deltaTime;

            if (obs.transform.localPosition.z < -8f)
            {
                Object.Destroy(obs);
                obstacles.RemoveAt(i);
                continue;
            }

            Vector3 op = obs.transform.localPosition;
            float obsH = obs.GetComponent<ObstacleTag>()?.height ?? 1.5f;
            float obsHalfW = obs.transform.localScale.x * 0.5f;

            if (Mathf.Abs(op.z) < 0.8f &&
                Mathf.Abs(op.x - playerX) < (obsHalfW + ph) &&
                playerY < obsH - 0.3f)
            {
                DoGameOver();
                return;
            }
        }
    }

    void MoveCoins()
    {
        float ph = 0.5f;

        for (int i = coins.Count - 1; i >= 0; i--)
        {
            var coin = coins[i];
            if (coin == null) { coins.RemoveAt(i); continue; }

            coin.transform.localPosition += Vector3.back * speed * Time.deltaTime;
            // Spin
            coin.transform.Rotate(0f, 180f * Time.deltaTime, 0f);

            if (coin.transform.localPosition.z < -8f)
            {
                Object.Destroy(coin);
                coins.RemoveAt(i);
                continue;
            }

            // Collect
            Vector3 cp = coin.transform.localPosition;
            if (Mathf.Abs(cp.z) < 1f &&
                Mathf.Abs(cp.x - playerX) < ph &&
                Mathf.Abs(cp.y - 1f - playerY) < 1.2f)
            {
                coinCount++;
                Object.Destroy(coin);
                coins.RemoveAt(i);
                // Update coin text
                var coinTextObj = GameObject.Find("Coins");
                if (coinTextObj != null)
                {
                    var t = coinTextObj.GetComponent<TMP_Text>();
                    if (t != null) t.text = "Coins: " + coinCount;
                }
            }
        }
    }

    void DoGameOver()
    {
        gameOver = true;
        if (infoText != null)
        {
            infoText.text = "GAME OVER!  Score: " + score + "  Coins: " + coinCount +
                "\nSPACE — restart  |  E/ESC — exit";
            infoText.color = new Color(1f, 0.3f, 0.3f, 1f);
            infoText.fontSize = 32;
        }
    }

    void Restart()
    {
        foreach (var o in obstacles) if (o != null) Object.Destroy(o);
        obstacles.Clear();
        foreach (var c in coins) if (c != null) Object.Destroy(c);
        coins.Clear();

        gameOver = false;
        score = 0;
        coinCount = 0;
        distanceTraveled = 0f;
        speed = 10f;
        spawnTimer = 0f;
        spawnInterval = 1.2f;
        targetLane = 1;
        playerX = 0f;
        playerY = 0f;
        velocityY = 0f;
        isJumping = false;

        if (scoreText != null) scoreText.text = "0";
        if (infoText != null)
        {
            infoText.text = "A/D — lanes  |  SPACE — jump  |  E/ESC — exit";
            infoText.color = new Color(1, 1, 1, 0.5f);
            infoText.fontSize = 24;
        }
        var coinTextObj = GameObject.Find("Coins");
        if (coinTextObj != null)
        {
            var t = coinTextObj.GetComponent<TMP_Text>();
            if (t != null) t.text = "Coins: 0";
        }
    }

    public void Cleanup()
    {
        foreach (var o in obstacles) if (o != null) Object.Destroy(o);
        obstacles.Clear();
        foreach (var c in coins) if (c != null) Object.Destroy(c);
        coins.Clear();
    }

    TMP_Text MakeText(Transform parent, string name, string text, float size,
        Vector2 anchor, Vector2 pos)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, anchor.y > 0.5f ? 1f : 0f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(600f, 80f);
        return tmp;
    }
}

public class ObstacleTag : MonoBehaviour
{
    public float height;
}
