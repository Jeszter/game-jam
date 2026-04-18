using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UpdateHUDLook
{
    public static void Execute()
    {
        var hud = GameObject.Find("GameUICanvas/HUD");
        if (hud == null)
        {
            Debug.LogError("HUD not found");
            return;
        }

        // Root HUD image → full black, opaque
        var img = hud.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0f, 0f, 0f, 1f);
            EditorUtility.SetDirty(img);
        }

        // StylizedBG inside HUD (if exists) → full black opaque
        var stylized = hud.transform.Find("StylizedBG");
        if (stylized != null)
        {
            var sbg = stylized.GetComponent<Image>();
            if (sbg != null)
            {
                sbg.color = new Color(0f, 0f, 0f, 1f);
                EditorUtility.SetDirty(sbg);
            }
        }

        // HUD size — трохи більший для ширших барів
        var hudRT = hud.transform as RectTransform;
        if (hudRT != null)
        {
            hudRT.sizeDelta = new Vector2(300f, 90f);
            EditorUtility.SetDirty(hudRT);
        }

        // DopamineBarBG — збільшити
        var barBG = hud.transform.Find("DopamineBarBG") as RectTransform;
        if (barBG != null)
        {
            barBG.sizeDelta = new Vector2(150f, 18f);
            barBG.anchoredPosition = new Vector2(76f, -14f);
            EditorUtility.SetDirty(barBG);
            var barImg = barBG.GetComponent<Image>();
            if (barImg != null)
            {
                barImg.color = new Color(0f, 0f, 0f, 0.8f);
                EditorUtility.SetDirty(barImg);
            }
        }

        // DopLabel — трохи ближче зверху
        var dopLabel = hud.transform.Find("DopLabel") as RectTransform;
        if (dopLabel != null)
        {
            dopLabel.sizeDelta = new Vector2(64f, 18f);
            dopLabel.anchoredPosition = new Vector2(8f, -14f);
            EditorUtility.SetDirty(dopLabel);
        }

        // Coin елементи — змістити нижче щоб не накладалось на бар
        var coinAmount = hud.transform.Find("CoinAmount") as RectTransform;
        if (coinAmount != null)
        {
            coinAmount.anchoredPosition = new Vector2(-8f, -28f);
            EditorUtility.SetDirty(coinAmount);
        }
        var coinIcon = hud.transform.Find("CoinIcon") as RectTransform;
        if (coinIcon != null)
        {
            coinIcon.anchoredPosition = new Vector2(-98f, -28f);
            EditorUtility.SetDirty(coinIcon);
        }

        // Оновити поля на контролері (hudSize + panelBGColor) через SerializedObject
        var ctrl = hud.GetComponent<GameHUDController>();
        if (ctrl != null)
        {
            var so = new SerializedObject(ctrl);
            var hudSizeProp = so.FindProperty("hudSize");
            if (hudSizeProp != null) hudSizeProp.vector2Value = new Vector2(300f, 90f);
            var panelColorProp = so.FindProperty("panelBGColor");
            if (panelColorProp != null) panelColorProp.colorValue = new Color(0f, 0f, 0f, 1f);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ctrl);
        }

        // Save scene
        var scene = hud.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[UpdateHUDLook] Done. HUD is now fully black with larger bar.");
    }
}
