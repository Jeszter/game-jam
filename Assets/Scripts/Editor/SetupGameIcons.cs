using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor window to assign game icon sprites to phone app buttons and laptop game menu.
/// Open via Tools > Setup Game Icons.
/// 
/// PHONE ICONS: Replaces the emoji TMP_Text "Icon" child with an Image showing your sprite.
/// LAPTOP ICONS: Assigns sprites to the LaptopGames component fields.
/// </summary>
public class SetupGameIcons : EditorWindow
{
    // Phone app icons
    private Sprite knifeHitIcon;
    private Sprite tikTokIcon;
    private Sprite shopIcon;
    private Sprite doomifyIcon;

    // Laptop game icons
    private Sprite subwaySurfIcon;
    private Sprite policeChaseIcon;
    private Sprite casinoIcon;

    private Vector2 scrollPos;

    [MenuItem("Tools/Setup Game Icons")]
    public static void ShowWindow()
    {
        var win = GetWindow<SetupGameIcons>("Game Icons");
        win.minSize = new Vector2(350, 500);
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("📱  PHONE APP ICONS", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drag your icon sprites here. Click 'Apply Phone Icons' to replace the emoji icons on the phone home screen with images.",
            MessageType.Info);
        GUILayout.Space(5);

        knifeHitIcon = (Sprite)EditorGUILayout.ObjectField("Knife Hit", knifeHitIcon, typeof(Sprite), false);
        tikTokIcon = (Sprite)EditorGUILayout.ObjectField("TikTok", tikTokIcon, typeof(Sprite), false);
        shopIcon = (Sprite)EditorGUILayout.ObjectField("Shop", shopIcon, typeof(Sprite), false);
        doomifyIcon = (Sprite)EditorGUILayout.ObjectField("Doomify", doomifyIcon, typeof(Sprite), false);

        GUILayout.Space(5);
        if (GUILayout.Button("Apply Phone Icons", GUILayout.Height(30)))
            ApplyPhoneIcons();

        GUILayout.Space(20);
        EditorGUILayout.LabelField("💻  LAPTOP GAME ICONS", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drag your icon sprites here. Click 'Apply Laptop Icons' to assign them to the LaptopGames component on the Desk/Laptop object.",
            MessageType.Info);
        GUILayout.Space(5);

        subwaySurfIcon = (Sprite)EditorGUILayout.ObjectField("Subway Surf", subwaySurfIcon, typeof(Sprite), false);
        policeChaseIcon = (Sprite)EditorGUILayout.ObjectField("Police Chase", policeChaseIcon, typeof(Sprite), false);
        casinoIcon = (Sprite)EditorGUILayout.ObjectField("Casino", casinoIcon, typeof(Sprite), false);

        GUILayout.Space(5);
        if (GUILayout.Button("Apply Laptop Icons", GUILayout.Height(30)))
            ApplyLaptopIcons();

        GUILayout.Space(20);
        if (GUILayout.Button("Apply ALL Icons", GUILayout.Height(35)))
        {
            ApplyPhoneIcons();
            ApplyLaptopIcons();
        }

        EditorGUILayout.EndScrollView();
    }

    void ApplyPhoneIcons()
    {
        var appGrid = GameObject.Find("GameUICanvas/PhonePanel/PhoneScreen/HomeScreen/AppGrid");
        if (appGrid == null)
        {
            Debug.LogError("[SetupGameIcons] AppGrid not found! Make sure the phone UI exists in the scene.");
            return;
        }

        int count = 0;
        if (knifeHitIcon != null) count += ReplacePhoneAppIcon(appGrid.transform, "KnifeAppBtn", knifeHitIcon);
        if (tikTokIcon != null) count += ReplacePhoneAppIcon(appGrid.transform, "TikTokAppBtn", tikTokIcon);
        if (shopIcon != null) count += ReplacePhoneAppIcon(appGrid.transform, "ShopAppBtn", shopIcon);
        if (doomifyIcon != null) count += ReplacePhoneAppIcon(appGrid.transform, "DoomifyAppBtn", doomifyIcon);

        if (count > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
        Debug.Log($"[SetupGameIcons] Applied {count} phone icon(s).");
    }

    /// <summary>
    /// Replaces the TMP_Text "Icon" child of a phone app button with an Image showing the sprite.
    /// If the Icon child already has an Image component, just updates the sprite.
    /// </summary>
    int ReplacePhoneAppIcon(Transform appGrid, string buttonName, Sprite sprite)
    {
        var btn = appGrid.Find(buttonName);
        if (btn == null)
        {
            Debug.LogWarning($"[SetupGameIcons] Button '{buttonName}' not found in AppGrid.");
            return 0;
        }

        var iconTransform = btn.Find("Icon");
        if (iconTransform == null)
        {
            Debug.LogWarning($"[SetupGameIcons] 'Icon' child not found in '{buttonName}'.");
            return 0;
        }

        Undo.RecordObject(iconTransform.gameObject, "Replace Phone Icon");

        // Check if already has an Image component
        var existingImg = iconTransform.GetComponent<Image>();
        if (existingImg != null)
        {
            Undo.RecordObject(existingImg, "Update Phone Icon Sprite");
            existingImg.sprite = sprite;
            existingImg.preserveAspect = true;
            EditorUtility.SetDirty(existingImg);
            Debug.Log($"[SetupGameIcons] Updated existing Image on '{buttonName}/Icon'.");
            return 1;
        }

        // Remove the TMP_Text component and add Image instead
        var tmpText = iconTransform.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            Undo.DestroyObjectImmediate(tmpText);
        }

        // Also remove CanvasRenderer if it exists (Image will add its own)
        // Actually Image needs CanvasRenderer, so leave it

        var img = Undo.AddComponent<Image>(iconTransform.gameObject);
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = Color.white;

        // Keep the same RectTransform anchors (stretches within the button)
        var rt = iconTransform.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.1f);
        rt.anchorMax = new Vector2(0.9f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        EditorUtility.SetDirty(iconTransform.gameObject);
        Debug.Log($"[SetupGameIcons] Replaced emoji with sprite on '{buttonName}/Icon'.");
        return 1;
    }

    void ApplyLaptopIcons()
    {
        // Find all LaptopGames components in the scene
        var laptops = Object.FindObjectsByType<LaptopGames>(FindObjectsSortMode.None);
        if (laptops.Length == 0)
        {
            Debug.LogError("[SetupGameIcons] No LaptopGames component found in the scene!");
            return;
        }

        int count = 0;
        foreach (var laptop in laptops)
        {
            Undo.RecordObject(laptop, "Set Laptop Game Icons");

            if (subwaySurfIcon != null) { laptop.iconSubwaySurf = subwaySurfIcon; count++; }
            if (policeChaseIcon != null) { laptop.iconPoliceChase = policeChaseIcon; count++; }
            if (casinoIcon != null) { laptop.iconCasino = casinoIcon; count++; }

            EditorUtility.SetDirty(laptop);
        }

        if (count > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
        Debug.Log($"[SetupGameIcons] Applied {count} laptop icon(s) to {laptops.Length} LaptopGames component(s).");
    }
}
