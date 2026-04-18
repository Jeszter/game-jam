using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AddPauseMenu
{
    public static void Execute()
    {
        // Найти или создать объект PauseMenu в активной сцене
        var existing = GameObject.Find("PauseMenu");
        if (existing != null)
        {
            var existingPmc = existing.GetComponent<PauseMenuController>();
            if (existingPmc == null)
                existing.AddComponent<PauseMenuController>();
            Debug.Log("[AddPauseMenu] PauseMenu уже существует.");
        }
        else
        {
            var go = new GameObject("PauseMenu");
            go.AddComponent<PauseMenuController>();
            Debug.Log("[AddPauseMenu] PauseMenu создан.");
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AddPauseMenu] Сцена сохранена.");
    }
}
