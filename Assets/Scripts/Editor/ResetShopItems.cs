using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// Сбрасывает список items в ShopController так, чтобы использовалась
/// дефолтная генерация (в том числе food с isRestockable=true и кулдаунами).
/// </summary>
public static class ResetShopItems
{
    public static void Execute()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[ResetShopItems] Нет активной сцены");
            return;
        }

        ShopController shop = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            shop = root.GetComponentInChildren<ShopController>(true);
            if (shop != null) break;
        }
        if (shop == null)
        {
            Debug.LogError("[ResetShopItems] ShopController не найден в сцене");
            return;
        }

        var field = typeof(ShopController).GetField("items",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            Debug.LogError("[ResetShopItems] Не найдено поле items (рефлексия)");
            return;
        }

        var list = field.GetValue(shop) as IList<ShopController.ShopItem>;
        int before = list != null ? list.Count : -1;
        Debug.Log($"[ResetShopItems] Было {before} items в инспекторе. Очищаем.");

        var fresh = new List<ShopController.ShopItem>();
        field.SetValue(shop, fresh);

        EditorUtility.SetDirty(shop);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ResetShopItems] Список очищен, сцена сохранена. При запуске игры ShopController сгенерирует items заново.");
    }
}
