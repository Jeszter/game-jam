using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PopulateShopItems
{
    public static void Execute()
    {
        var shop = Object.FindFirstObjectByType<ShopController>();
        if (shop == null) { Debug.LogError("No ShopController"); return; }
        var so = new SerializedObject(shop);
        var items = so.FindProperty("items");
        items.ClearArray();

        // ---- Non-food upgrades ----
        AddItem(items, "Laptop",   "💻", 300, "PC to play mini-games",                   "House/Laptop",  10f, false, 0f);
        AddItem(items, "TV + PS5", "📺", 800, "Play Flappy Bird on the TV",             "House/Tv_unit", 15f, false, 0f);
        AddItem(items, "Vape",     "~",   50, "+5 Dopamine, -10 Health",                "",              10f, false, 0f);
        AddItem(items, "Music",    "#",   30, "Lo-fi beats to scroll to",               "",               8f, false, 0f);
        AddItem(items, "Phone+",   "+",  500, "Faster scrolling speed",                 "",               5f, false, 0f);

        // ---- FOOD with cooldowns — показуються на кухонном столе ----
        AddItem(items, "Pizza",  "🍕", 40, "Order pizza — appears on kitchen table (8s CD)",  "House/Food/Pizza",  5f, true, 8f);
        AddItem(items, "Burger", "🍔", 30, "Order burger — appears on kitchen table (7s CD)", "House/Food/Burger", 5f, true, 7f);
        // Energy = Soda (газировка) — спавнится вместо старого "Energy"
        AddItem(items, "Energy", "!",  75, "Energy drink (soda) — appears on kitchen table (5s CD)", "House/Food/Soda", 8f, true, 5f);
        // Snack = Apple (яблоко) — спавнится вместо старого "Snack"
        AddItem(items, "Snack",  "*",  25, "Late-night snack (apple) — appears on kitchen table (4s CD)", "House/Food/Apple", 4f, true, 4f);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(shop);
        EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
        EditorSceneManager.SaveScene(shop.gameObject.scene);
        Debug.Log($"[PopulateShopItems] Added {items.arraySize} items. Energy→Soda, Snack→Apple. Scene saved.");
    }

    static void AddItem(SerializedProperty arr, string name, string emoji, int price,
                        string desc, string unlockPath, float dopamineGain,
                        bool restockable, float cooldownSec)
    {
        arr.arraySize++;
        var el = arr.GetArrayElementAtIndex(arr.arraySize - 1);
        el.FindPropertyRelative("itemName").stringValue = name;
        el.FindPropertyRelative("emoji").stringValue = emoji;
        el.FindPropertyRelative("price").intValue = price;
        el.FindPropertyRelative("description").stringValue = desc;
        el.FindPropertyRelative("unlockObjectPath").stringValue = unlockPath;
        el.FindPropertyRelative("dopamineGain").floatValue = dopamineGain;
        el.FindPropertyRelative("isRestockable").boolValue = restockable;
        el.FindPropertyRelative("cooldownSeconds").floatValue = cooldownSec;
        el.FindPropertyRelative("purchased").boolValue = false;
        el.FindPropertyRelative("nextAvailableTime").floatValue = 0f;
        el.FindPropertyRelative("spawnPointCached").boolValue = false;
    }
}
