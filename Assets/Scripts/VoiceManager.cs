using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton voice manager. Plays random voice lines from categories.
/// Usage: VoiceManager.Instance.Play("wakeup");
/// </summary>
public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }

    [System.Serializable]
    public class VoiceCategory
    {
        public string categoryName;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Min delay before next line from ANY category can play")]
        public float cooldownAfter = 0.5f;
    }

    [Header("Voice Categories")]
    public VoiceCategory[] categories;

    [Header("Audio")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    public bool interruptPrevious = false;

    private AudioSource audioSource;
    private Dictionary<string, VoiceCategory> categoryMap;
    private float nextAllowedTime = 0f;
    private string lastPlayedCategory = "";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound

        // Build lookup
        categoryMap = new Dictionary<string, VoiceCategory>();
        if (categories != null)
        {
            foreach (var cat in categories)
            {
                if (cat != null && !string.IsNullOrEmpty(cat.categoryName))
                    categoryMap[cat.categoryName.ToLower()] = cat;
            }
        }
    }

    /// <summary>
    /// Play a random clip from category. Returns true if started.
    /// </summary>
    public bool Play(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName)) return false;
        if (Time.time < nextAllowedTime && !interruptPrevious) return false;

        string key = categoryName.ToLower();
        if (!categoryMap.TryGetValue(key, out var cat) || cat.clips == null || cat.clips.Length == 0)
        {
            Debug.LogWarning("[VoiceManager] Нет клипов для категории: " + categoryName);
            return false;
        }

        // Pick random clip that isn't last one (if possible)
        AudioClip clip = cat.clips[Random.Range(0, cat.clips.Length)];
        if (clip == null) return false;

        if (interruptPrevious && audioSource.isPlaying) audioSource.Stop();

        audioSource.volume = masterVolume * cat.volume;
        audioSource.PlayOneShot(clip);

        nextAllowedTime = Time.time + clip.length + cat.cooldownAfter;
        lastPlayedCategory = key;
        return true;
    }

    /// <summary>
    /// Play after a delay.
    /// </summary>
    public void PlayDelayed(string categoryName, float delay)
    {
        StartCoroutine(PlayDelayedCoroutine(categoryName, delay));
    }

    private IEnumerator PlayDelayedCoroutine(string categoryName, float delay)
    {
        yield return new WaitForSeconds(delay);
        Play(categoryName);
    }

    /// <summary>
    /// Force stop current voice.
    /// </summary>
    public void Stop()
    {
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        nextAllowedTime = 0f;
    }

    public bool IsPlaying() => audioSource != null && audioSource.isPlaying;
}