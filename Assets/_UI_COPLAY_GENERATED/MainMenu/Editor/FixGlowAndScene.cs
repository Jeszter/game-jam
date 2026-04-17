using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixGlowAndScene
{
    public static void Execute()
    {
        // ===== 1. REMOVE OLD HoverGlow CHILD OBJECTS =====
        string[] buttonPaths = new string[]
        {
            "MainMenuCanvas/PhonePanel/ButtonsContainer/StartButton",
            "MainMenuCanvas/PhonePanel/ButtonsContainer/SettingsButton",
            "MainMenuCanvas/PhonePanel/ButtonsContainer/ExitButton"
        };

        foreach (string path in buttonPaths)
        {
            GameObject btnObj = GameObject.Find(path);
            if (btnObj == null) continue;

            // Remove all HoverGlow children
            for (int i = btnObj.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = btnObj.transform.GetChild(i);
                if (child.name == "HoverGlow")
                {
                    Object.DestroyImmediate(child.gameObject);
                    Debug.Log($"Removed HoverGlow from {path}");
                }
            }
        }

        // ===== 2. CONFIGURE GLOW COLORS =====
        foreach (string path in buttonPaths)
        {
            GameObject btnObj = GameObject.Find(path);
            if (btnObj == null) continue;

            ButtonHoverGlow glow = btnObj.GetComponent<ButtonHoverGlow>();
            if (glow == null) continue;

            SerializedObject so = new SerializedObject(glow);

            if (path.Contains("Start"))
            {
                // Red glow for Start (matches red play icon)
                so.FindProperty("glowColor").colorValue = new Color(1f, 0.3f, 0.3f, 1f);
                so.FindProperty("glowIntensity").floatValue = 0.5f;
                so.FindProperty("iconBrightnessBoost").floatValue = 0.5f;
            }
            else
            {
                // White glow for Settings and Exit
                so.FindProperty("glowColor").colorValue = new Color(1f, 1f, 1f, 1f);
                so.FindProperty("glowIntensity").floatValue = 0.35f;
                so.FindProperty("iconBrightnessBoost").floatValue = 0.4f;
            }

            so.FindProperty("glowFadeSpeed").floatValue = 5f;
            so.ApplyModifiedProperties();
            Debug.Log($"Configured glow on {path}");
        }

        // ===== 3. FIX SCENE NAME IN MenuTransition =====
        GameObject canvasObj = GameObject.Find("MainMenuCanvas");
        if (canvasObj != null)
        {
            MenuTransition mt = canvasObj.GetComponent<MenuTransition>();
            if (mt != null)
            {
                SerializedObject so = new SerializedObject(mt);
                so.FindProperty("nextSceneName").stringValue = "Game1";
                so.ApplyModifiedProperties();
                Debug.Log("Set nextSceneName to 'Game1'");
            }
        }

        // ===== 4. ADD Game1 SCENE TO BUILD SETTINGS =====
        string game1Path = "Assets/Scenes/Game1.unity";
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // Check if SampleScene is in build settings
        bool hasSampleScene = false;
        bool hasGame1 = false;
        foreach (var s in scenes)
        {
            if (s.path == "Assets/SampleScene.unity") hasSampleScene = true;
            if (s.path == game1Path) hasGame1 = true;
        }

        if (!hasSampleScene)
        {
            scenes.Insert(0, new EditorBuildSettingsScene("Assets/SampleScene.unity", true));
            Debug.Log("Added SampleScene to Build Settings");
        }

        if (!hasGame1)
        {
            scenes.Add(new EditorBuildSettingsScene(game1Path, true));
            Debug.Log("Added Game1 to Build Settings");
        }

        EditorBuildSettings.scenes = scenes.ToArray();

        // Save
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("=== All fixes applied ===");
    }
}
