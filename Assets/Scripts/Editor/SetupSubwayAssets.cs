using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Готує префаби для Subway Surf у Resources, щоб runtime-код міг їх завантажити через
/// Resources.Load. Також налаштовує текстуру пакета як головний матеріал.
/// </summary>
public static class SetupSubwayAssets
{
    const string FBX_ROOT = "Assets/Barriers pack Demo/Barriers pack Demo/Fbx/";
    const string TEXTURE_PATH = FBX_ROOT + "texture.jpg";
    const string RES_DIR = "Assets/Resources/SubwaySurf";
    const string MAT_PATH = RES_DIR + "/SubwayBarrier.mat";

    public static void Execute()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(RES_DIR))
            AssetDatabase.CreateFolder("Assets/Resources", "SubwaySurf");

        // --- Material with pack's texture (URP/Lit) ---
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MAT_PATH);
        }
        else
        {
            mat.shader = shader;
        }
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
        EditorUtility.SetDirty(mat);

        // --- Character prefab ---
        BuildPrefab("Stickalungu_Animated", "SubwayPlayer", mat, playerTune: true);

        // --- Obstacle prefabs ---
        BuildPrefab("Cone",                  "Obs_Cone",       mat);
        BuildPrefab("Cone_001",              "Obs_Cone2",      mat);
        BuildPrefab("Barricade",             "Obs_Barricade",  mat);
        BuildPrefab("Barricade_Light",       "Obs_BarricadeLight", mat);
        BuildPrefab("Barricades",            "Obs_Barricades", mat);
        BuildPrefab("Barricades_Sign",       "Obs_Sign",       mat);
        BuildPrefab("Metal_Barricade",       "Obs_MetalBarr",  mat);
        BuildPrefab("Metal_Fence",           "Obs_MetalFence", mat);
        BuildPrefab("Concrete_Barrier",      "Obs_Concrete",   mat);
        BuildPrefab("Wooden_Barricade",      "Obs_Wooden",     mat);
        BuildPrefab("Stop_Sign",             "Obs_Stop",       mat);
        BuildPrefab("Safety_Barrier",        "Obs_Safety",     mat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupSubwayAssets] Prefabs created in Resources/SubwaySurf/");
    }

    static void BuildPrefab(string fbxName, string prefabName, Material sharedMat, bool playerTune = false)
    {
        string fbxPath = FBX_ROOT + fbxName + ".fbx";
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbx == null)
        {
            Debug.LogWarning("[SetupSubwayAssets] Missing FBX: " + fbxPath);
            return;
        }

        // Instantiate, apply material to all renderers, then save as prefab
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        instance.name = prefabName;

        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = sharedMat;
            r.sharedMaterials = mats;
        }

        // --- Scale / normalize so что prefab has sane size ---
        Bounds b = CalcBounds(instance);
        if (b.size != Vector3.zero)
        {
            float targetHeight = playerTune ? 1.6f : 1.4f; // обычный размер для subway
            float scale = targetHeight / Mathf.Max(0.01f, b.size.y);
            instance.transform.localScale = Vector3.one * scale;

            // Recompute bounds after scale, recenter so pivot is at base
            b = CalcBounds(instance);
            Vector3 offset = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            foreach (Transform child in instance.transform)
                child.localPosition += offset;
        }

        string prefabPath = RES_DIR + "/" + prefabName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        Debug.Log("[SetupSubwayAssets] Saved " + prefabPath);
    }

    static Bounds CalcBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
