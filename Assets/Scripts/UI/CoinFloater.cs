using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Небольшой всплывающий "+N DC" на HUD когда игрок получает монеты.
/// Создаётся автоматически на первом Canvas.
/// </summary>
public class CoinFloater : MonoBehaviour
{
    private static Canvas cachedCanvas;
    private static TMP_FontAsset cachedFont;

    public static void Spawn(int amount)
    {
        if (cachedCanvas == null)
        {
            // ищем верхний оверлейный canvas — обычно GameUICanvas
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    if (cachedCanvas == null || c.sortingOrder > cachedCanvas.sortingOrder)
                        cachedCanvas = c;
                }
            }
            if (cachedCanvas == null) return;
        }

        if (cachedFont == null)
            cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        var go = new GameObject("CoinFloater");
        go.transform.SetParent(cachedCanvas.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(260f, 56f);
        rt.anchoredPosition = new Vector2(-40f, -130f + Random.Range(-12f, 12f));

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = $"+{amount} DC";
        tmp.fontSize = 44f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Right;
        tmp.color = new Color(1f, 0.85f, 0.2f, 1f);
        tmp.raycastTarget = false;
        if (cachedFont != null) tmp.font = cachedFont;
        tmp.outlineWidth = 0.18f;
        tmp.outlineColor = Color.black;

        var fl = go.AddComponent<CoinFloater>();
        fl.StartCoroutine(fl.Animate(rt, tmp));
    }

    private IEnumerator Animate(RectTransform rt, TMP_Text txt)
    {
        Vector2 start = rt.anchoredPosition;
        Vector2 end = start + new Vector2(0f, 80f);
        float dur = 1.2f;
        float t = 0f;
        Color orig = txt.color;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            rt.anchoredPosition = Vector2.Lerp(start, end, k);
            txt.color = new Color(orig.r, orig.g, orig.b, Mathf.Lerp(1f, 0f, k));
            yield return null;
        }
        Destroy(gameObject);
    }
}
