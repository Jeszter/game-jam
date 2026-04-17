using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WakeUpSequence : MonoBehaviour
{
    [Header("Refs (optional)")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform phone;
    [SerializeField] private Transform phoneStartAnchor;
    [SerializeField] private Transform lookTarget;

    [Header("Camera In Bed")]
    [SerializeField] private bool snapCameraToBedView = true;
    [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 1.58f, 0.06f);
    [SerializeField] private Vector3 cameraLocalEuler = new Vector3(0f, -88f, 0f);

    [Header("Timeline")]
    [SerializeField] private float darkTime = 4f;
    [SerializeField] private float shortBlinkOpen = 0.14f;
    [SerializeField] private float awakeLookTime = 3.5f;
    [SerializeField] private float longBlinkClose = 0.14f;
    [SerializeField] private float longBlinkHold = 3.5f;
    [SerializeField] private float longBlinkOpen = 0.2f;
    [SerializeField] private float turnToNightstandTime = 3.5f;
    [SerializeField] private float pickPhoneTime = 1.4f;
    [SerializeField] private float pauseBeforePick = 0.2f;

    [Header("Micro movement")]
    [SerializeField] private float sleepyYaw = 0.7f;
    [SerializeField] private float sleepyPitch = 0.45f;
    [SerializeField] private float sleepySpeed = 1.1f;

    [Header("Phone Near Face")]
    [SerializeField] private Vector3 nearFaceLocalPos = new Vector3(0.22f, -0.12f, 0.42f);
    [SerializeField] private Vector3 nearFaceLocalEuler = new Vector3(7f, 175f, -3f);

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    private Transform nearFaceAnchor;
    private CanvasGroup blinkOverlay;
    private Quaternion baseCamLocalRot;
    private bool started;

    private void Awake()
    {
        AutoBind();

        if (cameraTransform == null)
        {
            Log("No camera found, sequence will not run.");
            enabled = false;
            return;
        }

        DisableOtherCameraScripts();

        if (snapCameraToBedView)
        {
            cameraTransform.SetParent(transform, false);
            cameraTransform.localPosition = cameraLocalPosition;
            cameraTransform.localRotation = Quaternion.Euler(cameraLocalEuler);
        }

        baseCamLocalRot = cameraTransform.localRotation;
        nearFaceAnchor = CreateNearFaceAnchor(cameraTransform);
        blinkOverlay = CreateOrGetBlinkOverlay();

        if (phone != null)
        {
            if (phoneStartAnchor == null)
            {
                GameObject start = new GameObject("PhoneStartAnchor_Auto");
                start.transform.SetPositionAndRotation(phone.position, phone.rotation);
                phoneStartAnchor = start.transform;
            }

            phone.SetPositionAndRotation(phoneStartAnchor.position, phoneStartAnchor.rotation);
        }

        if (lookTarget == null)
            lookTarget = phoneStartAnchor != null ? phoneStartAnchor : phone;
    }

    private void OnEnable()
    {
        if (!started)
        {
            started = true;
            StartCoroutine(Play());
        }
    }

    private IEnumerator Play()
    {
        Log("Sequence started");

        if (blinkOverlay != null)
            blinkOverlay.alpha = 1f;

        if (darkTime > 0f)
            yield return new WaitForSeconds(darkTime);

        yield return FadeBlink(1f, 0f, shortBlinkOpen);
        yield return SleepyLook(awakeLookTime);
        yield return FadeBlink(0f, 1f, longBlinkClose);

        if (longBlinkHold > 0f)
            yield return new WaitForSeconds(longBlinkHold);

        yield return FadeBlink(1f, 0f, longBlinkOpen);
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
            cameraTransform.localRotation = baseCamLocalRot * Quaternion.Euler(pitch, yaw, 0f);
            yield return null;
        }

        cameraTransform.localRotation = baseCamLocalRot;
    }

    private IEnumerator TurnToNightstand()
    {
        if (turnToNightstandTime <= 0f)
            yield break;

        if (lookTarget == null)
        {
            Log("Turn skipped: lookTarget is missing.");
            yield break;
        }

        Quaternion from = cameraTransform.rotation;
        Quaternion to = Quaternion.LookRotation((lookTarget.position - cameraTransform.position).normalized, Vector3.up);

        float elapsed = 0f;
        while (elapsed < turnToNightstandTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / turnToNightstandTime);
            float smooth = t * t * (3f - 2f * t);
            cameraTransform.rotation = Quaternion.Slerp(from, to, smooth);
            yield return null;
        }
    }

    private IEnumerator PickPhone()
    {
        if (phone == null)
        {
            Log("Pick skipped: phone missing.");
            yield break;
        }

        Vector3 startPos = phone.position;
        Quaternion startRot = phone.rotation;
        Vector3 endPos = nearFaceAnchor.position;
        Quaternion endRot = nearFaceAnchor.rotation;

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
        phone.SetParent(nearFaceAnchor, true);
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

    private void AutoBind()
    {
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        if (phone == null)
        {
            GameObject byName = GameObject.Find("smartphone+3d+model");
            if (byName != null) phone = byName.transform;
        }

        if (phone == null)
        {
            foreach (GameObject go in FindObjectsOfType<GameObject>())
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

    private void DisableOtherCameraScripts()
    {
        if (cameraTransform == null) return;

        PlayerCameraFollow follow = cameraTransform.GetComponent<PlayerCameraFollow>();
        if (follow != null) follow.enabled = false;

        BedWakeCutscene other = cameraTransform.GetComponent<BedWakeCutscene>();
        if (other != null) other.enabled = false;
    }

    private Transform CreateNearFaceAnchor(Transform cam)
    {
        Transform existing = cam.Find("PhoneNearFaceAnchor");
        if (existing != null) return existing;

        GameObject anchor = new GameObject("PhoneNearFaceAnchor");
        anchor.transform.SetParent(cam, false);
        anchor.transform.localPosition = nearFaceLocalPos;
        anchor.transform.localRotation = Quaternion.Euler(nearFaceLocalEuler);
        return anchor.transform;
    }

    private CanvasGroup CreateOrGetBlinkOverlay()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        Transform root;

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("WakeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            root = canvas.transform;
        }
        else
        {
            root = canvas.transform;
            Transform existing = root.Find("BlinkOverlay");
            if (existing != null)
            {
                CanvasGroup cg = existing.GetComponent<CanvasGroup>();
                if (cg != null) return cg;
            }
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

    private void Log(string text)
    {
        if (verboseLogs)
            Debug.Log("[WakeUpSequence] " + text, this);
    }
}
