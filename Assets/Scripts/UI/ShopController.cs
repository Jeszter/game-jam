using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the Shop screen with items, prices, and buy buttons.
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
    }

    [SerializeField] private List<ShopItem> items = new List<ShopItem>();

    void Start()
    {
        if (items.Count == 0)
            GenerateDefaultItems();

        BuildShop();
    }

    private void GenerateDefaultItems()
    {
        items.Add(new ShopItem { itemName = "Vape", emoji = "~", price = 50, description = "+5 Dopamine, -10 Health" });
        items.Add(new ShopItem { itemName = "Casino", emoji = "$", price = 200, description = "Win big or lose all" });
        items.Add(new ShopItem { itemName = "Music", emoji = "#", price = 30, description = "Lo-fi beats to scroll to" });
        items.Add(new ShopItem { itemName = "Phone+", emoji = "+", price = 500, description = "Faster scrolling speed" });
        items.Add(new ShopItem { itemName = "Energy", emoji = "!", price = 75, description = "+20 Dopamine boost" });
        items.Add(new ShopItem { itemName = "Snack", emoji = "*", price = 25, description = "Late night fuel" });
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
                    txt.text = $"{items[i].price} DC";
                else if (txt.gameObject.name == "ItemDesc")
                    txt.text = items[i].description;
            }

            // Wire buy button
            Button buyBtn = itemObj.GetComponentInChildren<Button>();
            if (buyBtn != null)
            {
                int index = i;
                buyBtn.onClick.AddListener(() => BuyItem(index));
            }
        }
    }

    private void BuyItem(int index)
    {
        if (index < 0 || index >= items.Count) return;

        if (hudController != null)
        {
            if (hudController.SpendCoins(items[index].price))
            {
                hudController.AddDopamine(15f);
                Debug.Log($"Bought {items[index].itemName} for {items[index].price} DC");
            }
            else
            {
                Debug.Log("Not enough DoomCoins!");
            }
        }
    }
}
