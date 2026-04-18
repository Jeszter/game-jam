using UnityEditor;
using UnityEngine;

public static class InspectBarrier
{
    public static void Execute()
    {
        string[] names = { "Cone.fbx", "Stop_Sign.fbx", "Barricade.fbx", "Metal_Fence.fbx" };
        foreach (var n in names)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Barriers pack Demo/Barriers pack Demo/Fbx/" + n);
            if (go == null) { Debug.Log(n + ": NOT FOUND"); continue; }

            // Log root rotation and scale
            Debug.Log($"{n}: rootRot={go.transform.localEulerAngles} rootScale={go.transform.localScale}");

            // List all transforms in hierarchy
            var all = go.GetComponentsInChildren<Transform>();
            foreach (var t in all)
            {
                var mf = t.GetComponent<MeshFilter>();
                Debug.Log($"   {t.name}  rot={t.localEulerAngles} scale={t.localScale} mesh={(mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "-")}" +
                    (mf != null && mf.sharedMesh != null ? $"  bounds={mf.sharedMesh.bounds.size}" : ""));
            }
        }
    }
}
