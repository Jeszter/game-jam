using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class FixMainMenuCornerUI
{
    public static void Execute()
    {
        GameObject cornerUI = GameObject.Find("MainMenuCanvas/CornerUI");
        if (cornerUI == null)
        {
            Debug.LogError("CornerUI not found. Make sure MainMenu scene is open.");
            return;
        }

        // Make CornerUI wider so labels fit
        RectTransform cornerRT = cornerUI.GetComponent<RectTransform>();
        cornerRT.sizeDelta = new Vector2(360, 60);
        cornerRT.anchoredPosition = new Vector2(-30, -30);

        HorizontalLayoutGroup hlg = cornerUI.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.spacing = 24;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(8, 8, 4, 4);
        }

        FixCornerButton(cornerUI.transform.Find("SettingsCorner"), 150);
        FixCornerButton(cornerUI.transform.Find("ExitCorner"), 100);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("CornerUI fixed: SETTINGS label now fits on one line.");
    }

    static void FixCornerButton(Transform t, float width)
    {
        if (t == null) return;

        LayoutElement le = t.GetComponent<LayoutElement>();
        if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = width;
        le.preferredHeight = 40;
        le.minHeight = 40;

        RectTransform rt = t.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 40);

        // Ensure Label fits
        Transform lbl = t.Find("Label");
        if (lbl != null)
        {
            TMP_Text tmp = lbl.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.fontSize = 14;
            }
            RectTransform lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0, 0);
            lblRT.anchorMax = new Vector2(1, 1);
            lblRT.offsetMin = new Vector2(32, 0);
            lblRT.offsetMax = new Vector2(-4, 0);
        }
    }
}
