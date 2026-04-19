using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD controller with AUTO-FIND, proper layout, and old element cleanup.
/// </summary>
public class GameHUDController : MonoBehaviour
{
    [Header("=== Auto-find (names in hierarchy) ===")]
    [SerializeField] private string dopamineBarFillName = "DopamineBarFill";
    [SerializeField] private string dopamineBarBGName   = "DopamineBarBG";
    [SerializeField] private string dopamineTextName    = "DopamineText";
    [SerializeField] private string dopamineLabelName   = "DopLabel";
    [SerializeField] private string coinTextName        = "CoinAmount";
    [SerializeField] private string coinIconName        = "CoinIcon";

    [Header("=== Manual override (optional) ===")]
    [SerializeField] private Image dopamineBarFill;
    [SerializeField] private Image dopamineBarBG;
    [SerializeField] private TMP_Text dopamineText;
    [SerializeField] private TMP_Text dopamineLabel;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Image coinIcon;

    [Header("Layout Fix")]
    [SerializeField] private bool disableLayoutGroup = true;
    // Збільшений HUD для кращої видимості. 3 компактні рядки: DOPAMINE / HUNGER / GOLD.
    [SerializeField] private Vector2 hudSize = new Vector2(500f, 150f);

    [Header("Auto-create Hunger Bar")]
    [SerializeField] private bool autoCreateHungerBar = true;
    private Image hungerBarFill;
    private TMP_Text hungerText;
    private TMP_Text hungerLabel;

    [Header("Values")]
    [SerializeField] private float maxDopamine = 100f;
    [SerializeField] private float currentDopamine = 65f;
    [SerializeField] private float dopamineDecayRate = 0.5f;
    [SerializeField] private int currentCoins = 1250;

    // ДОДАНО: префікс/суфікс для монет
    [Header("Coin Display")]
    [SerializeField] private string coinPrefix = "$ ";
    [SerializeField] private string coinSuffix = "";

    [Header("Style — Colors")]
    [SerializeField] private Color highDopamineColor = new Color(0.3f, 1f, 0.55f);
    [SerializeField] private Color midDopamineColor  = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color lowDopamineColor  = new Color(1f, 0.25f, 0.4f);
    [SerializeField] private Color hungerHighColor   = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private Color hungerLowColor    = new Color(0.8f, 0.15f, 0.15f);
    [SerializeField] private Color coinColor         = new Color(1f, 0.82f, 0.25f);

    [Header("Style — Panel Background")]
    [SerializeField] private bool addPanelBackground = false;
    [SerializeField] private Color panelBGColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color accentColor  = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private bool showAccentLines = false;

    [Header("Animation")]
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float lowPulseThreshold = 0.25f;

    private CanvasGroup dopamineGroup;
    private float displayedDopamine;
    private int   displayedCoins;
    private float displayedHunger;
    private float pulseTimer;

    // ДОДАНО: окремий текст-мітка для монет ("GOLD" над числом)
    private TMP_Text coinLabel;

    void Awake()
    {
        // Disable layout group immediately to prevent "torn" first frame
        if (disableLayoutGroup)
        {
            var hlg = GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.enabled = false;

            var vlg = GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.enabled = false;

            var csf = GetComponent<ContentSizeFitter>();
            if (csf != null) csf.enabled = false;
        }

        // Set HUD size immediately
        var hudRT = transform as RectTransform;
        if (hudRT != null)
        {
            hudRT.sizeDelta = hudSize;
        }
    }

    void Start()
    {
        AutoFindReferences();

        Debug.Log("=== GameHUDController ===");
        Debug.Log("dopamineLabel: "    + (dopamineLabel    != null ? dopamineLabel.name    : "NOT FOUND"));
        Debug.Log("dopamineText: "     + (dopamineText     != null ? dopamineText.name     : "NOT FOUND"));
        Debug.Log("dopamineBarFill: "  + (dopamineBarFill  != null ? dopamineBarFill.name  : "NOT FOUND"));
        Debug.Log("dopamineBarBG: "    + (dopamineBarBG    != null ? dopamineBarBG.name    : "NOT FOUND"));
        Debug.Log("coinText: "         + (coinText         != null ? coinText.name         : "NOT FOUND"));

        displayedDopamine = currentDopamine;
        displayedCoins    = currentCoins;

        if (GameEconomy.Instance == null)
        {
            var econGo = new GameObject("GameEconomy");
            econGo.AddComponent<GameEconomy>();
        }
        displayedHunger = GameEconomy.Instance != null ? GameEconomy.Instance.CurrentHunger : 100f;

        FixLayout();
        StyleExistingElements();

        // Make HUD image transparent
        var hudImg = GetComponent<Image>();
        if (hudImg != null) hudImg.color = new Color(0f, 0f, 0f, 0f);

        if (addPanelBackground) AddBackgroundPanel();
        if (autoCreateHungerBar && hungerBarFill == null) CreateHungerBar();

        // ДОДАНО: створити мітку "GOLD" над монетами
        CreateCoinLabel();

        if (dopamineBarFill != null)
        {
            var parent = dopamineBarFill.transform.parent;
            if (parent != null)
            {
                dopamineGroup = parent.GetComponent<CanvasGroup>();
                if (dopamineGroup == null) dopamineGroup = parent.gameObject.AddComponent<CanvasGroup>();
            }
        }

        UpdateUI();
    }

    // ============================================================
    // FIX LAYOUT
    // ============================================================

    void FixLayout()
    {
        if (disableLayoutGroup)
        {
            var hlg = GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) { hlg.enabled = false; Debug.Log("[HUD] Disabled HorizontalLayoutGroup"); }

            var vlg = GetComponent<VerticalLayoutGroup>();
            if (vlg != null) { vlg.enabled = false; Debug.Log("[HUD] Disabled VerticalLayoutGroup"); }

            var csf = GetComponent<ContentSizeFitter>();
            if (csf != null) { csf.enabled = false; Debug.Log("[HUD] Disabled ContentSizeFitter"); }
        }

        var hudRT = transform as RectTransform;
        if (hudRT != null)
        {
            hudRT.sizeDelta = hudSize;
            hudRT.anchoredPosition = new Vector2(-10f, -10f);
            Debug.Log("[HUD] Set size to " + hudSize);
        }

        // ------- Позиції рядків (всі вирівняні по лівому краю HUD, спільний label-X=12, bar-X=130) -------
        const float rowDopY   = -22f;
        const float rowHungerY = -58f;
        const float rowGoldY  = -94f;
        const float labelX    = 12f;
        const float labelW    = 110f;
        const float barX      = 130f;
        const float barW      = 280f;
        const float rowH      = 28f;

        // DopLabel (row 1)
        if (dopamineLabel != null)
        {
            var rt = dopamineLabel.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(labelW, rowH);
            rt.anchoredPosition = new Vector2(labelX, rowDopY);
        }

        // DopamineBarBG (row 1)
        if (dopamineBarBG != null)
        {
            var rt = dopamineBarBG.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(barW, rowH);
            rt.anchoredPosition = new Vector2(barX, rowDopY);

            var le = dopamineBarBG.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = true;
        }

        // DopamineBarFill (inside BG — stretch)
        if (dopamineBarFill != null)
        {
            var rt = dopamineBarFill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
        }

        // DopamineText
        if (dopamineText != null)
        {
            var rt = dopamineText.rectTransform;
            if (dopamineBarBG != null && dopamineText.transform.parent == dopamineBarBG.transform)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(60f, 20f);
                rt.anchoredPosition = new Vector2(260f, -22f);
            }
        }

        // CoinAmount (row 3, праворуч від "GOLD"-мітки в кінці бар-ліній)
        if (coinText != null)
        {
            var rt = coinText.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(barW, rowH);
            rt.anchoredPosition = new Vector2(barX, rowGoldY);
        }

        // CoinIcon (may be Image or TMP_Text) — не використовуємо, ховаємо щоб не плавав
        {
            Transform coinIconT = null;
            if (coinIcon != null)
                coinIconT = coinIcon.transform;
            else
                coinIconT = FindChildByName(transform, coinIconName);

            if (coinIconT != null)
            {
                coinIconT.gameObject.SetActive(false);
            }
        }

        HideOldElements();
    }

    void HideOldElements()
    {
        string[] hideNames = { "DC", "DCLabel", "DCText", "Sep", "Separator", "CoinLabel", "DopLabelOld", "DPM" };

        foreach (var name in hideNames)
        {
            var found = FindChildByName(transform, name);
            if (found == null) continue;
            if (IsOurElement(found)) continue;
            found.gameObject.SetActive(false);
            Debug.Log("[HUD] Hidden old element by name: " + name);
        }

        var allTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in allTexts)
        {
            if (t == null) continue;
            if (IsOurElement(t.transform)) continue;

            string txt = t.text != null ? t.text.Trim().ToUpper() : "";
            if (txt == "DC" || txt == "DPM" || txt == "D" || txt == "C" ||
                txt == "DC:" || txt == "DPM:" || txt == "$")
            {
                t.gameObject.SetActive(false);
                Debug.Log("[HUD] Hidden leftover text: '" + t.text + "' on " + t.gameObject.name);
            }
        }
    }

    bool IsOurElement(Transform t)
    {
        if (t == null) return false;
        if (coinText    != null && t == coinText.transform)    return true;
        if (coinIcon    != null && t == coinIcon.transform)    return true;
        if (coinLabel   != null && t == coinLabel.transform)   return true;
        if (dopamineLabel  != null && t == dopamineLabel.transform)  return true;
        if (dopamineText   != null && t == dopamineText.transform)   return true;
        if (dopamineBarFill != null && t == dopamineBarFill.transform) return true;
        if (dopamineBarBG  != null && t == dopamineBarBG.transform)  return true;
        if (hungerLabel != null && t == hungerLabel.transform) return true;
        if (hungerText  != null && t == hungerText.transform)  return true;
        if (hungerBarFill != null && t == hungerBarFill.transform) return true;
        return false;
    }

    // ============================================================
    // AUTO-FIND
    // ============================================================

    void AutoFindReferences()
    {
        if (dopamineBarFill == null) dopamineBarFill = FindChildImage(dopamineBarFillName);
        if (dopamineBarBG   == null) dopamineBarBG   = FindChildImage(dopamineBarBGName);
        if (dopamineText    == null) dopamineText    = FindChildText(dopamineTextName);
        if (dopamineLabel   == null) dopamineLabel   = FindChildText(dopamineLabelName);
        if (coinText        == null) coinText        = FindChildText(coinTextName);
        if (coinIcon        == null) coinIcon        = FindChildImage(coinIconName);
    }

    Transform FindChildByName(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
                return child;
            var nested = FindChildByName(child, targetName);
            if (nested != null) return nested;
        }
        return null;
    }

    Image FindChildImage(string name)
    {
        var t = FindChildByName(transform, name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    TMP_Text FindChildText(string name)
    {
        var t = FindChildByName(transform, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    // ============================================================
    // CREATE COIN LABEL  (НОВИЙ МЕТОД)
    // ============================================================

    void CreateCoinLabel()
    {
        if (coinText == null) return;

        // Перевіряємо чи вже є
        var existing = FindChildByName(transform, "CoinTitleLabel");
        if (existing != null)
        {
            coinLabel = existing.GetComponent<TMP_Text>();
            return;
        }

        var go = new GameObject("CoinTitleLabel", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var rt = go.GetComponent<RectTransform>();
        // row 3 — узгоджено з FixLayout (rowGoldY = -94, labelX = 12, labelW = 110, rowH = 28)
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(110f, 28f);
        rt.anchoredPosition = new Vector2(12f, -94f);

        coinLabel = go.AddComponent<TextMeshProUGUI>();
        coinLabel.text      = "GOLD";
        coinLabel.fontSize  = 14f;
        coinLabel.fontStyle = FontStyles.Bold;
        coinLabel.characterSpacing = 2f;
        coinLabel.color     = coinColor;
        coinLabel.alignment = TextAlignmentOptions.MidlineLeft;
        coinLabel.enableWordWrapping = false;
        coinLabel.overflowMode = TextOverflowModes.Overflow;
        coinLabel.raycastTarget = false;
        AssignFont(coinLabel);

        Debug.Log("[HUD] CoinTitleLabel created");
    }

    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        currentDopamine -= dopamineDecayRate * Time.deltaTime;
        currentDopamine = Mathf.Clamp(currentDopamine, 0f, maxDopamine);

        displayedDopamine = Mathf.Lerp(displayedDopamine, currentDopamine, Time.deltaTime * 8f);
        displayedCoins    = (int)Mathf.Lerp(displayedCoins, currentCoins, Time.deltaTime * 10f);

        if (GameEconomy.Instance != null)
            displayedHunger = Mathf.Lerp(displayedHunger, GameEconomy.Instance.CurrentHunger, Time.deltaTime * 6f);

        pulseTimer += Time.deltaTime * pulseSpeed;
        if (dopamineGroup != null)
        {
            float ratio = displayedDopamine / maxDopamine;
            if (ratio < lowPulseThreshold)
                dopamineGroup.alpha = 0.7f + Mathf.Sin(pulseTimer * 3f) * 0.3f;
            else
                dopamineGroup.alpha = Mathf.Lerp(dopamineGroup.alpha, 1f, Time.deltaTime * 4f);
        }

        UpdateUI();
    }

    // ============================================================
    // STYLE
    // ============================================================

    void StyleExistingElements()
    {
        if (dopamineLabel != null)
        {
            dopamineLabel.text = "DOPAMINE";
            dopamineLabel.fontSize = 14f;
            dopamineLabel.fontStyle = FontStyles.Bold;
            dopamineLabel.characterSpacing = 2f;
            dopamineLabel.color = midDopamineColor;
            dopamineLabel.alignment = TextAlignmentOptions.MidlineLeft;
            dopamineLabel.enableWordWrapping = false;
            dopamineLabel.overflowMode = TextOverflowModes.Overflow;
        }

        if (dopamineText != null)
        {
            dopamineText.fontSize = 14f;
            dopamineText.fontStyle = FontStyles.Bold;
            dopamineText.color = Color.white;
            dopamineText.outlineWidth = 0.2f;
            dopamineText.outlineColor = new Color(0f, 0f, 0f, 0.9f);
            dopamineText.alignment = TextAlignmentOptions.Center;
            dopamineText.enableWordWrapping = false;
            dopamineText.overflowMode = TextOverflowModes.Overflow;
        }

        if (dopamineBarBG != null)
        {
            dopamineBarBG.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            var outline = dopamineBarBG.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
        }

        if (coinText != null)
        {
            coinText.fontSize = 22f;
            coinText.fontStyle = FontStyles.Bold;
            coinText.color = coinColor;
            coinText.outlineWidth = 0.2f;
            coinText.outlineColor = new Color(0.3f, 0.15f, 0f, 1f);
            coinText.alignment = TextAlignmentOptions.MidlineLeft;
            coinText.enableWordWrapping = false;
            coinText.overflowMode = TextOverflowModes.Overflow;
            coinText.enableAutoSizing = false;
        }

        if (coinIcon != null)
        {
            coinIcon.color = coinColor;
        }

        // CoinIcon might be a TMP_Text instead of Image — style it too
        var coinIconText = FindChildText(coinIconName);
        if (coinIconText != null)
        {
            coinIconText.fontSize = 22f;
            coinIconText.fontStyle = FontStyles.Bold;
            coinIconText.color = coinColor;
        }
    }

    // ============================================================
    // BACKGROUND PANEL
    // ============================================================

    void AddBackgroundPanel()
    {
        Transform hudContainer = transform;

        var existing = hudContainer.Find("StylizedBG");
        if (existing != null) return;

        var bg = new GameObject("StylizedBG", typeof(RectTransform));
        bg.transform.SetParent(hudContainer, false);
        bg.transform.SetAsFirstSibling();

        var bgRT = (RectTransform)bg.transform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        var bgImg = bg.AddComponent<Image>();
        bgImg.color = panelBGColor;
        bgImg.raycastTarget = false;

        if (showAccentLines)
        {
            var accent = new GameObject("TopAccent", typeof(RectTransform));
            accent.transform.SetParent(bg.transform, false);
            var aRT = (RectTransform)accent.transform;
            aRT.anchorMin = new Vector2(0f, 1f);
            aRT.anchorMax = new Vector2(1f, 1f);
            aRT.pivot = new Vector2(0.5f, 1f);
            aRT.sizeDelta = new Vector2(0f, 1f);
            aRT.anchoredPosition = Vector2.zero;
            var aImg = accent.AddComponent<Image>();
            aImg.color = accentColor;
            aImg.raycastTarget = false;

            var accentBot = new GameObject("BotAccent", typeof(RectTransform));
            accentBot.transform.SetParent(bg.transform, false);
            var abRT = (RectTransform)accentBot.transform;
            abRT.anchorMin = new Vector2(0f, 0f);
            abRT.anchorMax = new Vector2(1f, 0f);
            abRT.pivot = new Vector2(0.5f, 0f);
            abRT.sizeDelta = new Vector2(0f, 0.5f);
            abRT.anchoredPosition = Vector2.zero;
            var abImg = accentBot.AddComponent<Image>();
            abImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.1f);
            abImg.raycastTarget = false;
        }
    }

    // ============================================================
    // CREATE HUNGER BAR
    // ============================================================

    void CreateHungerBar()
    {
        if (dopamineBarBG == null || dopamineLabel == null) return;
        Transform hudContainer = transform;

        // Hunger label (row 2 — синхронізовано з FixLayout: rowHungerY=-58)
        var hLabelGO = new GameObject("HungerLabel", typeof(RectTransform));
        hLabelGO.transform.SetParent(hudContainer, false);
        var hlRT = (RectTransform)hLabelGO.transform;
        hlRT.anchorMin = new Vector2(0f, 1f);
        hlRT.anchorMax = new Vector2(0f, 1f);
        hlRT.pivot = new Vector2(0f, 0.5f);
        hlRT.sizeDelta = new Vector2(110f, 28f);
        hlRT.anchoredPosition = new Vector2(12f, -58f);
        hungerLabel = hLabelGO.AddComponent<TextMeshProUGUI>();
        hungerLabel.text = "HUNGER";
        hungerLabel.fontSize = 14f;
        hungerLabel.fontStyle = FontStyles.Bold;
        hungerLabel.characterSpacing = 2f;
        hungerLabel.color = hungerHighColor;
        hungerLabel.alignment = TextAlignmentOptions.MidlineLeft;
        hungerLabel.enableWordWrapping = false;
        hungerLabel.overflowMode = TextOverflowModes.Overflow;
        hungerLabel.raycastTarget = false;
        AssignFont(hungerLabel);

        // Hunger BG
        var hBgGO = new GameObject("HungerBarBG", typeof(RectTransform));
        hBgGO.transform.SetParent(hudContainer, false);
        var hbgRT = (RectTransform)hBgGO.transform;
        hbgRT.anchorMin = new Vector2(0f, 1f);
        hbgRT.anchorMax = new Vector2(0f, 1f);
        hbgRT.pivot = new Vector2(0f, 0.5f);
        hbgRT.sizeDelta = new Vector2(280f, 28f);
        hbgRT.anchoredPosition = new Vector2(130f, -58f);
        var hBgImg = hBgGO.AddComponent<Image>();
        hBgImg.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        hBgImg.raycastTarget = false;

        // Hunger Fill
        var hFillGO = new GameObject("HungerBarFill", typeof(RectTransform));
        hFillGO.transform.SetParent(hBgGO.transform, false);
        var hfRT = (RectTransform)hFillGO.transform;
        hfRT.anchorMin = Vector2.zero;
        hfRT.anchorMax = Vector2.one;
        hfRT.offsetMin = new Vector2(2f, 2f);
        hfRT.offsetMax = new Vector2(-2f, -2f);
        hungerBarFill = hFillGO.AddComponent<Image>();
        hungerBarFill.type = Image.Type.Filled;
        hungerBarFill.fillMethod = Image.FillMethod.Horizontal;
        hungerBarFill.fillAmount = 1f;
        hungerBarFill.color = hungerHighColor;
        hungerBarFill.raycastTarget = false;

        // Hunger Text
        var hTxtGO = new GameObject("HungerText", typeof(RectTransform));
        hTxtGO.transform.SetParent(hBgGO.transform, false);
        var htRT = (RectTransform)hTxtGO.transform;
        htRT.anchorMin = Vector2.zero;
        htRT.anchorMax = Vector2.one;
        htRT.offsetMin = Vector2.zero;
        htRT.offsetMax = Vector2.zero;
        hungerText = hTxtGO.AddComponent<TextMeshProUGUI>();
        hungerText.text = "100%";
        hungerText.alignment = TextAlignmentOptions.Center;
        hungerText.fontSize = 14f;
        hungerText.fontStyle = FontStyles.Bold;
        hungerText.color = Color.white;
        hungerText.outlineWidth = 0.2f;
        hungerText.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        hungerText.enableWordWrapping = false;
        hungerText.overflowMode = TextOverflowModes.Overflow;
        hungerText.raycastTarget = false;
        AssignFont(hungerText);

        Debug.Log("[HUD] Hunger bar created");
    }

    void AssignFont(TMP_Text t)
    {
        if (dopamineText != null && dopamineText.font != null)
            t.font = dopamineText.font;
    }

    // ============================================================
    // UPDATE UI
    // ============================================================

    void UpdateUI()
    {
        float ratio = displayedDopamine / maxDopamine;

        if (dopamineBarFill != null)
        {
            dopamineBarFill.fillAmount = ratio;
            Color c = ratio > 0.5f
                ? Color.Lerp(midDopamineColor, highDopamineColor, (ratio - 0.5f) * 2f)
                : Color.Lerp(lowDopamineColor, midDopamineColor, ratio * 2f);
            dopamineBarFill.color = c;
            if (dopamineLabel != null) dopamineLabel.color = c;
        }

        if (dopamineText != null)
            dopamineText.text = Mathf.RoundToInt(displayedDopamine) + "%";

        // ЗМІНЕНО: додано coinPrefix і coinSuffix навколо числа
        if (coinText != null)
        {
            string amount = displayedCoins.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture);
            coinText.text = coinPrefix + amount + coinSuffix;
        }

        if (hungerBarFill != null && GameEconomy.Instance != null)
        {
            float hungerRatio = Mathf.Clamp01(displayedHunger / GameEconomy.Instance.MaxHunger);
            hungerBarFill.fillAmount = hungerRatio;
            Color hc = Color.Lerp(hungerLowColor, hungerHighColor, hungerRatio);
            hungerBarFill.color = hc;
            if (hungerLabel != null) hungerLabel.color = hc;
        }

        if (hungerText != null)
            hungerText.text = Mathf.RoundToInt(displayedHunger) + "%";
    }

    // ============================================================
    // PUBLIC API
    // ============================================================

    public void AddDopamine(float amount) => currentDopamine = Mathf.Clamp(currentDopamine + amount, 0f, maxDopamine);
    public void AddCoins(int amount) => currentCoins += amount;
    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount) { currentCoins -= amount; return true; }
        return false;
    }
    public int GetCoins() => currentCoins;
    public float GetDopamine() => currentDopamine;
}
