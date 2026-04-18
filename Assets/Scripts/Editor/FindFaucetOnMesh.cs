#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Debug helper - викликаємо Raycast зверху над Sink і Bath щоб знайти
/// де реальний кран на мешу (найближча до верху точка з малим розміром, що стирчить).
/// </summary>
public static class FindFaucetOnMesh
{
    public static void Execute()
    {
        Debug.Log("=== Sink mesh submeshes ===");
        InspectMesh(GameObject.Find("House/Sink"));
        Debug.Log("=== Bath mesh submeshes ===");
        InspectMesh(GameObject.Find("House/Bath"));
    }

    static void InspectMesh(GameObject go)
    {
        if (go == null) return;
        var mf = go.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;
        var m = mf.sharedMesh;
        Debug.Log($"[{go.name}] verts={m.vertexCount} submeshes={m.subMeshCount} bounds={m.bounds} worldMin={go.GetComponent<Renderer>().bounds.min} worldMax={go.GetComponent<Renderer>().bounds.max}");
        for (int s = 0; s < m.subMeshCount; s++)
        {
            var sm = m.GetSubMesh(s);
            Debug.Log($"  submesh[{s}] indexCount={sm.indexCount} firstVertex={sm.firstVertex} vertexCount={sm.vertexCount} bounds={sm.bounds}");
        }
    }
}
#endif
