using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Рух")]
    public float walkSpeed  = 2.5f;
    public float runSpeed   = 5f;
    public float crouchSpeed = 1.5f;
    public float jumpHeight = 1.0f;
    public float gravity    = -18f;

    [Header("Камера")]
    public float cameraHeight        = 1.75f;
    public float cameraHeightCrouch  = 1.0f;
    public float crouchLerpSpeed     = 10f;
    public float mouseSensitivity    = 2f;
    public float maxLookUp           = 80f;
    public float maxLookDown         = 80f;

    [Header("Капсула")]
    public float standHeight  = 1.8f;
    public float crouchHeight = 1.0f;

    [HideInInspector] public bool phoneLock = false;

    [Header("Footsteps")]
    public float footstepIntervalWalk = 0.5f;
    public float footstepIntervalRun = 0.32f;
    public float footstepIntervalCrouch = 0.7f;
    [Range(0f, 1f)] public float footstepVolume = 0.55f;

    private CharacterController cc;
    private Transform           camTransform;
    private Vector3             velocity;
    private float               xRotation = 0f;
    private bool                isCrouching = false;
    private float               currentCamY;
    private AudioSource         footstepSource;
    private float               footstepTimer;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();

        cc.height    = standHeight;
        cc.radius    = 0.3f;
        cc.center    = new Vector3(0f, standHeight * 0.5f, 0f);
        cc.skinWidth = 0.08f;

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            camTransform = cam.transform;
            cam.nearClipPlane = 0.15f;
        }
        currentCamY = cameraHeight;

        // Footsteps audio source on the player (non-spatial — attached to listener)
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 0f;
        footstepSource.volume = footstepVolume;
    }

    private void OnEnable()
    {
        if (camTransform != null)
            camTransform.localPosition = new Vector3(0f, cameraHeight, 0f);

        if (camTransform != null)
        {
            float currentX = camTransform.localEulerAngles.x;
            if (currentX > 180f) currentX -= 360f;
            xRotation = Mathf.Clamp(currentX, -maxLookDown, maxLookUp);
        }

        currentCamY = cameraHeight;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void Update()
    {
        EnforceCursorLock();
        Look();
        Move();
        UpdateCrouch();
    }

    /// <summary>
    /// Гвард: якщо гра в активному стані (не пауза, телефон закритий, мінігри закриті) —
    /// курсор має бути locked/invisible. Це захищає від випадкових "залипань" курсора
    /// після дій типу F-з'їсти-їжу чи закриття будь-якого UI.
    /// </summary>
    private void EnforceCursorLock()
    {
        if (phoneLock) return;                  // телефон/діалог відкритий — курсор видимий
        if (Time.timeScale == 0f) return;       // пауза — курсор видимий

        // Якщо хтось залишив курсор видимим — повертаємо назад у locked-режим.
        if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Look()
    {
        if (camTransform == null || Mouse.current == null) return;
        if (phoneLock) return;
        Vector2 delta = Mouse.current.delta.ReadValue() * 0.05f * mouseSensitivity;
        xRotation -= delta.y;
        xRotation  = Mathf.Clamp(xRotation, -maxLookDown, maxLookUp);
        camTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * delta.x);
    }

    private void UpdateCrouch()
    {
        bool wantCrouch = Keyboard.current != null &&
                          (Keyboard.current.leftCtrlKey.isPressed ||
                           Keyboard.current.cKey.isPressed);

        if (!wantCrouch && isCrouching)
        {
            // перш ніж встати, перевіряємо чи є місце над головою
            Vector3 top = transform.position + Vector3.up * (standHeight - 0.1f);
            bool blocked = Physics.CheckSphere(top, 0.28f, ~0, QueryTriggerInteraction.Ignore);
            if (blocked) wantCrouch = true; // не можемо встати - тримаємось присівши
        }

        isCrouching = wantCrouch;

        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetCamY   = isCrouching ? cameraHeightCrouch : cameraHeight;

        // плавно інтерполюємо висоту капсули і камери
        cc.height  = Mathf.MoveTowards(cc.height, targetHeight, crouchLerpSpeed * Time.deltaTime);
        cc.center  = new Vector3(0f, cc.height * 0.5f, 0f);
        currentCamY = Mathf.MoveTowards(currentCamY, targetCamY, crouchLerpSpeed * Time.deltaTime);

        if (camTransform != null)
        {
            Vector3 p = camTransform.localPosition;
            p.y = currentCamY;
            camTransform.localPosition = p;
        }
    }

    private void Move()
    {
        bool grounded = cc.isGrounded;
        if (grounded && velocity.y < 0f) velocity.y = -2f;

        float h = 0f, v = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    v += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  v -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
        }

        bool  run   = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && !isCrouching;
        float speed;
        if (isCrouching)     speed = crouchSpeed;
        else if (run)        speed = runSpeed;
        else                 speed = walkSpeed;

    Vector3 move = (transform.right * h + transform.forward * v).normalized;
        cc.Move(move * speed * Time.deltaTime);

        // Footsteps
        HandleFootsteps(move.sqrMagnitude > 0.01f, grounded, run);

        // стрибок не дозволяємо коли присів
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && grounded && !isCrouching)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    private void HandleFootsteps(bool moving, bool grounded, bool running)
    {
        if (footstepSource == null) return;

        if (!moving || !grounded)
        {
            footstepTimer = 0f;
            return;
        }

        float interval = isCrouching ? footstepIntervalCrouch
                       : running     ? footstepIntervalRun
                                     : footstepIntervalWalk;

        footstepTimer += Time.deltaTime;
        if (footstepTimer >= interval)
        {
            footstepTimer = 0f;
            var sm = SoundManager.Instance;
            var clip = sm != null ? sm.footstep : null;
            if (clip != null)
            {
                footstepSource.pitch = Random.Range(0.92f, 1.08f);
                footstepSource.PlayOneShot(clip, footstepVolume);
            }
        }
    }
}
