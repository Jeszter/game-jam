using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Повністю відбудовує сцену головного меню з нуля:
/// - створює Assets/Scenes/MainMenu.unity
/// - будує MainMenuCanvas (фон, Phone, PhonePanel, TitleGroup, CornerUI, FadeOverlay)
/// - підключає скрипти (MenuTransition -> "Game1 2", PhoneFlickerEffect, IdleAutoScroll)
/// - робить Button-и Start / Settings / Exit робочими
/// - додає EventSystem, музику меню
/// - прописує Build Settings: MainMenu (0), Game1 2 (1)
/// </summary>
public static class CreateMainMenuScene
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Game1 2.unity";
    const string IconsPath = "Assets/_UI_COPLAY_GENERATED/MainMenu/Textures/Icons/";
    const string MusicPath = "Assets/Sound/MainMenu.mp3";
    const string PhoneTexturePath = "Assets/_UI_COPLAY_GENERATED/MainMenu/Textures/phone.png";
    const string BackgroundTexturePath = "Assets/_UI_COPLAY_GENERATED/MainMenu/Textures/background.png";
    const string DoomLogoTexturePath = "Assets/_UI_COPLAY_GENERATED/MainMenu/Textures/Doom.png";

    static Color darkBg = new Color(0.08f, 0.08f, 0.1f, 1f);
    static Color textWhite = new Color(0.92f, 0.92f, 0.92f, 1f);
    static Color textGray = new Color(0.55f, 0.55f, 0.6f, 1f);
    static Color accentRed = new Color(0.9f, 0.2f, 0.2f, 1f);
    static Color navBarBg = new Color(0.06f, 0.06f, 0.08f, 1f);
    static Color statusBarBg = new Color(0.06f, 0.06f, 0.08f, 0.9f);

    static TMP_FontAsset tmpFont;

    [MenuItem("Tools/Coplay/Build Main Menu Scene")]
    public static void Execute()
    {
        EnsureSpriteImportForIcons();

        tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        // ---- 1. Create new scene ----
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        GameObject cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        Camera camComp = cam.GetComponent<Camera>();
        camComp.clearFlags = CameraClearFlags.SolidColor;
        camComp.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
        camComp.orthographic = false;
        cam.transform.position = new Vector3(0, 1, -10);

        // EventSystem
        GameObject es = new GameObject("EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));

        // Canvas
        GameObject canvasGo = new GameObject("MainMenuCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Background image (full screen)
        GameObject bg = CreateUIObject("Background", canvasGo.transform);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.raycastTarget = false;
        Sprite bgSprite = LoadSpriteFromTexture(BackgroundTexturePath);
        if (bgSprite != null)
        {
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
            bgImg.color = Color.white;
        }
        else
        {
            bgImg.color = new Color(0.03f, 0.03f, 0.05f, 1f);
        }

        // BackgroundGroup CanvasGroup wrapper for fade
        CanvasGroup backgroundGroup = bg.AddComponent<CanvasGroup>();
        backgroundGroup.alpha = 1f;

        // Title (DOOM-style logo / text)
        GameObject titleGroupGo = CreateUIObject("TitleGroup", canvasGo.transform);
        RectTransform titleRT = titleGroupGo.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(0f, 1f);
        titleRT.pivot = new Vector2(0f, 1f);
        titleRT.anchoredPosition = new Vector2(60, -50);
        titleRT.sizeDelta = new Vector2(600, 220);
        CanvasGroup titleGroup = titleGroupGo.AddComponent<CanvasGroup>();
        titleGroup.alpha = 1f;

        // Title logo image (optional, if exists)
        Sprite doomSprite = LoadSpriteFromTexture(DoomLogoTexturePath);
        if (doomSprite != null)
        {
            GameObject logo = CreateUIObject("DoomLogo", titleGroupGo.transform);
            RectTransform logoRT = logo.GetComponent<RectTransform>();
            logoRT.anchorMin = new Vector2(0, 0);
            logoRT.anchorMax = new Vector2(1, 1);
            logoRT.offsetMin = Vector2.zero;
            logoRT.offsetMax = Vector2.zero;
            Image logoImg = logo.AddComponent<Image>();
            logoImg.sprite = doomSprite;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;
        }
        else
        {
            // Fallback text title
            GameObject titleText = CreateTMPText("TitleText", titleGroupGo.transform,
                "DOOMIFY", 80, new Color(0.9f, 0.15f, 0.15f, 1f), TextAlignmentOptions.TopLeft);
            RectTransform ttRT = titleText.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(0, 0);
            ttRT.anchorMax = new Vector2(1, 1);
            ttRT.offsetMin = Vector2.zero;
            ttRT.offsetMax = Vector2.zero;
            var tmpT = titleText.GetComponent<TextMeshProUGUI>();
            tmpT.fontStyle = FontStyles.Bold;
        }

        // Corner UI
        GameObject cornerUIGo = CreateUIObject("CornerUI", canvasGo.transform);
        RectTransform cornerRT = cornerUIGo.GetComponent<RectTransform>();
        cornerRT.anchorMin = new Vector2(1f, 1f);
        cornerRT.anchorMax = new Vector2(1f, 1f);
        cornerRT.pivot = new Vector2(1f, 1f);
        cornerRT.anchoredPosition = new Vector2(-30, -30);
        cornerRT.sizeDelta = new Vector2(260, 60);
        CanvasGroup cornerGroup = cornerUIGo.AddComponent<CanvasGroup>();
        cornerGroup.alpha = 1f;

        HorizontalLayoutGroup cornerHLG = cornerUIGo.AddComponent<HorizontalLayoutGroup>();
        cornerHLG.spacing = 16;
        cornerHLG.childAlignment = TextAnchor.MiddleRight;
        cornerHLG.childControlWidth = false;
        cornerHLG.childControlHeight = false;
        cornerHLG.childForceExpandWidth = false;
        cornerHLG.childForceExpandHeight = false;

        CreateCornerButton("SettingsCorner", cornerUIGo.transform, "SETTINGS", "icon_settings_corner");
        CreateCornerButton("ExitCorner", cornerUIGo.transform, "EXIT", "icon_exit_corner");

        // Phone image (decorative photo of a phone, the PhonePanel goes on top of its screen area)
        GameObject phone = CreateUIObject("Phone", canvasGo.transform);
        RectTransform phoneRT = phone.GetComponent<RectTransform>();
        phoneRT.anchorMin = new Vector2(0.5f, 0.5f);
        phoneRT.anchorMax = new Vector2(0.5f, 0.5f);
        phoneRT.pivot = new Vector2(0.5f, 0.5f);
        phoneRT.anchoredPosition = new Vector2(250, 0);
        phoneRT.localRotation = Quaternion.Euler(0, 0, -5.4f);
        phoneRT.sizeDelta = new Vector2(480, 800);
        Image phoneImg = phone.AddComponent<Image>();
        phoneImg.raycastTarget = false;
        phoneImg.preserveAspect = true;
        Sprite phoneSprite = LoadSpriteFromTexture(PhoneTexturePath);
        if (phoneSprite != null)
        {
            phoneImg.sprite = phoneSprite;
            phoneImg.color = Color.white;
        }
        else
        {
            phoneImg.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        }

        // PhonePanel - inner interactive screen
        GameObject phonePanel = CreateUIObject("PhonePanel", canvasGo.transform);
        RectTransform phonePanelRT = phonePanel.GetComponent<RectTransform>();
        phonePanelRT.anchorMin = new Vector2(0.5f, 0.5f);
        phonePanelRT.anchorMax = new Vector2(0.5f, 0.5f);
        phonePanelRT.pivot = new Vector2(0.5f, 0.5f);
        phonePanelRT.sizeDelta = new Vector2(300, 560);
        phonePanelRT.anchoredPosition = new Vector2(250, 8);
        phonePanelRT.localRotation = Quaternion.Euler(0, 0, -5.4f);
        CanvasGroup phonePanelCG = phonePanel.AddComponent<CanvasGroup>();
        phonePanelCG.alpha = 1f;
        Image phonePanelImg = phonePanel.AddComponent<Image>();
        phonePanelImg.color = darkBg;
        phonePanelImg.raycastTarget = true;
        Mask phoneMask = phonePanel.AddComponent<Mask>();
        phoneMask.showMaskGraphic = true;

        // Status bar
        GameObject statusBar = CreateUIObject("StatusBar", phonePanel.transform);
        RectTransform statusBarRT = statusBar.GetComponent<RectTransform>();
        statusBarRT.anchorMin = new Vector2(0, 1);
        statusBarRT.anchorMax = new Vector2(1, 1);
        statusBarRT.pivot = new Vector2(0.5f, 1);
        statusBarRT.sizeDelta = new Vector2(0, 26);
        statusBarRT.anchoredPosition = Vector2.zero;
        Image sbImg = statusBar.AddComponent<Image>();
        sbImg.color = statusBarBg;
        sbImg.raycastTarget = false;

        var timeText = CreateTMPText("TimeText", statusBar.transform, "02:47", 11,
            textWhite, TextAlignmentOptions.MidlineLeft);
        var timeRT = timeText.GetComponent<RectTransform>();
        timeRT.anchorMin = new Vector2(0, 0);
        timeRT.anchorMax = new Vector2(0.3f, 1);
        timeRT.offsetMin = new Vector2(12, 0);
        timeRT.offsetMax = Vector2.zero;

        var signalText = CreateTMPText("SignalText", statusBar.transform, "|||  o  ||", 9,
            textGray, TextAlignmentOptions.MidlineRight);
        var signalRT = signalText.GetComponent<RectTransform>();
        signalRT.anchorMin = new Vector2(0.5f, 0);
        signalRT.anchorMax = new Vector2(1, 1);
        signalRT.offsetMin = Vector2.zero;
        signalRT.offsetMax = new Vector2(-12, 0);

        // Phone content (scrollable area)
        GameObject phoneContent = CreateUIObject("PhoneContent", phonePanel.transform);
        RectTransform phoneContentRT = phoneContent.GetComponent<RectTransform>();
        phoneContentRT.anchorMin = new Vector2(0, 0);
        phoneContentRT.anchorMax = new Vector2(1, 1);
        phoneContentRT.offsetMin = new Vector2(0, 46);
        phoneContentRT.offsetMax = new Vector2(0, -26);

        // Buttons container
        GameObject buttonsContainer = CreateUIObject("ButtonsContainer", phoneContent.transform);
        RectTransform btRT = buttonsContainer.GetComponent<RectTransform>();
        btRT.anchorMin = new Vector2(0, 0.3f);
        btRT.anchorMax = new Vector2(0.75f, 0.88f);
        btRT.offsetMin = new Vector2(10, 0);
        btRT.offsetMax = new Vector2(0, 0);
        VerticalLayoutGroup vlg = buttonsContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(8, 8, 8, 8);

        GameObject startBtn = CreateMenuButton("StartButton", buttonsContainer.transform, "START", "icon_play", Color.white, 58);
        GameObject settingsBtn = CreateMenuButton("SettingsButton", buttonsContainer.transform, "SETTINGS", "icon_gear", Color.white, 46);
        GameObject exitBtn = CreateMenuButton("ExitButton", buttonsContainer.transform, "EXIT", "icon_cross", Color.white, 46);

        // Social bar (right)
        GameObject socialBar = CreateUIObject("SocialBar", phoneContent.transform);
        RectTransform sbRT = socialBar.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0.78f, 0.08f);
        sbRT.anchorMax = new Vector2(1f, 0.6f);
        sbRT.offsetMin = new Vector2(-5, 0);
        sbRT.offsetMax = new Vector2(-3, 0);
        VerticalLayoutGroup svlg = socialBar.AddComponent<VerticalLayoutGroup>();
        svlg.spacing = 10;
        svlg.childAlignment = TextAnchor.MiddleCenter;
        svlg.childControlWidth = false;
        svlg.childControlHeight = false;
        svlg.childForceExpandWidth = false;
        svlg.childForceExpandHeight = false;

        CreateSocialItem("HeartIcon", socialBar.transform, "12.3k", "icon_heart", new Color(1f, 0.3f, 0.3f, 1f));
        CreateSocialItem("CommentIcon", socialBar.transform, "567", "icon_comment", textGray);
        CreateSocialItem("ShareIcon", socialBar.transform, "890", "icon_share", textGray);

        // NavBar (bottom)
        GameObject navBar = CreateUIObject("NavBar", phonePanel.transform);
        RectTransform nbRT = navBar.GetComponent<RectTransform>();
        nbRT.anchorMin = new Vector2(0, 0);
        nbRT.anchorMax = new Vector2(1, 0);
        nbRT.pivot = new Vector2(0.5f, 0);
        nbRT.sizeDelta = new Vector2(0, 46);
        nbRT.anchoredPosition = Vector2.zero;
        Image nbImg = navBar.AddComponent<Image>();
        nbImg.color = navBarBg;
        nbImg.raycastTarget = false;

        HorizontalLayoutGroup hlg = navBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(5, 5, 4, 4);

        CreateNavItem("NavHome", navBar.transform, "icon_home");
        CreateNavItem("NavSearch", navBar.transform, "icon_search");
        CreateNavItem("NavPlus", navBar.transform, "icon_plus");
        CreateNavItem("NavGrid", navBar.transform, "icon_grid");
        CreateNavItem("NavProfile", navBar.transform, "icon_profile");

        // FadeOverlay (full screen black, alpha=0 by default)
        GameObject fadeGo = CreateUIObject("FadeOverlay", canvasGo.transform);
        RectTransform fadeRT = fadeGo.GetComponent<RectTransform>();
        fadeRT.anchorMin = Vector2.zero;
        fadeRT.anchorMax = Vector2.one;
        fadeRT.offsetMin = Vector2.zero;
        fadeRT.offsetMax = Vector2.zero;
        Image fadeImg = fadeGo.AddComponent<Image>();
        fadeImg.color = Color.black;
        fadeImg.raycastTarget = false;
        CanvasGroup fadeCG = fadeGo.AddComponent<CanvasGroup>();
        fadeCG.alpha = 0f;
        fadeCG.blocksRaycasts = false;
        fadeCG.interactable = false;

        // Attach scripts to canvas
        MenuTransition menuTrans = canvasGo.AddComponent<MenuTransition>();
        PhoneFlickerEffect flicker = canvasGo.AddComponent<PhoneFlickerEffect>();
        IdleAutoScroll idle = canvasGo.AddComponent<IdleAutoScroll>();

        // Wire up serialized fields on scripts
        var mtSO = new SerializedObject(menuTrans);
        mtSO.FindProperty("phonePanel").objectReferenceValue = phonePanelRT;
        mtSO.FindProperty("backgroundGroup").objectReferenceValue = backgroundGroup;
        mtSO.FindProperty("overlayFade").objectReferenceValue = fadeCG;
        mtSO.FindProperty("titleGroup").objectReferenceValue = titleGroup;
        mtSO.FindProperty("cornerUIGroup").objectReferenceValue = cornerGroup;
        mtSO.FindProperty("nextSceneName").stringValue = "Game1 2";
        mtSO.FindProperty("zoomDuration").floatValue = 1.2f;
        mtSO.FindProperty("fadeDuration").floatValue = 0.8f;
        mtSO.FindProperty("targetScale").floatValue = 3f;
        mtSO.ApplyModifiedProperties();

        var flickSO = new SerializedObject(flicker);
        flickSO.FindProperty("phoneCanvasGroup").objectReferenceValue = phonePanelCG;
        flickSO.FindProperty("flickerSpeed").floatValue = 3f;
        flickSO.FindProperty("flickerIntensity").floatValue = 0.04f;
        flickSO.FindProperty("randomFlickerChance").floatValue = 0.02f;
        flickSO.ApplyModifiedProperties();

        var idleSO = new SerializedObject(idle);
        idleSO.FindProperty("phoneContent").objectReferenceValue = phoneContentRT;
        idleSO.FindProperty("idleTimeout").floatValue = 5f;
        idleSO.FindProperty("scrollSpeed").floatValue = 15f;
        idleSO.FindProperty("maxScrollDistance").floatValue = 80f;
        idleSO.FindProperty("fadeOverlay").objectReferenceValue = fadeCG;
        idleSO.ApplyModifiedProperties();

        // Wire buttons to MenuTransition methods
        WireButton(startBtn, menuTrans, "OnStartPressed");
        WireButton(settingsBtn, menuTrans, "OnSettingsPressed");
        WireButton(exitBtn, menuTrans, "OnExitPressed");

        // Corner buttons: wire to the same MenuTransition as well
        GameObject cornerSettings = cornerUIGo.transform.Find("SettingsCorner").gameObject;
        GameObject cornerExit = cornerUIGo.transform.Find("ExitCorner").gameObject;
        WireButton(cornerSettings, menuTrans, "OnSettingsPressed");
        WireButton(cornerExit, menuTrans, "OnExitPressed");

        // Add hover glow to menu buttons
        AddHoverGlow(startBtn, new Color(1f, 0.3f, 0.3f, 1f), 0.5f, 0.5f);
        AddHoverGlow(settingsBtn, Color.white, 0.35f, 0.4f);
        AddHoverGlow(exitBtn, Color.white, 0.35f, 0.4f);

        // AudioSource with menu music
        AudioSource audio = canvasGo.AddComponent<AudioSource>();
        AudioClip music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        if (music != null)
        {
            audio.clip = music;
            audio.loop = true;
            audio.playOnAwake = true;
            audio.volume = 0.5f;
            audio.spatialBlend = 0f;
        }
        else
        {
            Debug.LogWarning($"[MainMenu] Music clip not found at {MusicPath}");
        }

        // ---- 2. Save scene ----
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(newScene, ScenePath);

        // ---- 3. Build Settings: MainMenu (0), Game1 2 (1) ----
        var buildScenes = new List<EditorBuildSettingsScene>();
        buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        if (System.IO.File.Exists(GameScenePath))
            buildScenes.Add(new EditorBuildSettingsScene(GameScenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MainMenu] Scene created at {ScenePath}. Build Settings: MainMenu=0, Game1 2=1. nextSceneName='Game1 2'.");
    }

    // ------------------------- HELPERS -------------------------

    static void EnsureSpriteImportForIcons()
    {
        string[] icons =
        {
            "icon_play", "icon_gear", "icon_cross", "icon_heart", "icon_comment",
            "icon_share", "icon_home", "icon_search", "icon_plus", "icon_grid",
            "icon_profile", "icon_settings_corner", "icon_exit_corner"
        };
        foreach (string name in icons)
        {
            string path = IconsPath + name + ".png";
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null && (ti.textureType != TextureImporterType.Sprite))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
        }
        // Ensure phone.png / background.png / Doom.png as sprites
        foreach (string p in new[] { PhoneTexturePath, BackgroundTexturePath, DoomLogoTexturePath })
        {
            TextureImporter ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
        }
    }

    static Sprite LoadSpriteFromTexture(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite LoadIcon(string iconName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(IconsPath + iconName + ".png");
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0) go.layer = uiLayer;
        return go;
    }

    static GameObject CreateTMPText(string name, Transform parent, string text, float fontSize,
        Color color, TextAlignmentOptions alignment)
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
        if (tmpFont != null) tmp.font = tmpFont;
        return go;
    }

    static GameObject CreateMenuButton(string name, Transform parent, string label,
        string iconName, Color labelColor, float height)
    {
        GameObject btnGo = CreateUIObject(name, parent);
        LayoutElement le = btnGo.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;

        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0, 0, 0, 0.001f); // almost invisible, still raycasts
        btnImg.raycastTarget = true;

        Button btn = btnGo.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(1, 1, 1, 1f);
        cb.highlightedColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        cb.pressedColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        cb.selectedColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.targetGraphic = btnImg;

        // Icon
        GameObject iconGo = CreateUIObject("IconImage", btnGo.transform);
        RectTransform iconRT = iconGo.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.sizeDelta = new Vector2(24, 24);
        iconRT.anchoredPosition = new Vector2(12, 0);
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        iconImg.color = labelColor;
        Sprite iconSprite = LoadIcon(iconName);
        if (iconSprite != null) iconImg.sprite = iconSprite;

        // Label
        GameObject lblGo = CreateTMPText("Label", btnGo.transform, "     " + label, 20,
            labelColor, TextAlignmentOptions.MidlineLeft);
        RectTransform lblRT = lblGo.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 1);
        lblRT.offsetMin = new Vector2(44, 0);
        lblRT.offsetMax = Vector2.zero;
        var lblTMP = lblGo.GetComponent<TextMeshProUGUI>();
        lblTMP.fontStyle = FontStyles.Bold;
        if (label == "START") lblTMP.fontSize = 22;

        return btnGo;
    }

    static void CreateSocialItem(string name, Transform parent, string countText,
        string iconName, Color iconColor)
    {
        GameObject container = CreateUIObject(name, parent);
        RectTransform cRT = container.GetComponent<RectTransform>();
        cRT.sizeDelta = new Vector2(40, 48);

        LayoutElement clE = container.AddComponent<LayoutElement>();
        clE.preferredWidth = 40;
        clE.preferredHeight = 48;

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        GameObject icon = CreateUIObject("Icon", container.transform);
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(22, 22);
        LayoutElement ile = icon.AddComponent<LayoutElement>();
        ile.preferredWidth = 22;
        ile.preferredHeight = 22;
        Image iImg = icon.AddComponent<Image>();
        iImg.raycastTarget = false;
        iImg.preserveAspect = true;
        iImg.color = iconColor;
        Sprite s = LoadIcon(iconName);
        if (s != null) iImg.sprite = s;

        GameObject countGo = CreateTMPText("Count", container.transform, countText, 8,
            textGray, TextAlignmentOptions.Center);
        RectTransform countRT = countGo.GetComponent<RectTransform>();
        countRT.sizeDelta = new Vector2(40, 14);
        LayoutElement cle = countGo.AddComponent<LayoutElement>();
        cle.preferredWidth = 40;
        cle.preferredHeight = 14;
    }

    static void CreateNavItem(string name, Transform parent, string iconName)
    {
        GameObject navItem = CreateUIObject(name, parent);
        Image navImg = navItem.AddComponent<Image>();
        navImg.color = new Color(0, 0, 0, 0.001f);
        navImg.raycastTarget = true;

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
        Sprite s = LoadIcon(iconName);
        if (s != null) iconImg.sprite = s;
    }

    static void CreateCornerButton(string name, Transform parent, string label, string iconName)
    {
        GameObject go = CreateUIObject(name, parent);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 120;
        le.preferredHeight = 40;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.001f);
        img.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
        btn.colors = cb;
        btn.targetGraphic = img;

        // Icon
        GameObject iconGo = CreateUIObject("Icon", go.transform);
        RectTransform iconRT = iconGo.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.sizeDelta = new Vector2(22, 22);
        iconRT.anchoredPosition = new Vector2(4, 0);
        Image iImg = iconGo.AddComponent<Image>();
        iImg.raycastTarget = false;
        iImg.preserveAspect = true;
        iImg.color = new Color(0.6f, 0.6f, 0.65f, 1f);
        Sprite s = LoadIcon(iconName);
        if (s != null) iImg.sprite = s;

        // Label
        GameObject lbl = CreateTMPText("Label", go.transform, label, 14, textWhite, TextAlignmentOptions.MidlineLeft);
        RectTransform lblRT = lbl.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 1);
        lblRT.offsetMin = new Vector2(32, 0);
        lblRT.offsetMax = Vector2.zero;
        var lblTMP = lbl.GetComponent<TextMeshProUGUI>();
        lblTMP.fontStyle = FontStyles.Bold;
    }

    static void WireButton(GameObject btnGo, MonoBehaviour target, string methodName)
    {
        if (btnGo == null || target == null) return;
        Button btn = btnGo.GetComponent<Button>();
        if (btn == null) return;

        var so = new SerializedObject(btn);
        var onClickProp = so.FindProperty("m_OnClick");
        var callsProp = onClickProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
        callsProp.arraySize++;
        var newCall = callsProp.GetArrayElementAtIndex(callsProp.arraySize - 1);
        newCall.FindPropertyRelative("m_Target").objectReferenceValue = target;
        newCall.FindPropertyRelative("m_MethodName").stringValue = methodName;
        newCall.FindPropertyRelative("m_Mode").intValue = 1; // Void
        newCall.FindPropertyRelative("m_CallState").intValue = 2; // RuntimeOnly
        // try to set target assembly type name for 2022+ serialization robustness
        var targetAssemblyTypeProp = newCall.FindPropertyRelative("m_TargetAssemblyTypeName");
        if (targetAssemblyTypeProp != null)
            targetAssemblyTypeProp.stringValue = target.GetType().AssemblyQualifiedName;
        so.ApplyModifiedProperties();
    }

    static void AddHoverGlow(GameObject btnGo, Color glowColor, float intensity, float iconBoost)
    {
        if (btnGo == null) return;
        ButtonHoverGlow glow = btnGo.GetComponent<ButtonHoverGlow>();
        if (glow == null) glow = btnGo.AddComponent<ButtonHoverGlow>();
        var so = new SerializedObject(glow);
        var gcProp = so.FindProperty("glowColor");
        if (gcProp != null) gcProp.colorValue = glowColor;
        var intProp = so.FindProperty("glowIntensity");
        if (intProp != null) intProp.floatValue = intensity;
        var fadeProp = so.FindProperty("glowFadeSpeed");
        if (fadeProp != null) fadeProp.floatValue = 5f;
        var iconProp = so.FindProperty("iconBrightnessBoost");
        if (iconProp != null) iconProp.floatValue = iconBoost;
        so.ApplyModifiedProperties();
    }
}
