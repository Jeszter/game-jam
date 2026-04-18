#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ScanDoorway
{
    public static void Execute()
    {
        // Прохід у wall_014: x=787.63..789.08, z=-7.34..-6.97
        // Перевіряємо всі колайдери в цьому об'ємі (з запасом по z)
        Vector3 center = new Vector3((787.63f + 789.08f) * 0.5f, 193f, -7.15f);
        Vector3 halfExt = new Vector3(0.7f, 2f, 0.5f);

        Debug.Log($"[ScanDoorway] querying at center={center} halfExt={halfExt}");
        var hits = Physics.OverlapBox(center, halfExt, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        foreach (var c in hits)
        {
            Debug.Log($"  HIT: {GetPath(c.transform)}  type={c.GetType().Name}  bounds={c.bounds}  enabled={c.enabled}  trigger={c.isTrigger}");
        }

        // також подивимось на проміжок між wall_007 секціями на x=790.27, z=-6.27..-4.76
        Vector3 center2 = new Vector3(790.27f, 193f, (-6.27f + -4.76f) * 0.5f);
        Vector3 halfExt2 = new Vector3(0.5f, 2f, 0.75f);
        Debug.Log($"[ScanDoorway-2] querying at center={center2} halfExt={halfExt2}");
        var hits2 = Physics.OverlapBox(center2, halfExt2, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        foreach (var c in hits2)
        {
            Debug.Log($"  HIT2: {GetPath(c.transform)}  type={c.GetType().Name}  bounds={c.bounds}  enabled={c.enabled}  trigger={c.isTrigger}");
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
