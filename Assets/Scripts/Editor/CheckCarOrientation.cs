using UnityEngine;
using UnityEditor;

public class CheckCarOrientation
{
    public static void Execute()
    {
        string fbxPath = "Assets/TextursAssets/games/Low_Poly_Cars_DevilsWorkShop_V03/FBX 2013/";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath + "Low_Poly_Vehicles_car01.fbx");
        if (prefab == null) { Debug.Log("car01 not found"); return; }

        var inst = Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;

        var renderers = inst.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        Debug.Log("car01 forward test: bounds center=" + b.center + " size=" + b.size);
        Debug.Log("car01 transform.forward=" + inst.transform.forward);
        // Check which axis is longest (that's the car length direction)
        if (b.size.z > b.size.x)
            Debug.Log("Car faces Z axis (forward/back)");
        else
            Debug.Log("Car faces X axis (left/right)");

        // Check children
        foreach (Transform child in inst.transform)
            Debug.Log("  child: " + child.name + " pos=" + child.localPosition + " rot=" + child.localEulerAngles);

        Object.DestroyImmediate(inst);
    }
}
