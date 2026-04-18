using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CheckAndSaveScene
{
    public static void Execute()
    {
        var active = EditorSceneManager.GetActiveScene();
        Debug.Log($"[CheckAndSaveScene] Active scene: '{active.name}' path='{active.path}' isDirty={active.isDirty}");

        // Open the canonical scene
        string target = "Assets/Scenes/Game1 2.unity";
        if (active.path != target)
        {
            Debug.Log($"[CheckAndSaveScene] Switching to {target}");
            EditorSceneManager.OpenScene(target, OpenSceneMode.Single);
            active = EditorSceneManager.GetActiveScene();
        }

        // Verify PlayerSmoking on player
        var player = GameObject.Find("player");
        if (player != null)
        {
            var ps = player.GetComponent<PlayerSmoking>();
            if (ps == null)
            {
                player.AddComponent<PlayerSmoking>();
                Debug.Log("[CheckAndSaveScene] Added missing PlayerSmoking to player");
            }
            else
            {
                Debug.Log("[CheckAndSaveScene] PlayerSmoking present on player");
            }
        }
        else
        {
            Debug.LogError("[CheckAndSaveScene] No 'player' in active scene!");
        }

        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CheckAndSaveScene] Saved scene {active.path}");

        // Also re-apply build settings
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        list.Add(new EditorBuildSettingsScene(target, true));
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == target) continue;
            list.Add(new EditorBuildSettingsScene(s.path, false));
        }
        EditorBuildSettings.scenes = list.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("[CheckAndSaveScene] Build settings updated: Game1 2 is first & enabled.");
    }
}
