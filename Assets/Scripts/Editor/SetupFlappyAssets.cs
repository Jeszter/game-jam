using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class SetupFlappyAssets
{
    const string Root = "Assets/Flappy_Bird_assets by kosresetr55/Flappy_Bird_assets by kosresetr55";

    [MenuItem("Tools/Flappy/Setup Assets (Sprites + Audio)")]
    public static void SetupImporters()
    {
        int changed = 0;
        string[] textureFolders = new[] {
            Root + "/Game Objects",
            Root + "/UI",
            Root + "/UI/Numbers",
        };
        foreach (var folder in textureFolders)
        {
            if (!Directory.Exists(folder)) { Debug.LogWarning("Missing: " + folder); continue; }
            var files = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            foreach (var f in files)
            {
                string assetPath = f.Replace('\\', '/');
                var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (ti == null) continue;
                bool dirty = false;
                if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; dirty = true; }
                if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (ti.filterMode != FilterMode.Point) { ti.filterMode = FilterMode.Point; dirty = true; }
                if (ti.mipmapEnabled) { ti.mipmapEnabled = false; dirty = true; }
                if (ti.spritePixelsPerUnit != 100f) { ti.spritePixelsPerUnit = 100f; dirty = true; }
                if (ti.alphaIsTransparency != true) { ti.alphaIsTransparency = true; dirty = true; }
                if (dirty) { ti.SaveAndReimport(); changed++; }
            }
        }

        string audioFolder = Root + "/Sound Efects";
        if (Directory.Exists(audioFolder))
        {
            var files = Directory.GetFiles(audioFolder, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var f in files)
            {
                if (!(f.EndsWith(".wav") || f.EndsWith(".ogg") || f.EndsWith(".mp3"))) continue;
                string assetPath = f.Replace('\\', '/');
                var ai = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (ai == null) continue;
                var settings = ai.defaultSampleSettings;
                bool dirty = false;
                if (settings.loadType != AudioClipLoadType.DecompressOnLoad)
                {
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    ai.defaultSampleSettings = settings;
                    dirty = true;
                }
                if (dirty) { ai.SaveAndReimport(); changed++; }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SetupFlappyAssets] Importers updated. {changed} asset(s) reimported.");
    }

    [MenuItem("Tools/Flappy/Assign Assets To TV")]
    public static void AssignAssetsToTv()
    {
        SetupImporters();

        var bg      = Load<Sprite>(Root + "/Game Objects/background-day.png");
        var baseSpr = Load<Sprite>(Root + "/Game Objects/base.png");
        var pipe    = Load<Sprite>(Root + "/Game Objects/pipe-green.png");
        var message = Load<Sprite>(Root + "/UI/message.png");
        var gameOver= Load<Sprite>(Root + "/UI/gameover.png");

        var birdDown= Load<Sprite>(Root + "/Game Objects/yellowbird-downflap.png");
        var birdMid = Load<Sprite>(Root + "/Game Objects/yellowbird-midflap.png");
        var birdUp  = Load<Sprite>(Root + "/Game Objects/yellowbird-upflap.png");

        Sprite[] numbers = new Sprite[10];
        for (int i = 0; i < 10; i++)
            numbers[i] = Load<Sprite>(Root + "/UI/Numbers/" + i + ".png");

        AudioClip wing  = LoadAudio(Root + "/Sound Efects/wing");
        AudioClip point = LoadAudio(Root + "/Sound Efects/point");
        AudioClip hit   = LoadAudio(Root + "/Sound Efects/hit");
        AudioClip die   = LoadAudio(Root + "/Sound Efects/die");

        var scene = EditorSceneManager.GetActiveScene();
        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            var tvs = root.GetComponentsInChildren<TvFlappyBird>(true);
            foreach (var tv in tvs)
            {
                Undo.RecordObject(tv, "Assign Flappy Assets");
                tv.sprBackground = bg;
                tv.sprBase = baseSpr;
                tv.sprPipe = pipe;
                tv.sprMessage = message;
                tv.sprGameOver = gameOver;
                tv.birdFrames = new Sprite[] { birdDown, birdMid, birdUp };
                tv.numberSprites = numbers;
                tv.sfxWing = wing;
                tv.sfxPoint = point;
                tv.sfxHit = hit;
                tv.sfxDie = die;
                EditorUtility.SetDirty(tv);
                count++;
            }
        }

        if (count == 0) { Debug.LogWarning("[SetupFlappyAssets] No TvFlappyBird found in active scene."); return; }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SetupFlappyAssets] Assigned assets to {count} TvFlappyBird component(s). Scene saved.");
    }

    static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) Debug.LogWarning("[SetupFlappyAssets] Missing: " + path);
        return a;
    }

    static AudioClip LoadAudio(string pathNoExt)
    {
        // Prefer ogg, fallback wav
        var a = AssetDatabase.LoadAssetAtPath<AudioClip>(pathNoExt + ".ogg");
        if (a == null) a = AssetDatabase.LoadAssetAtPath<AudioClip>(pathNoExt + ".wav");
        if (a == null) Debug.LogWarning("[SetupFlappyAssets] Missing audio: " + pathNoExt);
        return a;
    }
}
