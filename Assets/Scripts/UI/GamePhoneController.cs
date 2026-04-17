using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

/// <summary>
/// Controls the phone UI: slide in/out animation, app switching.
/// Press TAB to toggle phone. Phone slides up from bottom-right.
/// </summary>
public class GamePhoneController : MonoBehaviour
{
    [Header("Phone Panel")]
    [SerializeField] private RectTransform phonePanel;
    [SerializeField] private CanvasGroup phoneCanvasGroup;

    [Header("App Panels")]
    [SerializeField] private GameObject homeScreen;
    [SerializeField] private GameObject tikTokScreen;
    [SerializeField] private GameObject shopScreen;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private float hideOffsetY = -900f;

    [Header("Home Buttons")]
    [SerializeField] private Button tikTokButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button backButtonTikTok;
    [SerializeField] private Button backButtonShop;

    private bool isOpen;
    private bool isAnimating;
    private Vector2 shownPosition;
    private Vector2 hiddenPosition;

    private enum PhoneScreen { Home, TikTok, Shop }
    private PhoneScreen currentScreen = PhoneScreen.Home;

    void Start()
    {
        if (phonePanel != null)
        {
            shownPosition = phonePanel.anchoredPosition;
            hiddenPosition = shownPosition + new Vector2(0, hideOffsetY);
            phonePanel.anchoredPosition = hiddenPosition;
        }

        if (phoneCanvasGroup != null)
        {
            phoneCanvasGroup.alpha = 0f;
            phoneCanvasGroup.interactable = false;
            phoneCanvasGroup.blocksRaycasts = false;
        }

        // Wire up buttons
        if (tikTokButton != null) tikTokButton.onClick.AddListener(() => SwitchScreen(PhoneScreen.TikTok));
        if (shopButton != null) shopButton.onClick.AddListener(() => SwitchScreen(PhoneScreen.Shop));
        if (backButtonTikTok != null) backButtonTikTok.onClick.AddListener(() => SwitchScreen(PhoneScreen.Home));
        if (backButtonShop != null) backButtonShop.onClick.AddListener(() => SwitchScreen(PhoneScreen.Home));

        SwitchScreen(PhoneScreen.Home);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame && !isAnimating)
        {
            if (isOpen)
                ClosePhone();
            else
                OpenPhone();
        }
    }

    public void OpenPhone()
    {
        if (isOpen || isAnimating) return;
        SwitchScreen(PhoneScreen.Home);
        StartCoroutine(SlidePhone(true));
    }

    public void ClosePhone()
    {
        if (!isOpen || isAnimating) return;
        StartCoroutine(SlidePhone(false));
    }

    private IEnumerator SlidePhone(bool open)
    {
        isAnimating = true;

        Vector2 from = open ? hiddenPosition : shownPosition;
        Vector2 to = open ? shownPosition : hiddenPosition;
        float fromAlpha = open ? 0f : 1f;
        float toAlpha = open ? 1f : 0f;

        if (open && phoneCanvasGroup != null)
        {
            phoneCanvasGroup.interactable = true;
            phoneCanvasGroup.blocksRaycasts = true;
        }

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / slideDuration;
            // Smooth ease-out curve
            float smooth = 1f - Mathf.Pow(1f - t, 3f);

            if (phonePanel != null)
                phonePanel.anchoredPosition = Vector2.Lerp(from, to, smooth);
            if (phoneCanvasGroup != null)
                phoneCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, smooth);

            yield return null;
        }

        if (phonePanel != null)
            phonePanel.anchoredPosition = to;
        if (phoneCanvasGroup != null)
        {
            phoneCanvasGroup.alpha = toAlpha;
            if (!open)
            {
                phoneCanvasGroup.interactable = false;
                phoneCanvasGroup.blocksRaycasts = false;
            }
        }

        isOpen = open;
        isAnimating = false;
    }

    private void SwitchScreen(PhoneScreen screen)
    {
        currentScreen = screen;
        if (homeScreen != null) homeScreen.SetActive(screen == PhoneScreen.Home);
        if (tikTokScreen != null) tikTokScreen.SetActive(screen == PhoneScreen.TikTok);
        if (shopScreen != null) shopScreen.SetActive(screen == PhoneScreen.Shop);
    }
}
