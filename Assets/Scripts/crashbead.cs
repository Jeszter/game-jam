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

    // HUD / hint objects hidden during the cutscene, restored at the end.
    private GameObject hiddenHud;
    private GameObject hiddenPickupHint;
    private GameObject hiddenFoodHint;
    private GameObject hiddenVictoryTicker;
    private bool hudWasActive;
    private bool pickupHintWasActive;
    private bool foodHintWasActive;
    private bool victoryTickerWasActive;

    [Header("Wake-up intro sound")]
    [Tooltip("Звук який грає на самому початку катсцени (напр. будильник / голосове інтро). Якщо порожньо — використовується SoundManager.cutsceneVoice.")]
    public AudioClip introSound;
    [Range(0f, 1f)] public float introSoundVolume = 1f;

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

        HideHudDuringCutscene();
        SetupEyelidOverlay();
    }

    private void HideHudDuringCutscene()
    {
        // Хід HUD (полоси дофаміну/голоду, монети) — не потрібні, поки очі закриті.
        var canvas = GameObject.Find("GameUICanvas");
        if (canvas != null)
        {
            var hudT = canvas.transform.Find("HUD");
            if (hudT != null)
            {
                hiddenHud = hudT.gameObject;
                hudWasActive = hiddenHud.activeSelf;
                hiddenHud.SetActive(false);
            }
        }

        // Шукаємо також підказки "[E] Pick up" / "[F] Eat", якщо вони встигли створитись.
        var pickupHint = GameObject.Find("PickupHint");
        if (pickupHint != null)
        {
            hiddenPickupHint = pickupHint;
            pickupHintWasActive = pickupHint.activeSelf;
            pickupHint.SetActive(false);
        }

        var foodHint = GameObject.Find("FoodEatHint");
        if (foodHint != null)
        {
            hiddenFoodHint = foodHint;
            foodHintWasActive = foodHint.activeSelf;
            foodHint.SetActive(false);
        }

        TryHideVictoryTicker();
    }

    private void TryHideVictoryTicker()
    {
        if (hiddenVictoryTicker != null) return; // вже сховано

        // VictoryTicker створюється VictoryManager'ом у Start(), тому при першому виклику
        // з Awake() його ще нема. Спробуємо знайти; якщо зараз нема — знайдемо пізніше у корутині.
        var ticker = GameObject.Find("VictoryTicker");
        if (ticker != null)
        {
            hiddenVictoryTicker = ticker;
            victoryTickerWasActive = ticker.activeSelf;
            ticker.SetActive(false);
        }
    }

    private void RestoreHudAfterCutscene()
    {
        if (hiddenHud != null)
            hiddenHud.SetActive(hudWasActive);
        if (hiddenPickupHint != null)
            hiddenPickupHint.SetActive(pickupHintWasActive);
        if (hiddenFoodHint != null)
            hiddenFoodHint.SetActive(foodHintWasActive);
        if (hiddenVictoryTicker != null)
            hiddenVictoryTicker.SetActive(victoryTickerWasActive);
    }

    private void PlayIntroSound()
    {
        AudioClip clip = introSound;

        // Якщо в інспекторі не заданий — пробуємо знайти файл ElevenLabs_*_sp72_s26_sb23* у проекті
        // (файл, який запросив користувач на першому скріні).
        if (clip == null)
        {
            // Шлях для білда: копія в Resources/Sound/bedWakeIntro.*
            clip = Resources.Load<AudioClip>("Sound/bedWakeIntro");
#if UNITY_EDITOR
            if (clip == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip ElevenLabs_2026-04-19");
                foreach (var g in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    string fname = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (fname.Contains("sp72_s26_sb23"))
                    {
                        clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        break;
                    }
                }
            }
#endif
        }

        // Фолбек — cutsceneVoice із SoundManager (стара логіка)
        if (clip == null && SoundManager.Instance != null)
            clip = SoundManager.Instance.cutsceneVoice;

        if (clip == null) return;

        // Створюємо тимчасовий AudioSource, щоб звук пережив перехід між сценами та
        // не обрізався на 1-му кадрі (PlayOneShot SoundManager'а теж норм, але ми хочемо
        // повний контроль над гучністю і відносною 2D-подачею).
        var go = new GameObject("BedWakeIntroSound");
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 0f;
        src.volume = introSoundVolume;
        src.playOnAwake = false;
        src.Play();
        Destroy(go, clip.length + 0.5f);
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

        // Грає інтро-звук катсцени (будильник/"wake up" голос) одразу на самому початку.
        PlayIntroSound();

        // Дочекаємось кадру, щоб VictoryManager.Start() встиг створити VictoryTicker — і тоді ховаємо його теж.
        yield return null;
        TryHideVictoryTicker();

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
        var sm = SoundManager.Instance;
        if (sm != null && sm.cutsceneVoice != null)
            sm.Play(sm.cutsceneVoice, 1.0f);
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

        // HUD знову видимий — катсцена завершена.
        RestoreHudAfterCutscene();

        // Ще раз гарантуємо, що курсор прихований — деякі UI (CoinFloater/HUD) можуть
        // створюватись із `ScreenSpaceOverlay` канвасом і несвідомо повертати його.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // 7) фраза когда окончательно встал
        PlayVoice(voiceStoodUp);

        // Катсцена відпрацювала — вимикаємо компонент, щоб GameOverManager
        // знову почав перевіряти смерть (він пропускає тик поки cutscene enabled).
        enabled = false;
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
