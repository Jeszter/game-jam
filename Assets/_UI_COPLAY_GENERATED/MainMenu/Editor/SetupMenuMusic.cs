using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupMenuMusic
{
    public static void Execute()
    {
        GameObject canvas = GameObject.Find("MainMenuCanvas");
        if (canvas == null)
        {
            Debug.LogError("MainMenuCanvas not found");
            return;
        }

        AudioSource audioSource = canvas.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = canvas.AddComponent<AudioSource>();
        }

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/MainMenu.mp3");
        if (clip == null)
        {
            Debug.LogError("MainMenu.mp3 not found at Assets/Sound/MainMenu.mp3");
            return;
        }

        audioSource.clip = clip;
        audioSource.playOnAwake = true;
        audioSource.loop = true;
        audioSource.volume = 0.5f;
        audioSource.spatialBlend = 0f; // 2D sound

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("Menu music set up: MainMenu.mp3, loop=true, volume=0.5");
    }
}
