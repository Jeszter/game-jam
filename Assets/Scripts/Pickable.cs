using UnityEngine;

// Вішай на будь-який предмет який хочеш підняти
// Предмет повинен мати Rigidbody і Collider
[RequireComponent(typeof(Rigidbody))]
public class Pickable : MonoBehaviour
{
    [Header("Item name (shown on screen)")]
    public string itemName = "Item";

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public bool isHeld = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
}