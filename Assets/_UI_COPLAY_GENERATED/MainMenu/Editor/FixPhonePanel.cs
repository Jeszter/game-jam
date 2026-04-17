using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class FixPhonePanel
{
    public static void Execute()
    {
        // The Phone image: anchoredPosition (815.34, 184.75), sizeDelta 24x24, scale 31.35, pivot (0,0), rotation Z=354.6119 (=-5.39)
        // Actual rendered size: 24*31.35 = 752.3 x 752.3
        // Since preserveAspect=true and the phone image is taller than wide, the actual phone shape is narrower
        // The phone screen area is roughly the inner part of the phone image
        
        // Let me find the Phone object and compute where the screen area is
        GameObject phone = GameObject.Find("MainMenuCanvas/Phone");
        if (phone == null) { Debug.LogError("Phone not found"); return; }
        
        RectTransform phoneRT = phone.GetComponent<RectTransform>();
        
        // Phone actual pixel size = sizeDelta * localScale = 24 * 31.35 = ~752
        float phonePixelSize = phoneRT.sizeDelta.x * phoneRT.localScale.x; // 752
        
        // The phone image is square (24x24) but preserveAspect is on
        // The phone texture shows a phone that's roughly 45% width and 90% height of the image
        // The screen area within the phone is roughly 38% width and 70% height
        
        // Phone center in canvas local coords:
        // pivot is (0,0), so center = anchoredPosition + (sizeDelta * scale) / 2
        // But we need to account for the anchor being at (0,0) of the canvas
        float halfSize = phonePixelSize / 2f;
        Vector2 phoneCenter = phoneRT.anchoredPosition + new Vector2(halfSize, halfSize);
        
        Debug.Log($"Phone center: {phoneCenter}, pixel size: {phonePixelSize}");
        
        // Now adjust PhonePanel
        GameObject phonePanel = GameObject.Find("MainMenuCanvas/PhonePanel");
        if (phonePanel == null) { Debug.LogError("PhonePanel not found"); return; }
        
        RectTransform panelRT = phonePanel.GetComponent<RectTransform>();
        
        // The phone screen area within the phone image:
        // Looking at the reference, the screen is about 40% of the total image width and 75% of height
        // The screen is centered horizontally and slightly above center vertically
        float screenWidth = phonePixelSize * 0.36f;   // ~271
        float screenHeight = phonePixelSize * 0.72f;  // ~542
        
        // The screen center is offset slightly up from the phone center
        Vector2 screenCenter = phoneCenter + new Vector2(0, phonePixelSize * 0.02f);
        
        // PhonePanel uses anchor (0.5, 0.5) of the canvas, so anchoredPosition is relative to canvas center
        // Canvas reference resolution is 1920x1080, so canvas center is (960, 540)
        // But the canvas anchors are at center, so anchoredPosition = screenCenter - canvasCenter
        // Actually, PhonePanel anchor is (0.5, 0.5), so anchoredPosition is offset from canvas center
        // Canvas center in local coords = (1920/2, 1080/2) = (960, 540)
        // Wait - the canvas uses CanvasScaler with reference 1920x1080
        // The Phone uses anchor (0,0) so its position is from bottom-left
        // PhonePanel uses anchor (0.5, 0.5) so its position is from center
        
        // Canvas local center = (960, 540) (half of reference resolution)
        Vector2 canvasCenter = new Vector2(960, 540);
        Vector2 panelPos = screenCenter - canvasCenter;
        
        Debug.Log($"Screen center: {screenCenter}, Panel pos: {panelPos}, Screen size: {screenWidth}x{screenHeight}");
        
        panelRT.anchoredPosition = panelPos;
        panelRT.sizeDelta = new Vector2(screenWidth, screenHeight);
        
        // Match the phone rotation
        panelRT.localRotation = phoneRT.localRotation;
        panelRT.localScale = Vector3.one;
        
        // Fix buttons container - make it wider so text doesn't get cut off
        Transform buttonsContainer = phonePanel.transform.Find("PhoneContent/ButtonsContainer");
        if (buttonsContainer != null)
        {
            RectTransform buttonsRT = buttonsContainer.GetComponent<RectTransform>();
            buttonsRT.anchorMin = new Vector2(0, 0.3f);
            buttonsRT.anchorMax = new Vector2(0.75f, 0.88f);
            buttonsRT.offsetMin = new Vector2(10, 0);
            buttonsRT.offsetMax = new Vector2(0, 0);
        }
        
        // Fix social bar - make it narrower and properly positioned
        Transform socialBar = phonePanel.transform.Find("PhoneContent/SocialBar");
        if (socialBar != null)
        {
            RectTransform socialRT = socialBar.GetComponent<RectTransform>();
            socialRT.anchorMin = new Vector2(0.8f, 0.08f);
            socialRT.anchorMax = new Vector2(1f, 0.6f);
            socialRT.offsetMin = new Vector2(-5, 0);
            socialRT.offsetMax = new Vector2(-3, 0);
        }
        
        // Fix social items size
        FixSocialItemSize("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/HeartIcon", 40, 48);
        FixSocialItemSize("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/CommentIcon", 40, 48);
        FixSocialItemSize("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/ShareIcon", 40, 48);
        
        // Fix nav bar height
        Transform navBar = phonePanel.transform.Find("NavBar");
        if (navBar != null)
        {
            RectTransform navRT = navBar.GetComponent<RectTransform>();
            navRT.sizeDelta = new Vector2(0, 40);
            
            HorizontalLayoutGroup hlg = navBar.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.padding = new RectOffset(5, 5, 4, 4);
            }
        }
        
        // Fix status bar height
        Transform statusBar = phonePanel.transform.Find("StatusBar");
        if (statusBar != null)
        {
            RectTransform statusRT = statusBar.GetComponent<RectTransform>();
            statusRT.sizeDelta = new Vector2(0, 24);
        }
        
        // Fix phone content offsets to match new bar sizes
        Transform phoneContent = phonePanel.transform.Find("PhoneContent");
        if (phoneContent != null)
        {
            RectTransform contentRT = phoneContent.GetComponent<RectTransform>();
            contentRT.offsetMin = new Vector2(0, 40); // above nav bar
            contentRT.offsetMax = new Vector2(0, -24); // below status bar
        }
        
        // Fix button font sizes to be smaller
        FixButtonFontSize("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/StartButton/Label", 20);
        FixButtonFontSize("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/SettingsButton/Label", 18);
        FixButtonFontSize("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/ExitButton/Label", 18);
        
        // Fix button heights
        FixButtonHeight("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/StartButton", 48);
        FixButtonHeight("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/SettingsButton", 42);
        FixButtonHeight("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/ExitButton", 42);
        
        // Fix icon sizes in buttons
        FixIconSize("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/StartButton/IconImage", 22, 22, 10);
        FixIconSize("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/SettingsButton/IconImage", 20, 20, 10);
        FixIconSize("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/ExitButton/IconImage", 20, 20, 10);
        
        // Fix social icon sizes
        FixSocialIconSize("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/HeartIcon/Icon", 22);
        FixSocialIconSize("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/CommentIcon/Icon", 22);
        FixSocialIconSize("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/ShareIcon/Icon", 22);
        
        // Fix social count font sizes
        FixCountFontSize("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/HeartIcon/Count", 8);
        FixCountFontSize("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/CommentIcon/Count", 8);
        FixCountFontSize("MainMenuCanvas/PhonePanel/PhoneContent/SocialBar/ShareIcon/Count", 8);
        
        // Fix time text font size
        Transform timeText = phonePanel.transform.Find("StatusBar/TimeText");
        if (timeText != null)
        {
            TextMeshProUGUI tmp = timeText.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.fontSize = 10;
        }
        
        // Fix signal text font size
        Transform signalText = phonePanel.transform.Find("StatusBar/SignalText");
        if (signalText != null)
        {
            TextMeshProUGUI tmp = signalText.GetComponent<TextMeshProUGUI>();
            if (tmp != null) { tmp.fontSize = 8; tmp.text = "|||  o  ||"; }
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log("PhonePanel positioning and sizing fixed!");
    }
    
    static void FixSocialItemSize(string path, float w, float h)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
    }
    
    static void FixButtonFontSize(string path, float size)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.fontSize = size;
    }
    
    static void FixButtonHeight(string path, float height)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le != null) { le.preferredHeight = height; le.minHeight = height; }
    }
    
    static void FixIconSize(string path, float w, float h, float xPos)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(xPos, 0);
    }
    
    static void FixSocialIconSize(string path, float size)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;
    }
    
    static void FixCountFontSize(string path, float size)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.fontSize = size;
        
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40, 14);
        
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le != null) { le.preferredWidth = 40; le.preferredHeight = 14; }
    }
}
