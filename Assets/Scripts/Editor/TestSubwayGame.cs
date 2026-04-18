using UnityEditor;
using UnityEngine;

public static class TestSubwayGame
{
    public static void Execute()
    {
        // Remove any previous test root
        var existing = GameObject.Find("__SubwayTestRoot");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("__SubwayTestRoot");
        root.transform.position = new Vector3(500f, 0f, 500f); // offset from main scene

        // Create a camera
        var camObj = new GameObject("__SubwayTestCam");
        camObj.transform.SetParent(root.transform, false);
        var cam = camObj.AddComponent<Camera>();
        cam.tag = "Untagged";

        var game = root.AddComponent<SubwayGame>();
        game.Init(root, cam);

        // Spawn a few rows forward so we can see obstacles
        var so = new SerializedObject(game);
        // Spawn several rows manually using reflection of private methods isn't easy;
        // Instead we just ensure SpawnRow runs a few times via invoking Update-like state
        // Use reflection to invoke SpawnRow
        var m = typeof(SubwayGame).GetMethod("SpawnRow",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (m != null)
        {
            for (int i = 0; i < 4; i++)
                m.Invoke(game, null);

            // Offset spawned obstacles/coins along Z so they're visible across the stretch
            int idx = 0;
            foreach (Transform t in root.transform)
            {
                if (t.name.StartsWith("Obs_") || t.name == "Obs" || t.name == "Coin")
                {
                    var p = t.localPosition;
                    p.z = 5f + (idx % 12) * 3f;
                    t.localPosition = p;
                    idx++;
                }
            }
        }

        // Position scene view camera behind player
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.pivot = root.transform.position + new Vector3(0f, 2f, 10f);
            sv.rotation = Quaternion.Euler(15f, 0f, 0f);
            sv.size = 12f;
            sv.Repaint();
        }

        Debug.Log("[TestSubwayGame] Subway game scene built at " + root.transform.position);
    }

    public static void Cleanup()
    {
        var existing = GameObject.Find("__SubwayTestRoot");
        if (existing != null) Object.DestroyImmediate(existing);
        Debug.Log("[TestSubwayGame] Cleaned up.");
    }
}
