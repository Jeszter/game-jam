using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Виправляє непрацюючі кнопки у головному меню:
/// 1. Замінює StandaloneInputModule на InputSystemUIInputModule (активний активний новий Input System).
/// 2. Вимикає Mask на PhonePanel, який блокує рейкасти поза межами маски (через поворот).
/// 3. Переналаштовує прозорість кнопок (alpha = 0) + Raycast Target = true.
/// 4. Перевіряє/перепрошиває OnClick() персистентні виклики Start/Settings/Exit -> MenuTransition.
/// </summary>
public static class FixMainMenuInput
{
    public static void Execute()
    {
        // Відкрити сцену меню якщо не відкрита
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "MainMenu")
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        }

        // 1. EventSystem -> новий Input System
        ReplaceInputModule();

        // 2. Mask на PhonePanel
        DisablePhoneMask();

        // 3. Button raycast / image прозорість
        FixButtonRaycast("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/StartButton");
        FixButtonRaycast("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/SettingsButton");
        FixButtonRaycast("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/ExitButton");
        FixButtonRaycast("MainMenuCanvas/CornerUI/SettingsCorner");
        FixButtonRaycast("MainMenuCanvas/CornerUI/ExitCorner");

        // 4. Перепрошити OnClick
        GameObject canvasGo = GameObject.Find("MainMenuCanvas");
        MenuTransition mt = canvasGo != null ? canvasGo.GetComponent<MenuTransition>() : null;
        if (mt == null)
        {
            Debug.LogError("MenuTransition not found on MainMenuCanvas.");
            return;
        }
        RewireClick("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/StartButton", mt, "OnStartPressed");
        RewireClick("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/SettingsButton", mt, "OnSettingsPressed");
        RewireClick("MainMenuCanvas/PhonePanel/PhoneContent/ButtonsContainer/ExitButton", mt, "OnExitPressed");
        RewireClick("MainMenuCanvas/CornerUI/SettingsCorner", mt, "OnSettingsPressed");
        RewireClick("MainMenuCanvas/CornerUI/ExitCorner", mt, "OnExitPressed");

        // 5. Перевірка GraphicRaycaster на Canvas
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        GraphicRaycaster gr = canvasGo.GetComponent<GraphicRaycaster>();
        if (gr == null)
        {
            canvasGo.AddComponent<GraphicRaycaster>();
            Debug.Log("[FixInput] Added missing GraphicRaycaster to MainMenuCanvas");
        }

        // 6. Переконатися, що FadeOverlay не блокує кліки (blocksRaycasts=false, raycastTarget=false)
        Transform fade = canvasGo.transform.Find("FadeOverlay");
        if (fade != null)
        {
            var cg = fade.GetComponent<CanvasGroup>();
            if (cg != null) { cg.blocksRaycasts = false; cg.interactable = false; }
            var img = fade.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[FixInput] MainMenu input fixed: new InputSystemUIInputModule, mask disabled, raycasts enabled, buttons rewired.");
    }

    static void ReplaceInputModule()
    {
        GameObject es = GameObject.Find("EventSystem");
        if (es == null)
        {
            es = new GameObject("EventSystem", typeof(EventSystem));
            Debug.Log("[FixInput] Created EventSystem");
        }

        // Видалити StandaloneInputModule якщо є
        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            Object.DestroyImmediate(standalone);
            Debug.Log("[FixInput] Removed StandaloneInputModule");
        }

        // Додати InputSystemUIInputModule (через відображення, бо інакше потрібен using на Input System)
        var uiModuleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (uiModuleType == null)
        {
            Debug.LogWarning("[FixInput] InputSystemUIInputModule type not found. Falling back to StandaloneInputModule.");
            if (es.GetComponent<StandaloneInputModule>() == null)
                es.AddComponent<StandaloneInputModule>();
            return;
        }

        if (es.GetComponent(uiModuleType) == null)
        {
            es.AddComponent(uiModuleType);
            Debug.Log("[FixInput] Added InputSystemUIInputModule");
        }
    }

    static void DisablePhoneMask()
    {
        GameObject panel = GameObject.Find("MainMenuCanvas/PhonePanel");
        if (panel == null) return;
        Mask mask = panel.GetComponent<Mask>();
        if (mask != null)
        {
            // Повністю видаляємо маску, бо вона при повороті -5.4° блокує рейкасти дочірніх елементів
            Object.DestroyImmediate(mask);
            Debug.Log("[FixInput] Removed Mask from PhonePanel (was blocking button raycasts)");
        }
        // Image на PhonePanel — raycastTarget залишаємо, він фоновий для маски вже не потрібен як mask graphic
        var img = panel.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true; // фон телефону може отримувати кліки (але кнопки вище в ієрархії рейкастів)
        }
    }

    static void FixButtonRaycast(string path)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) { Debug.LogWarning($"[FixInput] {path} not found"); return; }

        // Image: raycastTarget = true, alpha 0 з Color тягне alphaHitTestMinimumThreshold але по дефолту Image ловить клік незалежно від альфи
        Image img = go.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
            Color c = img.color;
            c.a = 0f; // прозоре
            img.color = c;
            // НЕ встановлюємо alphaHitTestMinimumThreshold -> для прозорого Image без sprite Unity все одно ловить клік по Rect
        }

        // Button
        Button btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            if (btn.targetGraphic == null) btn.targetGraphic = img;
        }

        // CanvasGroup на предках не повинен блокувати
        Transform t = go.transform;
        while (t != null)
        {
            CanvasGroup cg = t.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            t = t.parent;
        }
    }

    static void RewireClick(string path, MenuTransition target, string methodName)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        Button btn = go.GetComponent<Button>();
        if (btn == null) return;

        // Очистимо попередні персистентні виклики
        var so = new SerializedObject(btn);
        var onClick = so.FindProperty("m_OnClick");
        var calls = onClick.FindPropertyRelative("m_PersistentCalls.m_Calls");
        calls.arraySize = 0;
        calls.arraySize = 1;

        var c = calls.GetArrayElementAtIndex(0);
        c.FindPropertyRelative("m_Target").objectReferenceValue = target;
        c.FindPropertyRelative("m_MethodName").stringValue = methodName;
        c.FindPropertyRelative("m_Mode").intValue = 1; // Void
        c.FindPropertyRelative("m_CallState").intValue = 2; // RuntimeOnly
        var asmProp = c.FindPropertyRelative("m_TargetAssemblyTypeName");
        if (asmProp != null)
            asmProp.stringValue = typeof(MenuTransition).AssemblyQualifiedName;
        so.ApplyModifiedProperties();

        Debug.Log($"[FixInput] Rewired {path} -> MenuTransition.{methodName}");
    }
}
