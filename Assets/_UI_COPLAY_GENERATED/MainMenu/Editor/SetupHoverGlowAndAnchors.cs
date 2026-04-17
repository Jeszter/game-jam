using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class SetupHoverGlowAndAnchors
{
    public static void Execute()
    {
        // ===== 1. ADD HOVER GLOW TO BUTTONS =====
        string[] buttonPaths = new string[]
        {
            "MainMenuCanvas/PhonePanel/ButtonsContainer/StartButton",
            "MainMenuCanvas/PhonePanel/ButtonsContainer/SettingsButton",
            "MainMenuCanvas/PhonePanel/ButtonsContainer/ExitButton"
        };

        foreach (string path in buttonPaths)
        {
            GameObject btnObj = GameObject.Find(path);
            if (btnObj == null)
            {
                Debug.LogWarning($"Button not found: {path}");
                continue;
            }

            // Remove existing ButtonHoverGlow if any
            ButtonHoverGlow existing = btnObj.GetComponent<ButtonHoverGlow>();
            if (existing != null)
                Object.DestroyImmediate(existing);

            // Also remove any existing HoverGlow child
            Transform existingGlow = btnObj.transform.Find("HoverGlow");
            if (existingGlow != null)
                Object.DestroyImmediate(existingGlow.gameObject);

            ButtonHoverGlow glow = btnObj.AddComponent<ButtonHoverGlow>();

            // Set glow color based on button type
            if (path.Contains("Start"))
            {
                // Red-ish glow for Start button (matches the red play icon)
                SerializedObject so = new SerializedObject(glow);
                so.FindProperty("glowColor").colorValue = new Color(0.9f, 0.2f, 0.2f, 0.12f);
                so.FindProperty("glowFadeSpeed").floatValue = 5f;
                so.FindProperty("glowExpandSize").floatValue = 15f;
                so.FindProperty("labelBrightnessBoost").floatValue = 0.2f;
                so.ApplyModifiedProperties();
            }
            else
            {
                // White subtle glow for Settings and Exit
                SerializedObject so = new SerializedObject(glow);
                so.FindProperty("glowColor").colorValue = new Color(1f, 1f, 1f, 0.08f);
                so.FindProperty("glowFadeSpeed").floatValue = 5f;
                so.FindProperty("glowExpandSize").floatValue = 15f;
                so.FindProperty("labelBrightnessBoost").floatValue = 0.2f;
                so.ApplyModifiedProperties();
            }

            Debug.Log($"Added ButtonHoverGlow to: {path}");
        }

        // ===== 2. FIX ANCHORING FOR DIFFERENT SCREEN SIZES =====

        // The CanvasScaler is already set to "Scale With Screen Size" with 1920x1080 reference
        // and matchWidthOrHeight = 0.5, which is good.

        // Fix: PhonePanel and Phone use anchorMin/Max (0,0) with absolute positions.
        // We need to convert them to relative anchors so they stay in the correct position.

        // PhonePanel - currently at absolute position (1041, 530) with anchors (0,0)-(0,0)
        // This should be anchored relative to the canvas center-right area
        GameObject phonePanelObj = GameObject.Find("MainMenuCanvas/PhonePanel");
        if (phonePanelObj != null)
        {
            RectTransform rt = phonePanelObj.GetComponent<RectTransform>();
            // Reference resolution is 1920x1080
            // Current position: anchoredPosition (1041, 530), size (285.88, 579.29)
            // Convert to relative anchors: center of element at (1041/1920, 530/1080) = (0.542, 0.491)
            float centerX = 1041f / 1920f;
            float centerY = 530f / 1080f;
            rt.anchorMin = new Vector2(centerX, centerY);
            rt.anchorMax = new Vector2(centerX, centerY);
            rt.anchoredPosition = Vector2.zero;
            Debug.Log("Fixed PhonePanel anchoring");
        }

        // Phone (the phone frame image) - currently at absolute position with anchors (0,0)-(0,0)
        GameObject phoneObj = GameObject.Find("MainMenuCanvas/Phone");
        if (phoneObj != null)
        {
            RectTransform rt = phoneObj.GetComponent<RectTransform>();
            // Current anchoredPosition: (815.34, 184.75), pivot (0,0)
            // With scale 31.35 and size 24x24, the visual size is ~752x752
            // The center of the phone image in canvas coords:
            // x = 815.34 + (24 * 31.35 * 0.5) = 815.34 + 376.2 = ~1191.5 -> but visually it's at ~712
            // Let's keep the current absolute position but anchor it relative
            float anchorX = 815.34f / 1920f;
            float anchorY = 184.75f / 1080f;
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.anchoredPosition = Vector2.zero;
            Debug.Log("Fixed Phone anchoring");
        }

        // SettingsButton (2) - the phone frame overlay at top, anchors (0,0)-(0,0)
        GameObject settingsBtn2 = GameObject.Find("MainMenuCanvas/SettingsButton (2)");
        if (settingsBtn2 != null)
        {
            RectTransform rt = settingsBtn2.GetComponent<RectTransform>();
            // Current anchoredPosition: (876, 719)
            float anchorX = 876f / 1920f;
            float anchorY = 719f / 1080f;
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.anchoredPosition = Vector2.zero;
            Debug.Log("Fixed SettingsButton (2) anchoring");
        }

        // SettingsButton (1) inside PhonePanel - anchors (0,0)-(0,0) with position (0,0)
        // This one seems to be at the bottom of the phone panel, let's check
        GameObject settingsBtn1 = GameObject.Find("MainMenuCanvas/PhonePanel/SettingsButton (1)");
        if (settingsBtn1 != null)
        {
            RectTransform rt = settingsBtn1.GetComponent<RectTransform>();
            // It's inside PhonePanel which has size (285.88, 579.29)
            // Currently at (0,0) with anchors (0,0). It should be anchored to bottom-center of PhonePanel
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            Debug.Log("Fixed SettingsButton (1) anchoring");
        }

        // IconImage (1) inside PhonePanel - camera/notch icon at top
        GameObject iconImage1 = GameObject.Find("MainMenuCanvas/PhonePanel/IconImage (1)");
        if (iconImage1 != null)
        {
            RectTransform rt = iconImage1.GetComponent<RectTransform>();
            // Currently anchored to (0, 0.5) with position (36.24, 44.2)
            // Should be anchored to top-center of PhonePanel
            // PhonePanel height is 579.29, so 44.2 from center = top area
            // Let's anchor to top-center
            float parentHeight = 579.29f;
            float relY = (parentHeight / 2f + 44.2f) / parentHeight; // ~0.576
            rt.anchorMin = new Vector2(0.127f, relY);
            rt.anchorMax = new Vector2(0.127f, relY);
            rt.anchoredPosition = Vector2.zero;
            Debug.Log("Fixed IconImage (1) anchoring");
        }

        // IconImage (2) - the title/logo image, currently at (-18.86, 357) with anchor (0, 0.5)
        // This is the DOOM logo in the top-left. It should stay top-left.
        // TitleGroup is already anchored to top-left (0,1)-(0,1) which is correct.
        // IconImage (2) is a direct child of MainMenuCanvas, not TitleGroup
        GameObject iconImage2 = GameObject.Find("MainMenuCanvas/IconImage (2)");
        if (iconImage2 != null)
        {
            RectTransform rt = iconImage2.GetComponent<RectTransform>();
            // Currently at (-18.86, 357) with anchor (0, 0.5)
            // In canvas coords: x = -18.86, y = 540 + 357 = 897 (from bottom)
            // Should be anchored to top-left area
            float anchorX = 0f; // left edge
            float anchorY = (540f + 357f) / 1080f; // ~0.831
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.anchoredPosition = new Vector2(-18.86f, 0f);
            Debug.Log("Fixed IconImage (2) anchoring");
        }

        // Mark scene dirty so changes are saved
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("=== Setup complete: Hover glow added to buttons, anchoring fixed ===");
    }
}
