using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class ShrinkHUDMore
{
    public static void Execute()
    {
        var hud = GameObject.Find("GameUICanvas/HUD");
        if (hud == null) { Debug.LogError("HUD not found"); return; }

        var hudRT = hud.GetComponent<RectTransform>();
        // Значно менший, компактний HUD
        Vector2 hudSize = new Vector2(260f, 70f);
        hudRT.sizeDelta = hudSize;
        hudRT.anchoredPosition = new Vector2(-10f, -8f);

        // Оновити в контролері
        var ctrl = hud.GetComponent<GameHUDController>();
        if (ctrl != null)
        {
            var so = new SerializedObject(ctrl);
            var sp = so.FindProperty("hudSize");
            if (sp != null) { sp.vector2Value = hudSize; so.ApplyModifiedProperties(); }
        }

        // DopLabel
        var dopLabel = GameObject.Find("GameUICanvas/HUD/DopLabel");
        if (dopLabel != null)
        {
            var rt = (RectTransform)dopLabel.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(60f, 14f);
            rt.anchoredPosition = new Vector2(6f, -10f);
            var tmp = dopLabel.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.fontSize = 8f; tmp.characterSpacing = 2f; }
        }

        // DopamineBarBG
        var bg = GameObject.Find("GameUICanvas/HUD/DopamineBarBG");
        if (bg != null)
        {
            var rt = (RectTransform)bg.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(120f, 12f);
            rt.anchoredPosition = new Vector2(72f, -10f);
            var le = bg.GetComponent<LayoutElement>();
            if (le != null) Object.DestroyImmediate(le);
        }

        var fill = GameObject.Find("GameUICanvas/HUD/DopamineBarBG/DopamineBarFill");
        if (fill != null)
        {
            var rt = (RectTransform)fill.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(1.5f, 1.5f);
            rt.offsetMax = new Vector2(-1.5f, -1.5f);
        }

        var dopText = GameObject.Find("GameUICanvas/HUD/DopamineBarBG/DopamineText");
        if (dopText != null)
        {
            var rt = (RectTransform)dopText.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = dopText.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.fontSize = 8f; tmp.alignment = TextAlignmentOptions.Center; tmp.enableAutoSizing = false; }
        }

        // Hunger (row 2)
        var hLabel = GameObject.Find("GameUICanvas/HUD/HungerLabel");
        if (hLabel != null)
        {
            var rt = (RectTransform)hLabel.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(60f, 14f);
            rt.anchoredPosition = new Vector2(6f, -28f);
            var tmp = hLabel.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.fontSize = 8f; tmp.characterSpacing = 2f; }
        }

        var hBg = GameObject.Find("GameUICanvas/HUD/HungerBarBG");
        if (hBg != null)
        {
            var rt = (RectTransform)hBg.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(120f, 12f);
            rt.anchoredPosition = new Vector2(72f, -28f);
        }

        var hText = GameObject.Find("GameUICanvas/HUD/HungerBarBG/HungerText");
        if (hText != null)
        {
            var tmp = hText.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.fontSize = 8f; tmp.enableAutoSizing = false; }
        }

        // Sep hide
        var sep = GameObject.Find("GameUICanvas/HUD/Sep");
        if (sep != null) sep.SetActive(false);

        // CoinAmount (right)
        var coinTxt = GameObject.Find("GameUICanvas/HUD/CoinAmount");
        if (coinTxt != null)
        {
            var rt = (RectTransform)coinTxt.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(90f, 24f);
            rt.anchoredPosition = new Vector2(-8f, -22f);
            var tmp = coinTxt.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.fontSize = 14f; tmp.alignment = TextAlignmentOptions.Right; tmp.enableAutoSizing = false; }
        }

        var coinIcon = GameObject.Find("GameUICanvas/HUD/CoinIcon");
        if (coinIcon != null)
        {
            var rt = (RectTransform)coinIcon.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(14f, 14f);
            rt.anchoredPosition = new Vector2(-98f, -22f);
        }

        // CoinTitleLabel (GOLD)
        var coinLabel = GameObject.Find("GameUICanvas/HUD/CoinTitleLabel");
        if (coinLabel != null)
        {
            var rt = (RectTransform)coinLabel.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(80f, 10f);
            rt.anchoredPosition = new Vector2(-8f, -8f);
            var tmp = coinLabel.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.fontSize = 7f; }
        }

        EditorSceneManager.MarkSceneDirty(hud.scene);
        EditorSceneManager.SaveScene(hud.scene);
        Debug.Log("[ShrinkHUDMore] done. size=" + hudSize);
    }
}
