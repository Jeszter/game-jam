using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Door : MonoBehaviour
{
    [Header("Settings")]
    public float openAngle       = 90f;
    public float speed           = 3f;
    public float interactDistance = 3f;

    [Header("Hinge (auto-detected if zero)")]
    public Vector3 hingeLocalOffset = Vector3.zero;

    private bool       isOpen   = false;
    private bool       isMoving = false;
    private Quaternion closedRot;
    private Vector3    closedPos;
    private Transform  playerCam;
    private float      currentOpenAngle = 0f;

    private void Start()
    {
        closedRot = transform.localRotation;
        closedPos = transform.localPosition;

        if (hingeLocalOffset == Vector3.zero)
            AutoDetectHinge();

        GameObject player = GameObject.Find("player");
        if (player != null)
        {
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam != null) { playerCam = cam.transform; return; }
        }
        if (Camera.main != null) playerCam = Camera.main.transform;
    }

    private void AutoDetectHinge()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Bounds b = mf.sharedMesh.bounds;
        Vector3 size = b.size;

        float[] sizes = { size.x, size.y, size.z };
        int thinAxis = 0; float thinVal = float.MaxValue;
        int tallAxis = 0; float tallVal = 0f;

        for (int i = 0; i < 3; i++)
        {
            if (sizes[i] < thinVal) { thinVal = sizes[i]; thinAxis = i; }
            if (sizes[i] > tallVal) { tallVal = sizes[i]; tallAxis = i; }
        }

        int widthAxis = 0;
        for (int i = 0; i < 3; i++)
        {
            if (i != thinAxis && i != tallAxis) { widthAxis = i; break; }
        }
        if (thinAxis == tallAxis) widthAxis = 0;

        hingeLocalOffset = b.center;
        switch (widthAxis)
        {
            case 0: hingeLocalOffset.x = b.max.x; break;
            case 1: hingeLocalOffset.y = b.max.y; break;
            case 2: hingeLocalOffset.z = b.max.z; break;
        }
        hingeLocalOffset.y = 0f;
    }

    private void Update()
    {
        if (isMoving) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        if (playerCam == null || !playerCam.gameObject.activeInHierarchy)
        {
            GameObject player = GameObject.Find("player");
            if (player != null)
            {
                Camera cam = player.GetComponentInChildren<Camera>();
                if (cam != null) playerCam = cam.transform;
            }
            if (playerCam == null && Camera.main != null)
                playerCam = Camera.main.transform;
            if (playerCam == null) return;
        }

        // Raycast — only react if THIS door is hit
        Ray ray = new Ray(playerCam.position, playerCam.forward);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, interactDistance)) return;

        if (hit.collider.gameObject != gameObject &&
            !hit.collider.transform.IsChildOf(transform))
            return;

        StartCoroutine(Toggle());
    }

    private IEnumerator Toggle()
    {
        isMoving = true;

        if (!isOpen)
        {
            // Determine which direction to open based on player position
            // Door should open AWAY from the player
            Vector3 doorForward = transform.forward; // door's thin side normal
            Vector3 toPlayer = (playerCam.position - transform.position).normalized;
            float dot = Vector3.Dot(doorForward, toPlayer);

            // If player is on the forward side, open negative; otherwise positive
            currentOpenAngle = dot > 0 ? -openAngle : openAngle;
        }

        isOpen = !isOpen;

        float targetAngle = isOpen ? currentOpenAngle : 0f;
        float startAngle = isOpen ? 0f : currentOpenAngle;

        // Reset to closed state first to get correct hinge position
        Quaternion savedRot = transform.rotation;
        Vector3 savedPos = transform.position;

        // If closing, we need the original closed state for hinge calc
        // If opening, current state IS closed
        Quaternion baseRot;
        Vector3 basePos;

        if (isOpen)
        {
            baseRot = transform.rotation;
            basePos = transform.position;
        }
        else
        {
            // We're currently at the open angle, need to go back to closed
            // Reset to closed to calculate hinge
            transform.localRotation = closedRot;
            transform.localPosition = closedPos;
            baseRot = transform.rotation;
            basePos = transform.position;
            // Restore
            transform.rotation = savedRot;
            transform.position = savedPos;
        }

        Vector3 hingeWS = baseRot * hingeLocalOffset + basePos;

        // Animate from current to target
        Quaternion fromRot = savedRot;
        Vector3 fromPos = savedPos;

        // Calculate target rotation/position
        transform.rotation = baseRot;
        transform.position = basePos;
        transform.RotateAround(hingeWS, Vector3.up, targetAngle);
        Quaternion toRot = transform.rotation;
        Vector3 toPos = transform.position;

        // Restore to start
        transform.rotation = fromRot;
        transform.position = fromPos;

        float elapsed = 0f;
        float duration = 1f / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t); // smoothstep

            transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
            transform.position = Vector3.Lerp(fromPos, toPos, t);

            yield return null;
        }

        transform.rotation = toRot;
        transform.position = toPos;

        if (!isOpen)
        {
            // Snap to exact closed state
            transform.localRotation = closedRot;
            transform.localPosition = closedPos;
        }

        isMoving = false;
    }
}
