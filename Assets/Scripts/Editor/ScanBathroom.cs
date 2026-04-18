#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ScanBathroom
{
    public static void Execute()
    {
        Debug.Log("=== BATHROOM ITEMS ===");
        LogObj("House/Bath");
        LogObj("House/Sink");
        LogObj("House/сельський+толчок");
        LogObj("сельський+толчок");
        LogObj("House/Towel");
        LogObj("House/Towel (1)");

        Debug.Log("=== PLAYER + STAND UP POINT ===");
        LogObj("player");
        LogObj("StandUpPoint");

        Debug.Log("=== ALL WALLS world bounds ===");
        var walls = GameObject.Find("House/House_2");
        if (walls != null)
        {
            foreach (Transform child in walls.transform)
            {
                if (!child.name.StartsWith("wall")) continue;
                foreach (var col in child.GetComponentsInChildren<Collider>(true))
                {
                    var b = col.bounds;
                    Debug.Log($"[wall] path={GetPath(col.transform)} enabled={col.enabled} min={b.min} max={b.max}");
                }
            }
        }
    }

    static void LogObj(string path)
    {
        var go = GameObject.Find(path);
        if (go == null) { Debug.Log($"  [MISSING] {path}"); return; }
        var r = go.GetComponent<Renderer>();
        if (r != null)
            Debug.Log($"  [{path}] pos={go.transform.position} rendererBounds min={r.bounds.min} max={r.bounds.max}");
        else
            Debug.Log($"  [{path}] pos={go.transform.position}");
    }

    static string GetPath(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
#endif
