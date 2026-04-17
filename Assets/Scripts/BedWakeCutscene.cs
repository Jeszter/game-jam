using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BedWakeCutscene : MonoBehaviour
{
    [Header("Scene Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform phone;
    [SerializeField] private Transform phoneStartAnchor;
    [SerializeField] private Transform lookTarget;

    [Header("Camera Placement")]
    [SerializeField] private bool parentCameraToPlayer = true;
    [SerializeField] private Vector3 firstPersonLocalPosition = new Vector3(0f, 1.6f, 0.02f);
    [SerializeField] private Vector3 firstPersonLocalEuler = Vector3.zero;

    [Header("Timeline")]
    [SerializeField] private float darkTime = 4f;
    [SerializeField] private float firstOpenTime = 0.14f;
    [SerializeField] private float awakeLookTime = 3.5f;
    [SerializeField] private float longCloseTime = 0.14f;
    [SerializeField] private float longCloseHoldTime = 3.5f;
    [SerializeField] private float longOpenTime = 0.2f;
    [SerializeField] private float turnToNightstandTime = 3.5f;
    [SerializeField] private float pickPhoneTime = 1.4f;
    [SerializeField] private float pauseBeforePick = 0.2f;

    [Header("Micro Motion")]
    [SerializeField] private float sleepyYaw = 0.7f;
    [SerializeField] private float sleepyPitch = 0.45f;
    [SerializeField] private float sleepySpeed = 1.1f;

    [Header("Phone Near Face")]
    [SerializeField] private Vector3 phoneNearFaceLocalPos = new Vector3(0.22f, -0.12f, 0.42f);
    [SerializeField] private Vector3 phoneNearFaceLocalEuler = new Vector3(7f, 175f, -3f);
    [SerializeField] private bool verboseLogs = true;

    private CanvasGroup blinkOverlay;
    private Transform phoneNearFaceAnchor;
    private Quaternion baseLocalRotation;

    private void Awake()
    {
        TryAutoBind();
        DisableOtherControllers();

        if (parentCameraToPlayer && player != null)
        {
            transform.SetParent(player, false);
            transform.localPosition = firstPersonLocalPosition;
            transform.localRotation = Quaternion.Euler(firstPersonLocalEuler);
        }
        else if (parentCameraToPlayer && player == null)
        {
            parentCameraToPlayer = false;
            Log("Player not found. Keeping camera at current transform.");
        }

        baseLocalRotation = transform.localRotation;
        phoneNearFaceAnchor = CreateNearFaceAnchor();
        blinkOverlay = CreateOrGetBlinkOverlay();

        if (phone != null && phoneStartAnchor != null)
            phone.SetPositionAndRotation(phoneStartAnchor.position, phoneStartAnchor.rotation);

        if (phone != null && phoneStartAnchor == null)
        {
            GameObject start = new GameObject("PhoneStartAnchor_Auto");
            start.transform.SetPositionAndRotation(phone.position, phone.rotation);
            phoneStartAnchor = start.transform;
        }

        if (lookTarget == null)
            lookTarget = phoneStartAnchor != null ? phoneStartAnchor : phone;

        Log("Awake complete");
    }

    private void OnEnable()
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        Log("Sequence started");

        if (blinkOverlay != null)
            blinkOverlay.alpha = 1f;

        if (darkTime > 0f)
            yield return new WaitForSeconds(darkTime);

        yield return FadeBlink(1f, 0f, firstOpenTime);
        yield return SleepyLook(awakeLookTime);
        yield return FadeBlink(0f, 1f, longCloseTime);
        if (longCloseHoldTime > 0f)
            yield return new WaitForSeconds(longCloseHoldTime);
        yield return FadeBlink(1f, 0f, longOpenTime);
        yield return TurnToNightstand();

        if (pauseBeforePick > 0f)
            yield return new WaitForSeconds(pauseBeforePick);

        yield return PickPhone();
        Log("Sequence finished");
    }

    private IEnumerator SleepyLook(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float yaw = Mathf.Sin(elapsed * sleepySpeed) * sleepyYaw;
            float pitch = Mathf.Sin(elapsed * sleepySpeed * 0.77f + 0.4f) * sleepyPitch;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(pitch, yaw, 0f);
            yield return null;
        }

        transform.localRotation = baseLocalRotation;
    }

    private IEnumerator TurnToNightstand()
    {
        if (turnToNightstandTime <= 0f)
            yield break;

        Vector3 targetPos;
        if (lookTarget != null)
            targetPos = lookTarget.position;
        else if (phone != null)
            targetPos = phone.position;
        else
        {
            Log("Turn skipped: no look target and no phone.");
            yield break;
        }

        Quaternion from = transform.rotation;
        Quaternion to = Quaternion.LookRotation((targetPos - transform.position).normalized, Vector3.up);

        float elapsed = 0f;
        while (elapsed < turnToNightstandTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / turnToNightstandTime);
            float smooth = t * t * (3f - 2f * t);
            transform.rotation = Quaternion.Slerp(from, to, smooth);
            yield return null;
        }
    }

    private IEnumerator PickPhone()
    {
        if (phone == null || phoneNearFaceAnchor == null || pickPhoneTime <= 0f)
        {
            Log("Pick skipped: missing phone or near-face anchor.");
            yield break;
        }

        Vector3 startPos = phone.position;
        Quaternion startRot = phone.rotation;
        Vector3 endPos = phoneNearFaceAnchor.position;
        Quaternion endRot = phoneNearFaceAnchor.rotation;

        float elapsed = 0f;
        while (elapsed < pickPhoneTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pickPhoneTime);
            float smooth = t * t * (3f - 2f * t);
            phone.position = Vector3.Lerp(startPos, endPos, smooth);
            phone.rotation = Quaternion.Slerp(startRot, endRot, smooth);
            yield return null;
        }

        phone.SetPositionAndRotation(endPos, endRot);
        phone.SetParent(phoneNearFaceAnchor, true);
    }

    private IEnumerator FadeBlink(float from, float to, float duration)
    {
        if (blinkOverlay == null)
            yield break;

        if (duration <= 0f)
        {
            blinkOverlay.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            blinkOverlay.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        blinkOverlay.alpha = to;
    }

    private void TryAutoBind()
    {
        if (player == null)
        {
            GameObject p = GameObject.Find("player");
            if (p != null)
                player = p.transform;
        }

        if (phone == null)
        {
            GameObject byName = GameObject.Find("smartphone+3d+model");
            if (byName != null)
                phone = byName.transform;
        }

        if (phone == null)
        {
            GameObject[] all = FindObjectsOfType<GameObject>();
            foreach (GameObject go in all)
            {
                string n = go.name.ToLowerInvariant();
                if (n.Contains("phone") || n.Contains("smartphone"))
                {
                    phone = go.transform;
                    break;
                }
            }
        }
    }

    private void DisableOtherControllers()
    {
        PlayerCameraFollow follow = GetComponent<PlayerCameraFollow>();
        if (follow != null)
            follow.enabled = false;

        WakeUpSequence old = GetComponent<WakeUpSequence>();
        if (old != null)
            old.enabled = false;
    }

    private Transform CreateNearFaceAnchor()
    {
        Transform existing = transform.Find("PhoneNearFaceAnchor");
        if (existing != null)
            return existing;

        GameObject anchor = new GameObject("PhoneNearFaceAnchor");
        anchor.transform.SetParent(transform, false);
        anchor.transform.localPosition = phoneNearFaceLocalPos;
        anchor.transform.localRotation = Quaternion.Euler(phoneNearFaceLocalEuler);
        return anchor.transform;
    }

    private CanvasGroup CreateOrGetBlinkOverlay()
    {
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        Transform root = null;

        if (existingCanvas != null)
        {
            Transform existing = existingCanvas.transform.Find("BlinkOverlay");
            if (existing != null)
            {
                CanvasGroup cg = existing.GetComponent<CanvasGroup>();
                if (cg != null)
                    return cg;
            }
            root = existingCanvas.transform;
        }
        else
        {
            GameObject canvasObj = new GameObject("WakeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            root = canvasObj.transform;
        }

        GameObject overlay = new GameObject("BlinkOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        overlay.transform.SetParent(root, false);

        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = overlay.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        return overlay.GetComponent<CanvasGroup>();
    }

    private void Log(string message)
    {
        if (verboseLogs)
            Debug.Log("[BedWakeCutscene] " + message, this);
    }
}
