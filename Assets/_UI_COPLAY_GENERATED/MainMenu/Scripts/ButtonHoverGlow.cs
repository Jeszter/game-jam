using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Makes the button's label text and icon glow (brighten) on hover.
/// No background rectangles — the text and icon themselves emit light.
/// Uses TMP material properties for a real glow/emission effect on text,
/// and color brightening on the icon.
/// </summary>
public class ButtonHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Glow Settings")]
    [SerializeField] private Color glowColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float glowIntensity = 0.4f;
    [SerializeField] private float glowFadeSpeed = 6f;

    [Header("Icon Settings")]
    [SerializeField] private float iconBrightnessBoost = 0.5f;

    private TextMeshProUGUI label;
    private Image iconImage;

    private Color originalLabelColor;
    private Color originalIconColor;
    private Material labelMaterialInstance;

    private bool isHovered;
    private float currentT; // 0 = normal, 1 = fully glowing

    // TMP shader property IDs
    private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowOffsetID = Shader.PropertyToID("_GlowOffset");
    private static readonly int GlowInnerID = Shader.PropertyToID("_GlowInner");
    private static readonly int GlowOuterID = Shader.PropertyToID("_GlowOuter");
    private static readonly int GlowPowerID = Shader.PropertyToID("_GlowPower");

    void Awake()
    {
        // Find label
        label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            originalLabelColor = label.color;
            // Create a material instance so we don't affect other TMP texts
            labelMaterialInstance = new Material(label.fontSharedMaterial);
            label.fontMaterial = labelMaterialInstance;
            // Initialize glow off
            SetTMPGlow(0f);
        }

        // Find icon
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Icon"))
            {
                iconImage = child.GetComponent<Image>();
                if (iconImage != null)
                {
                    originalIconColor = iconImage.color;
                    break;
                }
            }
        }
    }

    void Update()
    {
        float target = isHovered ? 1f : 0f;
        currentT = Mathf.MoveTowards(currentT, target, glowFadeSpeed * Time.unscaledDeltaTime);

        // Text glow via TMP shader
        if (label != null && labelMaterialInstance != null)
        {
            SetTMPGlow(currentT);

            // Also slightly brighten the text color
            Color brightLabel = Color.Lerp(originalLabelColor,
                new Color(
                    Mathf.Min(1f, originalLabelColor.r + glowIntensity * 0.3f),
                    Mathf.Min(1f, originalLabelColor.g + glowIntensity * 0.3f),
                    Mathf.Min(1f, originalLabelColor.b + glowIntensity * 0.3f),
                    originalLabelColor.a),
                currentT);
            label.color = brightLabel;
        }

        // Icon brightness
        if (iconImage != null)
        {
            Color brightIcon = Color.Lerp(originalIconColor,
                new Color(
                    Mathf.Min(1f, originalIconColor.r + iconBrightnessBoost),
                    Mathf.Min(1f, originalIconColor.g + iconBrightnessBoost * 0.4f),
                    Mathf.Min(1f, originalIconColor.b + iconBrightnessBoost * 0.4f),
                    Mathf.Min(1f, originalIconColor.a + 0.4f)),
                currentT);
            iconImage.color = brightIcon;
        }
    }

    private void SetTMPGlow(float t)
    {
        if (labelMaterialInstance == null) return;

        // TMP SDF shader glow properties
        Color gc = glowColor;
        gc.a = t * glowIntensity;
        labelMaterialInstance.SetColor(GlowColorID, gc);
        labelMaterialInstance.SetFloat(GlowOffsetID, 0f);
        labelMaterialInstance.SetFloat(GlowInnerID, Mathf.Lerp(0f, 0.15f, t));
        labelMaterialInstance.SetFloat(GlowOuterID, Mathf.Lerp(0f, 0.3f, t));
        labelMaterialInstance.SetFloat(GlowPowerID, Mathf.Lerp(0f, 0.8f, t));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    void OnDisable()
    {
        isHovered = false;
        currentT = 0f;
        if (label != null)
            label.color = originalLabelColor;
        if (iconImage != null)
            iconImage.color = originalIconColor;
        if (labelMaterialInstance != null)
            SetTMPGlow(0f);
    }

    void OnDestroy()
    {
        // Clean up material instance
        if (labelMaterialInstance != null)
            Destroy(labelMaterialInstance);
    }
}
