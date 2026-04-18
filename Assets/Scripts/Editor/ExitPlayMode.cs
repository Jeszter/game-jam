using UnityEditor;
using UnityEngine;

public static class ExitPlayMode
{
    public static void Execute()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            Debug.Log("[ExitPlayMode] Exiting play mode.");
        }
        else
        {
            Debug.Log("[ExitPlayMode] Not in play mode.");
        }
    }
}
