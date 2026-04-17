using UnityEngine;
using UnityEngine.UI;

public class PhoneFlickerEffect : MonoBehaviour
{
    [SerializeField] private Image phoneGlow;
    [SerializeField] private CanvasGroup phoneCanvasGroup;
    [SerializeField] private float flickerSpeed = 3f;
    [SerializeField] private float flickerIntensity = 0.05f;
    [SerializeField] private float randomFlickerChance = 0.02f;

    private float baseAlpha;
    private float timer;

    void Start()
    {
        if (phoneCanvasGroup != null)
            baseAlpha = phoneCanvasGroup.alpha;
        else
            baseAlpha = 1f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float sineFlicker = Mathf.Sin(timer * flickerSpeed) * flickerIntensity;
        float randomFlicker = 0f;

        if (Random.value < randomFlickerChance)
            randomFlicker = Random.Range(-0.08f, 0.08f);

        if (phoneCanvasGroup != null)
            phoneCanvasGroup.alpha = Mathf.Clamp01(baseAlpha + sineFlicker + randomFlicker);

        if (phoneGlow != null)
        {
            Color c = phoneGlow.color;
            c.a = Mathf.Clamp01(0.15f + sineFlicker * 2f + randomFlicker);
            phoneGlow.color = c;
        }
    }
}
