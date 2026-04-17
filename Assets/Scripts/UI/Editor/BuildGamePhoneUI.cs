using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class BuildGamePhoneUI
{
    // Colors
    static readonly Color PhoneBG = new Color(0.06f, 0.06f, 0.09f, 0.95f);
    static readonly Color PhoneBorder = new Color(0.25f, 0.25f, 0.3f, 1f);
    static readonly Color AccentPink = new Color(0.95f, 0.2f, 0.5f);
    static readonly Color AccentCyan = new Color(0.2f, 0.9f, 0.95f);
    static readonly Color DarkPanel = new Color(0.08f, 0.08f, 0.12f, 0.9f);
    static readonly Color ButtonBG = new Color(0.15f, 0.15f, 0.2f, 1f);
    static readonly Color TextWhite = new Color(0.95f, 0.95f, 0.95f);
    static readonly Color TextGray = new Color(0.6f, 0.6f, 0.65f);
    static readonly Color GoldCoin = new Color(1f, 0.85f, 0.2f);
    static readonly Color HUDBg = new Color(0.05f, 0.05f, 0.08f, 0.75f);
    static readonly Color BarBG = new Color(0.15f, 0.15f, 0.2f, 1f);
    static readonly Color ShopItemBG = new Color(0.1f, 0.1f, 0.15f, 1f);
    static readonly Color BuyBtnColor = new Color(0.2f, 0.8f, 0.4f, 1f);

    static TMP_FontAsset font;

    public static void Execute()
    {
        // Load font
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null)
        {
            Debug.LogError("Font not found!");
            return;
        }

        // Remove old GameUI if exists
        GameObject oldUI = GameObject.Find("GameUICanvas");
        if (oldUI != null) Object.DestroyImmediate(oldUI);

        // ===== CREATE CANVAS =====
        GameObject canvasObj = new GameObject("GameUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Ensure EventSystem exists
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ===== HUD (top of screen) =====
        GameObject hudObj = BuildHUD(canvasObj);

        // ===== PHONE PANEL =====
        GameObject phoneRoot = BuildPhone(canvasObj);

        // ===== WIRE UP CONTROLLER =====
        GamePhoneController phoneCtrl = canvasObj.AddComponent<GamePhoneController>();
        GameHUDController hudCtrl = hudObj.AddComponent<GameHUDController>();

        // Set serialized fields via SerializedObject
        SerializedObject phoneCtrlSO = new SerializedObject(phoneCtrl);

        // Phone panel references
        GameObject phonePanel = phoneRoot;
        phoneCtrlSO.FindProperty("phonePanel").objectReferenceValue = phonePanel.GetComponent<RectTransform>();
        phoneCtrlSO.FindProperty("phoneCanvasGroup").objectReferenceValue = phonePanel.GetComponent<CanvasGroup>();

        // Screen references
        Transform homeScreen = phonePanel.transform.Find("PhoneScreen/HomeScreen");
        Transform tikTokScreen = phonePanel.transform.Find("PhoneScreen/TikTokScreen");
        Transform shopScreen = phonePanel.transform.Find("PhoneScreen/ShopScreen");

        phoneCtrlSO.FindProperty("homeScreen").objectReferenceValue = homeScreen.gameObject;
        phoneCtrlSO.FindProperty("tikTokScreen").objectReferenceValue = tikTokScreen.gameObject;
        phoneCtrlSO.FindProperty("shopScreen").objectReferenceValue = shopScreen.gameObject;

        // Button references
        phoneCtrlSO.FindProperty("tikTokButton").objectReferenceValue = homeScreen.Find("AppGrid/TikTokAppBtn").GetComponent<Button>();
        phoneCtrlSO.FindProperty("shopButton").objectReferenceValue = homeScreen.Find("AppGrid/ShopAppBtn").GetComponent<Button>();
        phoneCtrlSO.FindProperty("backButtonTikTok").objectReferenceValue = tikTokScreen.Find("TopBar/BackButton").GetComponent<Button>();
        phoneCtrlSO.FindProperty("backButtonShop").objectReferenceValue = shopScreen.Find("TopBar/BackButton").GetComponent<Button>();

        phoneCtrlSO.ApplyModifiedProperties();

        // HUD references
        SerializedObject hudSO = new SerializedObject(hudCtrl);
        Transform dopBar = hudObj.transform.Find("DopamineSection/DopamineBarBG/DopamineBarFill");
        Transform dopText = hudObj.transform.Find("DopamineSection/DopamineBarBG/DopamineText");
        Transform coinTxt = hudObj.transform.Find("CoinSection/CoinAmount");

        hudSO.FindProperty("dopamineBarFill").objectReferenceValue = dopBar.GetComponent<Image>();
        hudSO.FindProperty("dopamineText").objectReferenceValue = dopText.GetComponent<TMP_Text>();
        hudSO.FindProperty("coinText").objectReferenceValue = coinTxt.GetComponent<TMP_Text>();
        hudSO.ApplyModifiedProperties();

        // TikTok controller
        TikTokFeedController tikTokCtrl = tikTokScreen.gameObject.AddComponent<TikTokFeedController>();
        SerializedObject tikTokSO = new SerializedObject(tikTokCtrl);
        Transform feedScroll = tikTokScreen.Find("FeedScroll");
        tikTokSO.FindProperty("scrollRect").objectReferenceValue = feedScroll.GetComponent<ScrollRect>();
        tikTokSO.FindProperty("contentContainer").objectReferenceValue = feedScroll.Find("Viewport/Content").GetComponent<RectTransform>();
        tikTokSO.FindProperty("postPrefabTemplate").objectReferenceValue = feedScroll.Find("Viewport/Content/PostTemplate").gameObject;
        tikTokSO.ApplyModifiedProperties();

        // Shop controller
        ShopController shopCtrl = shopScreen.gameObject.AddComponent<ShopController>();
        SerializedObject shopSO = new SerializedObject(shopCtrl);
        Transform shopScroll = shopScreen.Find("ShopScroll");
        shopSO.FindProperty("itemsContainer").objectReferenceValue = shopScroll.Find("Viewport/Content").GetComponent<RectTransform>();
        shopSO.FindProperty("itemPrefabTemplate").objectReferenceValue = shopScroll.Find("Viewport/Content/ItemTemplate").gameObject;
        shopSO.FindProperty("hudController").objectReferenceValue = hudCtrl;
        shopSO.ApplyModifiedProperties();

        // Save
        EditorUtility.SetDirty(canvasObj);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("=== Game Phone UI built successfully! Press TAB in Play mode to open phone. ===");
    }

    // ==================== HUD ====================
    static GameObject BuildHUD(GameObject parent)
    {
        GameObject hud = CreateUIObj("HUD", parent, AnchorPreset.TopStretch);
        SetRT(hud, 0, -10, 0, 0, 0, 50);
        hud.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        hud.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        hud.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
        hud.GetComponent<RectTransform>().sizeDelta = new Vector2(-40, 50);
        hud.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);

        HorizontalLayoutGroup hlg = hud.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.padding = new RectOffset(20, 20, 5, 5);

        // Spacer to push items right
        GameObject spacer = CreateUIObj("Spacer", hud);
        LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.flexibleWidth = 1;

        // Dopamine section
        GameObject dopSection = CreateUIObj("DopamineSection", hud);
        LayoutElement dopLE = dopSection.AddComponent<LayoutElement>();
        dopLE.preferredWidth = 250;
        HorizontalLayoutGroup dopHLG = dopSection.AddComponent<HorizontalLayoutGroup>();
        dopHLG.spacing = 8;
        dopHLG.childAlignment = TextAnchor.MiddleCenter;
        dopHLG.childForceExpandWidth = false;
        dopHLG.childControlWidth = false;
        dopHLG.childControlHeight = true;

        // Dopamine icon
        GameObject dopIcon = CreateUIObj("DopamineIcon", dopSection);
        TMP_Text dopIconTxt = dopIcon.AddComponent<TextMeshProUGUI>();
        dopIconTxt.text = "🧠";
        dopIconTxt.fontSize = 22;
        dopIconTxt.alignment = TextAlignmentOptions.Center;
        dopIconTxt.font = font;
        LayoutElement dopIconLE = dopIcon.AddComponent<LayoutElement>();
        dopIconLE.preferredWidth = 30;

        // Dopamine bar background
        GameObject dopBarBG = CreateUIObj("DopamineBarBG", dopSection);
        Image dopBarBGImg = dopBarBG.AddComponent<Image>();
        dopBarBGImg.color = BarBG;
        LayoutElement dopBarLE = dopBarBG.AddComponent<LayoutElement>();
        dopBarLE.preferredWidth = 160;
        dopBarLE.preferredHeight = 22;

        // Dopamine bar fill
        GameObject dopBarFill = CreateUIObj("DopamineBarFill", dopBarBG);
        Image dopFillImg = dopBarFill.AddComponent<Image>();
        dopFillImg.color = new Color(0.2f, 0.9f, 0.4f);
        dopFillImg.type = Image.Type.Filled;
        dopFillImg.fillMethod = Image.FillMethod.Horizontal;
        dopFillImg.fillAmount = 0.65f;
        RectTransform dopFillRT = dopBarFill.GetComponent<RectTransform>();
        dopFillRT.anchorMin = Vector2.zero;
        dopFillRT.anchorMax = Vector2.one;
        dopFillRT.offsetMin = new Vector2(2, 2);
        dopFillRT.offsetMax = new Vector2(-2, -2);

        // Dopamine text overlay
        GameObject dopText = CreateUIObj("DopamineText", dopBarBG);
        TMP_Text dopTxt = dopText.AddComponent<TextMeshProUGUI>();
        dopTxt.text = "65%";
        dopTxt.fontSize = 14;
        dopTxt.font = font;
        dopTxt.fontStyle = FontStyles.Bold;
        dopTxt.color = TextWhite;
        dopTxt.alignment = TextAlignmentOptions.Center;
        RectTransform dopTxtRT = dopText.GetComponent<RectTransform>();
        dopTxtRT.anchorMin = Vector2.zero;
        dopTxtRT.anchorMax = Vector2.one;
        dopTxtRT.offsetMin = Vector2.zero;
        dopTxtRT.offsetMax = Vector2.zero;

        // Coin section
        GameObject coinSection = CreateUIObj("CoinSection", hud);
        LayoutElement coinLE = coinSection.AddComponent<LayoutElement>();
        coinLE.preferredWidth = 140;
        HorizontalLayoutGroup coinHLG = coinSection.AddComponent<HorizontalLayoutGroup>();
        coinHLG.spacing = 6;
        coinHLG.childAlignment = TextAnchor.MiddleCenter;
        coinHLG.childForceExpandWidth = false;
        coinHLG.childControlWidth = false;
        coinHLG.childControlHeight = true;

        // Coin icon
        GameObject coinIcon = CreateUIObj("CoinIcon", coinSection);
        Image coinIconImg = coinIcon.AddComponent<Image>();
        coinIconImg.color = GoldCoin;
        LayoutElement coinIconLE = coinIcon.AddComponent<LayoutElement>();
        coinIconLE.preferredWidth = 24;
        coinIconLE.preferredHeight = 24;

        // Coin amount
        GameObject coinAmount = CreateUIObj("CoinAmount", coinSection);
        TMP_Text coinTxt = coinAmount.AddComponent<TextMeshProUGUI>();
        coinTxt.text = "1250";
        coinTxt.fontSize = 20;
        coinTxt.font = font;
        coinTxt.fontStyle = FontStyles.Bold;
        coinTxt.color = GoldCoin;
        coinTxt.alignment = TextAlignmentOptions.Left;
        LayoutElement coinAmtLE = coinAmount.AddComponent<LayoutElement>();
        coinAmtLE.preferredWidth = 100;

        return hud;
    }

    // ==================== PHONE ====================
    static GameObject BuildPhone(GameObject parent)
    {
        // Phone root panel - right side, slides from bottom
        GameObject phone = CreateUIObj("PhonePanel", parent);
        RectTransform phoneRT = phone.GetComponent<RectTransform>();
        phoneRT.anchorMin = new Vector2(1, 0);
        phoneRT.anchorMax = new Vector2(1, 1);
        phoneRT.pivot = new Vector2(1, 0.5f);
        phoneRT.offsetMin = new Vector2(-380, 40);
        phoneRT.offsetMax = new Vector2(-30, -70);

        CanvasGroup phoneCG = phone.AddComponent<CanvasGroup>();
        phoneCG.alpha = 1f;

        // Phone border/frame
        Image phoneBorderImg = phone.AddComponent<Image>();
        phoneBorderImg.color = PhoneBorder;

        // Phone inner background
        GameObject phoneInner = CreateUIObj("PhoneInner", phone);
        Image phoneInnerImg = phoneInner.AddComponent<Image>();
        phoneInnerImg.color = PhoneBG;
        RectTransform innerRT = phoneInner.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(3, 3);
        innerRT.offsetMax = new Vector2(-3, -3);

        // Phone screen area (inside inner, with padding for status bar)
        GameObject phoneScreen = CreateUIObj("PhoneScreen", phone);
        RectTransform screenRT = phoneScreen.GetComponent<RectTransform>();
        screenRT.anchorMin = Vector2.zero;
        screenRT.anchorMax = Vector2.one;
        screenRT.offsetMin = new Vector2(6, 6);
        screenRT.offsetMax = new Vector2(-6, -35);

        // Status bar at top of phone
        GameObject statusBar = CreateUIObj("StatusBar", phone);
        RectTransform statusRT = statusBar.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 1);
        statusRT.anchorMax = new Vector2(1, 1);
        statusRT.pivot = new Vector2(0.5f, 1);
        statusRT.sizeDelta = new Vector2(0, 28);
        statusRT.anchoredPosition = new Vector2(0, -4);
        Image statusImg = statusBar.AddComponent<Image>();
        statusImg.color = new Color(0, 0, 0, 0.5f);

        // Time text
        GameObject timeText = CreateUIObj("TimeText", statusBar);
        TMP_Text timeTxt = timeText.AddComponent<TextMeshProUGUI>();
        timeTxt.text = "03:47";
        timeTxt.fontSize = 13;
        timeTxt.font = font;
        timeTxt.color = TextGray;
        timeTxt.alignment = TextAlignmentOptions.Center;
        RectTransform timeTxtRT = timeText.GetComponent<RectTransform>();
        timeTxtRT.anchorMin = Vector2.zero;
        timeTxtRT.anchorMax = Vector2.one;
        timeTxtRT.offsetMin = Vector2.zero;
        timeTxtRT.offsetMax = Vector2.zero;

        // ===== HOME SCREEN =====
        BuildHomeScreen(phoneScreen);

        // ===== TIKTOK SCREEN =====
        BuildTikTokScreen(phoneScreen);

        // ===== SHOP SCREEN =====
        BuildShopScreen(phoneScreen);

        return phone;
    }

    // ==================== HOME SCREEN ====================
    static void BuildHomeScreen(GameObject parent)
    {
        GameObject home = CreateUIObj("HomeScreen", parent);
        RectTransform homeRT = home.GetComponent<RectTransform>();
        homeRT.anchorMin = Vector2.zero;
        homeRT.anchorMax = Vector2.one;
        homeRT.offsetMin = Vector2.zero;
        homeRT.offsetMax = Vector2.zero;

        // Title
        GameObject title = CreateUIObj("Title", home);
        TMP_Text titleTxt = title.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "APPS";
        titleTxt.fontSize = 18;
        titleTxt.font = font;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = TextGray;
        titleTxt.alignment = TextAlignmentOptions.Center;
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.sizeDelta = new Vector2(0, 40);
        titleRT.anchoredPosition = new Vector2(0, -20);

        // App grid
        GameObject appGrid = CreateUIObj("AppGrid", home);
        RectTransform gridRT = appGrid.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0.1f, 0.35f);
        gridRT.anchorMax = new Vector2(0.9f, 0.85f);
        gridRT.offsetMin = Vector2.zero;
        gridRT.offsetMax = Vector2.zero;

        GridLayoutGroup grid = appGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(120, 130);
        grid.spacing = new Vector2(20, 20);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        // TikTok app button
        BuildAppButton(appGrid, "TikTokAppBtn", "📱", "DoomTok", AccentPink);

        // Shop app button
        BuildAppButton(appGrid, "ShopAppBtn", "🛒", "Shop", AccentCyan);

        // Hint text at bottom
        GameObject hint = CreateUIObj("HintText", home);
        TMP_Text hintTxt = hint.AddComponent<TextMeshProUGUI>();
        hintTxt.text = "TAB — close phone";
        hintTxt.fontSize = 12;
        hintTxt.font = font;
        hintTxt.color = new Color(0.4f, 0.4f, 0.45f);
        hintTxt.alignment = TextAlignmentOptions.Center;
        RectTransform hintRT = hint.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 0);
        hintRT.anchorMax = new Vector2(1, 0);
        hintRT.pivot = new Vector2(0.5f, 0);
        hintRT.sizeDelta = new Vector2(0, 30);
        hintRT.anchoredPosition = new Vector2(0, 10);
    }

    static void BuildAppButton(GameObject parent, string name, string emoji, string label, Color accentColor)
    {
        GameObject btn = CreateUIObj(name, parent);
        Image btnImg = btn.AddComponent<Image>();
        btnImg.color = DarkPanel;
        Button btnComp = btn.AddComponent<Button>();
        ColorBlock cb = btnComp.colors;
        cb.normalColor = DarkPanel;
        cb.highlightedColor = new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f, 1f);
        cb.pressedColor = new Color(accentColor.r * 0.5f, accentColor.g * 0.5f, accentColor.b * 0.5f, 1f);
        btnComp.colors = cb;

        // Emoji icon
        GameObject icon = CreateUIObj("Icon", btn);
        TMP_Text iconTxt = icon.AddComponent<TextMeshProUGUI>();
        iconTxt.text = emoji;
        iconTxt.fontSize = 40;
        iconTxt.font = font;
        iconTxt.alignment = TextAlignmentOptions.Center;
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.35f);
        iconRT.anchorMax = new Vector2(1, 1);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;

        // Label
        GameObject labelObj = CreateUIObj("Label", btn);
        TMP_Text labelTxt = labelObj.AddComponent<TextMeshProUGUI>();
        labelTxt.text = label;
        labelTxt.fontSize = 14;
        labelTxt.font = font;
        labelTxt.fontStyle = FontStyles.Bold;
        labelTxt.color = accentColor;
        labelTxt.alignment = TextAlignmentOptions.Center;
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 0.35f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
    }

    // ==================== TIKTOK SCREEN ====================
    static void BuildTikTokScreen(GameObject parent)
    {
        GameObject tikTok = CreateUIObj("TikTokScreen", parent);
        RectTransform ttRT = tikTok.GetComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero;
        ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = Vector2.zero;
        ttRT.offsetMax = Vector2.zero;
        tikTok.SetActive(false);

        // Top bar
        GameObject topBar = CreateUIObj("TopBar", tikTok);
        RectTransform topBarRT = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0, 1);
        topBarRT.anchorMax = new Vector2(1, 1);
        topBarRT.pivot = new Vector2(0.5f, 1);
        topBarRT.sizeDelta = new Vector2(0, 40);
        topBarRT.anchoredPosition = Vector2.zero;
        Image topBarImg = topBar.AddComponent<Image>();
        topBarImg.color = new Color(0, 0, 0, 0.7f);

        HorizontalLayoutGroup topHLG = topBar.AddComponent<HorizontalLayoutGroup>();
        topHLG.spacing = 8;
        topHLG.padding = new RectOffset(8, 8, 4, 4);
        topHLG.childAlignment = TextAnchor.MiddleLeft;
        topHLG.childForceExpandWidth = false;
        topHLG.childControlHeight = true;

        // Back button
        GameObject backBtn = CreateUIObj("BackButton", topBar);
        Image backImg = backBtn.AddComponent<Image>();
        backImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        Button backBtnComp = backBtn.AddComponent<Button>();
        LayoutElement backLE = backBtn.AddComponent<LayoutElement>();
        backLE.preferredWidth = 60;

        GameObject backTxt = CreateUIObj("Text", backBtn);
        TMP_Text backT = backTxt.AddComponent<TextMeshProUGUI>();
        backT.text = "← ";
        backT.fontSize = 18;
        backT.font = font;
        backT.color = TextWhite;
        backT.alignment = TextAlignmentOptions.Center;
        RectTransform backTxtRT = backTxt.GetComponent<RectTransform>();
        backTxtRT.anchorMin = Vector2.zero;
        backTxtRT.anchorMax = Vector2.one;
        backTxtRT.offsetMin = Vector2.zero;
        backTxtRT.offsetMax = Vector2.zero;

        // Title
        GameObject ttTitle = CreateUIObj("Title", topBar);
        TMP_Text ttTitleTxt = ttTitle.AddComponent<TextMeshProUGUI>();
        ttTitleTxt.text = "DoomTok";
        ttTitleTxt.fontSize = 18;
        ttTitleTxt.font = font;
        ttTitleTxt.fontStyle = FontStyles.Bold;
        ttTitleTxt.color = AccentPink;
        ttTitleTxt.alignment = TextAlignmentOptions.Left;
        LayoutElement ttTitleLE = ttTitle.AddComponent<LayoutElement>();
        ttTitleLE.flexibleWidth = 1;

        // Feed scroll
        GameObject feedScroll = CreateUIObj("FeedScroll", tikTok);
        RectTransform feedRT = feedScroll.GetComponent<RectTransform>();
        feedRT.anchorMin = Vector2.zero;
        feedRT.anchorMax = Vector2.one;
        feedRT.offsetMin = new Vector2(0, 0);
        feedRT.offsetMax = new Vector2(0, -40);

        ScrollRect scrollRect = feedScroll.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.decelerationRate = 0.135f;

        // Viewport
        GameObject viewport = CreateUIObj("Viewport", feedScroll);
        RectTransform vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = Color.white;
        Mask vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;
        scrollRect.viewport = vpRT;

        // Content
        GameObject content = CreateUIObj("Content", viewport);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);
        scrollRect.content = contentRT;

        VerticalLayoutGroup contentVLG = content.AddComponent<VerticalLayoutGroup>();
        contentVLG.spacing = 0;
        contentVLG.childForceExpandWidth = true;
        contentVLG.childForceExpandHeight = false;
        contentVLG.childControlWidth = true;
        contentVLG.childControlHeight = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Post template
        BuildPostTemplate(content);
    }

    static void BuildPostTemplate(GameObject parent)
    {
        GameObject post = CreateUIObj("PostTemplate", parent);
        Image postBG = post.AddComponent<Image>();
        postBG.color = new Color(0.06f, 0.04f, 0.12f);
        LayoutElement postLE = post.AddComponent<LayoutElement>();
        postLE.preferredHeight = 500;
        postLE.minHeight = 500;

        // Username
        GameObject username = CreateUIObj("Username", post);
        TMP_Text userTxt = username.AddComponent<TextMeshProUGUI>();
        userTxt.text = "@username";
        userTxt.fontSize = 16;
        userTxt.font = font;
        userTxt.fontStyle = FontStyles.Bold;
        userTxt.color = TextWhite;
        userTxt.alignment = TextAlignmentOptions.TopLeft;
        RectTransform userRT = username.GetComponent<RectTransform>();
        userRT.anchorMin = new Vector2(0, 0.7f);
        userRT.anchorMax = new Vector2(0.8f, 0.85f);
        userRT.offsetMin = new Vector2(15, 0);
        userRT.offsetMax = new Vector2(0, 0);

        // Description
        GameObject desc = CreateUIObj("Description", post);
        TMP_Text descTxt = desc.AddComponent<TextMeshProUGUI>();
        descTxt.text = "Post description...";
        descTxt.fontSize = 14;
        descTxt.font = font;
        descTxt.color = new Color(0.85f, 0.85f, 0.9f);
        descTxt.alignment = TextAlignmentOptions.TopLeft;
        descTxt.textWrappingMode = TextWrappingModes.Normal;
        RectTransform descRT = desc.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 0.45f);
        descRT.anchorMax = new Vector2(0.8f, 0.7f);
        descRT.offsetMin = new Vector2(15, 0);
        descRT.offsetMax = new Vector2(-10, 0);

        // Side panel (likes, comments)
        GameObject sidePanel = CreateUIObj("SidePanel", post);
        RectTransform sideRT = sidePanel.GetComponent<RectTransform>();
        sideRT.anchorMin = new Vector2(0.82f, 0.3f);
        sideRT.anchorMax = new Vector2(1, 0.8f);
        sideRT.offsetMin = Vector2.zero;
        sideRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup sideVLG = sidePanel.AddComponent<VerticalLayoutGroup>();
        sideVLG.spacing = 12;
        sideVLG.childAlignment = TextAnchor.MiddleCenter;
        sideVLG.childForceExpandWidth = true;
        sideVLG.childForceExpandHeight = false;
        sideVLG.childControlWidth = true;
        sideVLG.childControlHeight = true;

        // Like icon + count
        BuildSideIcon(sidePanel, "❤️", "LikeCount", "12.3K", AccentPink);
        BuildSideIcon(sidePanel, "💬", "CommentCount", "567", TextGray);
    }

    static void BuildSideIcon(GameObject parent, string emoji, string countName, string defaultCount, Color color)
    {
        GameObject container = CreateUIObj(countName + "Container", parent);
        LayoutElement le = container.AddComponent<LayoutElement>();
        le.preferredHeight = 50;

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;

        GameObject icon = CreateUIObj("Icon", container);
        TMP_Text iconTxt = icon.AddComponent<TextMeshProUGUI>();
        iconTxt.text = emoji;
        iconTxt.fontSize = 22;
        iconTxt.font = font;
        iconTxt.alignment = TextAlignmentOptions.Center;
        LayoutElement iconLE = icon.AddComponent<LayoutElement>();
        iconLE.preferredHeight = 28;

        GameObject count = CreateUIObj(countName, container);
        TMP_Text countTxt = count.AddComponent<TextMeshProUGUI>();
        countTxt.text = defaultCount;
        countTxt.fontSize = 12;
        countTxt.font = font;
        countTxt.color = color;
        countTxt.alignment = TextAlignmentOptions.Center;
        LayoutElement countLE = count.AddComponent<LayoutElement>();
        countLE.preferredHeight = 18;
    }

    // ==================== SHOP SCREEN ====================
    static void BuildShopScreen(GameObject parent)
    {
        GameObject shop = CreateUIObj("ShopScreen", parent);
        RectTransform shopRT = shop.GetComponent<RectTransform>();
        shopRT.anchorMin = Vector2.zero;
        shopRT.anchorMax = Vector2.one;
        shopRT.offsetMin = Vector2.zero;
        shopRT.offsetMax = Vector2.zero;
        shop.SetActive(false);

        // Top bar
        GameObject topBar = CreateUIObj("TopBar", shop);
        RectTransform topBarRT = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0, 1);
        topBarRT.anchorMax = new Vector2(1, 1);
        topBarRT.pivot = new Vector2(0.5f, 1);
        topBarRT.sizeDelta = new Vector2(0, 40);
        topBarRT.anchoredPosition = Vector2.zero;
        Image topBarImg = topBar.AddComponent<Image>();
        topBarImg.color = new Color(0, 0, 0, 0.7f);

        HorizontalLayoutGroup topHLG = topBar.AddComponent<HorizontalLayoutGroup>();
        topHLG.spacing = 8;
        topHLG.padding = new RectOffset(8, 8, 4, 4);
        topHLG.childAlignment = TextAnchor.MiddleLeft;
        topHLG.childForceExpandWidth = false;
        topHLG.childControlHeight = true;

        // Back button
        GameObject backBtn = CreateUIObj("BackButton", topBar);
        Image backImg = backBtn.AddComponent<Image>();
        backImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        Button backBtnComp = backBtn.AddComponent<Button>();
        LayoutElement backLE = backBtn.AddComponent<LayoutElement>();
        backLE.preferredWidth = 60;

        GameObject backTxt = CreateUIObj("Text", backBtn);
        TMP_Text backT = backTxt.AddComponent<TextMeshProUGUI>();
        backT.text = "← ";
        backT.fontSize = 18;
        backT.font = font;
        backT.color = TextWhite;
        backT.alignment = TextAlignmentOptions.Center;
        RectTransform backTxtRT = backTxt.GetComponent<RectTransform>();
        backTxtRT.anchorMin = Vector2.zero;
        backTxtRT.anchorMax = Vector2.one;
        backTxtRT.offsetMin = Vector2.zero;
        backTxtRT.offsetMax = Vector2.zero;

        // Title
        GameObject shopTitle = CreateUIObj("Title", topBar);
        TMP_Text shopTitleTxt = shopTitle.AddComponent<TextMeshProUGUI>();
        shopTitleTxt.text = "DoomShop";
        shopTitleTxt.fontSize = 18;
        shopTitleTxt.font = font;
        shopTitleTxt.fontStyle = FontStyles.Bold;
        shopTitleTxt.color = AccentCyan;
        shopTitleTxt.alignment = TextAlignmentOptions.Left;
        LayoutElement shopTitleLE = shopTitle.AddComponent<LayoutElement>();
        shopTitleLE.flexibleWidth = 1;

        // Shop scroll
        GameObject shopScroll = CreateUIObj("ShopScroll", shop);
        RectTransform shopScrollRT = shopScroll.GetComponent<RectTransform>();
        shopScrollRT.anchorMin = Vector2.zero;
        shopScrollRT.anchorMax = Vector2.one;
        shopScrollRT.offsetMin = new Vector2(0, 0);
        shopScrollRT.offsetMax = new Vector2(0, -40);

        ScrollRect scrollRect = shopScroll.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        // Viewport
        GameObject viewport = CreateUIObj("Viewport", shopScroll);
        RectTransform vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = Color.white;
        Mask vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;
        scrollRect.viewport = vpRT;

        // Content
        GameObject content = CreateUIObj("Content", viewport);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);
        scrollRect.content = contentRT;

        VerticalLayoutGroup contentVLG = content.AddComponent<VerticalLayoutGroup>();
        contentVLG.spacing = 6;
        contentVLG.padding = new RectOffset(8, 8, 8, 8);
        contentVLG.childForceExpandWidth = true;
        contentVLG.childForceExpandHeight = false;
        contentVLG.childControlWidth = true;
        contentVLG.childControlHeight = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Item template
        BuildShopItemTemplate(content);
    }

    static void BuildShopItemTemplate(GameObject parent)
    {
        GameObject item = CreateUIObj("ItemTemplate", parent);
        Image itemBG = item.AddComponent<Image>();
        itemBG.color = ShopItemBG;
        LayoutElement itemLE = item.AddComponent<LayoutElement>();
        itemLE.preferredHeight = 80;
        itemLE.minHeight = 80;

        // Horizontal layout for item content
        HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        // Item info (name + desc)
        GameObject infoGroup = CreateUIObj("InfoGroup", item);
        LayoutElement infoLE = infoGroup.AddComponent<LayoutElement>();
        infoLE.flexibleWidth = 1;
        infoLE.preferredWidth = 150;

        VerticalLayoutGroup infoVLG = infoGroup.AddComponent<VerticalLayoutGroup>();
        infoVLG.spacing = 2;
        infoVLG.childForceExpandWidth = true;
        infoVLG.childForceExpandHeight = false;
        infoVLG.childControlHeight = true;
        infoVLG.childAlignment = TextAnchor.MiddleLeft;

        GameObject itemName = CreateUIObj("ItemName", infoGroup);
        TMP_Text nameTxt = itemName.AddComponent<TextMeshProUGUI>();
        nameTxt.text = "📦 Item";
        nameTxt.fontSize = 16;
        nameTxt.font = font;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.color = TextWhite;
        nameTxt.alignment = TextAlignmentOptions.Left;
        LayoutElement nameLE = itemName.AddComponent<LayoutElement>();
        nameLE.preferredHeight = 24;

        GameObject itemDesc = CreateUIObj("ItemDesc", infoGroup);
        TMP_Text descTxt = itemDesc.AddComponent<TextMeshProUGUI>();
        descTxt.text = "Description";
        descTxt.fontSize = 11;
        descTxt.font = font;
        descTxt.color = TextGray;
        descTxt.alignment = TextAlignmentOptions.Left;
        LayoutElement descLE = itemDesc.AddComponent<LayoutElement>();
        descLE.preferredHeight = 18;

        // Price
        GameObject priceObj = CreateUIObj("ItemPrice", item);
        TMP_Text priceTxt = priceObj.AddComponent<TextMeshProUGUI>();
        priceTxt.text = "100 DC";
        priceTxt.fontSize = 14;
        priceTxt.font = font;
        priceTxt.fontStyle = FontStyles.Bold;
        priceTxt.color = GoldCoin;
        priceTxt.alignment = TextAlignmentOptions.Center;
        LayoutElement priceLE = priceObj.AddComponent<LayoutElement>();
        priceLE.preferredWidth = 60;

        // Buy button
        GameObject buyBtn = CreateUIObj("BuyButton", item);
        Image buyImg = buyBtn.AddComponent<Image>();
        buyImg.color = BuyBtnColor;
        Button buyBtnComp = buyBtn.AddComponent<Button>();
        ColorBlock buyCB = buyBtnComp.colors;
        buyCB.normalColor = BuyBtnColor;
        buyCB.highlightedColor = new Color(0.3f, 0.9f, 0.5f);
        buyCB.pressedColor = new Color(0.15f, 0.6f, 0.3f);
        buyBtnComp.colors = buyCB;
        LayoutElement buyLE = buyBtn.AddComponent<LayoutElement>();
        buyLE.preferredWidth = 55;

        GameObject buyTxt = CreateUIObj("Text", buyBtn);
        TMP_Text buyT = buyTxt.AddComponent<TextMeshProUGUI>();
        buyT.text = "BUY";
        buyT.fontSize = 13;
        buyT.font = font;
        buyT.fontStyle = FontStyles.Bold;
        buyT.color = new Color(0.05f, 0.05f, 0.08f);
        buyT.alignment = TextAlignmentOptions.Center;
        RectTransform buyTxtRT = buyTxt.GetComponent<RectTransform>();
        buyTxtRT.anchorMin = Vector2.zero;
        buyTxtRT.anchorMax = Vector2.one;
        buyTxtRT.offsetMin = Vector2.zero;
        buyTxtRT.offsetMax = Vector2.zero;
    }

    // ==================== HELPERS ====================
    enum AnchorPreset { TopStretch, Full }

    static GameObject CreateUIObj(string name, GameObject parent, AnchorPreset preset = AnchorPreset.Full)
    {
        GameObject obj = new GameObject(name);
        obj.layer = 5; // UI layer
        RectTransform rt = obj.AddComponent<RectTransform>();
        obj.transform.SetParent(parent.transform, false);
        return obj;
    }

    static void SetRT(GameObject obj, float left, float top, float right, float bottom, float width, float height)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchoredPosition = new Vector2(left, top);
        rt.sizeDelta = new Vector2(width, height);
    }
}
