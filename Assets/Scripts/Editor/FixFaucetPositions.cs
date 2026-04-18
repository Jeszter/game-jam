#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Фінальне налаштування кранів:
/// - SinkFaucet садимо на кінець носика реального крана раковини (на мешу)
/// - BathFaucet садимо під душову лійку (на мешу ванни)
/// - Видаляємо візуальний Spout (там вже є кран на мешу)
/// - ParticleSystem спрямовується строго вниз у чашу
/// - Box Collider робимо ТРИГЕРОМ та побільшим щоб зручно було прицілюватись з E
/// Інтерактив зберігається: гравець підходить, дивиться на кран, натискає E.
/// </summary>
public static class FixFaucetPositions
{
    public static void Execute()
    {
        // ---------- Sink ----------
        var sinkFaucet = GameObject.Find("SinkFaucet");
        if (sinkFaucet != null)
        {
            // Кінчик носика раковини - world (789.85, 193.50, -8.70) приблизно
            sinkFaucet.transform.position = new Vector3(789.85f, 193.50f, -8.70f);
            sinkFaucet.transform.rotation = Quaternion.identity;

            // Видаляємо візуальний Spout (у раковини вже є свій кран на мешу)
            var spout = sinkFaucet.transform.Find("Spout");
            if (spout != null) Object.DestroyImmediate(spout.gameObject);

            // Збільшуємо інтерактивний BoxCollider, робимо тригером
            foreach (var c in sinkFaucet.GetComponents<BoxCollider>()) Object.DestroyImmediate(c);
            var col = sinkFaucet.AddComponent<BoxCollider>();
            col.size      = new Vector3(0.35f, 0.4f, 0.35f);
            col.center    = new Vector3(0f, 0.05f, 0f);
            col.isTrigger = true;

            // Вода - строго вниз у центр чаші
            var water = sinkFaucet.transform.Find("Water");
            if (water != null)
            {
                water.localPosition = new Vector3(0f, -0.05f, 0f);
                water.localRotation = Quaternion.Euler(90f, 0f, 0f); // cone face down
                TweakWater(water.GetComponent<ParticleSystem>(), 1.5f, 0.02f, 80f);
            }

            EditorUtility.SetDirty(sinkFaucet);
        }

        // ---------- Bath ----------
        var bathFaucet = GameObject.Find("BathFaucet");
        if (bathFaucet != null)
        {
            // Душова лійка на мешу ванни: world (786.30, 194.83, -9.16)
            // Трохи нижче щоб вода виходила з-під лійки
            bathFaucet.transform.position = new Vector3(786.30f, 194.70f, -9.16f);
            bathFaucet.transform.rotation = Quaternion.identity;

            var spout = bathFaucet.transform.Find("Spout");
            if (spout != null) Object.DestroyImmediate(spout.gameObject);

            foreach (var c in bathFaucet.GetComponents<BoxCollider>()) Object.DestroyImmediate(c);
            var col = bathFaucet.AddComponent<BoxCollider>();
            col.size      = new Vector3(0.4f, 0.4f, 0.4f);
            col.center    = new Vector3(0f, 0f, 0f);
            col.isTrigger = true;

            var water = bathFaucet.transform.Find("Water");
            if (water != null)
            {
                water.localPosition = new Vector3(0f, -0.08f, 0f);
                water.localRotation = Quaternion.Euler(90f, 0f, 0f);
                // Душ - ширший конус, багато частинок
                TweakWater(water.GetComponent<ParticleSystem>(), 2.8f, 0.04f, 180f, coneAngle: 18f);
            }

            EditorUtility.SetDirty(bathFaucet);
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[FixFaucetPositions] Done - faucets aligned to mesh, water points down. Scene saved.");
    }

    private static void TweakWater(ParticleSystem ps, float speed, float size, float rate, float coneAngle = 4f)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startSpeed    = speed;
        main.startSize     = size;
        main.startLifetime = 1.0f;
        main.gravityModifier = 2.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 600;
        main.playOnAwake     = false;

        var emission = ps.emission;
        emission.rateOverTime = rate;
        emission.enabled      = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = coneAngle;
        shape.radius    = 0.01f;
    }
}
#endif
