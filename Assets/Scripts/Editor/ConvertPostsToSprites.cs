using UnityEngine;
using UnityEditor;
using System.IO;

public static class ConvertPostsToSprites
{
    public static void Execute()
    {
        string folder = "Assets/Resources/Posts";
        if (!Directory.Exists(folder))
        {
            Debug.LogError($"[ConvertPostsToSprites] Folder not found: {folder}");
            return;
        }

        string[] files = Directory.GetFiles(folder, "*.png");
        int converted = 0;
        foreach (string file in files)
        {
            string assetPath = file.Replace("\\", "/");
            if (assetPath.StartsWith("./")) assetPath = assetPath.Substring(2);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[ConvertPostsToSprites] No importer: {assetPath}");
                continue;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (!importer.isReadable)
            {
                // не обязательно, но безопаснее
                // importer.isReadable = true;
                // changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false; // UI обычно без mipmap
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                converted++;
                Debug.Log($"[ConvertPostsToSprites] Converted → Sprite: {assetPath}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ConvertPostsToSprites] Done. Converted {converted} files out of {files.Length}.");
    }
}
