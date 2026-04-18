using UnityEditor;
using UnityEngine;

public static class FrameSubwayTest
{
    public static void Execute()
    {
        var root = GameObject.Find("__SubwayTestRoot");
        if (root == null)
        {
            Debug.LogError("No __SubwayTestRoot — run TestSubwayGame.Execute first.");
            return;
        }

        var sv = SceneView.lastActiveSceneView;
        if (sv == null)
        {
            EditorApplication.ExecuteMenuItem("Window/General/Scene");
            sv = SceneView.lastActiveSceneView;
        }
        if (sv == null) return;

        sv.Focus();

        // Third-person chase cam, behind and above player (root at 500,0,500), looking forward down +Z
        sv.pivot = root.transform.position + new Vector3(0f, 3f, 20f);
        sv.rotation = Quaternion.Euler(22f, 0f, 0f);
        sv.size = 25f;
        sv.orthographic = false;
        sv.Repaint();

        // Select an obstacle so capture can frame the whole thing
        var all = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in all)
        {
            if (t.name.StartsWith("Obs_"))
            {
                Selection.activeGameObject = t.gameObject;
                break;
            }
        }

        Debug.Log($"Framed subway test at {root.transform.position}");
    }
}
