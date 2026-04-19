using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Залишає в Build Settings ТІЛЬКИ активну сцену. Прибирає всі інші записи
/// (SampleScene, Scenes/Game1 2 тощо).
/// </summary>
public static class CleanBuildSceneList
{
    [MenuItem("Tools/Build/Keep Only Active Scene In Build")]
    public static void Execute()
    {
        var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path))
        {
            Debug.LogError("[CleanBuildSceneList] Активна сцена не збережена!");
            return;
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(active.path, true)
        };
        AssetDatabase.SaveAssets();

        Debug.Log("[CleanBuildSceneList] Build scene list тепер:");
        foreach (var s in EditorBuildSettings.scenes)
            Debug.Log($"  {(s.enabled ? "[X]" : "[ ]")} {s.path}");
    }
}
