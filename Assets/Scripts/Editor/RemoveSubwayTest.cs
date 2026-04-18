using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RemoveSubwayTest
{
    public static void Execute()
    {
        var active = EditorSceneManager.GetActiveScene();
        var roots = active.GetRootGameObjects();
        int removed = 0;
        foreach (var go in roots)
        {
            if (go.name.StartsWith("__Subway") || go.name.StartsWith("__SubwayTestRoot"))
            {
                Debug.Log($"[RemoveSubwayTest] Destroying {go.name}");
                Object.DestroyImmediate(go);
                removed++;
            }
        }
        if (removed == 0)
        {
            Debug.Log("[RemoveSubwayTest] No __Subway* roots found");
        }
        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);
        Debug.Log($"[RemoveSubwayTest] Removed {removed} test root(s), saved scene.");
    }
}
