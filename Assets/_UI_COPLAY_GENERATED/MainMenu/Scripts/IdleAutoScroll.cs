using UnityEngine;
using UnityEngine.InputSystem;

public class IdleAutoScroll : MonoBehaviour
{
    [SerializeField] private RectTransform phoneContent;
    [SerializeField] private float idleTimeout = 5f;
    [SerializeField] private float scrollSpeed = 15f;
    [SerializeField] private float maxScrollDistance = 80f;
    [SerializeField] private CanvasGroup fadeOverlay;

    private float idleTimer;
    private bool isScrolling;
    private float scrollAmount;
    private Vector2 originalPosition;
    private Vector2 lastMousePos;

    void Start()
    {
        if (phoneContent != null)
            originalPosition = phoneContent.anchoredPosition;
        idleTimer = 0f;
        lastMousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    void Update()
    {
        bool hasInput = false;

        if (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
            hasInput = true;

        if (Mouse.current != null)
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            if (Vector2.Distance(currentMousePos, lastMousePos) > 1f)
                hasInput = true;
            if (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed)
                hasInput = true;
            lastMousePos = currentMousePos;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            hasInput = true;

        if (hasInput)
        {
            idleTimer = 0f;
            if (isScrolling)
            {
                isScrolling = false;
                scrollAmount = 0f;
                if (phoneContent != null)
                    phoneContent.anchoredPosition = originalPosition;
                if (fadeOverlay != null)
                    fadeOverlay.alpha = 0f;
            }
            return;
        }

        idleTimer += Time.deltaTime;

        if (idleTimer >= idleTimeout && !isScrolling)
        {
            isScrolling = true;
            scrollAmount = 0f;
        }

        if (isScrolling && phoneContent != null)
        {
            scrollAmount += scrollSpeed * Time.deltaTime;
            scrollAmount = Mathf.Min(scrollAmount, maxScrollDistance);
            phoneContent.anchoredPosition = originalPosition + Vector2.down * scrollAmount;

            if (fadeOverlay != null)
            {
                float t = scrollAmount / maxScrollDistance;
                fadeOverlay.alpha = t * 0.3f;
            }
        }
    }

    public void ResetIdle()
    {
        idleTimer = 0f;
        isScrolling = false;
        scrollAmount = 0f;
        if (phoneContent != null)
            phoneContent.anchoredPosition = originalPosition;
    }
}
