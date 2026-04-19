using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Вішай на player
public class PickupSystem : MonoBehaviour
{
    [Header("Налаштування")]
    public float pickupRange   = 3f;
    public float holdDistance  = 1.8f;
    public float holdHeight    = -0.2f;
    public float throwForce    = 6f;
    public float smoothSpeed   = 15f;

    [Header("UI підказка (опціонально — автоствориться)")]
    public TextMeshProUGUI hintText;

    private Transform cam;
    private Pickable  heldObject;
    private Pickable  lookedObject;

    private void Start()
    {
        Camera c = GetComponentInChildren<Camera>();
        if (c != null) cam = c.transform;
        if (hintText == null) hintText = CreateHintText();
    }

    private void Update()
    {
        if (cam == null) return;
        if (heldObject == null) CheckLook();
        else HoldObject();
        HandleInput();
        UpdateHint();
    }

    private void CheckLook()
    {
        lookedObject = null;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, pickupRange))
            lookedObject = hit.collider.GetComponent<Pickable>();
    }

    private void HoldObject()
    {
        Vector3 target = cam.position + cam.forward * holdDistance + cam.up * holdHeight;
        heldObject.rb.linearVelocity  = Vector3.zero;
        heldObject.rb.angularVelocity = Vector3.zero;
        heldObject.rb.MovePosition(
            Vector3.Lerp(heldObject.transform.position, target, Time.deltaTime * smoothSpeed));
        heldObject.transform.rotation = Quaternion.Slerp(
            heldObject.transform.rotation, cam.rotation, Time.deltaTime * 8f);
    }

    private void HandleInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldObject != null) ReleaseObject(false);
            else if (lookedObject != null) PickupObject(lookedObject);
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            heldObject != null)
            ReleaseObject(true);
    }

    private void PickupObject(Pickable p)
    {
        heldObject           = p;
        heldObject.isHeld    = true;
        heldObject.rb.useGravity    = false;
        heldObject.rb.linearDamping  = 10f;
        heldObject.rb.angularDamping = 10f;

        Collider pc = GetComponent<Collider>();
        Collider oc = heldObject.GetComponent<Collider>();
        if (pc && oc) Physics.IgnoreCollision(oc, pc, true);

        PlayPickupSfx(p.transform.position, 1.0f);
    }

    private void ReleaseObject(bool doThrow)
    {
        if (heldObject == null) return;

        heldObject.rb.useGravity     = true;
        heldObject.rb.linearDamping  = 0f;
        heldObject.rb.angularDamping = 0.05f;
        heldObject.isHeld            = false;

        if (doThrow)
            heldObject.rb.AddForce(cam.forward * throwForce, ForceMode.Impulse);

        Collider pc = GetComponent<Collider>();
        Collider oc = heldObject.GetComponent<Collider>();
        if (pc && oc) Physics.IgnoreCollision(oc, pc, false);

        // Lower pitch on drop, higher + louder on throw — makes the gesture feel intentional.
        PlayPickupSfx(heldObject.transform.position, doThrow ? 1.15f : 0.9f);

        heldObject = null;
    }

    private void PlayPickupSfx(Vector3 pos, float pitch)
    {
        var sm = SoundManager.Instance;
        if (sm == null || sm.pickupSound == null) return;
        sm.PlayAt(sm.pickupSound, pos, 0.85f, pitch * UnityEngine.Random.Range(0.95f, 1.05f));
    }

    private void UpdateHint()
    {
        if (hintText == null) return;
        if (heldObject != null)
            hintText.text = "<color=#FFD700>[E]</color> Drop   <color=#FFD700>[LMB]</color> Throw";
        else if (lookedObject != null)
            hintText.text = "<color=#FFD700>[E]</color> Pick up: <b>" + lookedObject.itemName + "</b>";
        else
            hintText.text = "";
    }

    private TextMeshProUGUI CreateHintText()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return null;
        GameObject go = new GameObject("PickupHint");
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 40f);
        rt.sizeDelta        = new Vector2(700f, 50f);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize  = 20f;
        tmp.color     = Color.white;
        return tmp;
    }
}

