using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BedWakeCutscene : MonoBehaviour
{
    [Header("Телефон")]
    public Transform phone;
    public Transform phoneStartAnchor;
    public Transform lookTarget;

    [Header("Куди встає гравець")]
    [Tooltip("Empty біля ліжка на підлозі — де гравець буде стояти")]
    public Transform standUpPoint;

    [Header("Phone Near Face (local to Camera)")]
    public Vector3 phoneNearFaceLocalPos   = new Vector3(0.18f, -0.10f, 0.38f);
    public Vector3 phoneNearFaceLocalEuler = new Vector3(10f, 178f, 0f);

    private Image          overlayImage;
    private Transform      cam;
    private Transform      nearFaceAnchor;
    private PlayerMovement cachedMovement;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        Camera child = GetComponentInChildren<Camera>(true);
        cam = child != null ? child.transform : Camera.main?.transform;

        if (cam == null)
        {
            Debug.LogError("[BedWakeCutscene] Camera not found!", this);
            enabled = false;
            return;
        }

        // вимикаємо рух
        cachedMovement = GetComponent<PlayerMovement>();
        if (cachedMovement != null) cachedMovement.enabled = false;

        CreateUI();
        PlacePhone();
        nearFaceAnchor = BuildNearFaceAnchor();

        StartCoroutine(WakeSequence());
    }

    private void CreateUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject go = new GameObject("Canvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }
        GameObject ovGO = new GameObject("WakeOverlay");
        ovGO.transform.SetParent(canvas.transform, false);
        RectTransform rt = ovGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        overlayImage = ovGO.AddComponent<Image>();
        overlayImage.color = Color.black;
        overlayImage.raycastTarget = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    private IEnumerator WakeSequence()
    {
        Vector3    basePos = cam.localPosition;
        Quaternion baseRot = cam.localRotation;

        // ── 1. Чорний екран ───────────────────────────────────────────────
        overlayImage.color = Color.black;
        yield return new WaitForSeconds(4f);

        // ── 2. Перше кліпання ─────────────────────────────────────────────
        yield return StartCoroutine(Fade(1f, 0f, 0.12f));
        yield return StartCoroutine(SleepyLook(baseRot, 3.5f));
        yield return StartCoroutine(Fade(0f, 1f, 0.10f));
        yield return new WaitForSeconds(3.0f);

        // ── 3. Прокидується ───────────────────────────────────────────────
        yield return StartCoroutine(Fade(1f, 0f, 0.35f));
        overlayImage.gameObject.SetActive(false);

        // ── 4. Голова піднімається лежачи ─────────────────────────────────
        yield return StartCoroutine(GroggyRise(basePos, baseRot));

        // ── 5. Повертається до тумбочки ───────────────────────────────────
        yield return StartCoroutine(TurnToNightstand());
        yield return new WaitForSeconds(0.3f);

        // ── 6. Бере телефон ───────────────────────────────────────────────
        yield return StartCoroutine(ReachAndPickPhone());

        yield return new WaitForSeconds(0.5f);

        // ── 7. ВСТАЄ З ЛІЖКА ──────────────────────────────────────────────
        yield return StartCoroutine(StandUpFromBed());

        // ── 8. Вмикаємо рух ───────────────────────────────────────────────
        if (cachedMovement != null) cachedMovement.enabled = true;
        if (overlayImage != null) Destroy(overlayImage.gameObject, 1f);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEPS
    // ═════════════════════════════════════════════════════════════════════════

    private IEnumerator SleepyLook(Quaternion baseRot, float duration)
    {
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float yaw   = Mathf.Sin(e * 1.1f)                * 0.7f;
            float pitch = Mathf.Sin(e * 1.1f * 0.77f + 0.4f) * 0.45f;
            float roll  = Mathf.Sin(e * 1.1f * 0.63f + 1.2f) * 0.35f;
            float noise = (Mathf.PerlinNoise(e * 0.95f, 0.31f) - 0.5f) * 0.2f;
            cam.localRotation = baseRot * Quaternion.Euler(pitch + noise, yaw, roll);
            yield return null;
        }
        cam.localRotation = baseRot;
    }

    private IEnumerator GroggyRise(Vector3 startPos, Quaternion startRot)
    {
        Vector3    endPos = startPos + new Vector3(0f, 0.18f, -0.05f);
        Quaternion endRot = startRot * Quaternion.Euler(-10f, 0f, 1f);

        float e = 0f;
        while (e < 2.0f)
        {
            e += Time.deltaTime;
            float t  = Mathf.Clamp01(e / 2.0f);
            float ez = 1f - Mathf.Pow(1f - t, 3f);
            cam.localPosition = Vector3.Lerp(startPos, endPos, Mathf.Clamp01(ez + Mathf.Sin(t * Mathf.PI) * 0.03f));
            cam.localRotation = Quaternion.Slerp(startRot, endRot, ez);
            yield return null;
        }
        cam.localPosition = endPos;
        cam.localRotation = endRot;

        // тремтіння
        e = 0f;
        while (e < 0.6f)
        {
            e += Time.deltaTime;
            float damp  = 1f - Mathf.Clamp01(e / 0.6f);
            float nudge = Mathf.Sin(e * 18f) * 0.008f * damp;
            cam.localPosition = endPos + new Vector3(0f, nudge * 0.5f, 0f);
            cam.localRotation = endRot * Quaternion.Euler(nudge * 50f, 0f, nudge * 20f);
            yield return null;
        }

        // дихання
        e = 0f;
        while (e < 2.0f)
        {
            e += Time.deltaTime;
            float c = e * 2.0f;
            cam.localPosition = endPos + new Vector3(0f, Mathf.Sin(c) * 0.008f, 0f);
            cam.localRotation = endRot * Quaternion.Euler(Mathf.Sin(c + 0.3f) * 0.6f, 0f, 0f);
            yield return null;
        }
        cam.localPosition = endPos;
        cam.localRotation = endRot;
    }

    private IEnumerator TurnToNightstand()
    {
        Vector3 tgt = lookTarget != null ? lookTarget.position :
                      phone      != null ? phone.position      :
                                          transform.position + transform.forward;

        Quaternion from = cam.rotation;
        Quaternion to   = Quaternion.LookRotation((tgt - cam.position).normalized, Vector3.up);

        float e = 0f;
        while (e < 2.8f)
        {
            e += Time.deltaTime;
            float t  = Mathf.Clamp01(e / 2.8f);
            float sm = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
            cam.rotation = Quaternion.Slerp(from, to, sm);
            yield return null;
        }
        cam.rotation = to;
    }

    private IEnumerator ReachAndPickPhone()
    {
        if (phone == null || nearFaceAnchor == null) yield break;

        Vector3    sp = phone.position;
        Quaternion sr = phone.rotation;
        Vector3    ep = nearFaceAnchor.position;
        Quaternion er = nearFaceAnchor.rotation;

        float e = 0f;
        while (e < 1.6f)
        {
            e += Time.deltaTime;
            float t      = Mathf.Clamp01(e / 1.6f);
            float phoneT = 1f - Mathf.Pow(1f - t, 3f);
            phone.position = Vector3.Lerp(sp, ep, phoneT);
            phone.rotation = Quaternion.Slerp(sr, er, phoneT);
            yield return null;
        }
        phone.SetPositionAndRotation(ep, er);
        phone.SetParent(nearFaceAnchor, true);
    }

    /// Камера іде вгору як людина встає з ліжка
    private IEnumerator StandUpFromBed()
    {
        float targetHeight = 1.7f;
        if (cachedMovement != null) targetHeight = cachedMovement.cameraHeight;

        // ── затемнення щоб не бачив телепорт ─────────────────────────────
        overlayImage.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(0f, 1f, 0.3f));

        // ── телепорт до точки біля ліжка ─────────────────────────────────
        if (standUpPoint != null)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = standUpPoint.position;
            transform.rotation = standUpPoint.rotation;
            if (cc != null) cc.enabled = true;
        }

        // ставимо камеру низько — людина ще присіла
        cam.localPosition = new Vector3(0f, 0.6f, 0f);
        cam.localRotation = Quaternion.Euler(15f, 0f, 0f);

        // ── розсвітлення ──────────────────────────────────────────────────
        yield return StartCoroutine(Fade(1f, 0f, 0.4f));
        overlayImage.gameObject.SetActive(false);

        // ── плавний підйом камери вгору ───────────────────────────────────
        Vector3    startPos = cam.localPosition;
        Quaternion startRot = cam.localRotation;
        Vector3    endPos   = new Vector3(0f, targetHeight, 0f);
        Quaternion endRot   = Quaternion.identity;

        float e = 0f;
        while (e < 1.2f)
        {
            e += Time.deltaTime;
            float t    = Mathf.Clamp01(e / 1.2f);
            float ez   = 1f - Mathf.Pow(1f - t, 3f);
            // легке хитання тіла
            float sway = Mathf.Sin(t * Mathf.PI) * 0.008f;
            cam.localPosition = Vector3.Lerp(startPos, endPos, ez)
                                + new Vector3(sway, 0f, 0f);
            cam.localRotation = Quaternion.Slerp(startRot, endRot, ez);
            yield return null;
        }
        cam.localPosition = endPos;
        cam.localRotation = endRot;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator Fade(float from, float to, float duration)
    {
        if (overlayImage == null) yield break;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            overlayImage.color = new Color(0f, 0f, 0f,
                Mathf.Lerp(from, to, Mathf.Clamp01(e / duration)));
            yield return null;
        }
        overlayImage.color = new Color(0f, 0f, 0f, to);
    }

    private void PlacePhone()
    {
        if (phone == null) return;
        if (phoneStartAnchor == null)
        {
            GameObject a = new GameObject("PhoneStartAnchor_Auto");
            a.transform.SetPositionAndRotation(phone.position, phone.rotation);
            phoneStartAnchor = a.transform;
        }
        phone.SetPositionAndRotation(phoneStartAnchor.position, phoneStartAnchor.rotation);
        if (lookTarget == null) lookTarget = phoneStartAnchor;
    }

    private Transform BuildNearFaceAnchor()
    {
        Transform ex = cam.Find("PhoneNearFaceAnchor");
        if (ex != null) return ex;
        GameObject a = new GameObject("PhoneNearFaceAnchor");
        a.transform.SetParent(cam, false);
        a.transform.localPosition = phoneNearFaceLocalPos;
        a.transform.localRotation = Quaternion.Euler(phoneNearFaceLocalEuler);
        return a.transform;
    }
}
