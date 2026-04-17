using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bridges the Doomify screen with the existing phone controller.
/// Handles showing/hiding Doomify screen and returning to home.
/// </summary>
public class DoomifyPhoneBridge : MonoBehaviour
{
    [SerializeField] private GamePhoneController phoneController;
    [SerializeField] private GameObject doomifyScreen;
    [SerializeField] private Button doomifyButton;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject homeScreen;

    void Start()
    {
        if (doomifyButton != null)
            doomifyButton.onClick.AddListener(OpenDoomify);
        if (backButton != null)
            backButton.onClick.AddListener(CloseDoomify);
    }

    private void OpenDoomify()
    {
        // Hide home screen, show doomify
        if (homeScreen != null) homeScreen.SetActive(false);
        if (doomifyScreen != null) doomifyScreen.SetActive(true);
    }

    private void CloseDoomify()
    {
        // Hide doomify, show home screen
        if (doomifyScreen != null) doomifyScreen.SetActive(false);
        if (homeScreen != null) homeScreen.SetActive(true);
    }
}
