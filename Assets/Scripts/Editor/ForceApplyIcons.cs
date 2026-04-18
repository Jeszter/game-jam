using UnityEngine;
using UnityEditor;

public class ForceApplyIcons
{
    [MenuItem("Tools/Force Apply Game Icons")]
    public static void Execute()
    {
        // Ensure sprites are imported correctly
        string[] paths = {
            "Assets/knife_hit.png",
            "Assets/subway_surf.png",
            "Assets/police_chase.png",
            "Assets/casino.png"
        };
        foreach (var p in paths)
        {
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
        }

        Sprite subwaySurf = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/subway_surf.png");
        Sprite policeChase = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/police_chase.png");
        Sprite casino = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/casino.png");

        Debug.Log($"[ForceApply] subway={subwaySurf != null}, police={policeChase != null}, casino={casino != null}");

        // Find ALL LaptopGames including inactive
        var allLaptops = Resources.FindObjectsOfTypeAll<LaptopGames>();
        Debug.Log($"[ForceApply] Found {allLaptops.Length} LaptopGames components (including inactive)");

        foreach (var laptop in allLaptops)
        {
            // Skip prefab assets, only modify scene objects
            if (EditorUtility.IsPersistent(laptop)) continue;

            Undo.RecordObject(laptop, "Force Apply Icons");
            if (subwaySurf != null) laptop.iconSubwaySurf = subwaySurf;
            if (policeChase != null) laptop.iconPoliceChase = policeChase;
            if (casino != null) laptop.iconCasino = casino;
            EditorUtility.SetDirty(laptop);
            Debug.Log("[ForceApply] Assigned to: " + laptop.gameObject.name + " (active=" + laptop.gameObject.activeInHierarchy + ")");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[ForceApply] Done!");
    }
}
