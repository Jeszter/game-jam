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

    [Header("Dopamine Bar Colors")]
    [SerializeField] private Color highDopamineColor = new Color(0.2f, 0.9f, 0.4f);
    [SerializeField] private Color midDopamineColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color lowDopamineColor = new Color(0.9f, 0.2f, 0.2f);

    private float displayedDopamine;
    private int displayedCoins;

    void Start()
    {
        displayedDopamine = currentDopamine;
        displayedCoins = currentCoins;
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

        UpdateUI();
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
