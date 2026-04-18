using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Інтерактивний кран: гравець дивиться на нього і натискає E -
/// вмикається/вимикається струмінь води (ParticleSystem).
/// </summary>
public class FaucetWater : MonoBehaviour
{
    [Header("Interact")]
    public float interactDistance = 2.5f;

    [Header("Water")]
    public ParticleSystem waterParticles;
    public AudioClip      turnOnSound;
    public AudioClip      waterLoopSound;

    private AudioSource audioSrc;
    private Transform   playerCam;
    private bool        isOn = false;

    private void Start()
    {
        if (waterParticles == null)
            waterParticles = GetComponentInChildren<ParticleSystem>(true);
        if (waterParticles != null)
        {
            var emission = waterParticles.emission;
            emission.enabled = false;
            waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.spatialBlend = 1f;
        audioSrc.minDistance  = 1f;
        audioSrc.maxDistance  = 8f;
        audioSrc.loop         = true;
        audioSrc.playOnAwake  = false;

        CachePlayerCam();
    }

    private void CachePlayerCam()
    {
        var player = GameObject.Find("player");
        if (player != null)
        {
            var cam = player.GetComponentInChildren<Camera>();
            if (cam != null) { playerCam = cam.transform; return; }
        }
        if (Camera.main != null) playerCam = Camera.main.transform;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        if (playerCam == null || !playerCam.gameObject.activeInHierarchy) CachePlayerCam();
        if (playerCam == null) return;

        Ray r = new Ray(playerCam.position, playerCam.forward);
        if (!Physics.Raycast(r, out RaycastHit hit, interactDistance)) return;
        if (hit.collider.gameObject != gameObject &&
            !hit.collider.transform.IsChildOf(transform))
            return;

        Toggle();
    }

    public void Toggle()
    {
        isOn = !isOn;
        if (waterParticles != null)
        {
            var emission = waterParticles.emission;
            emission.enabled = isOn;
            if (isOn) waterParticles.Play(true);
            else       waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (audioSrc != null)
        {
            if (isOn)
            {
                if (turnOnSound != null) audioSrc.PlayOneShot(turnOnSound);
                if (waterLoopSound != null)
                {
                    audioSrc.clip = waterLoopSound;
                    audioSrc.Play();
                }
            }
            else
            {
                audioSrc.Stop();
            }
        }
    }
}
