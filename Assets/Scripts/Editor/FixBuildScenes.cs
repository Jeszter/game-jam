using UnityEngine;
using UnityEditor;

public static class FixBuildScenes
{
    [MenuItem("Tools/Fix Build Scenes (use Game1 2)")]
    public static void Execute()
    {
        string target = "Assets/Scenes/Game1 2.unity";
        var guid = AssetDatabase.AssetPathToGUID(target);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"Scene not found: {target}");
            return;
        }

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        list.Add(new EditorBuildSettingsScene(target, true));

        // Keep other existing scenes after, but disabled
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == target) continue;
            list.Add(new EditorBuildSettingsScene(s.path, false));
        }

        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"[FixBuildScenes] Set active scene to {target}. Build list:");
        foreach (var s in EditorBuildSettings.scenes)
            Debug.Log($"  {(s.enabled ? "[x]" : "[ ]")} {s.path}");
    }
}
