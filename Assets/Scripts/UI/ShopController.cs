using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the Shop screen with items, prices, and buy buttons.
/// Buying an item can unlock a GameObject in the scene (e.g. Laptop, TV).
/// </summary>
public class ShopController : MonoBehaviour
{
    [SerializeField] private RectTransform itemsContainer;
    [SerializeField] private GameObject itemPrefabTemplate;
    [SerializeField] private GameHUDController hudController;

    [System.Serializable]
    public class ShopItem
    {
        public string itemName = "Item";
        public string emoji = "📦";
        public int price = 100;
        public string description = "A cool item";
        public string unlockObjectPath = ""; // Scene path to unlock (activate) after purchase
        public float dopamineGain = 15f;
        [Tooltip("Еда — можно покупать повторно, респавнит объект на сцене")]
        public bool isRestockable = false;
        [Tooltip("Кулдаун между покупками (сек). Работает только для isRestockable.")]
        public float cooldownSeconds = 5f;
        [HideInInspector] public bool purchased = false;
        [HideInInspector] public float nextAvailableTime = 0f;

        // Оригинальная точка спавна еды — запоминается при первом поиске,
        // чтобы все клоны появлялись там же где лежала еда изначально.
        [HideInInspector] public Vector3 spawnPosition;
        [HideInInspector] public Quaternion spawnRotation = Quaternion.identity;
        [HideInInspector] public Transform spawnParent;
        [HideInInspector] public GameObject originalPrefabRef;
        [HideInInspector] public bool spawnPointCached = false;
    }

    [SerializeField] private List<ShopItem> items = new List<ShopItem>();

    private Dictionary<int, Button> itemButtons = new Dictionary<int, Button>();
    private Dictionary<int, TMP_Text> itemButtonTexts = new Dictionary<int, TMP_Text>();
    private Dictionary<int, TMP_Text> itemPriceTexts = new Dictionary<int, TMP_Text>();

    /// <summary>Перевірити чи куплено предмет по назві (для витратних — чи хоч раз куплений).</summary>
    public bool IsPurchased(string itemName)
    {
        foreach (var it in items)
        {
            if (it.itemName == itemName)
            {
                if (it.isRestockable)
                    return it.nextAvailableTime > 0f || it.purchased;
                return it.purchased;
            }
        }
        return false;
    }

    void Start()
    {
        if (items.Count == 0)
            GenerateDefaultItems();

        // Кешируем точку спавна для еды + прячем ключевые объекты (Laptop/TV) если не куплены
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.unlockObjectPath)) continue;
            var go = FindAnyByPath(item.unlockObjectPath);
            if (go == null) continue;

            if (item.isRestockable)
            {
                // ЕДА — запоминаем позицию чтобы все будущие клоны появлялись тут же
                CacheFoodSpawnPoint(item, go);
            }
            else
            {
                if (!item.purchased) go.SetActive(false);
            }
        }

        BuildShop();
    }

    void Update()
    {
        // Обновляем тексты кулдаунов каждый кадр (без создания UI каждый раз)
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!item.isRestockable) continue;
            if (!itemButtons.TryGetValue(i, out var btn) || btn == null) continue;
            if (!itemButtonTexts.TryGetValue(i, out var btnText) || btnText == null) continue;

            float remaining = item.nextAvailableTime - Time.time;
            if (remaining > 0f)
            {
                btn.interactable = false;
                btnText.text = Mathf.CeilToInt(remaining) + "s";
                btnText.color = new Color(1f, 0.5f, 0.3f);
            }
            else
            {
                btn.interactable = true;
                btnText.text = "BUY";
                btnText.color = Color.white;
            }
        }
    }

    private void CacheFoodSpawnPoint(ShopItem item, GameObject original)
    {
        if (item.spawnPointCached) return;
        item.spawnPosition = original.transform.position;
        item.spawnRotation = original.transform.rotation;
        item.spawnParent = original.transform.parent;
        item.originalPrefabRef = original;
        item.spawnPointCached = true;
    }

    private void GenerateDefaultItems()
    {
        // Key items — unlock actual gameplay objects
        items.Add(new ShopItem {
            itemName = "Laptop",
            emoji = "💻",
            price = 300,
            description = "PC to play mini-games",
            unlockObjectPath = "House/Laptop",
            dopamineGain = 10f
        });
        items.Add(new ShopItem {
            itemName = "TV + PS5",
            emoji = "📺",
            price = 800,
            description = "Play Flappy Bird on the TV",
            unlockObjectPath = "House/Tv_unit",
            dopamineGain = 15f
        });

        // Regular consumables — restockable so player can repeatedly get dopamine hit
        items.Add(new ShopItem {
            itemName = "Vape",
            emoji = "~",
            price = 50,
            description = "Buy once, press V anywhere to puff (+35 DP, 8s cd)",
            dopamineGain = 35f,
            isRestockable = true,
            cooldownSeconds = 8f
        });
        items.Add(new ShopItem {
            itemName = "IQOS",
            emoji = "🚬",
            price = 70,
            description = "Buy once, press Q anywhere to smoke (+45 DP, 10s cd)",
            dopamineGain = 45f,
            isRestockable = true,
            cooldownSeconds = 10f
        });
        items.Add(new ShopItem { itemName = "Music", emoji = "#", price = 30, description = "Lo-fi beats to scroll to", dopamineGain = 8f });
        items.Add(new ShopItem { itemName = "Phone+", emoji = "+", price = 500, description = "Faster scrolling speed", dopamineGain = 5f });
        items.Add(new ShopItem {
            itemName = "Energy",
            emoji = "!",
            price = 75,
            description = "Energy drink — +30 dopamine (6s cd)",
            dopamineGain = 30f,
            isRestockable = true,
            cooldownSeconds = 6f
        });

        // ЕДА — респавнится на кухне. unlockObjectPath должен совпадать с путём к FoodItem на сцене.
        items.Add(new ShopItem {
            itemName = "Pizza",
            emoji = "🍕",
            price = 40,
            description = "Order pizza — shows up on kitchen table (8s cooldown)",
            unlockObjectPath = "House/Food/Pizza",
            dopamineGain = 5f,
            isRestockable = true,
            cooldownSeconds = 8f
        });
        items.Add(new ShopItem {
            itemName = "Burger",
            emoji = "🍔",
            price = 30,
            description = "Order burger (7s cooldown)",
            unlockObjectPath = "House/Food/Burger",
            dopamineGain = 5f,
            isRestockable = true,
            cooldownSeconds = 7f
        });
        items.Add(new ShopItem {
            itemName = "Soda",
            emoji = "🥤",
            price = 20,
            description = "Order soda (5s cooldown)",
            unlockObjectPath = "House/Food/Soda",
            dopamineGain = 3f,
            isRestockable = true,
            cooldownSeconds = 5f
        });
        items.Add(new ShopItem {
            itemName = "Apple",
            emoji = "🍎",
            price = 15,
            description = "Order apple (4s cooldown)",
            unlockObjectPath = "House/Food/Apple",
            dopamineGain = 2f,
            isRestockable = true,
            cooldownSeconds = 4f
        });
    }

    private void BuildShop()
    {
        if (itemsContainer == null || itemPrefabTemplate == null) return;

        foreach (Transform child in itemsContainer)
        {
            if (child.gameObject != itemPrefabTemplate)
                Destroy(child.gameObject);
        }

        itemPrefabTemplate.SetActive(false);
        itemButtons.Clear();
        itemButtonTexts.Clear();
        itemPriceTexts.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            GameObject itemObj = Instantiate(itemPrefabTemplate, itemsContainer);
            itemObj.name = $"ShopItem_{items[i].itemName}";
            itemObj.SetActive(true);

            TMP_Text[] texts = itemObj.GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in texts)
            {
                if (txt.gameObject.name == "ItemName")
                    txt.text = $"{items[i].emoji} {items[i].itemName}";
                else if (txt.gameObject.name == "ItemPrice")
                { txt.text = $"{items[i].price} DC"; itemPriceTexts[i] = txt; }
                else if (txt.gameObject.name == "ItemDesc")
                    txt.text = items[i].description;
            }

            // Wire buy button
            Button buyBtn = itemObj.GetComponentInChildren<Button>();
            if (buyBtn != null)
            {
                int index = i;
                buyBtn.onClick.AddListener(() => BuyItem(index));
                itemButtons[i] = buyBtn;

                var btnText = buyBtn.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    itemButtonTexts[i] = btnText;
                    btnText.text = "BUY";
                }

                // Highlight unlockable items
                if (!string.IsNullOrEmpty(items[i].unlockObjectPath))
                {
                    var bg = itemObj.GetComponent<Image>();
                    if (bg != null)
                        bg.color = new Color(0.18f, 0.1f, 0.25f, 1f); // purple tint for "feature" items
                }

                if (items[i].purchased) MarkAsPurchased(i);
            }
        }
    }

    private void BuyItem(int index)
    {
        if (index < 0 || index >= items.Count) return;
        var item = items[index];
        if (item.purchased && !item.isRestockable) return;

        if (hudController == null) return;

        // Кулдаун для еды
        if (item.isRestockable && Time.time < item.nextAvailableTime)
        {
            if (itemPriceTexts.TryGetValue(index, out var priceTxt))
                StartCoroutine(FlashText(priceTxt, new Color(1f, 0.5f, 0.2f)));
            Debug.Log($"[Shop] {item.itemName} on cooldown ({item.nextAvailableTime - Time.time:F1}s)");
            return;
        }

        if (!hudController.SpendCoins(item.price))
        {
            // Flash red
            if (itemPriceTexts.TryGetValue(index, out var priceTxt))
                StartCoroutine(FlashText(priceTxt, new Color(1f, 0.3f, 0.3f)));
            Debug.Log("Not enough DoomCoins!");
            return;
        }

        hudController.AddDopamine(item.dopamineGain);
        // Завжди відзначаємо хоч один успішний бай (щоб IsPurchased працювало для витратних)
        item.purchased = true;

        // Unlock / spawn the associated game object
        if (item.isRestockable)
        {
            // Spawn associated food if configured, otherwise it's a consumable (vape/iqos/energy)
            if (!string.IsNullOrEmpty(item.unlockObjectPath))
                SpawnOrRestoreFood(item);
            else
                PlayConsumableEffect(item);

            item.nextAvailableTime = Time.time + item.cooldownSeconds;
            FlashGreen(index);
        }
        else if (!string.IsNullOrEmpty(item.unlockObjectPath))
        {
            GameObject go = FindAnyByPath(item.unlockObjectPath);
            if (go != null)
            {
                go.SetActive(true);
                SpawnPurchaseFx(go.transform.position + Vector3.up * 0.4f);
                Debug.Log($"[Shop] Unlocked {item.unlockObjectPath} at {go.transform.position}");

                if (VictoryManager.Instance != null)
                    VictoryManager.Instance.ReportItemPurchased(item.itemName);
            }
            else
            {
                Debug.LogWarning($"[Shop] Could not find {item.unlockObjectPath} to unlock");
            }
        }

        if (!item.isRestockable)
            MarkAsPurchased(index);

        Debug.Log($"Bought {item.itemName} for {item.price} DC");
    }

    /// <summary>
    /// Еда всегда появляется на КОНКРЕТНОЙ позиции на столе:
    /// - если оригинал (или какой-то уже заспавненный клон) неактивен на этой позиции — активируем его,
    /// - иначе создаём новый клон в той же точке, с микро-вариацией высоты чтобы не z-fighting.
    /// </summary>
    private void SpawnOrRestoreFood(ShopItem item)
    {
        // Убедимся что точка спавна закеширована
        if (!item.spawnPointCached)
        {
            var original = FindAnyByPath(item.unlockObjectPath);
            if (original != null) CacheFoodSpawnPoint(item, original);
        }

        // Попробуем переактивировать оригинал если он сейчас выключен
        if (item.originalPrefabRef != null && !item.originalPrefabRef.activeSelf)
        {
            item.originalPrefabRef.transform.position = item.spawnPosition;
            item.originalPrefabRef.transform.rotation = item.spawnRotation;
            item.originalPrefabRef.SetActive(true);
            SpawnPurchaseFx(item.spawnPosition + Vector3.up * 0.4f);
            Debug.Log($"[Shop] Restored original {item.itemName} at {item.spawnPosition}");
            return;
        }

        // Иначе спавним клон точно в ту же позицию, лишь чуть выше чтобы
        // не было z-fighting с другой едой; FoodItem коллайдер поднят физикой если надо.
        if (item.originalPrefabRef == null)
        {
            Debug.LogWarning($"[Shop] No reference to spawn {item.itemName}");
            return;
        }

        // Если оригинал активен — используем его как прототип
        var clone = Instantiate(item.originalPrefabRef,
                                item.spawnPosition,
                                item.spawnRotation,
                                item.spawnParent);
        clone.name = item.itemName + "_Restock";
        clone.SetActive(true);
        SpawnPurchaseFx(item.spawnPosition + Vector3.up * 0.4f);
        Debug.Log($"[Shop] Spawned {item.itemName} clone at {item.spawnPosition}");
    }

    private void FlashGreen(int index)
    {
        if (itemPriceTexts.TryGetValue(index, out var priceTxt))
            StartCoroutine(FlashText(priceTxt, new Color(0.3f, 1f, 0.4f)));
    }

    private void MarkAsPurchased(int index)
    {
        if (itemButtons.TryGetValue(index, out var btn) && btn != null)
        {
            btn.interactable = false;
            var cb = btn.colors;
            cb.disabledColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            btn.colors = cb;
        }
        if (itemButtonTexts.TryGetValue(index, out var txt) && txt != null)
        {
            txt.text = "OWNED";
            txt.color = new Color(0.5f, 0.9f, 0.5f);
        }
        if (itemPriceTexts.TryGetValue(index, out var price) && price != null)
        {
            price.text = "✓";
            price.color = new Color(0.4f, 0.9f, 0.4f);
        }
    }

    private GameObject FindInactiveByPath(string path)
    {
        // Recursively search all root GOs (including inactive) for the given path
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindRecursive(root.transform, path);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    /// <summary>
    /// Надёжный поиск: пробует GameObject.Find, затем рекурсивный
    /// обход всех корней сцены (с неактивными), затем fallback-поиск
    /// по последнему сегменту пути (на случай если кто-то переименовал).
    /// </summary>
    private GameObject FindAnyByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var go = GameObject.Find(path);
        if (go != null) return go;
        go = FindInactiveByPath(path);
        if (go != null) return go;

        // Fallback: поиск по имени последнего сегмента
        var parts = path.Split('/');
        string leaf = parts[parts.Length - 1];
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var t = FindByName(root.transform, leaf);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    private Transform FindByName(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindByName(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    /// <summary>
    /// Ефект "вживання" витратного товару (вейп/IQOS/енергетик) —
    /// хмарка диму над гравцем + floating text із дофаміном.
    /// </summary>
    private void PlayConsumableEffect(ShopItem item)
    {
        // Знайти гравця
        var player = GameObject.Find("player");
        Vector3 pos;
        if (player != null)
            pos = player.transform.position + Vector3.up * 1.7f;
        else
            pos = Camera.main != null
                ? Camera.main.transform.position + Camera.main.transform.forward * 1.2f
                : Vector3.up * 1.8f;

        Color color;
        switch (item.itemName)
        {
            case "IQOS":  color = new Color(0.9f, 0.85f, 0.95f, 1f); break; // білий дим
            case "Vape":  color = new Color(0.7f, 1f, 0.9f, 1f);     break; // м'ятний
            case "Energy":color = new Color(1f, 0.6f, 0.2f, 1f);     break; // помаранчевий буст
            default:      color = new Color(1f, 0.9f, 0.5f, 1f);     break;
        }

        StartCoroutine(SmokeBurst(pos, color));

        // Floating text "+X DP"
        CoinFloater.Spawn(Mathf.RoundToInt(item.dopamineGain));

        Debug.Log($"[Shop] Consumed {item.itemName} — +{item.dopamineGain} dopamine");
    }

    private System.Collections.IEnumerator SmokeBurst(Vector3 worldPos, Color tint)
    {
        int puffs = 8;
        var objs = new GameObject[puffs];
        var mats = new Material[puffs];
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

        for (int i = 0; i < puffs; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = "SmokePuff";
            var col = s.GetComponent<Collider>();
            if (col != null) Destroy(col);
            s.transform.position = worldPos + Random.insideUnitSphere * 0.2f;
            s.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);
            var m = new Material(unlitShader);
            m.color = tint;
            s.GetComponent<MeshRenderer>().material = m;
            s.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            objs[i] = s;
            mats[i] = m;
        }

        float dur = 1.2f;
        float t = 0f;
        var dirs = new Vector3[puffs];
        for (int i = 0; i < puffs; i++)
        {
            dirs[i] = new Vector3(
                Random.Range(-0.4f, 0.4f),
                Random.Range(0.5f, 1.2f),
                Random.Range(-0.4f, 0.4f));
        }

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            for (int i = 0; i < puffs; i++)
            {
                if (objs[i] == null) continue;
                objs[i].transform.position += dirs[i] * Time.deltaTime * 0.6f;
                objs[i].transform.localScale += Vector3.one * Time.deltaTime * 0.35f;
                var c = mats[i].color;
                c.a = 1f - k;
                mats[i].color = c;
            }
            yield return null;
        }
        for (int i = 0; i < puffs; i++)
            if (objs[i] != null) Destroy(objs[i]);
    }

    /// <summary>
    /// Покупка/respawn — без світової "вибуху-сфери", яка виглядала як бомба.
    /// Тепер просто показуємо "+N DC" CoinFloater через HUD (викликається в коді покупки).
    /// </summary>
    private void SpawnPurchaseFx(Vector3 worldPos)
    {
        // no-op — жодних світових сфер
    }

    // Кода FxAnim більше не потрібна, але залишаємо заглушку на випадок
    // якщо хтось ззовні посилається на неї через reflection.
    private System.Collections.IEnumerator FxAnim(GameObject fx)
    {
        if (fx != null) Destroy(fx);
        yield break;
    }

    private Transform FindRecursive(Transform root, string path)
    {
        // path may be like "House/Laptop"
        var parts = path.Split('/');
        if (parts.Length == 0) return null;
        if (root.name != parts[0])
        {
            foreach (Transform child in root)
            {
                var r = FindRecursive(child, path);
                if (r != null) return r;
            }
            return null;
        }

        Transform current = root;
        for (int i = 1; i < parts.Length; i++)
        {
            Transform next = null;
            for (int c = 0; c < current.childCount; c++)
            {
                if (current.GetChild(c).name == parts[i]) { next = current.GetChild(c); break; }
            }
            if (next == null) return null;
            current = next;
        }
        return current;
    }

    private System.Collections.IEnumerator FlashText(TMP_Text t, Color flashColor)
    {
        Color orig = t.color;
        for (int i = 0; i < 3; i++)
        {
            t.color = flashColor;
            yield return new WaitForSeconds(0.08f);
            t.color = orig;
            yield return new WaitForSeconds(0.08f);
        }
    }
}
