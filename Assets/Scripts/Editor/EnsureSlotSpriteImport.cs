using UnityEditor;
using UnityEngine;

/// <summary>
/// Гарантує що slot.png (і в оригінальному місці, і в Resources/) був
/// імпортований як Sprite (Multiple) з нарізаними підспрайтами.
/// </summary>
public static class EnsureSlotSpriteImport
{
    [MenuItem("Tools/Build/Verify Slot Atlas Import")]
    public static void Verify()
    {
        Configure("Assets/TextursAssets/slot.png");
        Configure("Assets/Resources/SlotSprites/slot.png");
    }

    static void Configure(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.Log("[EnsureSlotSpriteImport] Not found: " + path); return; }

        bool dirty = false;
        if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; dirty = true; }
        if (ti.spriteImportMode != SpriteImportMode.Multiple) { ti.spriteImportMode = SpriteImportMode.Multiple; dirty = true; }
        if (dirty) ti.SaveAndReimport();

        // Перевіряємо що підспрайти справді існують
        var sub = AssetDatabase.LoadAllAssetsAtPath(path);
        int count = 0;
        foreach (var s in sub) if (s is Sprite) count++;
        Debug.Log($"[EnsureSlotSpriteImport] {path} -> {count} sprites (needs 7 for slot symbols).");
    }
}
