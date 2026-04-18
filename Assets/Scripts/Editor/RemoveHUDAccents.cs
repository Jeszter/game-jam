using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RemoveHUDAccents
{
    public static void Execute()
    {
        var hud = GameObject.Find("GameUICanvas/HUD");
        if (hud == null) { Debug.LogError("HUD not found"); return; }

        // StylizedBG: видалити TopAccent / BotAccent та зробити панель чорною
        var stylized = hud.transform.Find("StylizedBG");
        if (stylized != null)
        {
            var top = stylized.Find("TopAccent");
            if (top != null)
            {
                if (Application.isPlaying) Object.Destroy(top.gameObject);
                else Object.DestroyImmediate(top.gameObject);
            }
            var bot = stylized.Find("BotAccent");
            if (bot != null)
            {
                if (Application.isPlaying) Object.Destroy(bot.gameObject);
                else Object.DestroyImmediate(bot.gameObject);
            }

            var sbg = stylized.GetComponent<Image>();
            if (sbg != null)
            {
                sbg.color = new Color(0f, 0f, 0f, 1f);
                EditorUtility.SetDirty(sbg);
            }
        }

        // Root HUD image чорний
        var img = hud.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0f, 0f, 0f, 1f);
            EditorUtility.SetDirty(img);
        }

        // Dopamine BarBG — темно-сірий, без outline
        var barBG = hud.transform.Find("DopamineBarBG");
        if (barBG != null)
        {
            var barImg = barBG.GetComponent<Image>();
            if (barImg != null)
            {
                barImg.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                EditorUtility.SetDirty(barImg);
            }
            var outline = barBG.GetComponent<Outline>();
            if (outline != null)
            {
                if (Application.isPlaying) Object.Destroy(outline);
                else Object.DestroyImmediate(outline);
            }
        }

        // Hunger BarBG також
        var hBarBG = hud.transform.Find("HungerBarBG");
        if (hBarBG != null)
        {
            var hBarImg = hBarBG.GetComponent<Image>();
            if (hBarImg != null)
            {
                hBarImg.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                EditorUtility.SetDirty(hBarImg);
            }
            var outline = hBarBG.GetComponent<Outline>();
            if (outline != null)
            {
                if (Application.isPlaying) Object.Destroy(outline);
                else Object.DestroyImmediate(outline);
            }
        }

        // Контролер: оновити accentColor і showAccentLines
        var ctrl = hud.GetComponent<GameHUDController>();
        if (ctrl != null && !Application.isPlaying)
        {
            var so = new SerializedObject(ctrl);
            var accentColorProp = so.FindProperty("accentColor");
            if (accentColorProp != null) accentColorProp.colorValue = new Color(0f, 0f, 0f, 1f);
            var showAccentProp = so.FindProperty("showAccentLines");
            if (showAccentProp != null) showAccentProp.boolValue = false;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ctrl);
        }

        if (!Application.isPlaying)
        {
            var scene = hud.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RemoveHUDAccents] Saved scene.");
        }
        else
        {
            Debug.Log("[RemoveHUDAccents] Applied in Play Mode (changes not persisted).");
        }
    }
}
