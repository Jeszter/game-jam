using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class ApplyGameIcons
{
    [MenuItem("Tools/Apply Game Icons Now")]
    public static void Execute()
    {
        // Icon paths
        string[] iconPaths = {
            "Assets/knife_hit.png",
            "Assets/subway_surf.png",
            "Assets/police_chase.png",
            "Assets/casino.png"
        };

        // Step 1: Fix texture import settings to Sprite
        bool reimported = false;
        foreach (var path in iconPaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[ApplyGameIcons] Not found: " + path);
                continue;
            }
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                reimported = true;
                Debug.Log("[ApplyGameIcons] Set Sprite type: " + path);
            }
        }

        if (reimported)
            AssetDatabase.Refresh();

        // Step 2: Load sprites
        Sprite knifeHit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/knife_hit.png");
        Sprite subwaySurf = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/subway_surf.png");
        Sprite policeChase = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/police_chase.png");
        Sprite casino = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/casino.png");

        Debug.Log($"[ApplyGameIcons] Loaded: knife={knifeHit != null}, subway={subwaySurf != null}, police={policeChase != null}, casino={casino != null}");

        // Step 3: Assign to LaptopGames component
        var laptops = Object.FindObjectsByType<LaptopGames>(FindObjectsSortMode.None);
        foreach (var laptop in laptops)
        {
            Undo.RecordObject(laptop, "Assign Game Icons");
            if (subwaySurf != null) laptop.iconSubwaySurf = subwaySurf;
            if (policeChase != null) laptop.iconPoliceChase = policeChase;
            if (casino != null) laptop.iconCasino = casino;
            EditorUtility.SetDirty(laptop);
            Debug.Log("[ApplyGameIcons] Assigned sprites to LaptopGames on: " + laptop.gameObject.name);
        }

        // Step 4: Replace phone KnifeAppBtn icon
        if (knifeHit != null)
        {
            var knifeBtn = GameObject.Find("GameUICanvas/PhonePanel/PhoneScreen/HomeScreen/AppGrid/KnifeAppBtn");
            if (knifeBtn != null)
            {
                var iconT = knifeBtn.transform.Find("Icon");
                if (iconT != null)
                {
                    // Remove TMP_Text if present
                    var tmp = iconT.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        Undo.DestroyObjectImmediate(tmp);
                    }

                    // Add or update Image
                    var img = iconT.GetComponent<Image>();
                    if (img == null)
                        img = Undo.AddComponent<Image>(iconT.gameObject);

                    Undo.RecordObject(img, "Set Knife Icon");
                    img.sprite = knifeHit;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    img.color = Color.white;
                    EditorUtility.SetDirty(img);

                    var rt = iconT.GetComponent<RectTransform>();
                    Undo.RecordObject(rt, "Fix Knife Icon RT");
                    rt.anchorMin = new Vector2(0.05f, 0.05f);
                    rt.anchorMax = new Vector2(0.95f, 0.95f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    EditorUtility.SetDirty(rt);

                    Debug.Log("[ApplyGameIcons] Knife Hit icon applied to phone.");
                }
            }
        }

        // Step 5: Save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[ApplyGameIcons] Done! All icons applied and scene saved.");
    }
}
