using UnityEngine;
using UnityEditor;
using System.Reflection;

public static class InspectShopController
{
    public static void Execute()
    {
        var shop = Object.FindFirstObjectByType<ShopController>();
        if (shop == null) { Debug.Log("No ShopController"); return; }
        var t = typeof(ShopController);
        var itemsContainer = t.GetField("itemsContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shop);
        var itemPrefabTemplate = t.GetField("itemPrefabTemplate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shop);
        var hud = t.GetField("hudController", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shop);
        Debug.Log($"itemsContainer={itemsContainer} itemPrefabTemplate={itemPrefabTemplate} hud={hud}");
        Debug.Log($"Shop enabled={shop.enabled}, go.active={shop.gameObject.activeSelf} inHierarchy={shop.gameObject.activeInHierarchy}");
    }
}
