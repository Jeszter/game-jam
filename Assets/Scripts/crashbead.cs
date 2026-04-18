using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public class BedWakeCutscene : MonoBehaviour
{
    [Header("Target - where the player ends up after standing")]
    public Transform standUpPoint;

    [Header("Phone (optional)")]
    public Transform phone;
    public Transform phoneStartAnchor;
    public Transform lookTarget;
    public Vector3 phoneNearFaceLocalPos   = new Vector3(0.18f, -0.1f, 0.38f);
    public Vector3 phoneNearFaceLocalEuler = new Vector3(10f, 178f, 0f);

    [Header("Blinking (eyelids)")]
    public float firstBlinkCloseTime = 0.12f;
    public float firstBlinkHoldTime  = 0.08f;
    public float firstBlinkOpenTime  = 0.15f;
    public float betweenBlinksDelay = 0.35f;
    public float longBlinkCloseTime = 0.35f;
    public float longBlinkHoldTime  = 4.0f;
    public float longBlinkOpenTime  = 0.6f;

    [Header("Look around")]
    public float lookSideAngle      = 35f;
    public float lookToLeftTime     = 0.8f;
    public float holdLeftTime       = 0.4f;
    public float lookToRightTime    = 1.2f;
    public float holdRightTime      = 0.4f;
    public float lookBackCenterTime = 0.7f;

    [Header("Standing up")]
    public float sitUpDuration = 1.2f;
    public float standUpDuration = 1.1f;

    [Header("Camera")]
    public float finalCameraHeight = 1.75f;
    public Vector3 cameraLyingLocalOffset = new Vector3(0.08f, 0.0f, -0.33f);

    [Header("Voice Lines")]
    [Tooltip("Категория фраз при долгом закрытии глаз (groggy/стон)")]
    public string voiceGroggy = "groggy";
    [Tooltip("Категория фраз когда осматривается")]
    public string voiceLookAround = "look_around";
    [Tooltip("Категория фраз когда сел/встаёт")]
    public string voiceSitUp = "sit_up";
    [Tooltip("Категория фраз когда встал окончательно")]
    public string voiceStoodUp = "stood_up";

    private PlayerMovement      pm;
    private CharacterController cc;
    private Transform           camTransform;

    private Canvas eyelidCanvas;
    private Image  eyelidImage;

    private void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        cc = GetComponent<CharacterController>();

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null) camTransform = cam.transform;

        if (pm != null)
        {
            pm.phoneLock = true;
            pm.enabled   = false;
        }
        if (cc != null) cc.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        SetupEyelidOverlay();
    }

    private void Start()
    {
        StartCoroutine(WakeUpRoutine());
    }

    private void SetupEyelidOverlay()
    {
        GameObject go = new GameObject("EyelidOverlay");
        go.transform.SetParent(transform, false);

        eyelidCanvas = go.AddComponent<Canvas>();
        eyelidCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        eyelidCanvas.sortingOrder = 32000;

        go.AddComponent<CanvasScaler>();

        GameObject imgGo = new GameObject("Eyelid");
        imgGo.transform.SetParent(go.transform, false);
        eyelidImage = imgGo.AddComponent<Image>();
        eyelidImage.color = Color.black;
        eyelidImage.raycastTarget = false;

        RectTransform rt = eyelidImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        SetEyelidAlpha(1f);
    }

    private void SetEyelidAlpha(float a)
    {
        if (eyelidImage == null) return;
        Color c = eyelidImage.color;
        c.a = Mathf.Clamp01(a);
        eyelidImage.color = c;
    }

    private IEnumerator FadeEyelid(float from, float to, float time)
    {
        if (time <= 0f) { SetEyelidAlpha(to); yield break; }
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            SetEyelidAlpha(Mathf.Lerp(from, to, k));
            yield return null;
        }
        SetEyelidAlpha(to);
    }

    private void PlayVoice(string category)
    {
        if (VoiceManager.Instance != null && !string.IsNullOrEmpty(category))
            VoiceManager.Instance.Play(category);
    }

    private IEnumerator WakeUpRoutine()
    {
        Vector3    startPos          = transform.position;
        Quaternion startRot          = transform.rotation;
        Vector3    camStartLocalPos  = camTransform != null ? camTransform.localPosition : cameraLyingLocalOffset;
        Quaternion camStartLocalRot  = camTransform != null ? camTransform.localRotation : Quaternion.identity;

        Vector3    endPos = standUpPoint != null ? standUpPoint.position : startPos;
        Quaternion endRot = standUpPoint != null
            ? Quaternion.Euler(0f, standUpPoint.eulerAngles.y, 0f)
            : Quaternion.Euler(0f, startRot.eulerAngles.y, 0f);
        Vector3    camEndLocalPos = new Vector3(0f, finalCameraHeight, 0f);
        Quaternion camEndLocalRot = Quaternion.identity;

        // 1) start closed, open eyes, quick blink
        SetEyelidAlpha(1f);
        yield return new WaitForSeconds(0.25f);
        yield return FadeEyelid(1f, 0f, firstBlinkOpenTime);
        yield return new WaitForSeconds(firstBlinkHoldTime);
        yield return FadeEyelid(0f, 1f, firstBlinkCloseTime);
        yield return FadeEyelid(1f, 0f, firstBlinkOpenTime);

        yield return new WaitForSeconds(betweenBlinksDelay);

        // 2) long sleepy close — играем groggy фразу ("ugh..." / "five more minutes")
        PlayVoice(voiceGroggy);
        yield return FadeEyelid(0f, 1f, longBlinkCloseTime);
        yield return new WaitForSeconds(longBlinkHoldTime);
        yield return FadeEyelid(1f, 0f, longBlinkOpenTime);

        yield return new WaitForSeconds(0.25f);

        // 3) look around — "what time is it..." / "another day..."
        PlayVoice(voiceLookAround);
        yield return RotateCameraYaw(camStartLocalRot, -lookSideAngle, lookToLeftTime, 0f);
        yield return new WaitForSeconds(holdLeftTime);
        yield return RotateCameraYaw(camStartLocalRot, +lookSideAngle, lookToRightTime, -lookSideAngle);
        yield return new WaitForSeconds(holdRightTime);
        yield return RotateCameraYaw(camStartLocalRot, 0f, lookBackCenterTime, +lookSideAngle);

        // 4) short blink before standing up
        yield return FadeEyelid(0f, 1f, firstBlinkCloseTime);
        yield return new WaitForSeconds(firstBlinkHoldTime);
        yield return FadeEyelid(1f, 0f, firstBlinkOpenTime);

        // 5) sit up — "alright..." / "come on..."
        PlayVoice(voiceSitUp);

        Quaternion sittingRot     = Quaternion.Euler(0f, endRot.eulerAngles.y, 0f);
        Vector3    camSittingLocal = Vector3.Lerp(camStartLocalPos, camEndLocalPos, 0.55f);

        Quaternion rotBeforeSit      = transform.rotation;
        Vector3    camLocalBeforeSit = camTransform != null ? camTransform.localPosition : camStartLocalPos;
        Quaternion camRotBeforeSit   = camTransform != null ? camTransform.localRotation : Quaternion.identity;

        float t = 0f;
        while (t < sitUpDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / sitUpDuration);
            float s = Ease(k);

            transform.rotation = Quaternion.Slerp(rotBeforeSit, sittingRot, s);
            if (camTransform != null)
            {
                camTransform.localPosition = Vector3.Lerp(camLocalBeforeSit, camSittingLocal, s);
                camTransform.localRotation = Quaternion.Slerp(camRotBeforeSit, Quaternion.identity, s);
            }
            yield return null;
        }

        // 6) stand up and walk
        Vector3    standStartPos    = transform.position;
        Quaternion standStartRot    = transform.rotation;
        Vector3    camStandStartLoc = camTransform != null ? camTransform.localPosition : camSittingLocal;
        Quaternion camStandStartRot = camTransform != null ? camTransform.localRotation : Quaternion.identity;

        t = 0f;
        while (t < standUpDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / standUpDuration);
            float s = Ease(k);

            transform.position = Vector3.Lerp(standStartPos, endPos, s);
            transform.rotation = Quaternion.Slerp(standStartRot, endRot, s);
            if (camTransform != null)
            {
                camTransform.localPosition = Vector3.Lerp(camStandStartLoc, camEndLocalPos, s);
                camTransform.localRotation = Quaternion.Slerp(camStandStartRot, camEndLocalRot, s);
            }
            yield return null;
        }

        // finalize
        transform.position = endPos;
        transform.rotation = endRot;
        if (camTransform != null)
        {
            camTransform.localPosition = camEndLocalPos;
            camTransform.localRotation = camEndLocalRot;
        }

        if (eyelidCanvas != null) Destroy(eyelidCanvas.gameObject);

        if (cc != null) cc.enabled = true;
        if (pm != null)
        {
            pm.enabled   = true;
            pm.phoneLock = false;
        }

        // 7) фраза когда окончательно встал
        PlayVoice(voiceStoodUp);
    }

    private IEnumerator RotateCameraYaw(Quaternion baseRot, float toYaw, float time, float fromYaw)
    {
        if (camTransform == null) yield break;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            float s = Ease(k);
            float yaw = Mathf.Lerp(fromYaw, toYaw, s);
            camTransform.localRotation = baseRot * Quaternion.Euler(0f, yaw, 0f);
            yield return null;
        }
        camTransform.localRotation = baseRot * Quaternion.Euler(0f, toYaw, 0f);
    }

    private static float Ease(float x)
    {
        return x < 0.5f
            ? 2f * x * x
            : 1f - Mathf.Pow(-2f * x + 2f, 2f) * 0.5f;
    }
}

