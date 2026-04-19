using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Налаштовує Build Settings: основна (активна) сцена йде першою.
/// </summary>
public static class FixBuildScenesNow
{
    [MenuItem("Tools/Build/Fix Build Scenes (use Active Scene)")]
    public static void Execute()
    {
        var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path))
        {
            Debug.LogError("[FixBuildScenes] Активна сцена не збережена на диск!");
            return;
        }

        Debug.Log("[FixBuildScenes] Active scene: " + active.path);

        // Збираємо унікальні сцени у правильному порядку.
        var list = new List<EditorBuildSettingsScene>();
        list.Add(new EditorBuildSettingsScene(active.path, true));

        // Додаємо решту існуючих сцен (вимкнених за замовчуванням), окрім дублікатів.
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == active.path) continue;
            if (string.IsNullOrEmpty(s.path)) continue;
            list.Add(new EditorBuildSettingsScene(s.path, false));
        }

        EditorBuildSettings.scenes = list.ToArray();
        AssetDatabase.SaveAssets();

        Debug.Log("[FixBuildScenes] Build scene list:");
        foreach (var s in EditorBuildSettings.scenes)
            Debug.Log($"  {(s.enabled ? "[X]" : "[ ]")} {s.path}");
    }
}
