using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class FixHUDLayout
{
    [MenuItem("Tools/Fix HUD Layout")]
    public static void Execute()
    {
        // Find the HUD object
        var hudGO = GameObject.Find("GameUICanvas/HUD");
        if (hudGO == null)
        {
            Debug.LogError("HUD not found!");
            return;
        }

        Undo.RecordObject(hudGO, "Fix HUD Layout");

        // Fix HUD RectTransform
        var hudRT = hudGO.GetComponent<RectTransform>();
        Undo.RecordObject(hudRT, "Fix HUD RT");
        hudRT.anchorMin = new Vector2(1f, 1f);
        hudRT.anchorMax = new Vector2(1f, 1f);
        hudRT.pivot = new Vector2(1f, 1f);
        hudRT.sizeDelta = new Vector2(500f, 140f);
        hudRT.anchoredPosition = new Vector2(-10f, -10f);
        EditorUtility.SetDirty(hudRT);

        // Disable HorizontalLayoutGroup
        var hlg = hudGO.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            Undo.RecordObject(hlg, "Disable HLG");
            hlg.enabled = false;
            EditorUtility.SetDirty(hlg);
        }

        // Fix GameHUDController hudSize
        var hud = hudGO.GetComponent<GameHUDController>();
        if (hud != null)
        {
            var so = new SerializedObject(hud);
            var hudSizeProp = so.FindProperty("hudSize");
            if (hudSizeProp != null)
            {
                hudSizeProp.vector2Value = new Vector2(500f, 140f);
                so.ApplyModifiedProperties();
                Debug.Log("[FixHUD] Set hudSize to (500, 140)");
            }
            else
            {
                Debug.LogWarning("[FixHUD] hudSize property not found");
            }
            EditorUtility.SetDirty(hud);
        }

        // Fix DopLabel
        var dopLabel = FindChild(hudGO.transform, "DopLabel");
        if (dopLabel != null)
        {
            var rt = dopLabel.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Fix DopLabel");
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(110f, 30f);
            rt.anchoredPosition = new Vector2(12f, -22f);
            EditorUtility.SetDirty(rt);

            var tmp = dopLabel.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                Undo.RecordObject(tmp, "Fix DopLabel Text");
                tmp.fontSize = 14f;
                tmp.text = "DOPAMINE";
                EditorUtility.SetDirty(tmp);
            }

            // Disable LayoutElement
            var le = dopLabel.GetComponent<LayoutElement>();
            if (le != null)
            {
                Undo.RecordObject(le, "Fix DopLabel LE");
                le.ignoreLayout = true;
                EditorUtility.SetDirty(le);
            }
        }

        // Fix DopamineBarBG
        var dopBarBG = FindChild(hudGO.transform, "DopamineBarBG");
        if (dopBarBG != null)
        {
            var rt = dopBarBG.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Fix DopBarBG");
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 30f);
            rt.anchoredPosition = new Vector2(125f, -22f);
            EditorUtility.SetDirty(rt);

            // Fix DopamineBarFill
            var fill = FindChild(dopBarBG.transform, "DopamineBarFill");
            if (fill != null)
            {
                var frt = fill.GetComponent<RectTransform>();
                Undo.RecordObject(frt, "Fix Fill");
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = Vector2.one;
                frt.offsetMin = new Vector2(2f, 2f);
                frt.offsetMax = new Vector2(-2f, -2f);
                EditorUtility.SetDirty(frt);
            }

            // Fix DopamineText
            var dopText = FindChild(dopBarBG.transform, "DopamineText");
            if (dopText != null)
            {
                var trt = dopText.GetComponent<RectTransform>();
                Undo.RecordObject(trt, "Fix DopText");
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
                EditorUtility.SetDirty(trt);

                var tmp = dopText.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    Undo.RecordObject(tmp, "Fix DopText TMP");
                    tmp.fontSize = 14f;
                    EditorUtility.SetDirty(tmp);
                }
            }
        }

        // Fix CoinIcon
        var coinIcon = FindChild(hudGO.transform, "CoinIcon");
        if (coinIcon != null)
        {
            var rt = coinIcon.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Fix CoinIcon");
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(36f, 36f);
            rt.anchoredPosition = new Vector2(-172f, -110f);
            EditorUtility.SetDirty(rt);

            var tmp = coinIcon.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                Undo.RecordObject(tmp, "Fix CoinIcon TMP");
                tmp.fontSize = 22f;
                EditorUtility.SetDirty(tmp);
            }

            var le = coinIcon.GetComponent<LayoutElement>();
            if (le != null)
            {
                Undo.RecordObject(le, "Fix CoinIcon LE");
                le.ignoreLayout = true;
                EditorUtility.SetDirty(le);
            }
        }

        // Fix CoinAmount
        var coinAmount = FindChild(hudGO.transform, "CoinAmount");
        if (coinAmount != null)
        {
            var rt = coinAmount.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Fix CoinAmount");
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(160f, 36f);
            rt.anchoredPosition = new Vector2(-12f, -110f);
            EditorUtility.SetDirty(rt);

            var tmp = coinAmount.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                Undo.RecordObject(tmp, "Fix CoinAmount TMP");
                tmp.fontSize = 22f;
                EditorUtility.SetDirty(tmp);
            }

            var le = coinAmount.GetComponent<LayoutElement>();
            if (le != null)
            {
                Undo.RecordObject(le, "Fix CoinAmount LE");
                le.ignoreLayout = true;
                EditorUtility.SetDirty(le);
            }
        }

        // Make HUD Image transparent
        var hudImg = hudGO.GetComponent<Image>();
        if (hudImg != null)
        {
            Undo.RecordObject(hudImg, "Fix HUD Image");
            hudImg.color = new Color(0f, 0f, 0f, 0f);
            EditorUtility.SetDirty(hudImg);
        }

        // Disable addPanelBackground
        if (hud != null)
        {
            var so2 = new SerializedObject(hud);
            var bgProp = so2.FindProperty("addPanelBackground");
            if (bgProp != null) bgProp.boolValue = false;
            var bgColorProp = so2.FindProperty("panelBGColor");
            if (bgColorProp != null) bgColorProp.colorValue = new Color(0f, 0f, 0f, 0f);
            so2.ApplyModifiedProperties();
        }

        // Fix Sep - hide it
        var sep = FindChild(hudGO.transform, "Sep");
        if (sep != null)
        {
            Undo.RecordObject(sep.gameObject, "Hide Sep");
            sep.gameObject.SetActive(false);
            EditorUtility.SetDirty(sep.gameObject);
        }

        // Mark scene dirty and save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[FixHUD] HUD layout fixed and scene saved!");
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
