using UnityEngine;
using UnityEditor;

public class FindCanvases
{
    public static void Execute()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            Debug.Log("Canvas: " + c.name + ", RenderMode: " + c.renderMode + ", SortOrder: " + c.sortingOrder);
        }
    }
}