using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class MeasureBarriers
{
    public static void Execute()
    {
        string path = "Assets/Barriers pack Demo/Barriers pack Demo/Fbx/";
        var names = System.IO.Directory.GetFiles(path, "*.fbx");
        var results = new List<(string file, Vector3 size, float height)>();

        foreach (var n in names)
        {
            string relative = n.Replace('\\', '/');
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(relative);
            if (go == null) continue;

            var temp = Object.Instantiate(go);
            temp.hideFlags = HideFlags.HideAndDontSave;
            Bounds? combined = null;
            foreach (var mf in temp.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var m = mf.transform.localToWorldMatrix;
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3(
                        (i & 1) == 0 ? -1 : 1,
                        (i & 2) == 0 ? -1 : 1,
                        (i & 4) == 0 ? -1 : 1));
                    Vector3 wp = m.MultiplyPoint3x4(c);
                    if (combined == null) combined = new Bounds(wp, Vector3.zero);
                    else { var b = combined.Value; b.Encapsulate(wp); combined = b; }
                }
            }
            Object.DestroyImmediate(temp);

            if (combined != null)
            {
                var b = combined.Value;
                string fname = System.IO.Path.GetFileName(n);
                results.Add((fname, b.size, b.size.y));
            }
        }

        // Sort by height
        results.Sort((a, b) => a.height.CompareTo(b.height));
        foreach (var r in results)
        {
            Debug.Log($"{r.file}  size=({r.size.x:F2},{r.size.y:F2},{r.size.z:F2})");
        }
    }
}
