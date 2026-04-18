using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD controller for Dopamine bar and DoomCoin counter.
/// Displayed at the top of the screen, outside the phone.
/// </summary>
public class GameHUDController : MonoBehaviour
{
    [Header("Dopamine")]
    [SerializeField] private Image dopamineBarFill;
    [SerializeField] private TMP_Text dopamineText;
    [SerializeField] private float maxDopamine = 100f;
    [SerializeField] private float currentDopamine = 65f;
    [SerializeField] private float dopamineDecayRate = 0.5f;

    [Header("DoomCoins")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private int currentCoins = 1250;

    [Header("Hunger (optional — auto-created if fields empty)")]
    [SerializeField] private Image hungerBarFill;
    [SerializeField] private TMP_Text hungerText;
    [SerializeField] private Color hungerHighColor = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private Color hungerLowColor = new Color(0.7f, 0.2f, 0.2f);
    [SerializeField] private bool autoCreateHungerBar = true;

    [Header("Dopamine Bar Colors")]
    [SerializeField] private Color highDopamineColor = new Color(0.2f, 0.9f, 0.4f);
    [SerializeField] private Color midDopamineColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color lowDopamineColor = new Color(0.9f, 0.2f, 0.2f);

    private float displayedDopamine;
    private int displayedCoins;
    private float displayedHunger;

    void Start()
    {
        displayedDopamine = currentDopamine;
        displayedCoins = currentCoins;

        // Ensure GameEconomy exists in the scene
        if (GameEconomy.Instance == null)
        {
            var econGo = new GameObject("GameEconomy");
            econGo.AddComponent<GameEconomy>();
        }

        if (autoCreateHungerBar && hungerBarFill == null)
            TryAutoBuildHungerBar();

        displayedHunger = GameEconomy.Instance != null ? GameEconomy.Instance.CurrentHunger : 100f;

        UpdateUI();
    }

    void Update()
    {
        // Dopamine slowly decays
        currentDopamine -= dopamineDecayRate * Time.deltaTime;
        currentDopamine = Mathf.Clamp(currentDopamine, 0f, maxDopamine);

        // Smooth display
        displayedDopamine = Mathf.Lerp(displayedDopamine, currentDopamine, Time.deltaTime * 8f);
        displayedCoins = (int)Mathf.Lerp(displayedCoins, currentCoins, Time.deltaTime * 10f);

        if (GameEconomy.Instance != null)
            displayedHunger = Mathf.Lerp(displayedHunger, GameEconomy.Instance.CurrentHunger, Time.deltaTime * 6f);

        UpdateUI();
    }

    private void TryAutoBuildHungerBar()
    {
        // Create a small hunger bar below the dopamine bar, cloning its parent layout
        if (dopamineBarFill == null) return;
        var dopeBG = dopamineBarFill.transform.parent;
        if (dopeBG == null) return;
        var dopeBGRect = dopeBG as RectTransform;
        if (dopeBGRect == null) return;

        // Container for hunger bar
        var hungerBG = new GameObject("HungerBarBG", typeof(RectTransform));
        hungerBG.transform.SetParent(dopeBG.parent, false);
        var bgRt = (RectTransform)hungerBG.transform;
        bgRt.anchorMin = dopeBGRect.anchorMin;
        bgRt.anchorMax = dopeBGRect.anchorMax;
        bgRt.pivot = dopeBGRect.pivot;
        bgRt.sizeDelta = new Vector2(dopeBGRect.sizeDelta.x * 0.75f, 18f);
        bgRt.anchoredPosition = dopeBGRect.anchoredPosition + new Vector2(0f, -(dopeBGRect.sizeDelta.y * 0.5f + 14f));
        var bgImg = hungerBG.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.55f);
        bgImg.raycastTarget = false;

        // Fill
        var fillGO = new GameObject("HungerBarFill", typeof(RectTransform));
        fillGO.transform.SetParent(hungerBG.transform, false);
        var frt = (RectTransform)fillGO.transform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(2f, 2f);
        frt.offsetMax = new Vector2(-2f, -2f);
        hungerBarFill = fillGO.AddComponent<Image>();
        hungerBarFill.type = Image.Type.Filled;
        hungerBarFill.fillMethod = Image.FillMethod.Horizontal;
        hungerBarFill.fillAmount = 1f;
        hungerBarFill.color = hungerHighColor;
        hungerBarFill.raycastTarget = false;

        // Text
        var txtGO = new GameObject("HungerText", typeof(RectTransform));
        txtGO.transform.SetParent(hungerBG.transform, false);
        var trt = (RectTransform)txtGO.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        hungerText = txtGO.AddComponent<TextMeshProUGUI>();
        hungerText.alignment = TextAlignmentOptions.Center;
        hungerText.fontSize = 14f;
        hungerText.color = Color.white;
        hungerText.text = "🍽 100%";
        hungerText.raycastTarget = false;
        var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (f != null) hungerText.font = f;
    }

    private void UpdateUI()
    {
        float ratio = displayedDopamine / maxDopamine;

        if (dopamineBarFill != null)
        {
            dopamineBarFill.fillAmount = ratio;

            // Color gradient based on level
            if (ratio > 0.5f)
                dopamineBarFill.color = Color.Lerp(midDopamineColor, highDopamineColor, (ratio - 0.5f) * 2f);
            else
                dopamineBarFill.color = Color.Lerp(lowDopamineColor, midDopamineColor, ratio * 2f);
        }

        if (dopamineText != null)
            dopamineText.text = $"{Mathf.RoundToInt(displayedDopamine)}%";

        if (coinText != null)
            coinText.text = $"{displayedCoins}";

        // Hunger bar
        if (hungerBarFill != null && GameEconomy.Instance != null)
        {
            float hungerRatio = Mathf.Clamp01(displayedHunger / GameEconomy.Instance.MaxHunger);
            hungerBarFill.fillAmount = hungerRatio;
            hungerBarFill.color = Color.Lerp(hungerLowColor, hungerHighColor, hungerRatio);
        }
        if (hungerText != null)
            hungerText.text = $"🍽 {Mathf.RoundToInt(displayedHunger)}%";
    }

    public void AddDopamine(float amount)
    {
        currentDopamine = Mathf.Clamp(currentDopamine + amount, 0f, maxDopamine);
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
    }

    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            return true;
        }
        return false;
    }

    public int GetCoins() => currentCoins;
    public float GetDopamine() => currentDopamine;
}
