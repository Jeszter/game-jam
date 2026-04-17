using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Door : MonoBehaviour
{
    [Header("Налаштування")]
    public float openAngle        = 90f;
    public float speed            = 3f;
    public float interactDistance = 2.5f;

    private bool       isOpen   = false;
    private bool       isMoving = false;
    private Quaternion closedRot;
    private Quaternion openRot;
    private Transform  playerCam;

    private void Start()
    {
        closedRot = transform.localRotation;
        openRot   = closedRot * Quaternion.Euler(0f, openAngle, 0f);
        if (Camera.main != null) playerCam = Camera.main.transform;
    }

    private void Update()
    {
        if (isMoving) return;
        if (Keyboard.current == null) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;
        if (playerCam == null) return;

        float dist = Vector3.Distance(playerCam.position, transform.position);
        if (dist > interactDistance) return;

        Vector3 dir = (transform.position - playerCam.position).normalized;
        if (Vector3.Dot(playerCam.forward, dir) < 0.3f) return;

        StartCoroutine(Toggle());
    }

    private IEnumerator Toggle()
    {
        isMoving = true;
        Quaternion from = transform.localRotation;
        Quaternion to   = isOpen ? closedRot : openRot;
        isOpen = !isOpen;

        float e = 0f;
        while (e < 1f)
        {
            e += Time.deltaTime * speed;
            float t = Mathf.Clamp01(e);
            transform.localRotation = Quaternion.Slerp(from, to, t * t * (3f - 2f * t));
            yield return null;
        }
        transform.localRotation = to;
        isMoving = false;
    }
}


