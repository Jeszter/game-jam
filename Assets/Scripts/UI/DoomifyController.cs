using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Doomify — in-game music player (Spotify-style).
/// Player can browse tracks, play/pause, adjust volume.
/// Music persists even when phone is closed.
/// </summary>
public class DoomifyController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("UI References")]
    [SerializeField] private RectTransform trackListContainer;
    [SerializeField] private GameObject trackItemTemplate;
    [SerializeField] private TMP_Text nowPlayingTitle;
    [SerializeField] private TMP_Text nowPlayingArtist;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private TMP_Text playPauseIcon;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button loopButton;
    [SerializeField] private TMP_Text loopIcon;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeText;
    [SerializeField] private Image progressBar;

    [System.Serializable]
    public class Track
    {
        public string title;
        public string artist;
        public AudioClip clip;
    }

    [SerializeField] private List<Track> tracks = new List<Track>();

    [Header("Auto Play")]
    [SerializeField] private bool autoPlayOnStart = true;
    [SerializeField] private bool shuffleOnStart = true;

    private int currentTrackIndex = -1;
    private bool isPlaying;
    private bool isLooping;
    private List<GameObject> trackItems = new List<GameObject>();

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        // Wire buttons
        if (playPauseButton != null) playPauseButton.onClick.AddListener(TogglePlayPause);
        if (prevButton != null) prevButton.onClick.AddListener(PrevTrack);
        if (nextButton != null) nextButton.onClick.AddListener(NextTrack);
        if (loopButton != null) loopButton.onClick.AddListener(ToggleLoop);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = 0.5f;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            audioSource.volume = 0.5f;
        }

        BuildTrackList();

        // Auto-play music from the start
        if (autoPlayOnStart && tracks.Count > 0)
        {
            int startIndex = 0;
            if (shuffleOnStart)
                startIndex = Random.Range(0, tracks.Count);
            PlayTrack(startIndex);
        }
        else
        {
            UpdateNowPlaying();
        }
    }

    void Update()
    {
        // When track ends: loop same track or play next
        if (isPlaying && audioSource != null && !audioSource.isPlaying && currentTrackIndex >= 0)
        {
            if (isLooping)
                PlayTrack(currentTrackIndex); // replay same track
            else
                NextTrack();
        }

        // Update progress bar
        if (progressBar != null && audioSource != null && audioSource.clip != null && audioSource.clip.length > 0)
        {
            progressBar.fillAmount = audioSource.time / audioSource.clip.length;
        }
        else if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
        }
    }

    private void BuildTrackList()
    {
        if (trackListContainer == null || trackItemTemplate == null) return;

        // Clear old items
        foreach (var item in trackItems)
        {
            if (item != null) Destroy(item);
        }
        trackItems.Clear();

        trackItemTemplate.SetActive(false);

        for (int i = 0; i < tracks.Count; i++)
        {
            GameObject item = Instantiate(trackItemTemplate, trackListContainer);
            item.name = $"Track_{i}";
            item.SetActive(true);

            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in texts)
            {
                if (txt.gameObject.name == "TrackTitle")
                    txt.text = tracks[i].title;
                else if (txt.gameObject.name == "TrackArtist")
                    txt.text = tracks[i].artist;
            }

            // Wire click
            Button btn = item.GetComponent<Button>();
            if (btn == null) btn = item.AddComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() => PlayTrack(index));

            // Highlight color on hover
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.1f, 0.1f, 0.14f, 1f);
            cb.highlightedColor = new Color(0.15f, 0.2f, 0.15f, 1f);
            cb.pressedColor = new Color(0.1f, 0.3f, 0.1f, 1f);
            cb.selectedColor = new Color(0.12f, 0.25f, 0.12f, 1f);
            btn.colors = cb;

            trackItems.Add(item);
        }
    }

    public void PlayTrack(int index)
    {
        if (index < 0 || index >= tracks.Count) return;
        if (tracks[index].clip == null) return;

        currentTrackIndex = index;
        audioSource.clip = tracks[index].clip;
        audioSource.Play();
        isPlaying = true;

        // Смена трека тоже чуть стимулирует дофамин
        if (GameEconomy.Instance != null)
            GameEconomy.Instance.AwardDopamine(GameEconomy.ActMusic);

        UpdateNowPlaying();
        HighlightCurrentTrack();
    }

    public void TogglePlayPause()
    {
        if (currentTrackIndex < 0 && tracks.Count > 0)
        {
            PlayTrack(0);
            return;
        }

        if (isPlaying)
        {
            audioSource.Pause();
            isPlaying = false;
        }
        else
        {
            audioSource.UnPause();
            isPlaying = true;
        }

        UpdateNowPlaying();
    }

    public void NextTrack()
    {
        if (tracks.Count == 0) return;
        int next = (currentTrackIndex + 1) % tracks.Count;
        PlayTrack(next);
    }

    public void ToggleLoop()
    {
        isLooping = !isLooping;
        if (audioSource != null)
            audioSource.loop = isLooping;
        UpdateLoopIcon();
    }

    private void UpdateLoopIcon()
    {
        if (loopIcon != null)
        {
            loopIcon.text = isLooping ? "[R]" : "R";
            loopIcon.color = isLooping
                ? new Color(0.3f, 1f, 0.4f)   // green when active
                : new Color(0.55f, 0.55f, 0.6f); // gray when off
        }
    }

    public void PrevTrack()
    {
        if (tracks.Count == 0) return;
        // If more than 3 seconds in, restart current track
        if (audioSource.time > 3f)
        {
            audioSource.time = 0f;
            return;
        }
        int prev = currentTrackIndex - 1;
        if (prev < 0) prev = tracks.Count - 1;
        PlayTrack(prev);
    }

    private void OnVolumeChanged(float value)
    {
        if (audioSource != null)
            audioSource.volume = value;
        if (volumeText != null)
            volumeText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    private void UpdateNowPlaying()
    {
        if (currentTrackIndex >= 0 && currentTrackIndex < tracks.Count)
        {
            if (nowPlayingTitle != null)
                nowPlayingTitle.text = tracks[currentTrackIndex].title;
            if (nowPlayingArtist != null)
                nowPlayingArtist.text = tracks[currentTrackIndex].artist;
        }
        else
        {
            if (nowPlayingTitle != null) nowPlayingTitle.text = "No track";
            if (nowPlayingArtist != null) nowPlayingArtist.text = "Select a song";
        }

        if (playPauseIcon != null)
            playPauseIcon.text = isPlaying ? "||" : ">";
    }

    private void HighlightCurrentTrack()
    {
        for (int i = 0; i < trackItems.Count; i++)
        {
            if (trackItems[i] == null) continue;
            Image bg = trackItems[i].GetComponent<Image>();
            if (bg != null)
            {
                bg.color = (i == currentTrackIndex)
                    ? new Color(0.08f, 0.2f, 0.08f, 1f)
                    : new Color(0.1f, 0.1f, 0.14f, 1f);
            }

            // Highlight title text of current track
            TMP_Text[] texts = trackItems[i].GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in texts)
            {
                if (txt.gameObject.name == "TrackTitle")
                    txt.color = (i == currentTrackIndex)
                        ? new Color(0.3f, 1f, 0.4f)
                        : new Color(0.95f, 0.95f, 0.95f);
            }
        }
    }
}
