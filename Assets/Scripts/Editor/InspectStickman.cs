using UnityEngine;
using UnityEditor;

public static class InspectStickman
{
    public static void Execute()
    {
        string path = "Assets/Barriers pack Demo/Barriers pack Demo/Fbx/Stickalungu_Animated.fbx";
        var all = AssetDatabase.LoadAllAssetsAtPath(path);
        Debug.Log($"[Stick] Loaded {all.Length} sub-assets from {path}");
        foreach (var a in all)
        {
            if (a == null) continue;
            Debug.Log($"  - {a.GetType().Name}: {a.name}");
        }

        var mainGO = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (mainGO != null)
        {
            var mr = mainGO.GetComponentsInChildren<MeshRenderer>(true);
            var smr = mainGO.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var anim = mainGO.GetComponentsInChildren<Animator>(true);
            Debug.Log($"[Stick] MeshRenderers={mr.Length} SkinnedMeshRenderers={smr.Length} Animators={anim.Length}");
            var r = mainGO.GetComponentInChildren<Renderer>(true);
            if (r != null)
            {
                Debug.Log($"[Stick] Bounds center={r.bounds.center} size={r.bounds.size}");
            }
        }

        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer != null)
        {
            Debug.Log($"[Stick] animType={importer.animationType} importAnim={importer.importAnimation} clipCount={importer.clipAnimations.Length} defCount={importer.defaultClipAnimations.Length}");
            foreach (var c in importer.defaultClipAnimations)
                Debug.Log($"  defClip: {c.name} frames {c.firstFrame}-{c.lastFrame} loop={c.loopTime}");
        }
    }
}
