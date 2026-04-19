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
    private TMP_Text coinTextRef;
    private Canvas uiCanvas;

    // --- Game tuning ---
    private float laneWidth = 2.5f;
    private float speed = 10f;
    private float speedIncrease = 0.3f;
    private float spawnInterval = 1.2f;
    private float jumpForce = 9f;
    private float gravityVal = -22f;

    // --- State ---
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
    private List<GameObject> groundTiles = new List<GameObject>();
    private int coinCount = 0;
    private float runCycle = 0f;

    // --- Scene pieces ---
    private GameObject ground;
    private Material roadMat;
    private Material coinMat;

    // --- Assets ---
    private GameObject stickmanPrefab;
    private GameObject[] laneObstaclePrefabs;
    private GameObject[] jumpObstaclePrefabs;
    private Texture2D roadTexture;

    private const string BARRIERS_FBX_PATH = "Assets/Barriers pack Demo/Barriers pack Demo/Fbx/";

    // Куратироване зілля компактних перешкод для однієї смуги.
    // Усі нормалізуються до висоти ~1.3м і ширини <= lane*0.45 у SpawnObstacle().
    private readonly string[] LANE_OBSTACLE_NAMES = new string[]
    {
        "Cone.fbx",                  // 1.67 x 2.57 — маленький конус
        "Cone_001.fbx",              // 3.67 x 3.75 — великий конус
        "Barricade_Light.fbx",       // 0.48 x 0.74 — мигалка, низька
        "Wooden_Barricade.fbx",      // 2.89 x 2.63
        "Barricades_001.fbx",        // 0.9 x 2.97 — тонка загорожа
        "Barricades_Sign.fbx",       // 0.14 x 0.35 x 4.27 — знак
        "Barricades_Stand.fbx",      // 0.9 x 2.97
        "Concrete_Barrier.fbx",      // 2.0 x 2.0
        "Metal_Barricade.fbx",       // 0.76 x 0.57 — низька
        "Parking_barrier.fbx",       // 0.65 x 5.77 — вузький шлагбаум
        "Stop_Sign.fbx",             // 2.84 x 2.0
    };

    // Широкі "стрибкові" перешкоди на всю дорогу — через них треба СТРИБАТИ (SPACE).
    // Спавняться рідше. Усі короткі по висоті, щоб гравець міг перестрибнути.
    private readonly string[] JUMP_OBSTACLE_NAMES = new string[]
    {
        "Guard_rail_road.fbx",       // 9.95 x 1.17
        "Guard_rail_road_rail.fbx",  // 9.95 x 0.82
        "Guard_rail_road_015.fbx",   // 9.95 x 1.41
        "Wooden_Fence_Part.fbx",     // 0.23 x 0.43 x 9.09 (обернута по Z, розтягуємо по X)
    };

    public void Init(GameObject gameRoot, Camera gameCam)
    {
        root = gameRoot;
        cam = gameCam;

        cam.transform.localPosition = new Vector3(0f, 6f, -8f);
        cam.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
        cam.backgroundColor = new Color(0.45f, 0.62f, 0.88f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.depth = 10;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        LoadAssets();

        // --- Player (Stickman from Barriers pack) ---
        CreatePlayer(shader);

        // --- Ground (textured road tiles from barrier-pack texture) ---
        CreateRoad(shader);

        // --- Sidewalks / curbs ---
        CreateSidewalks(shader);

        // --- Materials ---
        coinMat = new Material(shader);
        coinMat.SetColor("_BaseColor", new Color(1f, 0.85f, 0f));
        coinMat.color = new Color(1f, 0.85f, 0f);
        if (coinMat.HasProperty("_Metallic")) coinMat.SetFloat("_Metallic", 1f);
        if (coinMat.HasProperty("_Smoothness")) coinMat.SetFloat("_Smoothness", 0.9f);

        // --- Light ---
        var lightObj = new GameObject("Light");
        lightObj.transform.SetParent(root.transform);
        lightObj.transform.localPosition = new Vector3(0f, 15f, 5f);
        lightObj.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
        var lt = lightObj.AddComponent<Light>();
        lt.type = LightType.Directional;
        lt.intensity = 1.3f;

        // --- UI ---
        CreateUI();

        // --- Init state ---
        targetLane = 1;
        playerX = 0f;
        playerY = 0f;
        score = 0;
        coinCount = 0;
        gameOver = false;
        speed = 10f;
        spawnInterval = 1.2f;
        runCycle = 0f;
    }

    void LoadAssets()
    {
        // Основний шлях, який працює і в Editor і в Build — Resources/SubwaySurf/Barriers/
        stickmanPrefab = LoadBarrier("Stickalungu_Animated");
        roadTexture    = Resources.Load<Texture2D>("SubwaySurf/barriers_texture");

        var lane = new List<GameObject>();
        foreach (var name in LANE_OBSTACLE_NAMES)
        {
            var go = LoadBarrier(System.IO.Path.GetFileNameWithoutExtension(name));
            if (go != null) lane.Add(go);
        }
        laneObstaclePrefabs = lane.ToArray();

        var jump = new List<GameObject>();
        foreach (var name in JUMP_OBSTACLE_NAMES)
        {
            var go = LoadBarrier(System.IO.Path.GetFileNameWithoutExtension(name));
            if (go != null) jump.Add(go);
        }
        jumpObstaclePrefabs = jump.ToArray();
    }

    GameObject LoadBarrier(string nameNoExt)
    {
        var g = Resources.Load<GameObject>("SubwaySurf/Barriers/" + nameNoExt);
        if (g != null) return g;
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BARRIERS_FBX_PATH + nameNoExt + ".fbx");
#else
        return null;
#endif
    }

    void CreatePlayer(Shader shader)
    {
        player = new GameObject("RunnerPlayer");
        player.transform.SetParent(root.transform);
        player.transform.localPosition = new Vector3(0f, 0f, 0f);

        if (stickmanPrefab != null)
        {
            var vis = Object.Instantiate(stickmanPrefab);
            vis.name = "StickmanVisual";
            vis.transform.SetParent(player.transform, false);

            // FBX imports are in cm -> scale to ~1.6m tall runner
            vis.transform.localScale = Vector3.one * 1.8f;
            vis.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            vis.transform.localPosition = Vector3.zero;

            // Remove any colliders in the imported hierarchy
            foreach (var col in vis.GetComponentsInChildren<Collider>())
                Object.Destroy(col);

            // Darker tint material to look like an urban runner
            var runnerMat = new Material(shader);
            runnerMat.SetColor("_BaseColor", new Color(0.18f, 0.22f, 0.35f));
            runnerMat.color = new Color(0.18f, 0.22f, 0.35f);
            foreach (var r in vis.GetComponentsInChildren<Renderer>())
                r.material = runnerMat;
        }
        else
        {
            // Fallback capsule (should never happen in editor)
            var caps = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            caps.transform.SetParent(player.transform, false);
            caps.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            caps.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);
            Object.Destroy(caps.GetComponent<Collider>());
        }
    }

    void CreateRoad(Shader shader)
    {
        roadMat = new Material(shader);
        Color asphalt = new Color(0.22f, 0.22f, 0.24f);
        roadMat.color = asphalt;
        if (roadMat.HasProperty("_BaseColor"))
            roadMat.SetColor("_BaseColor", asphalt);

        // Procedural asphalt-noise texture
        var asphaltTex = new Texture2D(128, 128, TextureFormat.RGB24, true);
        asphaltTex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                float n = Random.Range(0.18f, 0.30f);
                asphaltTex.SetPixel(x, y, new Color(n, n, n * 1.02f));
            }
        asphaltTex.Apply();
        roadMat.mainTexture = asphaltTex;
        if (roadMat.HasProperty("_BaseMap"))
            roadMat.SetTexture("_BaseMap", asphaltTex);
        roadMat.mainTextureScale = new Vector2(2f, 60f);

        ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.localScale = new Vector3(laneWidth * 3f + 0.4f, 0.2f, 500f);
        ground.transform.localPosition = new Vector3(0f, -0.1f, 200f);
        ground.GetComponent<Renderer>().material = roadMat;
        Object.Destroy(ground.GetComponent<Collider>());

        // Lane dividers (painted white stripes)
        for (int i = -1; i <= 1; i += 2)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "LaneLine";
            line.transform.SetParent(root.transform);
            line.transform.localScale = new Vector3(0.1f, 0.22f, 500f);
            line.transform.localPosition = new Vector3(i * laneWidth * 0.5f, -0.01f, 200f);
            var lMat = new Material(shader);
            lMat.color = new Color(0.95f, 0.92f, 0.2f);
            if (lMat.HasProperty("_BaseColor"))
                lMat.SetColor("_BaseColor", new Color(0.95f, 0.92f, 0.2f));
            line.GetComponent<Renderer>().material = lMat;
            Object.Destroy(line.GetComponent<Collider>());
        }
    }

    void CreateSidewalks(Shader shader)
    {
        // Curb on each side + tall wall behind (buildings hint)
        for (int s = -1; s <= 1; s += 2)
        {
            var curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curb.name = "Curb";
            curb.transform.SetParent(root.transform);
            curb.transform.localScale = new Vector3(1.2f, 0.4f, 500f);
            curb.transform.localPosition = new Vector3(s * (laneWidth * 1.5f + 0.7f), 0.0f, 200f);
            var cMat = new Material(shader);
            cMat.color = new Color(0.55f, 0.55f, 0.58f);
            if (cMat.HasProperty("_BaseColor"))
                cMat.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.58f));
            curb.GetComponent<Renderer>().material = cMat;
            Object.Destroy(curb.GetComponent<Collider>());

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(root.transform);
            wall.transform.localScale = new Vector3(0.5f, 6f, 500f);
            wall.transform.localPosition = new Vector3(s * (laneWidth * 1.5f + 1.6f), 3f, 200f);
            var wMat = new Material(shader);
            wMat.color = new Color(0.6f, 0.35f, 0.28f);
            if (wMat.HasProperty("_BaseColor"))
                wMat.SetColor("_BaseColor", new Color(0.6f, 0.35f, 0.28f));
            wall.GetComponent<Renderer>().material = wMat;
            Object.Destroy(wall.GetComponent<Collider>());
        }
    }

    void CreateUI()
    {
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

        coinTextRef = MakeText(canvasObj.transform, "Coins", "Coins: 0", 32,
            new Vector2(0.9f, 1f), new Vector2(-20f, -40f));
        coinTextRef.color = new Color(1f, 0.85f, 0f);

        infoText = MakeText(canvasObj.transform, "Info",
            "A/D — lanes  |  SPACE — jump  |  E/ESC — exit", 24,
            new Vector2(0.5f, 0f), new Vector2(0f, 25f));
        infoText.color = new Color(1, 1, 1, 0.5f);
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

        player.transform.localPosition = new Vector3(playerX, playerY, 0f);

        // Run cycle animation — bob + tilt
        AnimatePlayer();

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

        MoveAndCollide();
        MoveCoins();
    }

    void AnimatePlayer()
    {
        runCycle += Time.deltaTime * (isJumping ? 4f : 12f);
        if (player.transform.childCount > 0)
        {
            var vis = player.transform.GetChild(0);
            float bob = isJumping ? 0f : Mathf.Abs(Mathf.Sin(runCycle)) * 0.08f;
            float tilt = isJumping ? 0f : Mathf.Sin(runCycle) * 5f;
            vis.localPosition = new Vector3(0f, bob, 0f);
            vis.localRotation = Quaternion.Euler(isJumping ? -10f : 0f, 180f, tilt);
        }
    }

    void SpawnRow()
    {
        List<int> usedLanes = new List<int>();

        // ~18% chance to spawn a wide jumpable obstacle that blocks 2-3 lanes.
        // Requires SPACE to jump over. Short height so the player fits in the air.
        if (Random.value < 0.18f && jumpObstaclePrefabs != null && jumpObstaclePrefabs.Length > 0)
        {
            SpawnJumpObstacle();
            // Mark 2 lanes as used so no lane obstacle+coin overlap it
            int startLane = Random.Range(0, 2); // 0 or 1
            usedLanes.Add(startLane);
            usedLanes.Add(startLane + 1);
        }
        else
        {
            // Regular lane obstacles: 1 or 2
            int obsCount = Random.value > 0.6f ? 2 : 1;
            for (int i = 0; i < obsCount; i++)
            {
                int lane;
                do { lane = Random.Range(0, 3); } while (usedLanes.Contains(lane));
                usedLanes.Add(lane);
                SpawnObstacle(lane);
            }
        }

        // Spawn coins in free lanes
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
                    coin.transform.localScale = new Vector3(0.45f, 0.45f, 0.12f);
                    coin.transform.localPosition = new Vector3(x, 1f, 35f + c * 1.5f);
                    coin.GetComponent<Renderer>().material = coinMat;
                    Object.Destroy(coin.GetComponent<Collider>());
                    coins.Add(coin);
                }
            }
        }
    }

    void SpawnJumpObstacle()
    {
        // Прості геометричні форми замість FBX — бо імпортовані бар'єри мають
        // "нестандартні" осі (Blender 270° X), і після всіх нормалізацій реальна
        // ширина/висота дико плаваюча → гравець загадково вмирає.
        // Тепер будуємо перекладину з простих cubes: 2 "стійки" + верхня балка.

        var container = new GameObject("JumpObs_Hurdle");
        container.transform.SetParent(root.transform, false);
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;

        float totalWidth = laneWidth * 3f + 0.6f; // трошки ширше за дорогу
        float hurdleHeight = 0.7f;                 // завжди 0.7м — гарантовано стрибається
        float barThickness = 0.12f;
        float postThickness = 0.16f;
        float depth = 0.25f;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        // Покрасимо кожну перекладину випадково в один з "警告" кольорів.
        Color[] palette = new Color[] {
            new Color(0.95f, 0.25f, 0.15f),  // червоний
            new Color(0.95f, 0.75f, 0.1f),   // жовтий
            new Color(0.2f, 0.2f, 0.22f),    // темний
            new Color(0.9f, 0.9f, 0.92f),    // білий
        };
        Color mainColor = palette[Random.Range(0, palette.Length)];
        Color stripeColor = new Color(1f - mainColor.r, 1f - mainColor.g, 1f - mainColor.b, 1f);

        Material mainMat = new Material(shader);
        mainMat.color = mainColor;
        if (mainMat.HasProperty("_BaseColor")) mainMat.SetColor("_BaseColor", mainColor);

        Material stripeMat = new Material(shader);
        stripeMat.color = stripeColor;
        if (stripeMat.HasProperty("_BaseColor")) stripeMat.SetColor("_BaseColor", stripeColor);

        // Ліва стійка
        CreateSubCube(container.transform, "PostL",
            new Vector3(-totalWidth * 0.5f + postThickness * 0.5f, hurdleHeight * 0.5f, 0f),
            new Vector3(postThickness, hurdleHeight, depth), mainMat);

        // Права стійка
        CreateSubCube(container.transform, "PostR",
            new Vector3(totalWidth * 0.5f - postThickness * 0.5f, hurdleHeight * 0.5f, 0f),
            new Vector3(postThickness, hurdleHeight, depth), mainMat);

        // Верхня балка (основна перешкода)
        CreateSubCube(container.transform, "Bar",
            new Vector3(0f, hurdleHeight - barThickness * 0.5f, 0f),
            new Vector3(totalWidth, barThickness, depth), stripeMat);

        // Нижня балка (декоративна)
        CreateSubCube(container.transform, "BarLow",
            new Vector3(0f, hurdleHeight * 0.4f, 0f),
            new Vector3(totalWidth - postThickness * 2f, barThickness * 0.7f, depth * 0.7f), stripeMat);

        container.transform.localPosition = new Vector3(0f, 0f, 35f);

        var tag = container.AddComponent<ObstacleTag>();
        tag.height = hurdleHeight;
        tag.halfWidth = laneWidth * 2f;
        tag.visualX = 0f;
        tag.isJumpObstacle = true;
        tag.depth = depth;
        obstacles.Add(container);
        Debug.Log($"[Subway] JumpObs Hurdle spawned  w={totalWidth:0.00}  h={hurdleHeight:0.00}  d={depth:0.00}");
    }

    static GameObject CreateSubCube(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = localScale;
        var col = cube.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        var r = cube.GetComponent<Renderer>();
        if (r != null && mat != null) r.material = mat;
        return cube;
    }

    void SpawnObstacle(int lane)
    {
        float x = (lane - 1) * laneWidth;

        GameObject obs;
        float obsHeight;
        float obsHalfWidth;

        if (laneObstaclePrefabs != null && laneObstaclePrefabs.Length > 0)
        {
            var prefab = laneObstaclePrefabs[Random.Range(0, laneObstaclePrefabs.Length)];
            var fbxInst = Object.Instantiate(prefab);

            // Wrap fbx in an empty container so we can place it freely without
            // disturbing the FBX's own rotation (often 270° X for Blender assets).
            var container = new GameObject("Obs_" + prefab.name);
            container.transform.SetParent(root.transform, false);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;

            fbxInst.transform.SetParent(container.transform, false);
            // Leave fbxInst's local transform alone — it preserves Blender->Unity axis fix

            obs = container;

            // Remove any colliders
            foreach (var col in obs.GetComponentsInChildren<Collider>())
                Object.Destroy(col);

            // Measure mesh bounds in LOCAL space of fbxInst (ignores fbx transform).
            // Then normalize: scale fbx so its height == 1.3, and offset fbxInst
            // such that the visual CENTER is at container origin (X,Z) and
            // the BOTTOM is at container y=0.
            Bounds fbxLocal = CalcLocalBounds(fbxInst);
            float currentHeight = Mathf.Max(0.01f, fbxLocal.size.y * fbxInst.transform.localScale.y);
            float desiredHeight = Random.Range(1.1f, 1.6f);
            float normScale = desiredHeight / currentHeight;
            fbxInst.transform.localScale = fbxInst.transform.localScale * normScale;

            // After scaling, re-centre the fbxInst inside the container so that
            // the mesh is perfectly centered on x=0, z=0 and rests on y=0.
            Bounds scaled = CalcLocalBoundsScaled(fbxInst);
            fbxInst.transform.localPosition = new Vector3(
                -scaled.center.x,
                -scaled.min.y,
                -scaled.center.z);

            // Now place container on the lane
            obs.transform.localPosition = new Vector3(x, 0f, 35f);

            obsHeight = scaled.size.y;
            // Cap obstacle half-width to 42% of lane so collision doesn't bleed into neighbours
            float rawHalf = scaled.size.x * 0.5f;
            obsHalfWidth = Mathf.Clamp(rawHalf, 0.35f, laneWidth * 0.42f);
        }
        else
        {
            // Fallback
            obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obs.name = "Obs";
            obs.transform.SetParent(root.transform);
            bool tall = Random.value > 0.5f;
            float h = tall ? 1.8f : 0.7f;
            float w = Random.value > 0.5f ? 2f : 1.2f;
            obs.transform.localScale = new Vector3(w, h, 1f);
            obs.transform.localPosition = new Vector3(x, h / 2f, 35f);
            var obsMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            obsMat.color = new Color(0.9f, 0.15f, 0.15f);
            obs.GetComponent<Renderer>().material = obsMat;
            Object.Destroy(obs.GetComponent<Collider>());
            obsHeight = h;
            obsHalfWidth = w * 0.5f;
        }

        var tag = obs.AddComponent<ObstacleTag>();
        tag.height = obsHeight;
        tag.halfWidth = obsHalfWidth;
        tag.visualX = x;
        obstacles.Add(obs);
    }

    static Bounds CalcBounds(GameObject go) => CalcWorldBounds(go);

    /// <summary>
    /// Bounds of go's meshes expressed in go's PARENT space, i.e. after applying
    /// go's own localPosition/localRotation/localScale. Used to know where the
    /// visual ended up inside a container.
    /// </summary>
    static Bounds CalcLocalBoundsScaled(GameObject go)
    {
        var parent = go.transform.parent;
        bool hasAny = false;
        Bounds b = new Bounds();
        Matrix4x4 toParent = parent != null ? parent.worldToLocalMatrix * go.transform.localToWorldMatrix
                                              : go.transform.localToWorldMatrix;

        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            // combine: go-local->world (from mf) then world->parent
            var goLocalToWorld = mf.transform.localToWorldMatrix;
            Matrix4x4 meshToParent = (parent != null ? parent.worldToLocalMatrix : Matrix4x4.identity) * goLocalToWorld;

            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1,
                    (i & 2) == 0 ? -1 : 1,
                    (i & 4) == 0 ? -1 : 1));
                Vector3 p = meshToParent.MultiplyPoint3x4(c);
                if (!hasAny) { b = new Bounds(p, Vector3.zero); hasAny = true; }
                else b.Encapsulate(p);
            }
        }
        foreach (var sm in go.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (sm.sharedMesh == null) continue;
            var goLocalToWorld = sm.transform.localToWorldMatrix;
            Matrix4x4 meshToParent = (parent != null ? parent.worldToLocalMatrix : Matrix4x4.identity) * goLocalToWorld;
            var mb = sm.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1,
                    (i & 2) == 0 ? -1 : 1,
                    (i & 4) == 0 ? -1 : 1));
                Vector3 p = meshToParent.MultiplyPoint3x4(c);
                if (!hasAny) { b = new Bounds(p, Vector3.zero); hasAny = true; }
                else b.Encapsulate(p);
            }
        }
        if (!hasAny) return new Bounds(Vector3.zero, Vector3.one);
        return b;
    }

    static Bounds CalcWorldBounds(GameObject go)
    {
        // Gather mesh filters / skinned meshes and compute their bounds in world-space
        // using mesh bounds and localToWorldMatrix — does NOT depend on rendering tick.
        bool hasAny = false;
        Bounds b = new Bounds();

        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var m = mf.transform.localToWorldMatrix;
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1,
                    (i & 2) == 0 ? -1 : 1,
                    (i & 4) == 0 ? -1 : 1));
                Vector3 wp = m.MultiplyPoint3x4(corner);
                if (!hasAny) { b = new Bounds(wp, Vector3.zero); hasAny = true; }
                else b.Encapsulate(wp);
            }
        }
        foreach (var sm in go.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (sm.sharedMesh == null) continue;
            var m = sm.transform.localToWorldMatrix;
            var mb = sm.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1,
                    (i & 2) == 0 ? -1 : 1,
                    (i & 4) == 0 ? -1 : 1));
                Vector3 wp = m.MultiplyPoint3x4(corner);
                if (!hasAny) { b = new Bounds(wp, Vector3.zero); hasAny = true; }
                else b.Encapsulate(wp);
            }
        }
        if (!hasAny) return new Bounds(go.transform.position, Vector3.one);
        return b;
    }

    /// <summary>
    /// Calculates mesh bounds in the local space of `go` (ignores go's own transform).
    /// Reliable immediately after Instantiate — does not depend on rendering.
    /// </summary>
    static Bounds CalcLocalBounds(GameObject go)
    {
        var filters = go.GetComponentsInChildren<MeshFilter>();
        var skinned = go.GetComponentsInChildren<SkinnedMeshRenderer>();

        bool hasAny = false;
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        Matrix4x4 worldToLocal = go.transform.worldToLocalMatrix;

        foreach (var mf in filters)
        {
            if (mf.sharedMesh == null) continue;
            var m = mf.transform.localToWorldMatrix;
            // Transform the mesh's local bounds corners into go's local space
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1,
                    (i & 2) == 0 ? -1 : 1,
                    (i & 4) == 0 ? -1 : 1));
                Vector3 world = m.MultiplyPoint3x4(corner);
                Vector3 local = worldToLocal.MultiplyPoint3x4(world);
                if (!hasAny) { b = new Bounds(local, Vector3.zero); hasAny = true; }
                else b.Encapsulate(local);
            }
        }
        foreach (var sm in skinned)
        {
            if (sm.sharedMesh == null) continue;
            var m = sm.transform.localToWorldMatrix;
            var mb = sm.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1,
                    (i & 2) == 0 ? -1 : 1,
                    (i & 4) == 0 ? -1 : 1));
                Vector3 world = m.MultiplyPoint3x4(corner);
                Vector3 local = worldToLocal.MultiplyPoint3x4(world);
                if (!hasAny) { b = new Bounds(local, Vector3.zero); hasAny = true; }
                else b.Encapsulate(local);
            }
        }

        if (!hasAny) return new Bounds(Vector3.zero, Vector3.one);
        return b;
    }

    void MoveAndCollide()
    {
        float ph = 0.45f;

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
            var tag = obs.GetComponent<ObstacleTag>();
            float obsH = tag != null ? tag.height : 1.5f;
            float obsHW = tag != null ? tag.halfWidth : obs.transform.localScale.x * 0.5f;
            float obsX = tag != null ? tag.visualX : op.x;

            // Z collision window: for jump obstacles, very tight (they're thin fences).
            // For lane obstacles, 0.9m each side around z=0.
            float zWindow = (tag != null && tag.isJumpObstacle) ? (tag.depth * 0.5f + 0.25f) : 0.9f;

            // For jump obstacles: require BOTTOM to be low (above-head clearance margin 0.2m)
            // and player Y must be below (obsH - margin) — i.e. not in the air yet.
            float yClearance = (tag != null && tag.isJumpObstacle) ? 0.2f : 0.3f;

            if (Mathf.Abs(op.z) < zWindow &&
                Mathf.Abs(obsX - playerX) < (obsHW + ph) &&
                playerY < obsH - yClearance)
            {
                Debug.Log($"[Subway] HIT by {obs.name}  obsZ={op.z:0.00}  obsX={obsX:0.00}  obsHW={obsHW:0.00}  obsH={obsH:0.00}  playerY={playerY:0.00}  isJump={(tag != null && tag.isJumpObstacle)}");
                DoGameOver();
                return;
            }
        }
    }

    void MoveCoins()
    {
        float ph = 0.6f;

        for (int i = coins.Count - 1; i >= 0; i--)
        {
            var coin = coins[i];
            if (coin == null) { coins.RemoveAt(i); continue; }

            coin.transform.localPosition += Vector3.back * speed * Time.deltaTime;
            coin.transform.Rotate(0f, 180f * Time.deltaTime, 0f);

            if (coin.transform.localPosition.z < -8f)
            {
                Object.Destroy(coin);
                coins.RemoveAt(i);
                continue;
            }

            Vector3 cp = coin.transform.localPosition;
            if (Mathf.Abs(cp.z) < 1f &&
                Mathf.Abs(cp.x - playerX) < ph &&
                Mathf.Abs(cp.y - 1f - playerY) < 1.3f)
            {
                coinCount++;
                Object.Destroy(coin);
                coins.RemoveAt(i);
                if (coinTextRef != null) coinTextRef.text = "Coins: " + coinCount;

                if (GameEconomy.Instance != null)
                {
                    GameEconomy.Instance.AwardDopamine(GameEconomy.ActSubway);
                    GameEconomy.Instance.AddCoins(1);
                }
            }
        }
    }

    void DoGameOver()
    {
        gameOver = true;

        // Spawn a big puff of smoke + spark right where the player crashed
        SpawnCrashSmoke();

        if (infoText != null)
        {
            infoText.text = "GAME OVER!  Score: " + score + "  Coins: " + coinCount +
                "\nSPACE — restart  |  E/ESC — exit";
            infoText.color = new Color(1f, 0.3f, 0.3f, 1f);
            infoText.fontSize = 32;
        }
    }

    void SpawnCrashSmoke()
    {
        if (player == null || root == null) return;

        var smokeRoot = new GameObject("CrashSmoke");
        smokeRoot.transform.SetParent(root.transform, false);
        smokeRoot.transform.localPosition = player.transform.localPosition + new Vector3(0f, 0.8f, 0f);

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        // Procedural soft circular smoke puff texture (radial gradient)
        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f;
                float dy = (y - 31.5f) / 31.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                a *= Random.Range(0.85f, 1f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();

        // Big grey smoke cloud
        CreateSmokeLayer(smokeRoot.transform, shader, tex,
            new Color(0.78f, 0.78f, 0.80f, 0.9f),
            2.5f, 6f, 3.5f, 80, 2.5f, -0.5f);

        // Dark smoke core
        CreateSmokeLayer(smokeRoot.transform, shader, tex,
            new Color(0.22f, 0.22f, 0.24f, 0.95f),
            1.8f, 4.5f, 2.5f, 60, 2.0f, -0.3f);

        // Orange flash burst
        CreateSmokeLayer(smokeRoot.transform, shader, tex,
            new Color(1f, 0.55f, 0.15f, 1f),
            1.5f, 0.3f, 6f, 40, 0.5f, 0f);

        var killer = smokeRoot.AddComponent<AutoDestroyAfter>();
        killer.lifetime = 4f;
    }

    void CreateSmokeLayer(Transform parent, Shader shader, Texture2D tex,
        Color color, float startSize, float endSize, float speed, int count,
        float lifetime, float gravity)
    {
        var go = new GameObject("SmokeLayer");
        go.transform.SetParent(parent, false);

        var ps = go.AddComponent<ParticleSystem>();
        var psr = go.GetComponent<ParticleSystemRenderer>();

        var mat = new Material(shader);
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent for URP
        if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        psr.material = mat;
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.sortingOrder = 5;

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = startSize;
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(count * 2, 200);
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, count)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.6f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        var sizeCurve = new AnimationCurve();
        float ratio = startSize / Mathf.Max(0.01f, endSize);
        sizeCurve.AddKey(0f, Mathf.Min(1f, ratio));
        sizeCurve.AddKey(1f, endSize > startSize ? 1f : 0.1f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(Mathf.Max(startSize, endSize), sizeCurve);

        var colOverLifetime = ps.colorOverLifetime;
        colOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color * 0.7f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(color.a * 0.85f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            });
        colOverLifetime.color = grad;

        var rotOverLifetime = ps.rotationOverLifetime;
        rotOverLifetime.enabled = true;
        rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

        ps.Play();
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
        if (coinTextRef != null) coinTextRef.text = "Coins: 0";
        if (infoText != null)
        {
            infoText.text = "A/D — lanes  |  SPACE — jump  |  E/ESC — exit";
            infoText.color = new Color(1, 1, 1, 0.5f);
            infoText.fontSize = 24;
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
    public float halfWidth;
    public float visualX;
    public bool isJumpObstacle;
    public float depth = 1.0f; // z-extent used for collision window
}

public class AutoDestroyAfter : MonoBehaviour
{
    public float lifetime = 3f;
    float t;
    void Update()
    {
        t += Time.deltaTime;
        if (t >= lifetime)
            Object.Destroy(gameObject);
    }
}
