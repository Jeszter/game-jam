#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Applies all fixes: ceiling collider, bath mesh collider,
/// lowered jump height. Safe to run multiple times.
/// </summary>
public static class FixHouseAndPlayer
{
    public static void Execute()
    {
        // --- Player: lower jump height, fix camera height ---
        var player = GameObject.Find("player");
        if (player != null)
        {
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.cameraHeight = 1.75f;
                pm.jumpHeight   = 0.6f;
                pm.walkSpeed    = Mathf.Max(pm.walkSpeed, 2.5f);
                pm.runSpeed     = Mathf.Max(pm.runSpeed, 5f);
                EditorUtility.SetDirty(pm);
            }
        }

        // --- House_2: Ceiling_Collider ---
        var houseRoot = GameObject.Find("House/House_2");
        if (houseRoot != null)
        {
            Transform existing = houseRoot.transform.Find("Ceiling_Collider");
            GameObject ceiling = existing != null ? existing.gameObject : new GameObject("Ceiling_Collider");
            if (existing == null) ceiling.transform.SetParent(houseRoot.transform, false);

            // world bounds from ground renderer
            var groundGo = GameObject.Find("House/House_2/ground");
            Bounds gb = default;
            if (groundGo != null)
            {
                var mr = groundGo.GetComponent<MeshRenderer>();
                if (mr != null) gb = mr.bounds;
            }

            // Низ стелі трохи ВИЩЕ росту гравця (1.8м) + маленький запас =
            // ground y=192.11 + 1.95 = 194.06, щоб при присіданні (1.0м) пролазило,
            // а стрибнути (jumpHeight=0.6 → максимум головою 194.71) не можна досягти 194.9.
            // Отже 194.9 нас влаштовує - гравець зі звичайним ростом 1.8м вміщається (192.11+1.8=193.91 < 194.9).
            float ceilingBottom = 194.9f;
            float thickness     = 3f;
            Vector3 ceilingCenterWorld;
            Vector3 ceilingSize;
            if (gb.size.sqrMagnitude > 0.001f)
            {
                ceilingCenterWorld = new Vector3(gb.center.x, ceilingBottom + thickness * 0.5f, gb.center.z);
                ceilingSize        = new Vector3(gb.size.x + 4f, thickness, gb.size.z + 4f);
            }
            else
            {
                ceilingCenterWorld = new Vector3(791.9f, ceilingBottom + thickness * 0.5f, -4.25f);
                ceilingSize        = new Vector3(19f, thickness, 19f);
            }

            ceiling.transform.position   = ceilingCenterWorld;
            ceiling.transform.rotation   = Quaternion.identity;
            ceiling.transform.localScale = Vector3.one;

            var box = ceiling.GetComponent<BoxCollider>();
            if (box == null) box = ceiling.AddComponent<BoxCollider>();
            box.center    = Vector3.zero;
            box.size      = ceilingSize;
            box.isTrigger = false;

            var mrC = ceiling.GetComponent<MeshRenderer>();
            if (mrC != null) Object.DestroyImmediate(mrC);
            var mfC = ceiling.GetComponent<MeshFilter>();
            if (mfC != null) Object.DestroyImmediate(mfC);

            EditorUtility.SetDirty(ceiling);
        }

        // --- Bath: replace BoxCollider with MeshCollider ---
        var bath = GameObject.Find("House/Bath");
        if (bath != null)
        {
            foreach (var bc in bath.GetComponents<BoxCollider>())
                Object.DestroyImmediate(bc);
            var mc = bath.GetComponent<MeshCollider>();
            if (mc == null) mc = bath.AddComponent<MeshCollider>();
            mc.convex   = false;
            mc.isTrigger = false;
            EditorUtility.SetDirty(bath);
        }

        // --- Save ---
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[FixHouseAndPlayer] Applied: ceiling, bath mesh collider, jumpHeight=0.6. Scene saved.");
    }
}
#endif
