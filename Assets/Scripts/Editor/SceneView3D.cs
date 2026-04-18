using UnityEngine;
using UnityEditor;

public class SceneView3D
{
    public static void Execute()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) sv = SceneView.currentDrawingSceneView;
        if (sv == null && SceneView.sceneViews.Count > 0) sv = SceneView.sceneViews[0] as SceneView;
        if (sv == null) { Debug.LogError("No SceneView"); return; }

        sv.in2DMode = false;
        sv.orthographic = false;
        sv.FrameSelected();
        sv.Repaint();
        Debug.Log("[SceneView3D] Toggled to 3D mode");
    }
}
