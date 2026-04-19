using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Копіює всі рантайм-ресурси (аудіо, текстури, FBX), на які скрипти посилаються через
/// AssetDatabase.LoadAssetAtPath / FindAssets, у папку Assets/Resources/.
/// Це ЄДИНИЙ надійний спосіб щоб вони потрапили у білд, бо AssetDatabase у білді не працює,
/// і ассети, на які немає direct-reference із сцени/прeфаба, Unity викидає зі збірки.
///
/// Запускається з меню: Tools/Build/Sync Resources For Build.
/// Перед кожним білдом — викликати вручну або прив'язати IPreprocessBuildWithReport.
/// </summary>
public static class CopyResourcesForBuild
{
    const string RES_ROOT = "Assets/Resources";

    [MenuItem("Tools/Build/Sync Resources For Build")]
    public static void Sync()
    {
        EnsureFolder(RES_ROOT);

        // ---- SOUNDS ----
        EnsureFolder(RES_ROOT + "/Sound");
        // UI sounds from Assets/Sound/
        CopyIfExists("Assets/Sound/smartphone tap.mp3",     RES_ROOT + "/Sound/smartphone tap.mp3");
        CopyIfExists("Assets/Sound/menu options sound.mp3", RES_ROOT + "/Sound/menu options sound.mp3");
        CopyIfExists("Assets/Sound/footsteps.mp3",          RES_ROOT + "/Sound/footsteps.mp3");
        CopyIfExists("Assets/Sound/MainMenu.mp3",           RES_ROOT + "/Sound/MainMenu.mp3");

        // AI-generated sounds in Assets/ root — копіюємо ВСЕ за префіксом
        CopyAudioByPrefix("A_deep,_long_inhale",     RES_ROOT + "/Sound/smokeInhale.mp3");
        CopyAudioByPrefix("Creature_gnawing_and",    RES_ROOT + "/Sound/foodEat.mp3");
        CopyAudioByPrefix("the_sound_of_a_door",     RES_ROOT + "/Sound/doorSound.mp3");
        CopyAudioByPrefix("the_sound_of_the_TV__#3-on", RES_ROOT + "/Sound/tvOn.mp3");
        CopyAudioByPrefix("the_sound_of_the_TV__off",   RES_ROOT + "/Sound/tvOff.mp3");
        CopyAudioByPrefix("ElevenLabs_2026-04-18T08_19_13_123", RES_ROOT + "/Sound/cutsceneVoice.mp3");
        CopyAudioByPrefix("Gentle_hand_picking",     RES_ROOT + "/Sound/pickupSound.mp3");
        // intro для PlayerSmoking (sp72_s26_sb23)
        CopyAudioContains("ElevenLabs_2026-04-19", "sp72_s26_sb23", RES_ROOT + "/Sound/bedWakeIntro.mp3");

        // ---- ICONS / SPRITES ----
        EnsureFolder(RES_ROOT + "/GameIcons");
        CopyIfExists("Assets/knife_hit.png",    RES_ROOT + "/GameIcons/knife_hit.png");
        CopyIfExists("Assets/subway_surf.png",  RES_ROOT + "/GameIcons/subway_surf.png");
        CopyIfExists("Assets/police_chase.png", RES_ROOT + "/GameIcons/police_chase.png");
        CopyIfExists("Assets/casino.png",       RES_ROOT + "/GameIcons/casino.png");

        // Slot atlas для казино
        EnsureFolder(RES_ROOT + "/SlotSprites");
        CopyIfExists("Assets/TextursAssets/slot.png", RES_ROOT + "/SlotSprites/slot.png");
        // Важливо: після копіювання треба переконатись, що імпорт = Sprite (Multiple)
        ConfigureSlotImporter(RES_ROOT + "/SlotSprites/slot.png");

        // ---- SUBWAY BARRIERS ----
        EnsureFolder(RES_ROOT + "/SubwaySurf");
        EnsureFolder(RES_ROOT + "/SubwaySurf/Barriers");
        const string barrierSrc = "Assets/Barriers pack Demo/Barriers pack Demo/Fbx";
        if (AssetDatabase.IsValidFolder(barrierSrc))
        {
            foreach (var fbx in Directory.GetFiles(barrierSrc, "*.fbx", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(fbx);
                string src = fbx.Replace('\\', '/');
                CopyIfExists(src, RES_ROOT + "/SubwaySurf/Barriers/" + fileName);
            }
        }
        CopyIfExists("Assets/Barriers pack Demo/Barriers pack Demo/texture.jpg",
                     RES_ROOT + "/SubwaySurf/barriers_texture.jpg");

        // ---- POLICE CHASE CARS ----
        // Шукаємо ймовірну папку з машинами (Low_Poly_Vehicles).
        // ВАЖЛИВО: копіюємо ТІЛЬКИ .fbx (не .obj/.dae/.mtl), інакше Resources.Load
        // може повернути "не той" асет.
        EnsureFolder(RES_ROOT + "/PoliceChase");
        EnsureFolder(RES_ROOT + "/PoliceChase/Cars");
        EnsureFolder(RES_ROOT + "/PoliceChase/Textures");
        CopyMatching("t:GameObject Low_Poly_Vehicles_",  RES_ROOT + "/PoliceChase/Cars",  ".fbx");
        CopyMatching("t:Texture2D car",                  RES_ROOT + "/PoliceChase/Textures");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CopyResourcesForBuild] Done. Resources synced to " + RES_ROOT);
    }

    // ---------------------------------------------------------------------

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf   = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static void CopyIfExists(string from, string to)
    {
        if (!File.Exists(from)) { return; }
        EnsureFolder(Path.GetDirectoryName(to).Replace('\\', '/'));
        if (File.Exists(to))
        {
            // Перезаписуємо тільки якщо джерело новіше
            if (File.GetLastWriteTime(from) <= File.GetLastWriteTime(to)) return;
            AssetDatabase.DeleteAsset(to);
        }
        if (!AssetDatabase.CopyAsset(from, to))
            Debug.LogWarning($"[CopyResourcesForBuild] Failed to copy {from} -> {to}");
    }

    static void CopyAudioByPrefix(string prefix, string targetPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            // Не копіюємо з Resources — уникаємо рекурсії
            if (p.StartsWith(RES_ROOT)) continue;
            string fn = Path.GetFileNameWithoutExtension(p);
            if (!string.IsNullOrEmpty(fn) && fn.StartsWith(prefix))
            {
                string ext = Path.GetExtension(p);
                string final = Path.ChangeExtension(targetPath, ext);
                CopyIfExists(p, final);
                return;
            }
        }
    }

    static void CopyAudioContains(string prefix, string contains, string targetPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (p.StartsWith(RES_ROOT)) continue;
            string fn = Path.GetFileNameWithoutExtension(p);
            if (!string.IsNullOrEmpty(fn) && fn.StartsWith(prefix) && fn.Contains(contains))
            {
                string ext = Path.GetExtension(p);
                string final = Path.ChangeExtension(targetPath, ext);
                CopyIfExists(p, final);
                return;
            }
        }
    }

    static void CopyMatching(string filter, string destDir, string extensionFilter = null)
    {
        string[] guids = AssetDatabase.FindAssets(filter);
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (p.StartsWith(RES_ROOT)) continue;
            if (!string.IsNullOrEmpty(extensionFilter))
            {
                string ext = Path.GetExtension(p).ToLower();
                if (ext != extensionFilter.ToLower()) continue;
            }
            string dst = destDir + "/" + Path.GetFileName(p);
            CopyIfExists(p, dst);
        }
    }

    static void ConfigureSlotImporter(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        if (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Multiple)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.SaveAndReimport();
        }
    }
}

/// <summary>
/// Автоматично синхронізує ресурси перед білдом, щоб користувач не забув.
/// </summary>
public class AutoSyncResourcesOnBuild : UnityEditor.Build.IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;
    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        Debug.Log("[AutoSyncResourcesOnBuild] Syncing Resources before build...");
        CopyResourcesForBuild.Sync();
    }
}
