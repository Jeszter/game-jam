using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// У Resources/PoliceChase/Cars/ можуть бути дублікати .fbx/.obj/.dae з однаковим ім'ям.
/// Resources.Load повертає лише ОДИН асет на ім'я — і може взяти "неправильний" (напр. .obj
/// без ієрархії рендерерів). Видаляємо .obj і .dae, лишаємо лише .fbx.
/// </summary>
public static class CleanDuplicateCarFormats
{
    [MenuItem("Tools/Build/Clean Duplicate Car Formats")]
    public static void Execute()
    {
        const string dir = "Assets/Resources/PoliceChase/Cars";
        if (!AssetDatabase.IsValidFolder(dir)) { Debug.Log("[CleanDuplicateCarFormats] Folder missing"); return; }

        int removed = 0;
        foreach (var path in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
        {
            string p = path.Replace('\\', '/');
            string ext = Path.GetExtension(p).ToLower();
            if (ext == ".obj" || ext == ".dae" || ext == ".mtl")
            {
                if (AssetDatabase.DeleteAsset(p)) removed++;
            }
        }
        AssetDatabase.Refresh();
        Debug.Log($"[CleanDuplicateCarFormats] Removed {removed} duplicate files (.obj/.dae/.mtl).");
    }
}
