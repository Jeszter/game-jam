using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class ApplyIconSprites
{
    static string iconPath = "Assets/_UI_COPLAY_GENERATED/MainMenu/Textures/Icons/";

    public static void Execute()
    {
        string[] iconFiles = new string[]
        {
            "icon_play", "icon_gear", "icon_cross", "icon_heart", "icon_comment",
            "icon_share", "icon_home", "icon_search", "icon_plus", "icon_grid",
            "icon_profile", "icon_settings_corner", "icon_exit_corner"
        };

        foreach (string name in iconFiles)
        {
            string path = iconPath + name + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.maxTextureSize = 128;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        AssetDatabase.Refresh();

        // === BUTTON ICONS: Replace TMP text with Image + Text layout ===
        ReplaceButtonWithIcon("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/StartButton", "icon_play", "START", Color.white);
        ReplaceButtonWithIcon("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/SettingsButton", "icon_gear", "SETTINGS", Color.white);
        ReplaceButtonWithIcon("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/ExitButton", "icon_cross", "EXIT", Color.white);

        // === SOCIAL ICONS: Replace TMP text icons with Image ===
        ReplaceSocialIcon("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/HeartIcon/Icon", "icon_heart");
        ReplaceSocialIcon("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/CommentIcon/Icon", "icon_comment");
        ReplaceSocialIcon("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/ShareIcon/Icon", "icon_share");

        // === NAV BAR ICONS: Replace TMP text with Image ===
        ReplaceNavIcon("MainMenuCanvas/PhonePanel/NavBar/NavHome/Icon", "icon_home");
        ReplaceNavIcon("MainMenuCanvas/PhonePanel/NavBar/NavSearch/Icon", "icon_search");
        ReplaceNavIcon("MainMenuCanvas/PhonePanel/NavBar/NavPlus/Icon", "icon_plus");
        ReplaceNavIcon("MainMenuCanvas/PhonePanel/NavBar/NavGrid/Icon", "icon_grid");
        ReplaceNavIcon("MainMenuCanvas/PhonePanel/NavBar/NavProfile/Icon", "icon_profile");

        // === CORNER UI: Add icon images before text ===
        AddCornerIcon("MainMenuCanvas/CornerUI/SettingsCorner", "icon_settings_corner", "SETTINGS");
        AddCornerIcon("MainMenuCanvas/CornerUI/ExitCorner", "icon_exit_corner", "EXIT");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("All icon sprites applied successfully!");
    }

    static Sprite LoadIcon(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + name + ".png");
    }

    static void ReplaceButtonWithIcon(string btnPath, string iconName, string text, Color textColor)
    {
        GameObject btnGo = GameObject.Find(btnPath);
        if (btnGo == null) return;

        Transform labelT = btnGo.transform.Find("Label");
        if (labelT != null)
        {
            TMP_Text tmp = labelT.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = "     " + text;
                tmp.richText = false;
                tmp.color = textColor;
                tmp.fontSize = 22;
            }
        }

        Transform existingIcon = btnGo.transform.Find("IconImage");
        if (existingIcon != null)
            Object.DestroyImmediate(existingIcon.gameObject);

        GameObject iconGo = new GameObject("IconImage", typeof(RectTransform));
        iconGo.transform.SetParent(btnGo.transform, false);
        iconGo.transform.SetAsFirstSibling();
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.sizeDelta = new Vector2(28, 28);
        iconRect.anchoredPosition = new Vector2(15, 0);

        Image iconImg = iconGo.AddComponent<Image>();
        Sprite sprite = LoadIcon(iconName);
        if (sprite != null)
        {
            iconImg.sprite = sprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        EditorUtility.SetDirty(btnGo);
    }

    static void ReplaceSocialIcon(string path, string iconName)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;

        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp != null)
            Object.DestroyImmediate(tmp);

        // Remove TMP SubMeshUI children
        for (int i = go.transform.childCount - 1; i >= 0; i--)
        {
            var child = go.transform.GetChild(i);
            if (child.name.Contains("TMP SubMeshUI"))
                Object.DestroyImmediate(child.gameObject);
        }

        Image img = go.GetComponent<Image>();
        if (img == null)
            img = go.AddComponent<Image>();

        Sprite sprite = LoadIcon(iconName);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null)
            le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 28;
        le.preferredWidth = 28;

        EditorUtility.SetDirty(go);
    }

    static void ReplaceNavIcon(string path, string iconName)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;

        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp != null)
            Object.DestroyImmediate(tmp);

        for (int i = go.transform.childCount - 1; i >= 0; i--)
        {
            var child = go.transform.GetChild(i);
            if (child.name.Contains("TMP SubMeshUI"))
                Object.DestroyImmediate(child.gameObject);
        }

        Image img = go.GetComponent<Image>();
        if (img == null)
            img = go.AddComponent<Image>();

        Sprite sprite = LoadIcon(iconName);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.15f, 0.1f);
        rt.anchorMax = new Vector2(0.85f, 0.9f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        EditorUtility.SetDirty(go);
    }

    static void AddCornerIcon(string textPath, string iconName, string label)
    {
        GameObject textGo = GameObject.Find(textPath);
        if (textGo == null) return;

        TMP_Text tmp = textGo.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = "  " + label;
            tmp.fontSize = 14;
        }

        Transform parent = textGo.transform.parent;
        Transform existingIcon = parent.Find(textGo.name + "_Icon");
        if (existingIcon != null)
            Object.DestroyImmediate(existingIcon.gameObject);

        GameObject iconGo = new GameObject(textGo.name + "_Icon", typeof(RectTransform));
        iconGo.transform.SetParent(parent, false);

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = textRect.anchorMin;
        iconRect.anchorMax = textRect.anchorMax;
        iconRect.pivot = textRect.pivot;
        iconRect.anchoredPosition = textRect.anchoredPosition + new Vector2(-10, 15);
        iconRect.sizeDelta = new Vector2(24, 24);

        Image iconImg = iconGo.AddComponent<Image>();
        Sprite sprite = LoadIcon(iconName);
        if (sprite != null)
        {
            iconImg.sprite = sprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = new Color(0.6f, 0.6f, 0.65f, 1f);
        }

        EditorUtility.SetDirty(parent.gameObject);
    }
}
