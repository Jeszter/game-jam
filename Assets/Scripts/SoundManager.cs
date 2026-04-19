using UnityEngine;

/// <summary>
/// Lightweight singleton that holds shared UI/SFX AudioClips loaded from Resources
/// or from disk via AudioClip asset reference. Provides a single AudioSource for
/// playing non-spatial one-shots (UI clicks, etc).
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var existing = FindFirstObjectByType<SoundManager>();
                if (existing != null) { _instance = existing; return _instance; }

                var go = new GameObject("SoundManager");
                _instance = go.AddComponent<SoundManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("UI SFX")]
    public AudioClip phoneTap;
    public AudioClip menuClick;
    public AudioClip footstep;

    [Header("World SFX")]
    public AudioClip smokeInhale;   // vape / iqos puff
    public AudioClip foodEat;       // chewing / gnawing
    public AudioClip doorSound;     // door open/close
    public AudioClip tvOn;
    public AudioClip tvOff;
    public AudioClip cutsceneVoice; // new: voice/sfx played during the wake-up cutscene
    public AudioClip pickupSound;   // picking up / dropping objects

    [Range(0f, 1f)] public float uiVolume = 0.7f;

    private AudioSource uiSource;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
        uiSource.spatialBlend = 0f; // 2D

        // Auto-load clips if not set in inspector.
        // У білді використовуємо виключно Resources.Load — AssetDatabase недоступний.
        if (phoneTap == null)    phoneTap    = LoadClip("smartphone tap", "Sound/smartphone tap");
        if (menuClick == null)   menuClick   = LoadClip("menu options sound", "Sound/menu options sound");
        if (footstep == null)    footstep    = LoadClip("footsteps", "Sound/footsteps");

        // New world SFX — копії лежать у Resources/Sound/ з коротким ім'ям (CopyResourcesForBuild).
        if (smokeInhale == null) smokeInhale = LoadClipResources("Sound/smokeInhale", "A_deep,_long_inhale");
        if (foodEat == null)     foodEat     = LoadClipResources("Sound/foodEat",     "Creature_gnawing_and");
        if (doorSound == null)   doorSound   = LoadClipResources("Sound/doorSound",   "the_sound_of_a_door");
        if (tvOn == null)        tvOn        = LoadClipResources("Sound/tvOn",        "the_sound_of_the_TV__#3-on");
        if (tvOff == null)       tvOff       = LoadClipResources("Sound/tvOff",       "the_sound_of_the_TV__off");
        if (cutsceneVoice == null) cutsceneVoice = LoadClipResources("Sound/cutsceneVoice", "ElevenLabs_2026-04-18T08_19_13_123");
        if (pickupSound == null)   pickupSound   = LoadClipResources("Sound/pickupSound",   "Gentle_hand_picking");
    }

    AudioClip LoadClip(string nameWithoutExt, string resourcesPath = null)
    {
        // Try Resources first (працює і в редакторі і в білді)
        if (!string.IsNullOrEmpty(resourcesPath))
        {
            var r = Resources.Load<AudioClip>(resourcesPath);
            if (r != null) return r;
        }
        var c = Resources.Load<AudioClip>(nameWithoutExt);
        if (c != null) return c;

#if UNITY_EDITOR
        // Editor fallback: load by path
        string[] candidates = {
            $"Assets/Sound/{nameWithoutExt}.mp3",
            $"Assets/Sound/{nameWithoutExt}.wav",
            $"Assets/Sound/{nameWithoutExt}.ogg",
        };
        foreach (var p in candidates)
        {
            var a = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            if (a != null) return a;
        }
#endif
        return null;
    }

    /// <summary>
    /// Завантажує AudioClip: 1) з Resources/ за коротким ім'ям,
    /// 2) редакторний фолбек через AssetDatabase по префіксу імені файлу.
    /// </summary>
    AudioClip LoadClipResources(string resourcesPath, string editorPrefix)
    {
        var c = Resources.Load<AudioClip>(resourcesPath);
        if (c != null) return c;
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip");
        foreach (var g in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(fileName) && fileName.StartsWith(editorPrefix))
            {
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) return clip;
            }
        }
#endif
        return null;
    }

    public void PlayPhoneTap() => Play(phoneTap);
    public void PlayMenuClick() => Play(menuClick);

    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || uiSource == null) return;
        uiSource.PlayOneShot(clip, uiVolume * volumeScale);
    }

    /// <summary>
    /// Play a short one-shot at a world position. Creates a temporary AudioSource.
    /// </summary>
    public void PlayAt(AudioClip clip, Vector3 pos, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        var go = new GameObject("OneShotAudio");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 0.8f;
        src.volume = volume;
        src.pitch = pitch;
        src.minDistance = 1f;
        src.maxDistance = 20f;
        src.Play();
        Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
    }
}
