#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Додає у ванну кімнату:
/// 1) Штору з фізикою (Cloth) яка реагує на гравця - "ShowerCurtainCloth"
/// 2) Кран над ванною з інтерактивним ParticleSystem води - "BathFaucet"
/// 3) Кран над раковиною з водою - "SinkFaucet"
/// Гравець натискає E дивлячись на кран щоб увімкнути/вимкнути воду.
/// </summary>
public static class BuildBathroomProps
{
    public static void Execute()
    {
        // ------------------------------------------------------------
        // 1. ШТОРА душа (Cloth)
        // ------------------------------------------------------------
        // Розміщуємо над ванною: ванна 784.93..786.99 (X), z=-10.98..-7.34
        // Вішаємо штору на стороні проходу - на передній (z = -7.4..-7.5).
        BuildShowerCurtain();

        // ------------------------------------------------------------
        // 2. Кран над ванною з водою
        // ------------------------------------------------------------
        // Ванна high end: max=(787.0, 195.24, -7.34), краї ванни близько y=193.5
        // Розміщуємо кран на верхньому краю ванни біля задньої стіни (z=-10.7).
        BuildBathFaucet();

        // ------------------------------------------------------------
        // 3. Кран над раковиною
        // ------------------------------------------------------------
        // Sink bounds: min=(789.38, 192.10, -9.39) max=(790.11, 194.50, -7.48)
        // Краник на верху, над чашею раковини
        BuildSinkFaucet();

        // зберегти
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[BuildBathroomProps] Done: shower curtain + 2 faucets. Scene saved.");
    }

    // ===================================================================
    // CURTAIN
    // ===================================================================
    private static void BuildShowerCurtain()
    {
        const string NAME = "ShowerCurtainCloth";
        // видаляємо стару якщо є
        var existing = GameObject.Find(NAME);
        if (existing != null) Object.DestroyImmediate(existing);

        // Штора висить перед ванною (на z = -7.4), X від 785.0 до 787.0, зверху y=195.2, знизу y=192.8
        Vector3 topLeft  = new Vector3(785.0f, 195.1f, -7.45f);
        Vector3 bottomRight = new Vector3(787.0f, 192.7f, -7.45f);

        GameObject go = new GameObject(NAME);
        go.transform.position = (topLeft + bottomRight) * 0.5f;

        float width  = Mathf.Abs(bottomRight.x - topLeft.x);
        float height = Mathf.Abs(topLeft.y - bottomRight.y);

        int cols = 10;
        int rows = 12;

        Mesh mesh = GeneratePlaneMesh(cols, rows, width, height);
        mesh.name = "CurtainMesh";

        // SkinnedMeshRenderer (Cloth потребує саме його)
        var smr = go.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mesh;

        // pink unlit material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")
                                   ?? Shader.Find("Unlit/Color"));
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.85f, 0.35f, 0.55f, 1f));
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     new Color(0.85f, 0.35f, 0.55f, 1f));
        mat.name = "ShowerCurtainMat";
        smr.sharedMaterial = mat;

        // Cloth
        var cloth = go.AddComponent<Cloth>();
        // pin top row of vertices - щоб штора висіла
        var coefs = new ClothSkinningCoefficient[mesh.vertexCount];
        var verts = mesh.vertices;
        float topY = float.MinValue;
        foreach (var v in verts) if (v.y > topY) topY = v.y;

        for (int i = 0; i < verts.Length; i++)
        {
            bool isTopRow = Mathf.Abs(verts[i].y - topY) < 0.01f;
            coefs[i].maxDistance    = isTopRow ? 0f : 2f;
            coefs[i].collisionSphereDistance = 0.02f;
        }
        cloth.coefficients = coefs;
        cloth.bendingStiffness  = 0.3f;
        cloth.stretchingStiffness = 0.8f;
        cloth.damping           = 0.15f;
        cloth.worldVelocityScale = 0.3f;
        cloth.worldAccelerationScale = 1f;
        cloth.friction          = 0.5f;

        // Зіткнення з гравцем: додаємо CapsuleCollider гравця як ClothCollider
        var player = GameObject.Find("player");
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            CapsuleCollider capCol = null;
            // Cloth приймає тільки Capsule/SphereCollider
            capCol = player.GetComponent<CapsuleCollider>();
            if (capCol == null)
            {
                capCol = player.AddComponent<CapsuleCollider>();
                capCol.center = new Vector3(0f, 0.9f, 0f);
                capCol.height = 1.8f;
                capCol.radius = 0.3f;
                capCol.isTrigger = true; // не заважатиме руху
            }
            cloth.capsuleColliders = new CapsuleCollider[] { capCol };
        }

        Debug.Log($"[ShowerCurtain] created {NAME} with {mesh.vertexCount} verts at {go.transform.position}");
    }

    private static Mesh GeneratePlaneMesh(int cols, int rows, float width, float height)
    {
        int vc = (cols + 1) * (rows + 1);
        Vector3[] verts = new Vector3[vc];
        Vector2[] uvs   = new Vector2[vc];
        for (int y = 0; y <= rows; y++)
        {
            for (int x = 0; x <= cols; x++)
            {
                int idx = y * (cols + 1) + x;
                float fx = (float)x / cols - 0.5f;
                float fy = (float)y / rows - 0.5f;
                verts[idx] = new Vector3(fx * width, fy * height, 0f);
                uvs[idx]   = new Vector2((float)x / cols, (float)y / rows);
            }
        }
        int[] tris = new int[cols * rows * 6 * 2]; // double-sided
        int t = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int i0 = y * (cols + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (cols + 1);
                int i3 = i2 + 1;
                // front
                tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
                // back
                tris[t++] = i0; tris[t++] = i1; tris[t++] = i2;
                tris[t++] = i1; tris[t++] = i3; tris[t++] = i2;
            }
        }
        Mesh m = new Mesh();
        m.vertices  = verts;
        m.uv        = uvs;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // ===================================================================
    // FAUCETS
    // ===================================================================
    private static void BuildBathFaucet()
    {
        const string NAME = "BathFaucet";
        var old = GameObject.Find(NAME);
        if (old != null) Object.DestroyImmediate(old);

        // Розміщуємо над ванною біля задньої стіни (z ~ -10.7), посередині X ~ 786
        Vector3 pos = new Vector3(786.0f, 193.8f, -10.5f);
        var root = BuildFaucetRoot(NAME, pos, Quaternion.Euler(0f, 0f, 0f));
        AttachWater(root, waterDir: new Vector3(0f, -1f, 0.3f));
    }

    private static void BuildSinkFaucet()
    {
        const string NAME = "SinkFaucet";
        var old = GameObject.Find(NAME);
        if (old != null) Object.DestroyImmediate(old);

        // Розміщуємо над раковиною (sink top ~194.5), посередині
        Vector3 pos = new Vector3(789.74f, 194.2f, -8.44f);
        var root = BuildFaucetRoot(NAME, pos, Quaternion.Euler(0f, 0f, 0f));
        AttachWater(root, waterDir: new Vector3(0f, -1f, 0f));
    }

    private static GameObject BuildFaucetRoot(string name, Vector3 pos, Quaternion rot)
    {
        GameObject root = new GameObject(name);
        root.transform.position = pos;
        root.transform.rotation = rot;
        root.tag = "Untagged";

        // Corpus краника - маленький циліндр (носик)
        GameObject spout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spout.name = "Spout";
        spout.transform.SetParent(root.transform, false);
        spout.transform.localPosition = new Vector3(0f, 0f, 0f);
        spout.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // горизонтальний
        spout.transform.localScale    = new Vector3(0.06f, 0.12f, 0.06f);
        var spoutMr = spout.GetComponent<MeshRenderer>();
        Material chrome = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (chrome.HasProperty("_BaseColor")) chrome.SetColor("_BaseColor", new Color(0.7f, 0.75f, 0.8f));
        if (chrome.HasProperty("_Metallic"))  chrome.SetFloat("_Metallic", 0.9f);
        if (chrome.HasProperty("_Smoothness")) chrome.SetFloat("_Smoothness", 0.85f);
        chrome.name = "FaucetChrome";
        spoutMr.sharedMaterial = chrome;

        // видаляємо дефолтний CapsuleCollider додамо свій побільше (interact)
        var pc = spout.GetComponent<Collider>();
        if (pc != null) Object.DestroyImmediate(pc);

        // інтерактивний колайдер - трохи більше щоб легше прицілитись
        var interactCol = root.AddComponent<BoxCollider>();
        interactCol.size   = new Vector3(0.2f, 0.15f, 0.25f);
        interactCol.center = Vector3.zero;
        interactCol.isTrigger = false;

        // компонент FaucetWater
        root.AddComponent<FaucetWater>();

        return root;
    }

    private static void AttachWater(GameObject root, Vector3 waterDir)
    {
        // ParticleSystem - струмінь води
        GameObject waterGo = new GameObject("Water");
        waterGo.transform.SetParent(root.transform, false);
        waterGo.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        waterGo.transform.localRotation = Quaternion.LookRotation(waterDir.normalized);

        var ps = waterGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime  = 0.8f;
        main.startSpeed     = 2.0f;
        main.startSize      = 0.025f;
        main.startColor     = new Color(0.55f, 0.75f, 0.95f, 0.9f);
        main.gravityModifier = 2.5f;
        main.maxParticles   = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake    = false;

        var emission = ps.emission;
        emission.enabled = false;
        emission.rateOverTime = 120f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 4f;
        shape.radius    = 0.005f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.6f, 0.8f, 1f), 0f),
                new GradientColorKey(new Color(0.5f, 0.7f, 0.95f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.4f, 1f)
            });
        col.color = grad;

        var sob = ps.sizeOverLifetime;
        sob.enabled = true;
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.5f));
        sob.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        Shader psShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                       ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
        if (psShader != null)
        {
            Material m = new Material(psShader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.6f, 0.8f, 1f, 1f));
            m.name = "WaterStream";
            rend.sharedMaterial = m;
        }
        rend.alignment = ParticleSystemRenderSpace.View;

        // прикріпляємо до FaucetWater
        var fw = root.GetComponent<FaucetWater>();
        if (fw != null) fw.waterParticles = ps;
    }
}
#endif
