using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AddPlayerSmoking
{
    [MenuItem("Tools/Add PlayerSmoking to player")]
    public static void Execute()
    {
        var player = GameObject.Find("player");
        if (player == null)
        {
            Debug.LogError("No 'player' GameObject found in scene");
            return;
        }
        if (player.GetComponent<PlayerSmoking>() == null)
        {
            player.AddComponent<PlayerSmoking>();
            Debug.Log("Added PlayerSmoking to player");
        }
        else
        {
            Debug.Log("PlayerSmoking already present on player");
        }
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
    }
}
