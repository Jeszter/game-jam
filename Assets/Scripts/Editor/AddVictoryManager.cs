using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AddVictoryManager
{
    public static void Execute()
    {
        var existing = GameObject.Find("VictoryManager");
        if (existing != null)
        {
            if (existing.GetComponent<VictoryManager>() == null)
                existing.AddComponent<VictoryManager>();
            Debug.Log("[AddVictoryManager] VictoryManager уже существует — проверил компонент.");
        }
        else
        {
            var go = new GameObject("VictoryManager");
            go.AddComponent<VictoryManager>();
            Debug.Log("[AddVictoryManager] VictoryManager создан.");
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AddVictoryManager] Сцена сохранена.");
    }
}
