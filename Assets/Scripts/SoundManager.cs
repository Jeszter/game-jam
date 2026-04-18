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

        // Auto-load clips if not set in inspector
        if (phoneTap == null) phoneTap = LoadClip("smartphone tap");
        if (menuClick == null) menuClick = LoadClip("menu options sound");
        if (footstep == null) footstep = LoadClip("footsteps");
    }

    AudioClip LoadClip(string nameWithoutExt)
    {
        // Try Resources first
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
