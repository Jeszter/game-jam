using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Рух")]
    public float walkSpeed  = 2.5f;
    public float runSpeed   = 5f;
    public float jumpHeight = 1.0f;
    public float gravity    = -18f;

    [Header("Камера")]
    public float cameraHeight     = 15f;
    public float mouseSensitivity = 2f;
    public float maxLookUp        = 80f;
    public float maxLookDown      = 80f;

    private CharacterController cc;
    private Transform           camTransform;
    private Vector3             velocity;
    private float               xRotation = 0f;

    private void Start()
    {
        cc = GetComponent<CharacterController>();

        // підганяємо CharacterController під ріст
        cc.height = 1.8f;
        cc.radius = 0.1f;   // вузький — щоб проходив у двері
        cc.center = new Vector3(0f, 0.9f, 0f);

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            camTransform = cam.transform;
            camTransform.localPosition = new Vector3(0f, cameraHeight, 0f);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void Update()
    {
        Look();
        Move();
    }

    private void Look()
    {
        if (camTransform == null || Mouse.current == null) return;
        Vector2 delta = Mouse.current.delta.ReadValue() * 0.05f * mouseSensitivity;
        xRotation -= delta.y;
        xRotation  = Mathf.Clamp(xRotation, -maxLookDown, maxLookUp);
        camTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * delta.x);
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

        bool  run   = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float speed = run ? runSpeed : walkSpeed;

        Vector3 move = (transform.right * h + transform.forward * v).normalized;
        cc.Move(move * speed * Time.deltaTime);

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && grounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
}