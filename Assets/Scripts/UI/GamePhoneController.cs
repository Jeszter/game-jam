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
    [SerializeField] private GameObject knifeScreen;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private float hideOffsetY = -900f;

    [Header("Home Buttons")]
    [SerializeField] private Button tikTokButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button knifeButton;
    [SerializeField] private Button backButtonTikTok;
    [SerializeField] private Button backButtonShop;
    [SerializeField] private Button backButtonKnife;

    private bool isOpen;
    private bool isAnimating;
    private Vector2 shownPosition;
    private Vector2 hiddenPosition;
    private KnifeHitGame knifeGame;

    private enum PhoneScreen { Home, TikTok, Shop, Knife }
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

        // Wire up buttons (with tap sfx)
        if (tikTokButton != null) tikTokButton.onClick.AddListener(() => { PlayTap(); SwitchScreen(PhoneScreen.TikTok); });
        if (shopButton != null) shopButton.onClick.AddListener(() => { PlayTap(); SwitchScreen(PhoneScreen.Shop); });
        if (knifeButton != null) knifeButton.onClick.AddListener(() => { PlayTap(); OpenKnifeGame(); });
        if (backButtonTikTok != null) backButtonTikTok.onClick.AddListener(() => { PlayTap(); SwitchScreen(PhoneScreen.Home); });
        if (backButtonShop != null) backButtonShop.onClick.AddListener(() => { PlayTap(); SwitchScreen(PhoneScreen.Home); });
        if (backButtonKnife != null) backButtonKnife.onClick.AddListener(() => { PlayTap(); CloseKnifeGame(); });

        // Hook any other buttons inside phone (bottom nav, etc) to play tap
        HookAllButtonsInPhone();

        SwitchScreen(PhoneScreen.Home);
    }

    private void HookAllButtonsInPhone()
    {
        if (phonePanel == null) return;
        var buttons = phonePanel.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            // Avoid double-hook: only add if not already wired by explicit handlers above
            if (b == tikTokButton || b == shopButton || b == knifeButton ||
                b == backButtonTikTok || b == backButtonShop || b == backButtonKnife)
                continue;
            b.onClick.AddListener(PlayTap);
        }
    }

    private void PlayTap()
    {
        var sm = SoundManager.Instance;
        if (sm != null) sm.PlayPhoneTap();
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
        PlayTap();
        StartCoroutine(SlidePhone(true));

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPlayerLookEnabled(false);
    }

    public void ClosePhone()
    {
        if (!isOpen || isAnimating) return;
        PlayTap();
        // If knife game open, clean it up
        CloseKnifeGame();
        StartCoroutine(SlidePhone(false));

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPlayerLookEnabled(true);
    }

    private void SetPlayerLookEnabled(bool enabled)
    {
        GameObject player = GameObject.Find("player");
        if (player != null)
        {
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.phoneLock = !enabled;
        }
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
        if (knifeScreen != null) knifeScreen.SetActive(screen == PhoneScreen.Knife);
    }

    private void OpenKnifeGame()
    {
        if (knifeScreen == null) return;
        SwitchScreen(PhoneScreen.Knife);

        // Instantiate the game inside the knife screen
        if (knifeGame == null)
        {
            knifeGame = knifeScreen.AddComponent<KnifeHitGame>();
            knifeGame.Init(knifeScreen, CloseKnifeGame);
        }
    }

    private void CloseKnifeGame()
    {
        if (knifeGame != null)
        {
            knifeGame.Cleanup();
            Destroy(knifeGame);
            knifeGame = null;
        }
        if (currentScreen == PhoneScreen.Knife)
            SwitchScreen(PhoneScreen.Home);
    }
}
