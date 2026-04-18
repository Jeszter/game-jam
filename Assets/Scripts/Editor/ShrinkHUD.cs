using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class ShrinkHUD
{
    public static void Execute()
    {
        var hud = GameObject.Find("GameUICanvas/HUD");
        if (hud == null) { Debug.LogError("HUD not found"); return; }

        var hudRT = hud.GetComponent<RectTransform>();
        // Менший компактний HUD
        Vector2 hudSize = new Vector2(360f, 110f);
        hudRT.sizeDelta = hudSize;
        hudRT.anchoredPosition = new Vector2(-15f, -10f);

        // Оновити розмір в GameHUDController
        var ctrl = hud.GetComponent<GameHUDController>();
        if (ctrl != null)
        {
            var so = new SerializedObject(ctrl);
            var sp = so.FindProperty("hudSize");
            if (sp != null)
            {
                sp.vector2Value = hudSize;
                so.ApplyModifiedProperties();
            }
        }

        // DopLabel
        var dopLabelGO = GameObject.Find("GameUICanvas/HUD/DopLabel");
        if (dopLabelGO != null)
        {
            var rt = (RectTransform)dopLabelGO.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(80f, 20f);
            rt.anchoredPosition = new Vector2(8f, -18f);
            var tmp = dopLabelGO.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.fontSize = 10f; tmp.characterSpacing = 3f; }
        }

        // DopamineBarBG (x=8+80+4=92), width ~180, height ~18
        var bgGO = GameObject.Find("GameUICanvas/HUD/DopamineBarBG");
        if (bgGO != null)
        {
            var rt = (RectTransform)bgGO.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(180f, 18f);
            rt.anchoredPosition = new Vector2(92f, -18f);

            // Remove LayoutElement to avoid stretch influence
            var le = bgGO.GetComponent<LayoutElement>();
            if (le != null) Object.DestroyImmediate(le);
        }

        var fillGO = GameObject.Find("GameUICanvas/HUD/DopamineBarBG/DopamineBarFill");
        if (fillGO != null)
        {
            var rt = (RectTransform)fillGO.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
        }

        var dopTextGO = GameObject.Find("GameUICanvas/HUD/DopamineBarBG/DopamineText");
        if (dopTextGO != null)
        {
            var rt = (RectTransform)dopTextGO.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = dopTextGO.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.fontSize = 12f; tmp.alignment = TextAlignmentOptions.Center; }
        }

        // Hide Sep
        var sepGO = GameObject.Find("GameUICanvas/HUD/Sep");
        if (sepGO != null) sepGO.SetActive(false);

        // CoinAmount (right side)
        var coinTxtGO = GameObject.Find("GameUICanvas/HUD/CoinAmount");
        if (coinTxtGO != null)
        {
            var rt = (RectTransform)coinTxtGO.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(110f, 36f);
            rt.anchoredPosition = new Vector2(-10f, -34f);
            var tmp = coinTxtGO.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.fontSize = 18f; tmp.alignment = TextAlignmentOptions.Right; tmp.enableAutoSizing = false; }
        }

        // CoinIcon — hide (just show $ text)
        var coinIconGO = GameObject.Find("GameUICanvas/HUD/CoinIcon");
        if (coinIconGO != null)
        {
            var rt = (RectTransform)coinIconGO.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(18f, 18f);
            rt.anchoredPosition = new Vector2(-120f, -34f);
        }

        EditorSceneManager.MarkSceneDirty(hud.scene);
        EditorSceneManager.SaveScene(hud.scene);
        Debug.Log("[ShrinkHUD] done. size=" + hudSize);
    }
}
