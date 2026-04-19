using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Предмет еды на кухне. Игрок подходит и жмёт F чтобы съесть.
/// - Восстанавливает голод
/// - Даёт дофамин (через GameEconomy, activity = Food)
/// - Удаляется со сцены, пока не будет снова "куплена" в магазине
///
/// ID должен совпадать с unlockObjectPath в ShopController, чтобы покупка в шопе
/// возвращала этот предмет обратно (ре-активировала GameObject).
/// </summary>
public class FoodItem : MonoBehaviour
{
    [Header("Food")]
    public string foodName = "Food";
    [Tooltip("Сколько восстанавливает голод")]
    public float hungerRestore = 40f;
    [Tooltip("Множитель дофамина по сравнению с базовым food")]
    public float dopamineMultiplier = 1f;

    [Header("Interaction")]
    public float interactDistance = 2.5f;
    public KeyCode legacyKey = KeyCode.F; // для совместимости — в рантайме используем Input System

    private Camera playerCam;
    private GameObject playerObj;

    private static TMP_Text sharedHintText;
    private static CanvasGroup sharedHintGroup;
    private static FoodItem currentLookedFood;

    void Start()
    {
        // Убедимся что есть коллайдер (иначе рэйкаст не сработает)
        if (GetComponent<Collider>() == null)
        {
            // попробуем взять Renderer для размеров; иначе примитивный BoxCollider
            var rend = GetComponentInChildren<Renderer>();
            var box = gameObject.AddComponent<BoxCollider>();
            if (rend != null)
            {
                var b = rend.bounds;
                box.center = transform.InverseTransformPoint(b.center);
                box.size = transform.InverseTransformVector(b.size);
                // абсолютные значения
                box.size = new Vector3(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y), Mathf.Abs(box.size.z));
            }
            else
            {
                box.size = Vector3.one * 0.3f;
            }
            box.isTrigger = false;
        }
    }

    void Update()
    {
        if (playerCam == null) FindPlayer();
        if (playerCam == null) return;

        // Распознаём только если именно на этот объект смотрит игрок
        bool looked = IsBeingLookedAt();
        if (looked)
        {
            currentLookedFood = this;
            ShowHint($"<color=#FFD700>[F]</color> Eat: <b>{foodName}</b>  " +
                     $"<color=#70FF70>+{Mathf.RoundToInt(hungerRestore)} 🍽</color>");

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                Eat();
        }
        else if (currentLookedFood == this)
        {
            currentLookedFood = null;
            HideHint();
        }
    }

    void OnDisable()
    {
        if (currentLookedFood == this)
        {
            currentLookedFood = null;
            HideHint();
        }
    }

    void FindPlayer()
    {
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj != null)
            playerCam = playerObj.GetComponentInChildren<Camera>();
        if (playerCam == null) playerCam = Camera.main;
    }

    bool IsBeingLookedAt()
    {
        if (playerCam == null || !playerCam.gameObject.activeInHierarchy) return false;

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        var hits = Physics.SphereCastAll(ray.origin, 0.2f, ray.direction, interactDistance);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
                return true;
        }
        return false;
    }

    void Eat()
    {
        var econ = GameEconomy.Instance;
        if (econ != null)
        {
            econ.AddHunger(hungerRestore);
            econ.AwardDopamine(GameEconomy.ActFood, dopamineMultiplier);
        }
        else
        {
            // Фолбэк — напрямую через HUD
            var hud = FindFirstObjectByType<GameHUDController>();
            if (hud != null) hud.AddDopamine(10f * dopamineMultiplier);
        }

        var sm = SoundManager.Instance;
        if (sm != null)
        {
            if (sm.foodEat != null)
                sm.PlayAt(sm.foodEat, transform.position, 0.9f, Random.Range(0.95f, 1.08f));
            else
                sm.PlayMenuClick();
        }

        HideHint();

        // Гарантуємо, що курсор не "виплив" випадково через UI-операції, викликані під час Eat()
        // (CoinFloater / HUD створюють ScreenSpaceOverlay канвас — у деяких випадках це
        // провокує InputSystemUIInputModule показати системний курсор на 1 кадр).
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        // "Съели" — отключаем объект (не уничтожаем, чтобы шоп мог его вернуть)
        gameObject.SetActive(false);
    }

    // ---------- Shared hint UI (одна подсказка на все FoodItem) ----------

    static void EnsureHintUI()
    {
        if (sharedHintText != null) return;
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("FoodEatHint");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 90f);
        rt.sizeDelta = new Vector2(700f, 44f);

        sharedHintGroup = go.AddComponent<CanvasGroup>();
        sharedHintGroup.alpha = 0f;

        sharedHintText = go.AddComponent<TextMeshProUGUI>();
        sharedHintText.alignment = TextAlignmentOptions.Center;
        sharedHintText.fontSize = 22f;
        sharedHintText.color = Color.white;
        sharedHintText.raycastTarget = false;
        var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (f != null) sharedHintText.font = f;
    }

    static void ShowHint(string msg)
    {
        EnsureHintUI();
        if (sharedHintText == null) return;
        sharedHintText.text = msg;
        sharedHintGroup.alpha = 1f;
    }

    static void HideHint()
    {
        if (sharedHintGroup != null) sharedHintGroup.alpha = 0f;
    }
}
