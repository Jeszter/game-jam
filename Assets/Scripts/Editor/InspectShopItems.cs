using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;

public static class InspectShopItems
{
    [MenuItem("Tools/Inspect Shop Items")]
    public static void Inspect()
    {
        var shop = Object.FindFirstObjectByType<ShopController>();
        if (shop == null) { Debug.Log("No ShopController"); return; }
        var field = typeof(ShopController).GetField("items", BindingFlags.NonPublic | BindingFlags.Instance);
        var list = field.GetValue(shop) as IList;
        if (list == null) { Debug.Log("items list null"); return; }
        Debug.Log($"Shop has {list.Count} items");
        foreach (var it in list)
        {
            var t = it.GetType();
            string name = t.GetField("itemName").GetValue(it).ToString();
            string path = t.GetField("unlockObjectPath").GetValue(it).ToString();
            bool restock = (bool)t.GetField("isRestockable").GetValue(it);
            float cd = (float)t.GetField("cooldownSeconds").GetValue(it);
            bool purchased = (bool)t.GetField("purchased").GetValue(it);
            Debug.Log($"- {name} | unlock='{path}' | restock={restock} cd={cd} purchased={purchased}");
        }
    }

    public static void Execute()
    {
        Inspect();
    }
}
