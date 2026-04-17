using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Creates material for each car FBX from the Low_Poly_Cars_DevilsWorkShop_V03 pack
/// and applies the corresponding texture. Also configures FBX import to use
/// external materials so we can remap them.
/// </summary>
public static class SetupCarMaterials
{
    static readonly string basePath = "Assets/TextursAssets/games/Low_Poly_Cars_DevilsWorkShop_V03/";
    static readonly string fbxPath = basePath + "FBX 2013/";
    static readonly string texPath = basePath + "Texture/";
    static readonly string matPath = basePath + "Materials/";

    static readonly string[] carModels = {
        "Low_Poly_Vehicles_bus",
        "Low_Poly_Vehicles_car01",
        "Low_Poly_Vehicles_car02",
        "Low_Poly_Vehicles_car03",
        "Low_Poly_Vehicles_carPolice",
        "Low_Poly_Vehicles_pickupTruck01",
        "Low_Poly_Vehicles_pickupTruck02",
    };
    static readonly string[] carTextures = {
        "bus01", "car01", "car02", "car03",
        "carPolice", "pickupTruck01", "pickupTruck02",
    };

    [MenuItem("Tools/Setup Car Materials")]
    public static void Run()
    {
        if (!Directory.Exists(matPath))
            Directory.CreateDirectory(matPath);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        for (int i = 0; i < carModels.Length; i++)
        {
            string model = carModels[i];
            string texName = carTextures[i];

            // Load texture (png preferred)
            string texAssetPath = texPath + texName + ".png";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texAssetPath);
            if (tex == null)
            {
                texAssetPath = texPath + texName + ".tga";
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texAssetPath);
            }
            if (tex == null)
            {
                Debug.LogWarning($"[SetupCarMaterials] Missing texture for {model}: {texName}");
                continue;
            }

            // Create or load material
            string matAssetPath = matPath + model + "_Mat.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matAssetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matAssetPath);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);

            // Apply to FBX as external material remap
            string fbxAssetPath = fbxPath + model + ".fbx";
            var importer = AssetImporter.GetAtPath(fbxAssetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[SetupCarMaterials] Missing FBX: {fbxAssetPath}");
                continue;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;

            // Remap all material slots on FBX to our single textured material
            var remap = importer.GetExternalObjectMap();
            foreach (var id in importer.GetExternalObjectMap())
            {
                // Remove existing
                if (id.Key.type == typeof(Material))
                    importer.RemoveRemap(id.Key);
            }

            // Load the FBX asset to find its material slots
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxAssetPath);
            foreach (var a in assets)
            {
                if (a is Material m)
                {
                    var ident = new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name);
                    importer.AddRemap(ident, mat);
                }
            }

            AssetDatabase.WriteImportSettingsIfDirty(fbxAssetPath);
            importer.SaveAndReimport();

            Debug.Log($"[SetupCarMaterials] Configured {model} with texture {texName}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupCarMaterials] Done!");
    }
}
