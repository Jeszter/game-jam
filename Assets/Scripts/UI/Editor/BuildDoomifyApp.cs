using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class BuildDoomifyApp
{
    static TMP_FontAsset font;
    static readonly Color GreenAccent = new Color(0.3f, 1f, 0.4f);
    static readonly Color DarkBG = new Color(0.06f, 0.06f, 0.09f);
    static readonly Color ItemBG = new Color(0.1f, 0.1f, 0.14f, 1f);
    static readonly Color TextWhite = new Color(0.95f, 0.95f, 0.95f);
    static readonly Color TextGray = new Color(0.55f, 0.55f, 0.6f);
    static readonly Color ControlBG = new Color(0.08f, 0.08f, 0.11f, 0.95f);

    public static void Execute()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        GameObject canvas = GameObject.Find("GameUICanvas");
        if (canvas == null) { Debug.LogError("GameUICanvas not found"); return; }

        Transform phoneScreen = canvas.transform.Find("PhonePanel/PhoneScreen");
        if (phoneScreen == null) { Debug.LogError("PhoneScreen not found"); return; }

        // ===== 1. ADD DOOMIFY APP BUTTON TO HOME SCREEN =====
        Transform appGrid = phoneScreen.Find("HomeScreen/AppGrid");
        if (appGrid != null)
        {
            // Remove old if exists
            Transform oldBtn = appGrid.Find("DoomifyAppBtn");
            if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);

            BuildAppButton(appGrid.gameObject, "DoomifyAppBtn", "~", "Doomify", GreenAccent);
        }

        // ===== 2. BUILD DOOMIFY SCREEN =====
        // Remove old
        Transform oldScreen = phoneScreen.Find("DoomifyScreen");
        if (oldScreen != null) Object.DestroyImmediate(oldScreen.gameObject);

        GameObject doomify = CreateUI("DoomifyScreen", phoneScreen.gameObject);
        RectTransform doomRT = doomify.GetComponent<RectTransform>();
        doomRT.anchorMin = Vector2.zero;
        doomRT.anchorMax = Vector2.one;
        doomRT.offsetMin = Vector2.zero;
        doomRT.offsetMax = Vector2.zero;
        doomify.SetActive(false);

        // --- TOP BAR ---
        GameObject topBar = CreateUI("TopBar", doomify);
        RectTransform topBarRT = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0, 1);
        topBarRT.anchorMax = new Vector2(1, 1);
        topBarRT.pivot = new Vector2(0.5f, 1);
        topBarRT.sizeDelta = new Vector2(0, 38);
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
        GameObject backBtn = CreateUI("BackButton", topBar);
        Image backImg = backBtn.AddComponent<Image>();
        backImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        backBtn.AddComponent<Button>();
        LayoutElement backLE = backBtn.AddComponent<LayoutElement>();
        backLE.preferredWidth = 50;

        GameObject backTxt = CreateUI("Text", backBtn);
        TMP_Text backT = backTxt.AddComponent<TextMeshProUGUI>();
        backT.text = "<-";
        backT.fontSize = 16;
        backT.font = font;
        backT.color = TextWhite;
        backT.alignment = TextAlignmentOptions.Center;
        StretchFill(backTxt);

        // Title
        GameObject titleObj = CreateUI("Title", topBar);
        TMP_Text titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "Doomify";
        titleTxt.fontSize = 17;
        titleTxt.font = font;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = GreenAccent;
        titleTxt.alignment = TextAlignmentOptions.Left;
        LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 1;

        // --- NOW PLAYING SECTION (bottom) ---
        GameObject playerBar = CreateUI("PlayerBar", doomify);
        RectTransform playerRT = playerBar.GetComponent<RectTransform>();
        playerRT.anchorMin = new Vector2(0, 0);
        playerRT.anchorMax = new Vector2(1, 0);
        playerRT.pivot = new Vector2(0.5f, 0);
        playerRT.sizeDelta = new Vector2(0, 120);
        playerRT.anchoredPosition = Vector2.zero;
        Image playerBG = playerBar.AddComponent<Image>();
        playerBG.color = ControlBG;

        // Progress bar (thin line at top of player)
        GameObject progressBG = CreateUI("ProgressBG", playerBar);
        RectTransform progBGRT = progressBG.GetComponent<RectTransform>();
        progBGRT.anchorMin = new Vector2(0, 1);
        progBGRT.anchorMax = new Vector2(1, 1);
        progBGRT.pivot = new Vector2(0.5f, 1);
        progBGRT.sizeDelta = new Vector2(-16, 4);
        progBGRT.anchoredPosition = new Vector2(0, -6);
        Image progBGImg = progressBG.AddComponent<Image>();
        progBGImg.color = new Color(0.2f, 0.2f, 0.25f);

        GameObject progressFill = CreateUI("ProgressFill", progressBG);
        Image progFillImg = progressFill.AddComponent<Image>();
        progFillImg.color = GreenAccent;
        progFillImg.type = Image.Type.Filled;
        progFillImg.fillMethod = Image.FillMethod.Horizontal;
        progFillImg.fillAmount = 0f;
        RectTransform progFillRT = progressFill.GetComponent<RectTransform>();
        progFillRT.anchorMin = Vector2.zero;
        progFillRT.anchorMax = Vector2.one;
        progFillRT.offsetMin = Vector2.zero;
        progFillRT.offsetMax = Vector2.zero;

        // Now playing info
        GameObject nowPlayingInfo = CreateUI("NowPlayingInfo", playerBar);
        RectTransform npInfoRT = nowPlayingInfo.GetComponent<RectTransform>();
        npInfoRT.anchorMin = new Vector2(0, 0.45f);
        npInfoRT.anchorMax = new Vector2(1, 0.85f);
        npInfoRT.offsetMin = new Vector2(12, 0);
        npInfoRT.offsetMax = new Vector2(-12, 0);

        VerticalLayoutGroup npVLG = nowPlayingInfo.AddComponent<VerticalLayoutGroup>();
        npVLG.spacing = 1;
        npVLG.childAlignment = TextAnchor.MiddleCenter;
        npVLG.childForceExpandWidth = true;
        npVLG.childForceExpandHeight = true;
        npVLG.childControlHeight = true;

        GameObject npTitle = CreateUI("NowPlayingTitle", nowPlayingInfo);
        TMP_Text npTitleTxt = npTitle.AddComponent<TextMeshProUGUI>();
        npTitleTxt.text = "No track";
        npTitleTxt.fontSize = 13;
        npTitleTxt.font = font;
        npTitleTxt.fontStyle = FontStyles.Bold;
        npTitleTxt.color = TextWhite;
        npTitleTxt.alignment = TextAlignmentOptions.Center;
        npTitleTxt.overflowMode = TextOverflowModes.Ellipsis;

        GameObject npArtist = CreateUI("NowPlayingArtist", nowPlayingInfo);
        TMP_Text npArtistTxt = npArtist.AddComponent<TextMeshProUGUI>();
        npArtistTxt.text = "Select a song";
        npArtistTxt.fontSize = 11;
        npArtistTxt.font = font;
        npArtistTxt.color = TextGray;
        npArtistTxt.alignment = TextAlignmentOptions.Center;

        // Controls row
        GameObject controls = CreateUI("Controls", playerBar);
        RectTransform ctrlRT = controls.GetComponent<RectTransform>();
        ctrlRT.anchorMin = new Vector2(0.1f, 0.05f);
        ctrlRT.anchorMax = new Vector2(0.9f, 0.45f);
        ctrlRT.offsetMin = Vector2.zero;
        ctrlRT.offsetMax = Vector2.zero;

        HorizontalLayoutGroup ctrlHLG = controls.AddComponent<HorizontalLayoutGroup>();
        ctrlHLG.spacing = 6;
        ctrlHLG.childAlignment = TextAnchor.MiddleCenter;
        ctrlHLG.childForceExpandWidth = true;
        ctrlHLG.childForceExpandHeight = true;
        ctrlHLG.childControlWidth = true;
        ctrlHLG.childControlHeight = true;

        // Prev button
        BuildControlButton(controls, "PrevButton", "|<", TextWhite);
        // Play/Pause button
        BuildControlButton(controls, "PlayPauseButton", ">", GreenAccent);
        // Next button
        BuildControlButton(controls, "NextButton", ">|", TextWhite);

        // --- VOLUME SECTION (between top bar and track list) ---
        GameObject volumeSection = CreateUI("VolumeSection", doomify);
        RectTransform volRT = volumeSection.GetComponent<RectTransform>();
        volRT.anchorMin = new Vector2(0, 1);
        volRT.anchorMax = new Vector2(1, 1);
        volRT.pivot = new Vector2(0.5f, 1);
        volRT.sizeDelta = new Vector2(0, 35);
        volRT.anchoredPosition = new Vector2(0, -38);

        HorizontalLayoutGroup volHLG = volumeSection.AddComponent<HorizontalLayoutGroup>();
        volHLG.spacing = 6;
        volHLG.padding = new RectOffset(12, 12, 4, 4);
        volHLG.childAlignment = TextAnchor.MiddleCenter;
        volHLG.childForceExpandWidth = false;
        volHLG.childForceExpandHeight = true;
        volHLG.childControlWidth = true;
        volHLG.childControlHeight = true;

        // Volume icon
        GameObject volIcon = CreateUI("VolIcon", volumeSection);
        TMP_Text volIconTxt = volIcon.AddComponent<TextMeshProUGUI>();
        volIconTxt.text = "Vol";
        volIconTxt.fontSize = 11;
        volIconTxt.font = font;
        volIconTxt.color = TextGray;
        volIconTxt.alignment = TextAlignmentOptions.Center;
        LayoutElement volIconLE = volIcon.AddComponent<LayoutElement>();
        volIconLE.preferredWidth = 28;

        // Volume slider
        GameObject sliderObj = BuildVolumeSlider(volumeSection);

        // Volume text
        GameObject volText = CreateUI("VolumeText", volumeSection);
        TMP_Text volTxt = volText.AddComponent<TextMeshProUGUI>();
        volTxt.text = "50%";
        volTxt.fontSize = 11;
        volTxt.font = font;
        volTxt.color = TextGray;
        volTxt.alignment = TextAlignmentOptions.Center;
        LayoutElement volTxtLE = volText.AddComponent<LayoutElement>();
        volTxtLE.preferredWidth = 35;

        // --- TRACK LIST (scrollable) ---
        GameObject trackScroll = CreateUI("TrackScroll", doomify);
        RectTransform tsRT = trackScroll.GetComponent<RectTransform>();
        tsRT.anchorMin = Vector2.zero;
        tsRT.anchorMax = Vector2.one;
        tsRT.offsetMin = new Vector2(0, 120); // above player bar
        tsRT.offsetMax = new Vector2(0, -73); // below top bar + volume

        ScrollRect scrollRect = trackScroll.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        GameObject viewport = CreateUI("Viewport", trackScroll);
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

        GameObject content = CreateUI("Content", viewport);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);
        scrollRect.content = contentRT;

        VerticalLayoutGroup contentVLG = content.AddComponent<VerticalLayoutGroup>();
        contentVLG.spacing = 3;
        contentVLG.padding = new RectOffset(6, 6, 6, 6);
        contentVLG.childForceExpandWidth = true;
        contentVLG.childForceExpandHeight = false;
        contentVLG.childControlWidth = true;
        contentVLG.childControlHeight = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Track item template
        BuildTrackTemplate(content);

        // ===== 3. WIRE UP DOOMIFY CONTROLLER =====
        DoomifyController doomCtrl = doomify.AddComponent<DoomifyController>();

        // Create AudioSource on the canvas (persists when screen is hidden)
        AudioSource audioSrc = canvas.GetComponent<AudioSource>();
        if (audioSrc == null)
        {
            audioSrc = canvas.AddComponent<AudioSource>();
        }
        // Configure for music
        audioSrc.playOnAwake = false;
        audioSrc.loop = false;
        audioSrc.spatialBlend = 0f;
        audioSrc.volume = 0.5f;

        SerializedObject doomSO = new SerializedObject(doomCtrl);
        doomSO.FindProperty("audioSource").objectReferenceValue = audioSrc;
        doomSO.FindProperty("trackListContainer").objectReferenceValue = contentRT;
        doomSO.FindProperty("trackItemTemplate").objectReferenceValue = content.transform.Find("TrackTemplate").gameObject;
        doomSO.FindProperty("nowPlayingTitle").objectReferenceValue = npTitleTxt;
        doomSO.FindProperty("nowPlayingArtist").objectReferenceValue = npArtistTxt;
        doomSO.FindProperty("playPauseButton").objectReferenceValue = controls.transform.Find("PlayPauseButton").GetComponent<Button>();
        doomSO.FindProperty("playPauseIcon").objectReferenceValue = controls.transform.Find("PlayPauseButton/Text").GetComponent<TMP_Text>();
        doomSO.FindProperty("prevButton").objectReferenceValue = controls.transform.Find("PrevButton").GetComponent<Button>();
        doomSO.FindProperty("nextButton").objectReferenceValue = controls.transform.Find("NextButton").GetComponent<Button>();
        doomSO.FindProperty("volumeSlider").objectReferenceValue = sliderObj.GetComponent<Slider>();
        doomSO.FindProperty("volumeText").objectReferenceValue = volTxt;
        doomSO.FindProperty("progressBar").objectReferenceValue = progFillImg;

        // Load tracks
        string[] trackFiles = new string[]
        {
            "Assets/Sound/Doomify/bfcmusic-sad-violin-music-(clasic).mp3",
            "Assets/Sound/Doomify/freesound_community-rock-music-6211.mp3",
            "Assets/Sound/Doomify/idoberg-melodic-techno-loop-408268(1).mp3",
            "Assets/Sound/Doomify/sigma_phonk_music-midnight-velocity-phonk-488566.mp3",
            "Assets/Sound/Doomify/u_uphdku5hhu-chinese-newyears-song-480472.mp3",
            "Assets/Sound/Doomify/white_records-breath-of-autumn-instrumental-background-music-for-video-35-sec-487274.mp3"
        };

        string[] trackTitles = new string[]
        {
            "Sad Violin",
            "Rock Music",
            "Melodic Techno",
            "Midnight Velocity",
            "Chinese New Year",
            "Breath of Autumn"
        };

        string[] trackArtists = new string[]
        {
            "BFC Music",
            "Freesound Community",
            "Idoberg",
            "Sigma Phonk",
            "UPHDKU5HHU",
            "White Records"
        };

        SerializedProperty tracksProp = doomSO.FindProperty("tracks");
        tracksProp.arraySize = trackFiles.Length;

        for (int i = 0; i < trackFiles.Length; i++)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(trackFiles[i]);
            SerializedProperty trackElem = tracksProp.GetArrayElementAtIndex(i);
            trackElem.FindPropertyRelative("title").stringValue = trackTitles[i];
            trackElem.FindPropertyRelative("artist").stringValue = trackArtists[i];
            trackElem.FindPropertyRelative("clip").objectReferenceValue = clip;

            if (clip == null)
                Debug.LogWarning($"Track not found: {trackFiles[i]}");
        }

        doomSO.ApplyModifiedProperties();

        // ===== 4. UPDATE PHONE CONTROLLER — add Doomify screen =====
        GamePhoneController phoneCtrl = canvas.GetComponent<GamePhoneController>();
        if (phoneCtrl != null)
        {
            // We need to add Doomify to the phone controller
            // Since the controller only has Home/TikTok/Shop, we'll extend it via a helper
            DoomifyPhoneBridge bridge = canvas.GetComponent<DoomifyPhoneBridge>();
            if (bridge == null) bridge = canvas.AddComponent<DoomifyPhoneBridge>();

            SerializedObject bridgeSO = new SerializedObject(bridge);
            bridgeSO.FindProperty("phoneController").objectReferenceValue = phoneCtrl;
            bridgeSO.FindProperty("doomifyScreen").objectReferenceValue = doomify;
            bridgeSO.FindProperty("doomifyButton").objectReferenceValue = appGrid.Find("DoomifyAppBtn").GetComponent<Button>();
            bridgeSO.FindProperty("backButton").objectReferenceValue = topBar.transform.Find("BackButton").GetComponent<Button>();
            bridgeSO.FindProperty("homeScreen").objectReferenceValue = phoneScreen.Find("HomeScreen").gameObject;
            bridgeSO.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("=== Doomify app built! 6 tracks loaded. ===");
    }

    static void BuildAppButton(GameObject parent, string name, string icon, string label, Color accent)
    {
        GameObject btn = CreateUI(name, parent);
        Image btnImg = btn.AddComponent<Image>();
        btnImg.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);
        Button btnComp = btn.AddComponent<Button>();
        ColorBlock cb = btnComp.colors;
        cb.normalColor = new Color(0.08f, 0.08f, 0.12f, 0.9f);
        cb.highlightedColor = new Color(accent.r * 0.3f, accent.g * 0.3f, accent.b * 0.3f, 1f);
        cb.pressedColor = new Color(accent.r * 0.5f, accent.g * 0.5f, accent.b * 0.5f, 1f);
        btnComp.colors = cb;

        GameObject iconObj = CreateUI("Icon", btn);
        TMP_Text iconTxt = iconObj.AddComponent<TextMeshProUGUI>();
        iconTxt.text = $"<size=32><color=#{ColorUtility.ToHtmlStringRGB(accent)}>{icon}</color></size>";
        iconTxt.fontSize = 32;
        iconTxt.font = font;
        iconTxt.richText = true;
        iconTxt.alignment = TextAlignmentOptions.Center;
        RectTransform iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.35f);
        iconRT.anchorMax = new Vector2(1, 1);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;

        GameObject labelObj = CreateUI("Label", btn);
        TMP_Text labelTxt = labelObj.AddComponent<TextMeshProUGUI>();
        labelTxt.text = label;
        labelTxt.fontSize = 14;
        labelTxt.font = font;
        labelTxt.fontStyle = FontStyles.Bold;
        labelTxt.color = accent;
        labelTxt.alignment = TextAlignmentOptions.Center;
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 0.35f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
    }

    static void BuildControlButton(GameObject parent, string name, string symbol, Color color)
    {
        GameObject btn = CreateUI(name, parent);
        Image btnImg = btn.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        Button btnComp = btn.AddComponent<Button>();
        ColorBlock cb = btnComp.colors;
        cb.normalColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        cb.highlightedColor = new Color(0.2f, 0.25f, 0.2f, 1f);
        cb.pressedColor = new Color(0.1f, 0.3f, 0.1f, 1f);
        btnComp.colors = cb;

        GameObject txt = CreateUI("Text", btn);
        TMP_Text t = txt.AddComponent<TextMeshProUGUI>();
        t.text = symbol;
        t.fontSize = 16;
        t.font = font;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        StretchFill(txt);
    }

    static void BuildTrackTemplate(GameObject parent)
    {
        GameObject track = CreateUI("TrackTemplate", parent);
        Image trackBG = track.AddComponent<Image>();
        trackBG.color = ItemBG;
        LayoutElement trackLE = track.AddComponent<LayoutElement>();
        trackLE.preferredHeight = 52;
        trackLE.minHeight = 52;

        // Info section
        VerticalLayoutGroup trackVLG = track.AddComponent<VerticalLayoutGroup>();
        trackVLG.spacing = 1;
        trackVLG.padding = new RectOffset(12, 12, 6, 6);
        trackVLG.childAlignment = TextAnchor.MiddleLeft;
        trackVLG.childForceExpandWidth = true;
        trackVLG.childForceExpandHeight = true;
        trackVLG.childControlHeight = true;

        GameObject trackTitle = CreateUI("TrackTitle", track);
        TMP_Text titleTxt = trackTitle.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "Track Name";
        titleTxt.fontSize = 13;
        titleTxt.font = font;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = TextWhite;
        titleTxt.alignment = TextAlignmentOptions.Left;
        titleTxt.overflowMode = TextOverflowModes.Ellipsis;

        GameObject trackArtist = CreateUI("TrackArtist", track);
        TMP_Text artistTxt = trackArtist.AddComponent<TextMeshProUGUI>();
        artistTxt.text = "Artist";
        artistTxt.fontSize = 10;
        artistTxt.font = font;
        artistTxt.color = TextGray;
        artistTxt.alignment = TextAlignmentOptions.Left;
    }

    static GameObject BuildVolumeSlider(GameObject parent)
    {
        GameObject sliderObj = CreateUI("VolumeSlider", parent);
        LayoutElement sliderLE = sliderObj.AddComponent<LayoutElement>();
        sliderLE.flexibleWidth = 1;
        sliderLE.preferredHeight = 20;

        // Background
        GameObject bg = CreateUI("Background", sliderObj);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.35f);
        bgRT.anchorMax = new Vector2(1, 0.65f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Fill area
        GameObject fillArea = CreateUI("Fill Area", sliderObj);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.35f);
        fillAreaRT.anchorMax = new Vector2(1, 0.65f);
        fillAreaRT.offsetMin = Vector2.zero;
        fillAreaRT.offsetMax = Vector2.zero;

        GameObject fill = CreateUI("Fill", fillArea);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = GreenAccent;
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // Handle area
        GameObject handleArea = CreateUI("Handle Slide Area", sliderObj);
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = Vector2.zero;
        handleAreaRT.offsetMax = Vector2.zero;

        GameObject handle = CreateUI("Handle", handleArea);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(14, 14);

        // Slider component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0.5f;

        return sliderObj;
    }

    static void StretchFill(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static GameObject CreateUI(string name, GameObject parent)
    {
        GameObject obj = new GameObject(name);
        obj.layer = 5;
        obj.AddComponent<RectTransform>();
        obj.transform.SetParent(parent.transform, false);
        return obj;
    }
}
