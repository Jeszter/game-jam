using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    private enum CameraMode
    {
        FirstPerson = 0,
        ThirdPerson = 1
    }

    [SerializeField] private CameraMode mode = CameraMode.FirstPerson;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 1.62f, 0.04f);
    [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0f, 1.55f, -2.35f);
    [SerializeField] private Vector3 firstPersonEulerOffset = Vector3.zero;
    [SerializeField] private float positionSmooth = 8f;
    [SerializeField] private float rotationSmooth = 10f;
    [SerializeField] private bool lockCursorOnPlay;

    [Header("Third Person Optional")]
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 0.2f, 0f);
    private Vector3 runtimeLookOffset = Vector3.zero;

    private void Start()
    {
        if (target == null)
        {
            GameObject foundPlayer = GameObject.Find("player");
            if (foundPlayer != null)
                target = foundPlayer.transform;
        }

        if (lockCursorOnPlay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPos = mode == CameraMode.FirstPerson
            ? target.TransformPoint(firstPersonOffset)
            : target.TransformPoint(thirdPersonOffset);
        float posT = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, posT);

        Quaternion desiredRot;
        if (mode == CameraMode.FirstPerson)
        {
            desiredRot = target.rotation * Quaternion.Euler(firstPersonEulerOffset + runtimeLookOffset);
        }
        else
        {
            Vector3 lookPoint = lookAtTarget != null
                ? lookAtTarget.position + lookAtOffset
                : target.position + lookAtOffset;
            desiredRot = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        }

        float rotT = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotT);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetRuntimeLookOffset(Vector3 eulerOffset)
    {
        runtimeLookOffset = eulerOffset;
    }

    public void ResetRuntimeLookOffset()
    {
        runtimeLookOffset = Vector3.zero;
    }
}
