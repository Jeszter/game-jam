#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Призначає відсутні посилання у GamePhoneController:
///   tikTokScreen      -> PhonePanel/PhoneScreen/TikTokScreen
///   backButtonTikTok  -> PhonePanel/PhoneScreen/TikTokScreen/TopBar/BackButton
/// Після цього кнопка TikTokAppBtn на домашньому екрані телефона відкриватиме TikTok,
/// а кнопка BackButton повертатиме на Home.
/// </summary>
public static class WireTikTokScreen
{
    public static void Execute()
    {
        var canvas = GameObject.Find("GameUICanvas");
        if (canvas == null) { Debug.LogError("[WireTikTokScreen] GameUICanvas not found"); return; }

        var phoneCtrl = canvas.GetComponent<GamePhoneController>();
        if (phoneCtrl == null) { Debug.LogError("[WireTikTokScreen] GamePhoneController missing"); return; }

        // TikTokScreen (може бути неактивний - шукаємо через Transform)
        Transform tikTokScreen = canvas.transform.Find("PhonePanel/PhoneScreen/TikTokScreen");
        if (tikTokScreen == null)
        {
            Debug.LogError("[WireTikTokScreen] TikTokScreen not found");
            return;
        }

        // BackButton всередині TikTokScreen
        Transform backTr = tikTokScreen.Find("TopBar/BackButton");
        Button backBtn = backTr != null ? backTr.GetComponent<Button>() : null;
        if (backBtn == null)
        {
            Debug.LogWarning("[WireTikTokScreen] BackButton not found under TikTokScreen/TopBar");
        }

        var so = new SerializedObject(phoneCtrl);
        so.FindProperty("tikTokScreen").objectReferenceValue = tikTokScreen.gameObject;
        if (backBtn != null)
            so.FindProperty("backButtonTikTok").objectReferenceValue = backBtn;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(phoneCtrl);

        // Переконаємось що TikTokScreen увімкнено в ієрархії (його вимкне GamePhoneController.SwitchScreen)
        // Залишимо як є - GamePhoneController сам перемикне при старті.

        var scene = canvas.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[WireTikTokScreen] OK. tikTokScreen='{tikTokScreen.name}' backButtonTikTok='{(backBtn != null ? backBtn.name : "null")}'. Scene saved.");
    }
}
#endif
