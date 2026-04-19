using UnityEditor;
using UnityEngine;

public static class RunSync
{
    public static void Execute()
    {
        CopyResourcesForBuild.Sync();

        // Друкуємо звіт: що реально лежить в Resources/
        Debug.Log("=== RESOURCES REPORT ===");
        DumpFolder("Assets/Resources/Sound");
        DumpFolder("Assets/Resources/GameIcons");
        DumpFolder("Assets/Resources/SlotSprites");
        DumpFolder("Assets/Resources/SubwaySurf/Barriers");
        DumpFolder("Assets/Resources/PoliceChase/Cars");
        DumpFolder("Assets/Resources/PoliceChase/Textures");
    }

    static void DumpFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.Log($"[{folder}] MISSING");
            return;
        }
        var guids = AssetDatabase.FindAssets("", new[] { folder });
        Debug.Log($"[{folder}] {guids.Length} assets:");
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (!AssetDatabase.IsValidFolder(p))
                Debug.Log("  - " + p);
        }
    }
}
