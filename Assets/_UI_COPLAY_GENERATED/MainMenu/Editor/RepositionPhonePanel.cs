using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class RepositionPhonePanel
{
    public static void Execute()
    {
        GameObject canvas = GameObject.Find("MainMenuCanvas");
        GameObject phone = GameObject.Find("MainMenuCanvas/Phone");
        GameObject phonePanel = GameObject.Find("MainMenuCanvas/PhonePanel");
        
        if (canvas == null || phone == null || phonePanel == null)
        {
            Debug.LogError("Required objects not found!");
            return;
        }
        
        RectTransform phoneRT = phone.GetComponent<RectTransform>();
        RectTransform panelRT = phonePanel.GetComponent<RectTransform>();
        
        // Phone: anchor(0,0), pivot(0,0), anchoredPos(815.34, 184.75), size(24,24), scale(31.35), rot Z=-5.39
        // The phone image is 24x24 units * 31.35 scale = ~752x752 pixels
        // But preserveAspect is on, so the actual phone shape fits within that square
        
        // Looking at the phone texture, the phone body occupies roughly:
        // - Horizontally: centered, about 45% of the image width
        // - Vertically: centered, about 95% of the image height
        // The SCREEN area within the phone body is roughly:
        // - Horizontally: centered, about 38% of the image width  
        // - Vertically: from about 8% to 88% of the image height (80% of height)
        
        // Strategy: Make PhonePanel use the same anchor/pivot system as Phone
        // Set anchor to (0,0), pivot to center of screen area relative to phone
        
        // Actually, the simplest approach: match PhonePanel's anchor, pivot, position, rotation to Phone
        // but with adjusted size and offset to represent just the screen area
        
        float phoneSize = 24f; // sizeDelta
        float phoneScale = phoneRT.localScale.x; // 31.35
        float phonePixels = phoneSize * phoneScale; // ~752
        
        // Screen area within the phone image (normalized 0-1 of the 24x24 image):
        // The phone body in the image is roughly centered
        // Screen left edge: ~0.31, right edge: ~0.69 (width ~0.38)
        // Screen bottom edge: ~0.08, top edge: ~0.88 (height ~0.80)
        float screenLeft = 0.31f;
        float screenRight = 0.69f;
        float screenBottom = 0.10f;
        float screenTop = 0.87f;
        
        float screenW = (screenRight - screenLeft) * phoneSize; // in phone local units
        float screenH = (screenTop - screenBottom) * phoneSize;
        float screenCenterX = (screenLeft + screenRight) / 2f * phoneSize;
        float screenCenterY = (screenBottom + screenTop) / 2f * phoneSize;
        
        Debug.Log($"Screen area in phone local: center({screenCenterX}, {screenCenterY}), size({screenW}, {screenH})");
        
        // Set PhonePanel to use same anchor as Phone (0,0)
        panelRT.anchorMin = new Vector2(0, 0);
        panelRT.anchorMax = new Vector2(0, 0);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        
        // The PhonePanel needs to be at the same position as the phone screen center
        // Phone's local position places its (0,0) corner at anchoredPosition
        // The screen center in phone-local coords is (screenCenterX, screenCenterY)
        // But phone has rotation, so we need to rotate that offset
        
        float rotZ = phoneRT.localEulerAngles.z; // 354.6119 = -5.39 degrees
        float radZ = rotZ * Mathf.Deg2Rad;
        
        // Screen center offset from phone's pivot (0,0) in phone local space
        Vector2 localOffset = new Vector2(screenCenterX, screenCenterY);
        
        // Scale by phone's scale
        Vector2 scaledOffset = localOffset * phoneScale;
        
        // Rotate the offset
        float cos = Mathf.Cos(radZ);
        float sin = Mathf.Sin(radZ);
        Vector2 rotatedOffset = new Vector2(
            scaledOffset.x * cos - scaledOffset.y * sin,
            scaledOffset.x * sin + scaledOffset.y * cos
        );
        
        // Final position = phone anchor position + rotated offset
        Vector2 finalPos = phoneRT.anchoredPosition + rotatedOffset;
        
        panelRT.anchoredPosition = finalPos;
        panelRT.sizeDelta = new Vector2(screenW * phoneScale, screenH * phoneScale);
        panelRT.localRotation = phoneRT.localRotation;
        panelRT.localScale = Vector3.one;
        
        Debug.Log($"PhonePanel: pos({finalPos.x}, {finalPos.y}), size({screenW * phoneScale}, {screenH * phoneScale}), rot({rotZ})");
        
        // Update PhoneContent offsets
        Transform phoneContent = phonePanel.transform.Find("PhoneContent");
        if (phoneContent != null)
        {
            RectTransform contentRT = phoneContent.GetComponent<RectTransform>();
            contentRT.offsetMin = new Vector2(0, 40);
            contentRT.offsetMax = new Vector2(0, -24);
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log("PhonePanel repositioned to match phone screen area!");
    }
}
