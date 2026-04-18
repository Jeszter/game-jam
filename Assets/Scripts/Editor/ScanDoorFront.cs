#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ScanDoorFront
{
    public static void Execute()
    {
        // Двері на z=-7.29. Область перед дверима з боку спальні z=-7.0..-6.5
        Vector3 center = new Vector3(788.36f, 193f, -6.5f);
        Vector3 halfExt = new Vector3(1f, 1f, 1f);

        var hits = Physics.OverlapBox(center, halfExt, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        Debug.Log($"[ScanDoorFront] center={center} halfExt={halfExt} hits={hits.Length}");
        foreach (var c in hits)
        {
            Debug.Log($"  HIT: {GetPath(c.transform)}  type={c.GetType().Name}  bounds={c.bounds}");
        }

        // Також перевіримо чи може гравець (capsule r=0.5 h=2) стояти перед дверима
        Vector3 standPos = new Vector3(788.36f, 193.0f, -6.6f);
        bool blocked = Physics.CheckCapsule(standPos + Vector3.up * 0.5f, standPos + Vector3.up * 1.5f, 0.45f, ~0, QueryTriggerInteraction.Ignore);
        Debug.Log($"[ScanDoorFront] player blocked at {standPos}: {blocked}");

        // А може там bath_carpet чи щось таке блокує?
        Debug.Log("=== floor items ===");
        string[] floorItems = { "House/Bath_Carpet", "House/carpet1", "House/carpet2", "House/carpet_002" };
        foreach (var n in floorItems)
        {
            var go = GameObject.Find(n);
            if (go == null) continue;
            var cols = go.GetComponentsInChildren<Collider>(true);
            foreach (var col in cols)
                Debug.Log($"  {n} collider: {col.GetType().Name} bounds={col.bounds} enabled={col.enabled}");
        }
    }

    static string GetPath(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
#endif
