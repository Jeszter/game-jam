using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class BuildPhonePanel
{
    static string iconPath = "Assets/_UI_COPLAY_GENERATED/MainMenu/Textures/Icons/";
    static Color darkBg = new Color(0.08f, 0.08f, 0.1f, 1f);
    static Color textWhite = new Color(0.9f, 0.9f, 0.9f, 1f);
    static Color textGray = new Color(0.55f, 0.55f, 0.6f, 1f);
    static Color accentRed = new Color(0.85f, 0.15f, 0.15f, 1f);
    static Color transparent = new Color(0, 0, 0, 0);
    static Color navBarBg = new Color(0.06f, 0.06f, 0.08f, 1f);
    static Color statusBarBg = new Color(0.06f, 0.06f, 0.08f, 0.9f);
    static Color buttonHoverColor = new Color(0.15f, 0.15f, 0.18f, 1f);

    static TMP_FontAsset pixelFont;

    public static void Execute()
    {
        // Try to load a pixel-style font
        pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        // Find canvas
        GameObject canvas = GameObject.Find("MainMenuCanvas");
        if (canvas == null)
        {
            Debug.LogError("MainMenuCanvas not found!");
            return;
        }

        // Remove old PhonePanel if exists
        Transform oldPanel = canvas.transform.Find("PhonePanel");
        if (oldPanel != null)
            Object.DestroyImmediate(oldPanel.gameObject);

        // === CREATE PHONE PANEL (root container matching phone screen area) ===
        GameObject phonePanel = CreateUIObject("PhonePanel", canvas.transform);
        RectTransform phonePanelRT = phonePanel.GetComponent<RectTransform>();
        // Position to overlay on the Phone texture - match PhoneGlow roughly
        phonePanelRT.anchorMin = new Vector2(0.5f, 0.5f);
        phonePanelRT.anchorMax = new Vector2(0.5f, 0.5f);
        phonePanelRT.pivot = new Vector2(0.5f, 0.5f);
        phonePanelRT.sizeDelta = new Vector2(310, 560);
        phonePanelRT.anchoredPosition = new Vector2(80, -30);
        phonePanelRT.localRotation = Quaternion.Euler(0, 0, -5.4f); // slight tilt to match phone

        // Add CanvasGroup for flicker effect
        CanvasGroup phonePanelCG = phonePanel.AddComponent<CanvasGroup>();
        phonePanelCG.alpha = 1f;

        // Add Image as dark background for the phone screen
        Image phonePanelImg = phonePanel.AddComponent<Image>();
        phonePanelImg.color = darkBg;
        phonePanelImg.raycastTarget = true;

        // Add Mask to clip content within phone screen
        Mask mask = phonePanel.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // === STATUS BAR (top) ===
        GameObject statusBar = CreateUIObject("StatusBar", phonePanel.transform);
        RectTransform statusBarRT = statusBar.GetComponent<RectTransform>();
        statusBarRT.anchorMin = new Vector2(0, 1);
        statusBarRT.anchorMax = new Vector2(1, 1);
        statusBarRT.pivot = new Vector2(0.5f, 1);
        statusBarRT.sizeDelta = new Vector2(0, 30);
        statusBarRT.anchoredPosition = Vector2.zero;
        Image statusBarImg = statusBar.AddComponent<Image>();
        statusBarImg.color = statusBarBg;
        statusBarImg.raycastTarget = false;

        // Time text (left)
        GameObject timeText = CreateTMPText("TimeText", statusBar.transform, "02:47", 12, textWhite, TextAlignmentOptions.MidlineLeft);
        RectTransform timeRT = timeText.GetComponent<RectTransform>();
        timeRT.anchorMin = new Vector2(0, 0);
        timeRT.anchorMax = new Vector2(0.3f, 1);
        timeRT.offsetMin = new Vector2(12, 0);
        timeRT.offsetMax = Vector2.zero;

        // Signal/battery text (right)
        GameObject signalText = CreateTMPText("SignalText", statusBar.transform, "▐▐▐ ✦ ▮▮", 10, textGray, TextAlignmentOptions.MidlineRight);
        RectTransform signalRT = signalText.GetComponent<RectTransform>();
        signalRT.anchorMin = new Vector2(0.5f, 0);
        signalRT.anchorMax = new Vector2(1, 1);
        signalRT.offsetMin = Vector2.zero;
        signalRT.offsetMax = new Vector2(-12, 0);

        // === PHONE CONTENT (scrollable area between status bar and nav bar) ===
        GameObject phoneContent = CreateUIObject("PhoneContent", phonePanel.transform);
        RectTransform phoneContentRT = phoneContent.GetComponent<RectTransform>();
        phoneContentRT.anchorMin = new Vector2(0, 0);
        phoneContentRT.anchorMax = new Vector2(1, 1);
        phoneContentRT.offsetMin = new Vector2(0, 50); // above nav bar
        phoneContentRT.offsetMax = new Vector2(0, -30); // below status bar
        phoneContentRT.pivot = new Vector2(0.5f, 0.5f);

        // === BUTTONS CONTAINER ===
        GameObject buttonsContainer = CreateUIObject("ButtonsContainer", phoneContent.transform);
        RectTransform buttonsRT = buttonsContainer.GetComponent<RectTransform>();
        buttonsRT.anchorMin = new Vector2(0, 0.35f);
        buttonsRT.anchorMax = new Vector2(0.65f, 0.85f);
        buttonsRT.offsetMin = new Vector2(20, 0);
        buttonsRT.offsetMax = new Vector2(0, 0);

        VerticalLayoutGroup buttonsVLG = buttonsContainer.AddComponent<VerticalLayoutGroup>();
        buttonsVLG.spacing = 8;
        buttonsVLG.childAlignment = TextAnchor.UpperLeft;
        buttonsVLG.childControlWidth = true;
        buttonsVLG.childControlHeight = true;
        buttonsVLG.childForceExpandWidth = true;
        buttonsVLG.childForceExpandHeight = false;
        buttonsVLG.padding = new RectOffset(10, 10, 5, 5);

        // START button
        CreateMenuButton("StartButton", buttonsContainer.transform, "START", accentRed, 60);
        // SETTINGS button
        CreateMenuButton("SettingsButton", buttonsContainer.transform, "SETTINGS", textWhite, 50);
        // EXIT button
        CreateMenuButton("ExitButton", buttonsContainer.transform, "EXIT", textWhite, 50);

        // === SOCIAL BAR (right side) ===
        GameObject socialBar = CreateUIObject("SocialBar", phoneContent.transform);
        RectTransform socialBarRT = socialBar.GetComponent<RectTransform>();
        socialBarRT.anchorMin = new Vector2(0.78f, 0.1f);
        socialBarRT.anchorMax = new Vector2(1, 0.65f);
        socialBarRT.offsetMin = Vector2.zero;
        socialBarRT.offsetMax = new Vector2(-5, 0);

        VerticalLayoutGroup socialVLG = socialBar.AddComponent<VerticalLayoutGroup>();
        socialVLG.spacing = 12;
        socialVLG.childAlignment = TextAnchor.MiddleCenter;
        socialVLG.childControlWidth = false;
        socialVLG.childControlHeight = false;
        socialVLG.childForceExpandWidth = false;
        socialVLG.childForceExpandHeight = false;

        // Heart icon + count
        CreateSocialItem("HeartIcon", socialBar.transform, "12.3k", new Color(1f, 0.3f, 0.3f, 1f));
        // Comment icon + count
        CreateSocialItem("CommentIcon", socialBar.transform, "567", textGray);
        // Share icon + count
        CreateSocialItem("ShareIcon", socialBar.transform, "890", textGray);

        // === NAV BAR (bottom) ===
        GameObject navBar = CreateUIObject("NavBar", phonePanel.transform);
        RectTransform navBarRT = navBar.GetComponent<RectTransform>();
        navBarRT.anchorMin = new Vector2(0, 0);
        navBarRT.anchorMax = new Vector2(1, 0);
        navBarRT.pivot = new Vector2(0.5f, 0);
        navBarRT.sizeDelta = new Vector2(0, 50);
        navBarRT.anchoredPosition = Vector2.zero;
        Image navBarImg = navBar.AddComponent<Image>();
        navBarImg.color = navBarBg;
        navBarImg.raycastTarget = false;

        HorizontalLayoutGroup navHLG = navBar.AddComponent<HorizontalLayoutGroup>();
        navHLG.spacing = 0;
        navHLG.childAlignment = TextAnchor.MiddleCenter;
        navHLG.childControlWidth = true;
        navHLG.childControlHeight = true;
        navHLG.childForceExpandWidth = true;
        navHLG.childForceExpandHeight = true;
        navHLG.padding = new RectOffset(8, 8, 6, 6);

        // Nav icons
        CreateNavItem("NavHome", navBar.transform, "⌂");
        CreateNavItem("NavSearch", navBar.transform, "⌕");
        CreateNavItem("NavPlus", navBar.transform, "+");
        CreateNavItem("NavGrid", navBar.transform, "⊞");
        CreateNavItem("NavProfile", navBar.transform, "⊙");

        // === SET SIBLING ORDER so PhonePanel is after Phone but before FadeOverlay ===
        // Phone is index 2, so PhonePanel should be index 3
        Transform fadeOverlay = canvas.transform.Find("FadeOverlay");
        if (fadeOverlay != null)
        {
            phonePanel.transform.SetSiblingIndex(fadeOverlay.GetSiblingIndex());
        }

        // === WIRE UP SCRIPT REFERENCES ===
        // PhoneFlickerEffect
        PhoneFlickerEffect flicker = canvas.GetComponent<PhoneFlickerEffect>();
        if (flicker != null)
        {
            var flickerSO = new SerializedObject(flicker);
            flickerSO.FindProperty("phoneCanvasGroup").objectReferenceValue = phonePanelCG;
            flickerSO.ApplyModifiedProperties();
        }

        // IdleAutoScroll
        IdleAutoScroll idleScroll = canvas.GetComponent<IdleAutoScroll>();
        if (idleScroll != null)
        {
            var idleSO = new SerializedObject(idleScroll);
            idleSO.FindProperty("phoneContent").objectReferenceValue = phoneContentRT;
            idleSO.ApplyModifiedProperties();
        }

        // MenuTransition
        MenuTransition menuTrans = canvas.GetComponent<MenuTransition>();
        if (menuTrans != null)
        {
            var menuSO = new SerializedObject(menuTrans);
            menuSO.FindProperty("phonePanel").objectReferenceValue = phonePanelRT;
            menuSO.ApplyModifiedProperties();
        }

        // === WIRE UP BUTTON EVENTS ===
        WireButton("StartButton", buttonsContainer.transform, canvas, "OnStartPressed");
        WireButton("SettingsButton", buttonsContainer.transform, canvas, "OnSettingsPressed");
        WireButton("ExitButton", buttonsContainer.transform, canvas, "OnExitPressed");

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("PhonePanel built successfully with all interactive elements!");
    }

    static void WireButton(string buttonName, Transform parent, GameObject canvas, string methodName)
    {
        Transform btnT = parent.Find(buttonName);
        if (btnT == null) return;

        Button btn = btnT.GetComponent<Button>();
        if (btn == null) return;

        MenuTransition menuTrans = canvas.GetComponent<MenuTransition>();
        if (menuTrans == null) return;

        // Use UnityEditor to add persistent listener
        var btnSO = new SerializedObject(btn);
        var onClickProp = btnSO.FindProperty("m_OnClick");
        var callsProp = onClickProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
        callsProp.arraySize++;
        var newCall = callsProp.GetArrayElementAtIndex(callsProp.arraySize - 1);
        newCall.FindPropertyRelative("m_Target").objectReferenceValue = menuTrans;
        newCall.FindPropertyRelative("m_MethodName").stringValue = methodName;
        newCall.FindPropertyRelative("m_Mode").intValue = 1; // Void
        newCall.FindPropertyRelative("m_CallState").intValue = 2; // RuntimeOnly
        btnSO.ApplyModifiedProperties();
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    static GameObject CreateTMPText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        if (pixelFont != null)
            tmp.font = pixelFont;
        return go;
    }

    static void CreateMenuButton(string name, Transform parent, string label, Color labelColor, float height)
    {
        GameObject btnGo = CreateUIObject(name, parent);

        // Layout element for height
        LayoutElement le = btnGo.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;

        // Button background
        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = transparent;
        btnImg.raycastTarget = true;

        // Button component
        Button btn = btnGo.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = transparent;
        cb.highlightedColor = buttonHoverColor;
        cb.pressedColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        cb.selectedColor = buttonHoverColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // Label text
        GameObject labelGo = CreateTMPText("Label", btnGo.transform, label, 24, labelColor, TextAlignmentOptions.MidlineLeft);
        RectTransform labelRT = labelGo.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(50, 0);
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTMP = labelGo.GetComponent<TextMeshProUGUI>();
        labelTMP.fontStyle = FontStyles.Bold;

        // Icon placeholder (will be replaced by ApplyIconSprites)
        GameObject iconGo = CreateUIObject("IconImage", btnGo.transform);
        iconGo.transform.SetAsFirstSibling();
        RectTransform iconRT = iconGo.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.sizeDelta = new Vector2(28, 28);
        iconRT.anchoredPosition = new Vector2(15, 0);

        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        iconImg.color = labelColor;

        // Try to load icon
        string iconName = "";
        if (label == "START") iconName = "icon_play";
        else if (label == "SETTINGS") iconName = "icon_gear";
        else if (label == "EXIT") iconName = "icon_cross";

        if (!string.IsNullOrEmpty(iconName))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + iconName + ".png");
            if (sprite != null)
                iconImg.sprite = sprite;
        }
    }

    static void CreateSocialItem(string name, Transform parent, string countText, Color iconColor)
    {
        GameObject container = CreateUIObject(name, parent);
        RectTransform containerRT = container.GetComponent<RectTransform>();
        containerRT.sizeDelta = new Vector2(50, 55);

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        // Icon
        GameObject iconGo = CreateUIObject("Icon", container.transform);
        RectTransform iconRT = iconGo.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(28, 28);
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        iconImg.color = iconColor;

        // Try to load icon
        string iconName = "";
        if (name == "HeartIcon") iconName = "icon_heart";
        else if (name == "CommentIcon") iconName = "icon_comment";
        else if (name == "ShareIcon") iconName = "icon_share";

        if (!string.IsNullOrEmpty(iconName))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + iconName + ".png");
            if (sprite != null)
                iconImg.sprite = sprite;
        }

        // Count text
        GameObject countGo = CreateTMPText("Count", container.transform, countText, 10, textGray, TextAlignmentOptions.Center);
        RectTransform countRT = countGo.GetComponent<RectTransform>();
        countRT.sizeDelta = new Vector2(50, 18);

        LayoutElement countLE = countGo.AddComponent<LayoutElement>();
        countLE.preferredWidth = 50;
        countLE.preferredHeight = 18;
    }

    static void CreateNavItem(string name, Transform parent, string iconChar)
    {
        GameObject navItem = CreateUIObject(name, parent);
        Image navItemImg = navItem.AddComponent<Image>();
        navItemImg.color = transparent;
        navItemImg.raycastTarget = true;

        // Icon
        GameObject iconGo = CreateUIObject("Icon", navItem.transform);
        RectTransform iconRT = iconGo.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.15f, 0.1f);
        iconRT.anchorMax = new Vector2(0.85f, 0.9f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;

        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;

        // Try to load icon
        string iconName = "";
        if (name == "NavHome") iconName = "icon_home";
        else if (name == "NavSearch") iconName = "icon_search";
        else if (name == "NavPlus") iconName = "icon_plus";
        else if (name == "NavGrid") iconName = "icon_grid";
        else if (name == "NavProfile") iconName = "icon_profile";

        if (!string.IsNullOrEmpty(iconName))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath + iconName + ".png");
            if (sprite != null)
                iconImg.sprite = sprite;
        }
    }
}
