using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Після покупки в магазині:
///   V — пихнути VAPE  (перший раз +35 DP, далі приїдається; crash до ~8 DP за 4s; 8s cd)
///   Q — куриити IQOS  (перший раз +45 DP, далі приїдається; crash до ~5 DP за 5s; 10s cd)
///
/// Механіка:
///  - спайк мінус boredom (0..1) → після кількох юзів даватиме менше
///  - одразу потім DP плавно «crash» спадає до residual
///  - великий клубок диму перед обличчям гравця (billboard quads з Sprites/Default)
/// </summary>
public class PlayerSmoking : MonoBehaviour
{
    [Header("Hotkeys")]
    public Key vapeKey = Key.V;
    public Key iqosKey = Key.Q;

    [Header("Vape")]
    public float vapeSpike = 35f;
    public float vapeResidual = 8f;
    public float vapeCrashDuration = 4f;
    public float vapeCooldown = 8f;
    public Color vapeSmokeColor = new Color(0.85f, 1f, 0.95f, 1f);
    public int vapePuffCount = 40;
    public float vapeSmokeDuration = 2.4f;

    [Header("IQOS")]
    public float iqosSpike = 45f;
    public float iqosResidual = 5f;
    public float iqosCrashDuration = 5f;
    public float iqosCooldown = 10f;
    public Color iqosSmokeColor = new Color(0.96f, 0.94f, 1f, 1f);
    public int iqosPuffCount = 80;         // ДУЖЕ багато
    public float iqosSmokeDuration = 3.4f;

    [Header("Boredom (spike diminishing)")]
    [Range(0f, 1f)] public float boredomGrowth = 0.22f;
    [Range(0f, 1f)] public float boredomRecovery = 0.01f;
    [Range(0f, 1f)] public float minEffectiveness = 0.12f;

    private ShopController shop;
    private GameHUDController hud;

    private float nextVapeTime = 0f;
    private float nextIqosTime = 0f;

    private float vapeBoredom = 0f;
    private float iqosBoredom = 0f;

    private Coroutine activeCrash;
    private Texture2D cachedSmokeTex;
    private Shader cachedSpriteShader;

    void Start()
    {
        shop = FindFirstObjectByType<ShopController>();
        hud = FindFirstObjectByType<GameHUDController>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (shop == null) shop = FindFirstObjectByType<ShopController>();
        if (hud == null) hud = FindFirstObjectByType<GameHUDController>();
        if (shop == null) return;

        vapeBoredom = Mathf.Max(0f, vapeBoredom - boredomRecovery * Time.deltaTime);
        iqosBoredom = Mathf.Max(0f, iqosBoredom - boredomRecovery * Time.deltaTime);

        // VAPE — V
        if (Keyboard.current[vapeKey].wasPressedThisFrame)
        {
            if (!shop.IsPurchased("Vape"))
                Debug.Log("[Smoking] Vape not purchased yet");
            else if (Time.time < nextVapeTime)
                Debug.Log($"[Smoking] Vape cd {Mathf.CeilToInt(nextVapeTime - Time.time)}s");
            else
            {
                float mult = Mathf.Lerp(1f, minEffectiveness, vapeBoredom);
                float spike = vapeSpike * mult;
                float residual = vapeResidual * mult;
                Consume("Vape", spike, residual, vapeCrashDuration,
                    vapeSmokeColor, vapePuffCount, vapeSmokeDuration, vapeBoredom);
                vapeBoredom = Mathf.Min(1f, vapeBoredom + boredomGrowth);
                nextVapeTime = Time.time + vapeCooldown;
            }
        }

        // IQOS — Q
        if (Keyboard.current[iqosKey].wasPressedThisFrame)
        {
            if (!shop.IsPurchased("IQOS"))
                Debug.Log("[Smoking] IQOS not purchased yet");
            else if (Time.time < nextIqosTime)
                Debug.Log($"[Smoking] IQOS cd {Mathf.CeilToInt(nextIqosTime - Time.time)}s");
            else
            {
                float mult = Mathf.Lerp(1f, minEffectiveness, iqosBoredom);
                float spike = iqosSpike * mult;
                float residual = iqosResidual * mult;
                Consume("IQOS", spike, residual, iqosCrashDuration,
                    iqosSmokeColor, iqosPuffCount, iqosSmokeDuration, iqosBoredom);
                iqosBoredom = Mathf.Min(1f, iqosBoredom + boredomGrowth);
                nextIqosTime = Time.time + iqosCooldown;
            }
        }
    }

    private void Consume(string itemName, float spike, float residual, float crashDur,
                         Color smokeColor, int puffs, float smokeDuration, float boredom)
    {
        // 1) Spike DP
        if (hud != null) hud.AddDopamine(spike);

        // 2) Smoke spawn position — перед камерою (трохи нижче носа)
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[Smoking] Camera.main == null, пропускаю дим");
        }
        else
        {
            // ВАЖЛИВО: старт диму має бути поза near clip plane, інакше він буде clip-нутий.
            float startDist = Mathf.Max(cam.nearClipPlane + 0.05f, 0.5f);
            Vector3 pos = cam.transform.position
                + cam.transform.forward * startDist
                + cam.transform.up * -0.12f; // трохи нижче лінії погляду
            SpawnBillboardSmoke(pos, cam.transform.forward, smokeColor, puffs, smokeDuration);
        }

        CoinFloater.Spawn(Mathf.RoundToInt(spike));

        // 3) Crash — постепенно забираємо DP до residual
        if (activeCrash != null) StopCoroutine(activeCrash);
        activeCrash = StartCoroutine(DopamineCrash(spike, residual, crashDur));

        Debug.Log($"[Smoking] {itemName} +{spike:0.#} DP (boredom {Mathf.RoundToInt(boredom*100)}% → crash → {residual:0.#} DP) puffs={puffs}");
    }

    private IEnumerator DopamineCrash(float spike, float residual, float duration)
    {
        float toRemove = Mathf.Max(0f, spike - residual);
        if (toRemove <= 0f || hud == null) yield break;

        yield return new WaitForSeconds(0.6f);

        float elapsed = 0f, removed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            float target = toRemove * eased;
            float delta = target - removed;
            if (delta > 0f && hud != null)
            {
                hud.AddDopamine(-delta);
                removed += delta;
            }
            yield return null;
        }
        activeCrash = null;
    }

    // ============================================================
    // SMOKE — gameobjects з Quad меш + Sprites/Default шейдер
    // Sprites/Default ГАРАНТОВАНО підтримує alpha blending на всіх pipeline'ах.
    // ============================================================

    private void SpawnBillboardSmoke(Vector3 worldPos, Vector3 fwd, Color tint, int count, float dur)
    {
        var root = new GameObject($"SmokeBurst_{Time.frameCount}");
        root.transform.position = worldPos;

        if (cachedSmokeTex == null) cachedSmokeTex = BuildPuffTexture();
        if (cachedSpriteShader == null)
        {
            cachedSpriteShader = Shader.Find("Sprites/Default");
            if (cachedSpriteShader == null) cachedSpriteShader = Shader.Find("UI/Default");
            if (cachedSpriteShader == null) cachedSpriteShader = Shader.Find("Unlit/Transparent");
            Debug.Log("[Smoking] Using shader: " + (cachedSpriteShader != null ? cachedSpriteShader.name : "NONE!"));
        }

        var runner = root.AddComponent<SmokeBurstRunner>();
        runner.Init(cachedSpriteShader, cachedSmokeTex, tint, count, dur, fwd);
    }

    private Texture2D BuildPuffTexture()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float maxD = cx;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / maxD;
                float dy = (y - cy) / maxD;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                a *= Random.Range(0.85f, 1f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return tex;
    }
}

/// <summary>
/// Ранер який створює N білбордних квадів з дим-текстурою, анімує їх
/// вгору-вперед, змінюючи масштаб і альфу, потім сам себе знищує.
/// </summary>
public class SmokeBurstRunner : MonoBehaviour
{
    private struct Puff
    {
        public Transform t;
        public Material mat;
        public Vector3 vel;
        public float startSize;
        public float maxSize;
        public float startDelay;
        public float life;
        public Color baseColor;
    }

    private Puff[] puffs;
    private float elapsed;
    private float duration;
    private Transform cam;

    public void Init(Shader sh, Texture2D tex, Color tint, int count, float dur, Vector3 fwd)
    {
        duration = dur;
        cam = Camera.main != null ? Camera.main.transform : null;
        puffs = new Puff[count];

        // Use a Quad mesh primitive
        for (int i = 0; i < count; i++)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Puff";
            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);
            quad.transform.SetParent(transform, false);

            float jitter = 0.25f;
            quad.transform.localPosition = new Vector3(
                Random.Range(-jitter, jitter),
                Random.Range(-jitter * 0.3f, jitter * 0.6f),
                Random.Range(-jitter, jitter));

            var mr = quad.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            // Make new material with alpha
            var mat = new Material(sh);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;

            var tintVar = tint;
            tintVar.r = Mathf.Clamp01(tintVar.r + Random.Range(-0.05f, 0.05f));
            tintVar.g = Mathf.Clamp01(tintVar.g + Random.Range(-0.05f, 0.05f));
            tintVar.b = Mathf.Clamp01(tintVar.b + Random.Range(-0.05f, 0.05f));
            tintVar.a = 0f; // старт невидимо, потім зростає
            mat.color = tintVar;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tintVar);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tintVar);
            mat.renderQueue = 3500;

            mr.material = mat;

            float start = Random.Range(0.2f, 0.45f);
            float end = Random.Range(1.2f, 2.5f);
            quad.transform.localScale = Vector3.one * start;

            var puff = new Puff
            {
                t = quad.transform,
                mat = mat,
                vel = fwd * Random.Range(0.6f, 1.5f)
                      + Vector3.up * Random.Range(0.4f, 1.0f)
                      + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f)),
                startSize = start,
                maxSize = end,
                startDelay = Random.Range(0f, 0.4f),
                life = dur,
                baseColor = new Color(tintVar.r, tintVar.g, tintVar.b, 1f),
            };
            puffs[i] = puff;
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        for (int i = 0; i < puffs.Length; i++)
        {
            var p = puffs[i];
            if (p.t == null) continue;

            float localT = elapsed - p.startDelay;
            if (localT < 0f)
            {
                var c0 = p.baseColor;
                c0.a = 0f;
                p.mat.color = c0;
                continue;
            }

            float k = Mathf.Clamp01(localT / Mathf.Max(0.01f, p.life - p.startDelay));

            // рух
            p.t.position += p.vel * Time.deltaTime;
            // повільне гальмування
            p.vel *= (1f - Time.deltaTime * 0.3f);
            // розростання
            float size = Mathf.Lerp(p.startSize, p.maxSize, k);
            p.t.localScale = Vector3.one * size;

            // білбординг — повертаємось до камери
            if (cam != null)
                p.t.rotation = Quaternion.LookRotation(p.t.position - cam.position, Vector3.up);

            // альфа: швидко підіймається, потім плавно до 0
            float a;
            if (k < 0.12f) a = k / 0.12f;
            else a = Mathf.Clamp01(1f - (k - 0.12f) / 0.88f);
            var c = p.baseColor;
            c.a = a * 0.95f;
            p.mat.color = c;
            if (p.mat.HasProperty("_Color")) p.mat.SetColor("_Color", c);
            if (p.mat.HasProperty("_BaseColor")) p.mat.SetColor("_BaseColor", c);
        }

        if (elapsed > duration + 0.6f)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (puffs == null) return;
        for (int i = 0; i < puffs.Length; i++)
            if (puffs[i].mat != null) Destroy(puffs[i].mat);
    }
}
