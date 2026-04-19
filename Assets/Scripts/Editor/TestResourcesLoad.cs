using UnityEditor;
using UnityEngine;

public static class TestResourcesLoad
{
    public static void Execute()
    {
        string[] models = {
            "PoliceChase/Cars/Low_Poly_Vehicles_car03",
            "PoliceChase/Cars/Low_Poly_Vehicles_carPolice",
            "PoliceChase/Cars/Low_Poly_Vehicles_car01",
        };
        foreach (var m in models)
        {
            var go = Resources.Load<GameObject>(m);
            if (go == null)
            {
                Debug.Log($"[TestResourcesLoad] {m} -> NULL");
                continue;
            }
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var mfs = go.GetComponentsInChildren<MeshFilter>(true);
            Debug.Log($"[TestResourcesLoad] {m} -> OK name={go.name} childrenCount={go.transform.childCount} renderers={rs.Length} meshFilters={mfs.Length}");
        }

        string[] texs = {
            "PoliceChase/Textures/car03",
            "PoliceChase/Textures/carPolice",
        };
        foreach (var t in texs)
        {
            var tex = Resources.Load<Texture2D>(t);
            Debug.Log($"[TestResourcesLoad] tex {t} -> {(tex != null ? $"{tex.width}x{tex.height}" : "NULL")}");
        }
    }
}
