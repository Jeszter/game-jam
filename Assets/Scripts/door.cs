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
    private Vector3    hingeWorld;
    private Transform  playerCam;

    private void Start()
    {
        closedRot = transform.localRotation;
        closedPos = transform.localPosition;

        // Find hinge point — edge of the door mesh
        // We pick the edge of the bounding box that is closest to a wall
        if (hingeLocalOffset == Vector3.zero)
        {
            AutoDetectHinge();
        }

        // Find player camera
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
        // Get the mesh bounds in local space
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Bounds b = mf.sharedMesh.bounds;

        // The door is a thin rectangle. The hinge is on one of the long edges.
        // We need to figure out which local axis is the "width" of the door.
        // The thin axis is the door thickness.
        Vector3 size = b.size;

        // Find the thinnest axis (that's the door thickness — not the hinge axis)
        // Find the tallest axis (that's Y — height)
        // The remaining axis is the width — hinge is on one edge of that axis

        // Determine width axis (not Y, not thinnest)
        float[] sizes = { size.x, size.y, size.z };
        int thinAxis = 0;
        float thinVal = float.MaxValue;
        int tallAxis = 0;
        float tallVal = 0f;

        for (int i = 0; i < 3; i++)
        {
            if (sizes[i] < thinVal) { thinVal = sizes[i]; thinAxis = i; }
            if (sizes[i] > tallVal) { tallVal = sizes[i]; tallAxis = i; }
        }

        // Width axis is the one that's neither thin nor tall
        int widthAxis = 0;
        for (int i = 0; i < 3; i++)
        {
            if (i != thinAxis && i != tallAxis) { widthAxis = i; break; }
        }
        // If thin and tall are same axis (unlikely), fallback
        if (thinAxis == tallAxis)
        {
            widthAxis = 0; // X
        }

        // Hinge offset: go to the max edge of the width axis (opposite side from handle)
        hingeLocalOffset = b.center;
        switch (widthAxis)
        {
            case 0: hingeLocalOffset.x = b.max.x; break;
            case 1: hingeLocalOffset.y = b.max.y; break;
            case 2: hingeLocalOffset.z = b.max.z; break;
        }
        // Keep Y at 0 (we rotate around Y axis, hinge Y doesn't matter)
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

        float dist = Vector3.Distance(playerCam.position, transform.position);
        if (dist > interactDistance) return;

        Vector3 dir = (transform.position - playerCam.position).normalized;
        if (Vector3.Dot(playerCam.forward, dir) < 0.3f) return;

        StartCoroutine(Toggle());
    }

    private IEnumerator Toggle()
    {
        isMoving = true;
        isOpen = !isOpen;

        float targetAngle = isOpen ? openAngle : 0f;
        float startAngle = isOpen ? 0f : openAngle;

        // Calculate hinge world position at start
        Vector3 hingeWS = transform.TransformPoint(hingeLocalOffset);

        // Store starting state
        Quaternion startRot = transform.rotation;
        Vector3 startPos = transform.position;

        float elapsed = 0f;
        float duration = 1f / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth step
            t = t * t * (3f - 2f * t);

            float currentAngle = Mathf.Lerp(startAngle, targetAngle, t);
            float deltaAngle = currentAngle - startAngle;

            // Reset to start position/rotation
            transform.rotation = startRot;
            transform.position = startPos;

            // Rotate around hinge
            transform.RotateAround(hingeWS, Vector3.up, deltaAngle);

            yield return null;
        }

        // Final snap
        transform.rotation = startRot;
        transform.position = startPos;
        transform.RotateAround(hingeWS, Vector3.up, targetAngle - startAngle);

        isMoving = false;
    }
}
