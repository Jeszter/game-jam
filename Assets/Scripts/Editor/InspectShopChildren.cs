using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public static class InspectShopChildren
{
    public static void Execute()
    {
        var content = GameObject.Find("GameUICanvas/PhonePanel/PhoneScreen/ShopScreen/ShopScroll/Viewport/Content");
        if (content == null) { Debug.Log("No content"); return; }
        foreach (Transform c in content.transform)
        {
            var nameT = c.Find("InfoGroup/ItemName")?.GetComponent<TMP_Text>();
            var descT = c.Find("InfoGroup/ItemDesc")?.GetComponent<TMP_Text>();
            var priceT = c.Find("RightCol/ItemPrice")?.GetComponent<TMP_Text>();
            var btnT = c.Find("RightCol/BuyButton/Text")?.GetComponent<TMP_Text>();
            var btn = c.GetComponentInChildren<Button>(true);
            int listenerCount = btn != null ? btn.onClick.GetPersistentEventCount() : 0;
            Debug.Log($"[{c.name}] active={c.gameObject.activeSelf} name='{nameT?.text}' desc='{descT?.text}' price='{priceT?.text}' btn='{btnT?.text}' persistentListeners={listenerCount}");
        }
    }
}
