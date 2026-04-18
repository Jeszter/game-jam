using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Центральный менеджер экономики игры:
/// - Дофамин (DPM) + усталость (боредом) по каждой активности
/// - Шанс выпадения DoomCoin при получении дофамина
/// - Голод (Hunger) — падает со временем, при низком голоде дофамин даётся хуже
///
/// Формула дофамина:
///   effective = base * (1 - boredom[activity]) * hungerPenalty
/// Усталость растёт с каждой "сессией" и медленно спадает со временем.
/// Базовые активности (TikTok/Phone) теряют эффективность быстрее → мотивация покупать апгрейды (Laptop, TV+PS5).
/// </summary>
public class GameEconomy : MonoBehaviour
{
    public static GameEconomy Instance { get; private set; }

    // ----- Activity keys (используются во всех мини-играх и источниках дофамина) -----
    public const string ActTikTok   = "tiktok";
    public const string ActKnife    = "knife";
    public const string ActFlappy   = "flappy";
    public const string ActSubway   = "subway";
    public const string ActPolice   = "police";
    public const string ActCasino   = "casino";
    public const string ActFood     = "food";
    public const string ActMusic    = "music";

    [System.Serializable]
    public class ActivityConfig
    {
        public string key;
        [Tooltip("Сколько дофамина даёт 1 \"событие\" на свежую активность")]
        public float baseGain = 10f;
        [Tooltip("Сколько прибавляется к boredom за одно событие (0..1)")]
        public float boredomGrowth = 0.05f;
        [Tooltip("Как быстро boredom сам падает со временем (в секунду)")]
        public float boredomRecovery = 0.005f;
        [Tooltip("Мин. множитель эффективности (когда boredom=1). 0 = полностью приелось.")]
        [Range(0f, 1f)] public float minEffectiveness = 0.1f;
        [Tooltip("Шанс выпадения монет при событии (0..1)")]
        [Range(0f, 1f)] public float coinDropChance = 0.25f;
        [Tooltip("Сколько монет выпадает (мин-макс)")]
        public Vector2Int coinReward = new Vector2Int(3, 12);

        [HideInInspector] public float boredom; // 0..1
    }

    [Header("Activities")]
    [SerializeField] private List<ActivityConfig> activities = new List<ActivityConfig>();

    [Header("Hunger")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger = 100f;
    [SerializeField] private float hungerDecayRate = 0.4f; // per second
    [Tooltip("При голоде ниже этого уровня дофамин режется")]
    [SerializeField] private float hungerDebuffThreshold = 30f;
    [Tooltip("Минимальный множитель дофамина при нулевом голоде")]
    [SerializeField] private float hungerMinMultiplier = 0.25f;

    private Dictionary<string, ActivityConfig> activityMap = new Dictionary<string, ActivityConfig>();
    private GameHUDController hud;

    public float CurrentHunger => currentHunger;
    public float MaxHunger => maxHunger;
    public float HungerRatio => maxHunger > 0 ? currentHunger / maxHunger : 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (activities.Count == 0)
            SetupDefaultActivities();

        RebuildMap();
    }

    void Start()
    {
        hud = FindFirstObjectByType<GameHUDController>();
    }

    void SetupDefaultActivities()
    {
        // БАЗОВЫЕ — быстро приедаются, стимулируют покупать ноутбук/TV
        activities.Add(new ActivityConfig {
            key = ActTikTok,
            baseGain = 6f,
            boredomGrowth = 0.06f,      // каждый свайп +6% усталости
            boredomRecovery = 0.004f,   // мееедленно восстанавливается
            minEffectiveness = 0.05f,   // почти ноль при полной усталости
            coinDropChance = 0.18f,
            coinReward = new Vector2Int(2, 6)
        });
        activities.Add(new ActivityConfig {
            key = ActKnife,
            baseGain = 9f,
            boredomGrowth = 0.05f,
            boredomRecovery = 0.005f,
            minEffectiveness = 0.1f,
            coinDropChance = 0.25f,
            coinReward = new Vector2Int(3, 10)
        });

        // АПГРЕЙДЫ — дают намного больше, приедаются медленнее
        activities.Add(new ActivityConfig {
            key = ActFlappy,
            baseGain = 14f,
            boredomGrowth = 0.03f,
            boredomRecovery = 0.007f,
            minEffectiveness = 0.3f,
            coinDropChance = 0.4f,
            coinReward = new Vector2Int(5, 20)
        });
        activities.Add(new ActivityConfig {
            key = ActSubway,
            baseGain = 15f,
            boredomGrowth = 0.025f,
            boredomRecovery = 0.007f,
            minEffectiveness = 0.3f,
            coinDropChance = 0.5f,
            coinReward = new Vector2Int(5, 25)
        });
        activities.Add(new ActivityConfig {
            key = ActPolice,
            baseGain = 18f,
            boredomGrowth = 0.025f,
            boredomRecovery = 0.007f,
            minEffectiveness = 0.3f,
            coinDropChance = 0.5f,
            coinReward = new Vector2Int(8, 30)
        });
        activities.Add(new ActivityConfig {
            key = ActCasino,
            baseGain = 20f,
            boredomGrowth = 0.02f,
            boredomRecovery = 0.008f,
            minEffectiveness = 0.4f,
            coinDropChance = 0f,        // казино само даёт монеты
            coinReward = new Vector2Int(0, 0)
        });

        // ЕДА и МУЗЫКА — не приедаются
        activities.Add(new ActivityConfig {
            key = ActFood,
            baseGain = 12f,
            boredomGrowth = 0.01f,
            boredomRecovery = 0.02f,
            minEffectiveness = 0.6f,
            coinDropChance = 0f,
            coinReward = new Vector2Int(0, 0)
        });
        activities.Add(new ActivityConfig {
            key = ActMusic,
            baseGain = 3f,
            boredomGrowth = 0.005f,
            boredomRecovery = 0.01f,
            minEffectiveness = 0.5f,
            coinDropChance = 0f,
            coinReward = new Vector2Int(0, 0)
        });
    }

    void RebuildMap()
    {
        activityMap.Clear();
        foreach (var a in activities)
            activityMap[a.key] = a;
    }

    void Update()
    {
        // Boredom сам потихоньку восстанавливается
        foreach (var a in activities)
        {
            if (a.boredom > 0f)
                a.boredom = Mathf.Max(0f, a.boredom - a.boredomRecovery * Time.deltaTime);
        }

        // Hunger падает со временем
        currentHunger = Mathf.Max(0f, currentHunger - hungerDecayRate * Time.deltaTime);
    }

    /// <summary>
    /// Сообщить о событии в активности: даёт эффективный дофамин, растит усталость,
    /// с шансом выдаёт монеты. Возвращает сколько дофамина было добавлено.
    /// </summary>
    public float AwardDopamine(string activityKey, float multiplier = 1f)
    {
        if (!activityMap.TryGetValue(activityKey, out var act))
        {
            // неизвестная активность — как нейтральная
            if (hud != null) hud.AddDopamine(5f * multiplier);
            return 5f * multiplier;
        }

        float effectiveness = Mathf.Lerp(1f, act.minEffectiveness, act.boredom);
        float hungerMult = HungerMultiplier();
        float gain = act.baseGain * effectiveness * hungerMult * multiplier;

        if (hud == null) hud = FindFirstObjectByType<GameHUDController>();
        if (hud != null) hud.AddDopamine(gain);

        // Рост усталости
        act.boredom = Mathf.Min(1f, act.boredom + act.boredomGrowth * multiplier);

        // Шанс выпадения монет
        TryDropCoins(act, multiplier);

        return gain;
    }

    /// <summary>
    /// Попытка выдать монеты за активность (можно вызвать вручную).
    /// </summary>
    public int TryDropCoins(string activityKey, float multiplier = 1f)
    {
        if (!activityMap.TryGetValue(activityKey, out var act)) return 0;
        return TryDropCoins(act, multiplier);
    }

    private int TryDropCoins(ActivityConfig act, float multiplier)
    {
        if (act.coinDropChance <= 0f) return 0;
        float roll = Random.value;
        if (roll > act.coinDropChance * multiplier) return 0;

        int min = act.coinReward.x;
        int max = Mathf.Max(min + 1, act.coinReward.y);
        int amount = Random.Range(min, max + 1);
        if (amount <= 0) return 0;

        if (hud == null) hud = FindFirstObjectByType<GameHUDController>();
        if (hud != null) hud.AddCoins(amount);
        CoinFloater.Spawn(amount);
        return amount;
    }

    public void AddCoins(int amount)
    {
        if (hud == null) hud = FindFirstObjectByType<GameHUDController>();
        if (hud != null) hud.AddCoins(amount);
        if (amount > 0) CoinFloater.Spawn(amount);
    }

    // ---------------- HUNGER ----------------

    public float HungerMultiplier()
    {
        if (currentHunger >= hungerDebuffThreshold) return 1f;
        // Линейно от min до 1
        float t = currentHunger / Mathf.Max(0.01f, hungerDebuffThreshold);
        return Mathf.Lerp(hungerMinMultiplier, 1f, t);
    }

    public void AddHunger(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);
    }

    // ---------------- Debug helpers ----------------

    public float GetBoredom(string key)
    {
        return activityMap.TryGetValue(key, out var a) ? a.boredom : 0f;
    }

    /// <summary>Строка для отладочного оверлея: "tiktok 65% | knife 20% ..."</summary>
    public string GetBoredomSummary()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var a in activities)
        {
            sb.Append(a.key);
            sb.Append(' ');
            sb.Append(Mathf.RoundToInt(a.boredom * 100));
            sb.Append("% | ");
        }
        return sb.ToString().TrimEnd(' ', '|');
    }
}
