#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// wall_026 - це стіна душової кабіни що блокує прохід дверей у ванну кімнату.
/// Вимикаємо її колайдер (меш залишається видимим - візуально нічого не зміниться).
/// </summary>
public static class OpenBathroom
{
    public static void Execute()
    {
        string[] toDisable = {
            "House/House_2/wall_026",   // блокує прохід дверей у ванну кімнату
        };

        int disabled = 0;
        foreach (var path in toDisable)
        {
            var go = GameObject.Find(path);
            if (go == null) { Debug.LogWarning($"[OpenBathroom] not found: {path}"); continue; }
            foreach (var col in go.GetComponents<Collider>())
            {
                if (col.enabled)
                {
                    col.enabled = false;
                    EditorUtility.SetDirty(col);
                    disabled++;
                    Debug.Log($"[OpenBathroom] disabled collider on {path}");
                }
            }
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[OpenBathroom] done - disabled {disabled} colliders. Scene saved.");
    }
}
#endif
