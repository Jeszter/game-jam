using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PoliceChaseGame : MonoBehaviour
{
    private GameObject root;
    private Camera cam;
    private GameObject player;
    private GameObject police;
    private TMP_Text scoreText;
    private TMP_Text infoText;
    private TMP_Text speedText;

    private float laneWidth = 3f;
    private int laneCount = 4;
    private float speed = 14f;
    private float speedIncrease = 0.5f;
    private float spawnInterval = 0.8f;
    private float steerSpeed = 14f;

    private float playerX = 0f;
    private float targetX = 0f;
    private int score = 0;
    private float distanceTraveled = 0f;
    private float spawnTimer = 0f;
    private bool gameOver = false;
    private float policeDistance = 10f;
    private int nearMisses = 0;
    private List<GameObject> traffic = new List<GameObject>();

    private GameObject ground;
    private List<GameObject> roadLines = new List<GameObject>();

    // Paths (only FBX that actually exist!)
    private static readonly string basePath = "Assets/TextursAssets/games/Low_Poly_Cars_DevilsWorkShop_V03/";
    private static readonly string fbxPath = basePath + "FBX 2013/";
    private static readonly string texPath = basePath + "Texture/";

    // Traffic cars (all have FBX + matching texture)
    private static readonly string[] trafficModels = {
        "Low_Poly_Vehicles_car01",
        "Low_Poly_Vehicles_car02",
        "Low_Poly_Vehicles_car03",
        "Low_Poly_Vehicles_bus",
        "Low_Poly_Vehicles_pickupTruck01",
        "Low_Poly_Vehicles_pickupTruck02",
    };
    private static readonly string[] trafficTextures = {
        "car01", "car02", "car03", "bus01",
        "pickupTruck01", "pickupTruck02",
    };

    // Pre-cached pairs (model prefab + texture) loaded once in Init
    private struct CarAsset { public GameObject prefab; public Texture2D tex; }
    private List<CarAsset> trafficAssets = new List<CarAsset>();
    private CarAsset playerAsset;
    private CarAsset policeAsset;

    private float carScale = 0.45f;

    public void Init(GameObject gameRoot, Camera gameCam)
    {
        root = gameRoot;
        cam = gameCam;

        cam.transform.localPosition = new Vector3(0f, 16f, -8f);
        cam.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
        cam.backgroundColor = new Color(0.12f, 0.15f, 0.12f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.depth = 10;

        var shader = Shader.Find("Universal Render Pipeline/Lit");

        // Pre-cache assets
        PreloadAssets();

        // Road
        float roadWidth = laneCount * laneWidth + 3f;
        ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Road";
        ground.transform.SetParent(root.transform);
        ground.transform.localScale = new Vector3(roadWidth, 0.1f, 500f);
        ground.transform.localPosition = new Vector3(0f, 0f, 200f);
        var roadMat = new Material(shader);
        roadMat.SetColor("_BaseColor", new Color(0.22f, 0.22f, 0.25f));
        ground.GetComponent<Renderer>().material = roadMat;
        Object.Destroy(ground.GetComponent<Collider>());

        // Road edges
        for (int s = -1; s <= 1; s += 2)
        {
            var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "Edge";
            edge.transform.SetParent(root.transform);
            edge.transform.localScale = new Vector3(0.15f, 0.12f, 500f);
            edge.transform.localPosition = new Vector3(s * roadWidth * 0.5f, 0.06f, 200f);
            var eMat = new Material(shader);
            eMat.SetColor("_BaseColor", Color.white);
            edge.GetComponent<Renderer>().material = eMat;
            Object.Destroy(edge.GetComponent<Collider>());
        }

        // Dashed lines
        for (int i = 0; i < 30; i++)
        {
            for (int lane = 0; lane < laneCount - 1; lane++)
            {
                float lx = (lane - (laneCount - 2) * 0.5f) * laneWidth;
                var dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = "Dash";
                dash.transform.SetParent(root.transform);
                dash.transform.localScale = new Vector3(0.08f, 0.12f, 1.5f);
                dash.transform.localPosition = new Vector3(lx, 0.06f, i * 4f - 20f);
                var dMat = new Material(shader);
                dMat.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.9f));
                dash.GetComponent<Renderer>().material = dMat;
                Object.Destroy(dash.GetComponent<Collider>());
                roadLines.Add(dash);
            }
        }

        // Player — use car03 with its texture (nice looking sports car)
        player = InstantiateCar(playerAsset, "PlayerCar");
        if (player == null) player = MakeFallbackCar("PlayerCar", new Color(0.1f, 0.4f, 1f));
        player.transform.SetParent(root.transform);
        player.transform.localScale = Vector3.one * carScale;
        player.transform.localPosition = new Vector3(0f, 0f, 0f);
        player.transform.localRotation = Quaternion.identity;
        RemoveAllColliders(player);

        // Police car
        police = InstantiateCar(policeAsset, "PoliceCar");
        if (police == null) police = MakeFallbackCar("Police", new Color(0.1f, 0.1f, 0.6f));
        police.transform.SetParent(root.transform);
        police.transform.localScale = Vector3.one * carScale;
        police.transform.localPosition = new Vector3(0f, 0f, -policeDistance);
        police.transform.localRotation = Quaternion.identity;
        RemoveAllColliders(police);

        // Light
        var lightObj = new GameObject("Light");
        lightObj.transform.SetParent(root.transform);
        lightObj.transform.localPosition = new Vector3(0f, 15f, 5f);
        lightObj.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
        var lt = lightObj.AddComponent<Light>();
        lt.type = LightType.Directional;
        lt.intensity = 1.2f;

        // UI
        var canvasObj = new GameObject("ChaseCanvas");
        canvasObj.transform.SetParent(root.transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        scoreText = MakeText(canvasObj.transform, "Score", "0", 64,
            new Vector2(0.5f, 1f), new Vector2(0f, -40f));
        scoreText.color = Color.white;

        speedText = MakeText(canvasObj.transform, "Speed", "Speed: 50 km/h", 28,
            new Vector2(0.12f, 1f), new Vector2(0f, -40f));
        speedText.color = new Color(1f, 0.6f, 0.2f);

        var nmText = MakeText(canvasObj.transform, "NearMiss", "Near Misses: 0", 28,
            new Vector2(0.88f, 1f), new Vector2(0f, -40f));
        nmText.color = new Color(0.3f, 1f, 0.5f);

        infoText = MakeText(canvasObj.transform, "Info",
            "A/D — steer  |  E/ESC — exit", 24,
            new Vector2(0.5f, 0f), new Vector2(0f, 25f));
        infoText.color = new Color(1, 1, 1, 0.5f);

        playerX = 0f;
        targetX = 0f;
        score = 0;
        distanceTraveled = 0f;
        gameOver = false;
        speed = 14f;
        spawnInterval = 0.8f;
        nearMisses = 0;
        policeDistance = 10f;
    }

    void PreloadAssets()
    {
        trafficAssets.Clear();
#if UNITY_EDITOR
        for (int i = 0; i < trafficModels.Length; i++)
        {
            var a = LoadAssetPair(trafficModels[i], trafficTextures[i]);
            if (a.prefab != null) trafficAssets.Add(a);
        }
        playerAsset = LoadAssetPair("Low_Poly_Vehicles_car03", "car03");
        policeAsset = LoadAssetPair("Low_Poly_Vehicles_carPolice", "carPolice");
#endif
    }

#if UNITY_EDITOR
    CarAsset LoadAssetPair(string model, string tex)
    {
        var a = new CarAsset();
        a.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath + model + ".fbx");

        // Try png first, then tga
        a.tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath + tex + ".png");
        if (a.tex == null) a.tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath + tex + ".tga");

        if (a.prefab == null)
            Debug.LogWarning($"[PoliceChaseGame] Missing car model: {fbxPath}{model}.fbx");
        if (a.tex == null)
            Debug.LogWarning($"[PoliceChaseGame] Missing car texture: {texPath}{tex}.png/.tga");

        return a;
    }
#endif

    void Update()
    {
        if (root == null || player == null) return;
        if (Keyboard.current == null) return;

        if (gameOver)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame) Restart();
            return;
        }

        float steerInput = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steerInput -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steerInput += 1f;

        float halfRoad = (laneCount - 1) * laneWidth * 0.5f + 0.5f;
        targetX += steerInput * steerSpeed * Time.deltaTime;
        targetX = Mathf.Clamp(targetX, -halfRoad, halfRoad);
        playerX = Mathf.MoveTowards(playerX, targetX, steerSpeed * 1.5f * Time.deltaTime);

        player.transform.localPosition = new Vector3(playerX, 0f, 0f);
        // Player faces FORWARD (+Z, away from camera)
        player.transform.localRotation = Quaternion.Euler(0f, 0f + steerInput * 12f, -steerInput * 4f);

        // Police follows
        float policeTargetX = Mathf.MoveTowards(police.transform.localPosition.x, playerX, 5f * Time.deltaTime);
        policeDistance = Mathf.Max(5f, policeDistance - 0.08f * Time.deltaTime);
        police.transform.localPosition = new Vector3(policeTargetX, 0f, -policeDistance);
        // Police chases from behind (also faces +Z)
        police.transform.localRotation = Quaternion.identity;

        speed += speedIncrease * Time.deltaTime;
        distanceTraveled += speed * Time.deltaTime;
        score = (int)(distanceTraveled * 1.5f);
        if (scoreText != null) scoreText.text = score.ToString();
        if (speedText != null) speedText.text = "Speed: " + (int)(speed * 3.6f) + " km/h";

        // Scroll road lines
        foreach (var dash in roadLines)
        {
            if (dash == null) continue;
            dash.transform.localPosition += Vector3.back * speed * Time.deltaTime;
            if (dash.transform.localPosition.z < -25f)
                dash.transform.localPosition += new Vector3(0f, 0f, 120f);
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnTraffic();
            spawnTimer = 0f;
            spawnInterval = Mathf.Max(0.3f, spawnInterval - 0.005f);
        }

        MoveTraffic();
    }

    void SpawnTraffic()
    {
        int count = Random.value > 0.65f ? 2 : 1;
        List<int> usedLanes = new List<int>();

        for (int i = 0; i < count; i++)
        {
            int lane;
            do { lane = Random.Range(0, laneCount); } while (usedLanes.Contains(lane));
            usedLanes.Add(lane);

            float x = (lane - (laneCount - 1) * 0.5f) * laneWidth;

            GameObject car = null;
            if (trafficAssets.Count > 0)
            {
                var asset = trafficAssets[Random.Range(0, trafficAssets.Count)];
                car = InstantiateCar(asset, "Traffic");
            }
            if (car == null) car = MakeFallbackCar("Traffic", RandomColor());

            car.transform.SetParent(root.transform);
            car.transform.localScale = Vector3.one * carScale;
            car.transform.localPosition = new Vector3(x, 0f, 45f);
            // Traffic drives in the SAME direction as player (+Z) —
            // player overtakes them because his apparent speed is higher.
            // So visually traffic cars scroll toward the camera (-Z) but face forward.
            car.transform.localRotation = Quaternion.identity;
            RemoveAllColliders(car);
            traffic.Add(car);
        }
    }

    void MoveTraffic()
    {
        float playerHalfW = 0.55f;

        for (int i = traffic.Count - 1; i >= 0; i--)
        {
            var car = traffic[i];
            if (car == null) { traffic.RemoveAt(i); continue; }

            car.transform.localPosition += Vector3.back * speed * Time.deltaTime;

            if (car.transform.localPosition.z < -15f)
            {
                Object.Destroy(car);
                traffic.RemoveAt(i);
                continue;
            }

            Vector3 cp = car.transform.localPosition;
            float carHalfW = 0.55f;

            if (Mathf.Abs(cp.z) < 1.8f &&
                Mathf.Abs(cp.x - playerX) < (carHalfW + playerHalfW))
            {
                DoGameOver();
                return;
            }

            // Near miss
            if (cp.z < -0.5f && cp.z > -2.5f &&
                Mathf.Abs(cp.x - playerX) < (carHalfW + playerHalfW + 0.8f) &&
                Mathf.Abs(cp.x - playerX) >= (carHalfW + playerHalfW))
            {
                nearMisses++;
                score += 50;
                // Near miss — сильный прилив дофамина + шанс на монеты
                if (GameEconomy.Instance != null)
                    GameEconomy.Instance.AwardDopamine(GameEconomy.ActPolice);
                var nmObj = GameObject.Find("NearMiss");
                if (nmObj != null)
                {
                    var t = nmObj.GetComponent<TMP_Text>();
                    if (t != null) t.text = "Near Misses: " + nearMisses;
                }
            }
        }
    }

    void DoGameOver()
    {
        gameOver = true;
        if (infoText != null)
        {
            infoText.text = "BUSTED!  Score: " + score + "  Near Misses: " + nearMisses +
                "\nSPACE — restart  |  E/ESC — exit";
            infoText.color = new Color(1f, 0.3f, 0.3f, 1f);
            infoText.fontSize = 32;
        }
    }

    void Restart()
    {
        foreach (var c in traffic) if (c != null) Object.Destroy(c);
        traffic.Clear();

        gameOver = false;
        score = 0;
        distanceTraveled = 0f;
        speed = 14f;
        spawnTimer = 0f;
        spawnInterval = 0.8f;
        playerX = 0f;
        targetX = 0f;
        nearMisses = 0;
        policeDistance = 10f;

        if (player != null) player.transform.localPosition = new Vector3(0f, 0f, 0f);
        if (police != null) police.transform.localPosition = new Vector3(0f, 0f, -10f);
        if (scoreText != null) scoreText.text = "0";
        if (speedText != null) speedText.text = "Speed: 50 km/h";
        if (infoText != null)
        {
            infoText.text = "A/D — steer  |  E/ESC — exit";
            infoText.color = new Color(1, 1, 1, 0.5f);
            infoText.fontSize = 24;
        }
        var nmObj = GameObject.Find("NearMiss");
        if (nmObj != null)
        {
            var t = nmObj.GetComponent<TMP_Text>();
            if (t != null) t.text = "Near Misses: 0";
        }
    }

    public void Cleanup()
    {
        foreach (var c in traffic) if (c != null) Object.Destroy(c);
        traffic.Clear();
    }

    // ---- Car instantiation ----

    GameObject InstantiateCar(CarAsset asset, string nameHint)
    {
        if (asset.prefab == null) return null;
        var instance = Object.Instantiate(asset.prefab);
        instance.name = nameHint;

        // Apply texture to all child renderers
        if (asset.tex != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var renderers = instance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                // Keep one material per renderer (merge material slots to same textured mat)
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = new Material(shader);
                    mat.SetTexture("_BaseMap", asset.tex);
                    mat.SetColor("_BaseColor", Color.white);
                    mats[i] = mat;
                }
                r.materials = mats;
            }
        }

        return instance;
    }

    GameObject MakeFallbackCar(string name, Color color)
    {
        var car = GameObject.CreatePrimitive(PrimitiveType.Cube);
        car.name = name;
        car.transform.localScale = new Vector3(1.1f, 0.5f, 2f);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        car.GetComponent<Renderer>().material = mat;
        Object.Destroy(car.GetComponent<Collider>());
        return car;
    }

    void RemoveAllColliders(GameObject obj)
    {
        var colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var c in colliders) Object.Destroy(c);
    }

    Color RandomColor()
    {
        Color[] colors = {
            new Color(0.85f, 0.15f, 0.15f), new Color(0.15f, 0.7f, 0.15f),
            new Color(0.85f, 0.85f, 0.15f), new Color(0.9f, 0.5f, 0.1f),
            new Color(0.6f, 0.15f, 0.8f), new Color(0.9f, 0.9f, 0.9f),
        };
        return colors[Random.Range(0, colors.Length)];
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
