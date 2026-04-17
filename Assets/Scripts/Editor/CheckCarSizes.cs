using UnityEngine;
using UnityEditor;

public class CheckCarSizes
{
    public static void Execute()
    {
        string[] paths = {
            "Assets/TextursAssets/games/Low_Poly_Cars_DevilsWorkShop_V03/FBX 2013/Low_Poly_Vehicles_car01.fbx",
            "Assets/TextursAssets/games/Low_Poly_Cars_DevilsWorkShop_V03/FBX 2013/Low_Poly_Vehicles_carPolice.fbx",
            "Assets/TextursAssets/games/Low_Poly_Cars_DevilsWorkShop_V03/FBX 2013/Low_Poly_Vehicles_bus.fbx",
            "Assets/TextursAssets/games/Low_Poly_Cars_DevilsWorkShop_V03/FBX 2013/Low_Poly_Vehicles_pickupTruck01.fbx",
        };

        foreach (var path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.Log("NOT FOUND: " + path); continue; }

            var inst = Object.Instantiate(prefab);
            var renderers = inst.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                Debug.Log(prefab.name + " size: " + b.size + " center: " + b.center);
            }
            Object.DestroyImmediate(inst);
        }
    }
}
