using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MenuTransition : MonoBehaviour
{
    [SerializeField] private RectTransform phonePanel;
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup overlayFade;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup cornerUIGroup;
    [SerializeField] private string nextSceneName = "Game1 2";
    [SerializeField] private float zoomDuration = 1.2f;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private float targetScale = 3f;

    private bool isTransitioning;

    public void OnStartPressed()
    {
        if (!isTransitioning)
            StartCoroutine(TransitionCoroutine());
    }

    public void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnSettingsPressed()
    {
        Debug.Log("Settings pressed - not yet implemented");
    }

    private IEnumerator TransitionCoroutine()
    {
        isTransitioning = true;

        Vector3 startScale = phonePanel.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        Vector2 startPos = phonePanel.anchoredPosition;
        Vector2 endPos = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomDuration;
            float smooth = t * t * (3f - 2f * t);

            phonePanel.localScale = Vector3.Lerp(startScale, endScale, smooth);
            phonePanel.anchoredPosition = Vector2.Lerp(startPos, endPos, smooth);

            if (backgroundGroup != null)
                backgroundGroup.alpha = Mathf.Lerp(1f, 0f, smooth);
            if (titleGroup != null)
                titleGroup.alpha = Mathf.Lerp(1f, 0f, smooth * 2f);
            if (cornerUIGroup != null)
                cornerUIGroup.alpha = Mathf.Lerp(1f, 0f, smooth * 2f);

            yield return null;
        }

        if (overlayFade != null)
        {
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                overlayFade.alpha = t;
                yield return null;
            }
            overlayFade.alpha = 1f;
        }

        yield return new WaitForSeconds(0.3f);

        // Перед завантаженням скидаємо timeScale на випадок повернення з паузи
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("[MenuTransition] Scene '" + nextSceneName + "' is NOT in Build Settings or disabled. Add it via File > Build Settings.");
            // Скидаємо візуал, щоб користувач міг ще раз спробувати
            if (overlayFade != null) overlayFade.alpha = 0f;
            if (backgroundGroup != null) backgroundGroup.alpha = 1f;
            if (titleGroup != null) titleGroup.alpha = 1f;
            if (cornerUIGroup != null) cornerUIGroup.alpha = 1f;
            if (phonePanel != null)
            {
                phonePanel.localScale = Vector3.one;
                phonePanel.anchoredPosition = Vector2.zero;
            }
            isTransitioning = false;
        }
    }
}
