#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Переставляє існуючі крани BathFaucet/SinkFaucet у правильні позиції
/// над ванною і раковиною, щоб струмінь води падав у чашу.
/// Логіка інтерактиву (E) НЕ змінюється - гравець підходить і натискає E.
/// </summary>
public static class RepositionFaucets
{
    public static void Execute()
    {
        // -----------------------------------------------------------
        // SINK FAUCET  (над раковиною)
        // Sink bounds: min=(789.38, 192.10, -9.39)  max=(790.11, 194.50, -7.48)
        // Верхня площина чаші приблизно y=194.25.
        // Кран садимо на задній край чаші (великий Z від'ємний - біля стінки),
        // носик вперед до гравця (Z більший), вода падає вниз в центр чаші.
        // -----------------------------------------------------------
        var sinkFaucet = GameObject.Find("SinkFaucet");
        if (sinkFaucet != null)
        {
            sinkFaucet.transform.position = new Vector3(789.74f, 194.40f, -9.05f);
            sinkFaucet.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            // Spout - горизонтальний носик
            var spout = sinkFaucet.transform.Find("Spout");
            if (spout != null)
            {
                // носик вперед (в напрямку -Z коли дивимось зверху - в центр чаші)
                spout.localPosition = new Vector3(0f, 0f, 0.08f);
                spout.localRotation = Quaternion.Euler(90f, 0f, 0f);
                spout.localScale    = new Vector3(0.04f, 0.08f, 0.04f);
            }

            // Water - струмінь вниз у чашу
            var water = sinkFaucet.transform.Find("Water");
            if (water != null)
            {
                water.localPosition = new Vector3(0f, -0.08f, 0.15f);
                // обертаємо щоб ParticleSystem.forward дивився вниз (ParticleSystem емітує вздовж +Z локально)
                water.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            EditorUtility.SetDirty(sinkFaucet);
        }

        // -----------------------------------------------------------
        // BATH FAUCET  (над ванною, на задній стінці)
        // Bath bounds: min=(784.93, 192.05, -10.98) max=(786.99, 195.24, -7.34)
        // Ванна витягнута по Z. Ставимо кран ЗБОКУ (біля стіни кімнати), типово в ногах.
        // Кран на x=786.5 (біля правого краю ванни), z=-10.6 (майже впритул до задньої стіни),
        // висота y=193.2 (над краєм ванни).
        // -----------------------------------------------------------
        var bathFaucet = GameObject.Find("BathFaucet");
        if (bathFaucet != null)
        {
            bathFaucet.transform.position = new Vector3(786.50f, 193.20f, -10.65f);
            bathFaucet.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            var spout = bathFaucet.transform.Find("Spout");
            if (spout != null)
            {
                // носик дивиться в Z+ (у ванну)
                spout.localPosition = new Vector3(0f, 0f, 0.10f);
                spout.localRotation = Quaternion.Euler(90f, 0f, 0f);
                spout.localScale    = new Vector3(0.045f, 0.10f, 0.045f);
            }

            var water = bathFaucet.transform.Find("Water");
            if (water != null)
            {
                water.localPosition = new Vector3(0f, -0.08f, 0.20f);
                water.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            EditorUtility.SetDirty(bathFaucet);
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[RepositionFaucets] Done. SinkFaucet at (789.74,194.40,-9.05), BathFaucet at (786.50,193.20,-10.65). Scene saved.");
    }
}
#endif
