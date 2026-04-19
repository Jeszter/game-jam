using UnityEditor;
using UnityEngine;

public static class InspectPoliceCar
{
    public static void Execute()
    {
        // Перевіряємо що імпортер FBX не має Scale Factor = 0.01
        string[] fbxs = {
            "Assets/Resources/PoliceChase/Cars/Low_Poly_Vehicles_car03.fbx",
            "Assets/Resources/PoliceChase/Cars/Low_Poly_Vehicles_carPolice.fbx",
            "Assets/Resources/PoliceChase/Cars/Low_Poly_Vehicles_car01.fbx",
        };
        foreach (var p in fbxs)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var mi = AssetImporter.GetAtPath(p) as ModelImporter;
            if (go == null)
            {
                Debug.Log("[InspectPoliceCar] MISSING: " + p);
                continue;
            }
            // Обчислюємо AABB через renderers
            var rs = go.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds();
            bool has = false;
            foreach (var r in rs)
            {
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
            Debug.Log($"[InspectPoliceCar] {System.IO.Path.GetFileName(p)}: " +
                      $"renderers={rs.Length} bounds size={(has ? b.size.ToString("F3") : "NONE")} " +
                      $"globalScale={(mi != null ? mi.globalScale.ToString("F3") : "?")}");
        }

        // Текстури
        string[] texs = {
            "Assets/Resources/PoliceChase/Textures/car03.png",
            "Assets/Resources/PoliceChase/Textures/carPolice.png",
        };
        foreach (var t in texs)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(t);
            Debug.Log($"[InspectPoliceCar] tex {System.IO.Path.GetFileName(t)} -> {(tex != null ? $"{tex.width}x{tex.height}" : "MISSING")}");
        }
    }
}
