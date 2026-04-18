using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Displays photos on a GameObject (like a picture frame on a wall).
/// 
/// HOW TO USE:
/// 1. Create a Quad or Plane in the scene (or any object with a MeshRenderer)
/// 2. Attach this script
/// 3. Drag your photo Sprites into the "photos" array in the Inspector
/// 4. Done! The photos will display on the object.
///    - If multiple photos: press E while looking at it to cycle through them
///    - Or enable autoSlideshow to auto-cycle
/// </summary>
public class PhotoDisplay : MonoBehaviour
{
    [Header("Photos")]
    [Tooltip("Drag your photo sprites here")]
    public Sprite[] photos;

    [Header("Slideshow")]
    [Tooltip("Auto-cycle through photos")]
    public bool autoSlideshow = false;
    [Tooltip("Seconds between slides")]
    public float slideshowInterval = 5f;

    [Header("Interaction")]
    [Tooltip("Press E while looking at this to change photo")]
    public bool interactToChange = true;
    [Tooltip("Max distance for E interaction")]
    public float interactDistance = 4f;

    private int currentIndex = 0;
    private float slideshowTimer = 0f;
    private Material displayMaterial;
    private Camera playerCam;
    private GameObject playerObj;

    void Start()
    {
        // If no Renderer exists, auto-create a Quad so photos have something to display on
        var renderer = GetComponent<Renderer>();
        if (renderer == null)
            renderer = GetComponentInChildren<Renderer>();

        if (renderer == null)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "PhotoQuad";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(1.5f, 1f, 1f); // landscape aspect
            // Remove collider from quad, use parent's if needed
            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);
            renderer = quad.GetComponent<Renderer>();

            // Add a BoxCollider on the parent for interaction
            if (GetComponent<Collider>() == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(1.5f, 1f, 0.1f);
            }
        }

        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");

            displayMaterial = new Material(shader);
            displayMaterial.name = "PhotoDisplayMat";
            renderer.material = displayMaterial;
        }

        // Find player camera
        playerObj = GameObject.Find("player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null)
            playerCam = playerObj.GetComponentInChildren<Camera>();
        if (playerCam == null)
            playerCam = Camera.main;

        // Show first photo
        if (photos != null && photos.Length > 0)
            ShowPhoto(0);
    }

    void Update()
    {
        if (photos == null || photos.Length == 0) return;

        // Auto slideshow
        if (autoSlideshow && photos.Length > 1)
        {
            slideshowTimer += Time.deltaTime;
            if (slideshowTimer >= slideshowInterval)
            {
                slideshowTimer = 0f;
                NextPhoto();
            }
        }

        // E interaction to cycle
        if (interactToChange && photos.Length > 1 && Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
                CheckInteraction();
        }
    }

    void CheckInteraction()
    {
        if (playerCam == null || !playerCam.gameObject.activeInHierarchy)
        {
            playerObj = GameObject.Find("player");
            if (playerObj != null) playerCam = playerObj.GetComponentInChildren<Camera>();
            if (playerCam == null) return;
        }

        Vector3 origin = playerCam.transform.position;
        Vector3 dir = playerCam.transform.forward;

        // Raycast
        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, interactDistance))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform) ||
                transform.IsChildOf(hit.collider.transform))
            {
                NextPhoto();
            }
        }

        // SphereCast fallback
        var hits = Physics.SphereCastAll(origin, 0.3f, dir, interactDistance);
        foreach (var h in hits)
        {
            if (h.collider != null &&
                (h.collider.gameObject == gameObject ||
                 h.collider.transform.IsChildOf(transform)))
            {
                NextPhoto();
                return;
            }
        }
    }

    public void NextPhoto()
    {
        if (photos == null || photos.Length == 0) return;
        currentIndex = (currentIndex + 1) % photos.Length;
        ShowPhoto(currentIndex);
    }

    public void PrevPhoto()
    {
        if (photos == null || photos.Length == 0) return;
        currentIndex = (currentIndex - 1 + photos.Length) % photos.Length;
        ShowPhoto(currentIndex);
    }

    public void ShowPhoto(int index)
    {
        if (photos == null || index < 0 || index >= photos.Length) return;
        if (photos[index] == null) return;

        currentIndex = index;
        Texture2D tex = photos[index].texture;

        if (displayMaterial != null)
        {
            // Try common texture property names
            if (displayMaterial.HasProperty("_BaseMap"))
                displayMaterial.SetTexture("_BaseMap", tex);
            else if (displayMaterial.HasProperty("_MainTex"))
                displayMaterial.SetTexture("_MainTex", tex);

            // Set color to white so texture shows properly
            if (displayMaterial.HasProperty("_BaseColor"))
                displayMaterial.SetColor("_BaseColor", Color.white);
            if (displayMaterial.HasProperty("_Color"))
                displayMaterial.SetColor("_Color", Color.white);
        }
    }

    void OnDestroy()
    {
        if (displayMaterial != null)
            Destroy(displayMaterial);
    }
}
