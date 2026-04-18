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
        [HideInInspector] public bool purchased = false;
    }

    [SerializeField] private List<ShopItem> items = new List<ShopItem>();

    private Dictionary<int, Button> itemButtons = new Dictionary<int, Button>();
    private Dictionary<int, TMP_Text> itemButtonTexts = new Dictionary<int, TMP_Text>();
    private Dictionary<int, TMP_Text> itemPriceTexts = new Dictionary<int, TMP_Text>();

    void Start()
    {
        if (items.Count == 0)
            GenerateDefaultItems();

        // Hide all non-food "unlockable" objects at start if not yet purchased.
        // ЕДА (isRestockable) — на старте активна (лежит на кухне).
        foreach (var item in items)
        {
            if (item.isRestockable) continue;
            if (!item.purchased && !string.IsNullOrEmpty(item.unlockObjectPath))
            {
                var go = GameObject.Find(item.unlockObjectPath);
                if (go == null) go = FindInactiveByPath(item.unlockObjectPath);
                if (go != null) go.SetActive(false);
            }
        }

        BuildShop();
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

        // Regular consumables
        items.Add(new ShopItem { itemName = "Vape", emoji = "~", price = 50, description = "+5 Dopamine, -10 Health", dopamineGain = 10f });
        items.Add(new ShopItem { itemName = "Music", emoji = "#", price = 30, description = "Lo-fi beats to scroll to", dopamineGain = 8f });
        items.Add(new ShopItem { itemName = "Phone+", emoji = "+", price = 500, description = "Faster scrolling speed", dopamineGain = 5f });
        items.Add(new ShopItem { itemName = "Energy", emoji = "!", price = 75, description = "+20 Dopamine boost", dopamineGain = 20f });

        // ЕДА — респавнится на кухне. unlockObjectPath должен совпадать с путём к FoodItem на сцене.
        items.Add(new ShopItem {
            itemName = "Pizza",
            emoji = "🍕",
            price = 40,
            description = "Restock pizza in the kitchen (+hunger)",
            unlockObjectPath = "House/Food/Pizza",
            dopamineGain = 5f,
            isRestockable = true
        });
        items.Add(new ShopItem {
            itemName = "Burger",
            emoji = "🍔",
            price = 30,
            description = "Restock burger (+hunger)",
            unlockObjectPath = "House/Food/Burger",
            dopamineGain = 5f,
            isRestockable = true
        });
        items.Add(new ShopItem {
            itemName = "Soda",
            emoji = "🥤",
            price = 20,
            description = "Restock soda (+hunger)",
            unlockObjectPath = "House/Food/Soda",
            dopamineGain = 3f,
            isRestockable = true
        });
        items.Add(new ShopItem {
            itemName = "Apple",
            emoji = "🍎",
            price = 15,
            description = "Restock healthy snack",
            unlockObjectPath = "House/Food/Apple",
            dopamineGain = 2f,
            isRestockable = true
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
                if (btnText != null) itemButtonTexts[i] = btnText;

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

        if (!hudController.SpendCoins(item.price))
        {
            // Flash red
            if (itemPriceTexts.TryGetValue(index, out var priceTxt))
                StartCoroutine(FlashText(priceTxt, new Color(1f, 0.3f, 0.3f)));
            Debug.Log("Not enough DoomCoins!");
            return;
        }

        hudController.AddDopamine(item.dopamineGain);
        if (!item.isRestockable) item.purchased = true;

        // Unlock the associated game object
        if (!string.IsNullOrEmpty(item.unlockObjectPath))
        {
            var go = GameObject.Find(item.unlockObjectPath);
            if (go == null)
            {
                // Try searching inactive objects in all scenes
                go = FindInactiveByPath(item.unlockObjectPath);
            }
            if (go != null)
            {
                go.SetActive(true);
                Debug.Log($"[Shop] Unlocked {item.unlockObjectPath}");
            }
            else
            {
                Debug.LogWarning($"[Shop] Could not find {item.unlockObjectPath} to unlock");
            }
        }

        if (!item.isRestockable)
            MarkAsPurchased(index);
        else
            FlashGreen(index);
        Debug.Log($"Bought {item.itemName} for {item.price} DC");
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
