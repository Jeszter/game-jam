using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Slot machine casino mini-game. Played from the laptop.
/// Bet 10/50/100/500/1000 DC (DoomCoins). Spin for matching symbols.
/// 10% chance to match 2 symbols → 2x win. 5% chance to match 3 symbols → 10x win.
/// </summary>
public class CasinoGame : MonoBehaviour
{
    private GameObject root;
    private Camera cam;

    private int[] betOptions = { 10, 50, 100, 500, 1000 };
    private int currentBetIndex = 0;
    private int currentBet => betOptions[currentBetIndex];

    // Sprite names in Assets/TextursAssets/slot.png atlas
    private readonly string[] symbolNames = { "slot_0", "slot_4", "slot_9", "slot_11", "slot_12", "slot_13", "slot_15" };
    private readonly Color[] reelColors = new Color[] {
        new Color(0.2f, 0.15f, 0.3f),
        new Color(0.25f, 0.15f, 0.3f),
        new Color(0.15f, 0.1f, 0.25f),
        new Color(0.2f, 0.1f, 0.2f),
        new Color(0.25f, 0.1f, 0.3f),
        new Color(0.15f, 0.15f, 0.25f),
        new Color(0.3f, 0.1f, 0.2f),
    };

    private Sprite[] symbolSprites;
    private Image[] reelSymbolImages = new Image[3];
    private Image[] reelBGs = new Image[3];
    private TMP_Text betText;
    private TMP_Text balanceText;
    private TMP_Text resultText;
    private TMP_Text infoText;
    private Button spinButton;
    private Button betUpButton;
    private Button betDownButton;

    private GameHUDController hud;
    private bool isSpinning = false;
    private TMP_FontAsset font;

    public void Init(GameObject gameRoot, Camera gameCam)
    {
        root = gameRoot;
        cam = gameCam;

        cam.transform.localPosition = Vector3.zero;
        cam.backgroundColor = new Color(0.05f, 0.02f, 0.1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.depth = 10;

        // Find HUD to read/write coins
        hud = FindFirstObjectByType<GameHUDController>();

        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Load slot symbol sprites from atlas
        LoadSlotSprites();

        BuildUI();
        UpdateBalance();
        UpdateBet();
    }

    void LoadSlotSprites()
    {
        symbolSprites = new Sprite[symbolNames.Length];
#if UNITY_EDITOR
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/TextursAssets/slot.png");
        for (int i = 0; i < symbolNames.Length; i++)
        {
            foreach (var a in all)
            {
                if (a is Sprite s && s.name == symbolNames[i])
                { symbolSprites[i] = s; break; }
            }
        }
#endif
        // Runtime fallback: load all sprites from the texture at runtime
        if (symbolSprites[0] == null)
        {
            var loaded = Resources.LoadAll<Sprite>("SlotSprites");
            if (loaded != null && loaded.Length > 0)
            {
                for (int i = 0; i < symbolNames.Length; i++)
                {
                    foreach (var s in loaded)
                        if (s.name == symbolNames[i]) { symbolSprites[i] = s; break; }
                }
            }
        }
    }

    void BuildUI()
    {
        var canvasObj = new GameObject("CasinoCanvas");
        canvasObj.transform.SetParent(root.transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background
        var bg = MakePanel(canvasObj.transform, "BG", new Color(0.08f, 0.02f, 0.15f, 1f));
        SetFullStretch(bg);

        // Neon border
        var border = MakePanel(canvasObj.transform, "Border", new Color(0.95f, 0.2f, 0.6f, 0.3f));
        var bRt = border.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.1f, 0.08f);
        bRt.anchorMax = new Vector2(0.9f, 0.92f);
        bRt.offsetMin = Vector2.zero; bRt.offsetMax = Vector2.zero;

        // Title
        MakeText(canvasObj.transform, "Title", "🎰  DOOM CASINO  🎰", 96,
            new Vector2(0.5f, 0.9f), new Color(1f, 0.3f, 0.7f), FontStyles.Bold);

        MakeText(canvasObj.transform, "Subtitle", "Slot Machine", 32,
            new Vector2(0.5f, 0.83f), new Color(0.8f, 0.8f, 0.9f, 0.8f), FontStyles.Italic);

        // Reels
        float reelSize = 220f;
        float reelSpacing = 30f;
        float reelsY = 0.52f;
        for (int i = 0; i < 3; i++)
        {
            var reel = MakePanel(canvasObj.transform, "Reel" + i, new Color(0.15f, 0.1f, 0.2f, 1f));
            var rt = reel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, reelsY);
            rt.anchorMax = new Vector2(0.5f, reelsY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(reelSize, reelSize);
            rt.anchoredPosition = new Vector2((i - 1) * (reelSize + reelSpacing), 0);
            reelBGs[i] = reel.GetComponent<Image>();

            // Inner glow
            var inner = MakePanel(reel.transform, "Inner", new Color(0.05f, 0.02f, 0.1f, 1f));
            var irt = inner.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(8, 8); irt.offsetMax = new Vector2(-8, -8);

            var sym = new GameObject("Symbol");
            sym.transform.SetParent(inner.transform);
            var symImg = sym.AddComponent<Image>();
            symImg.preserveAspect = true;
            if (symbolSprites != null && symbolSprites.Length > 0 && symbolSprites[i % symbolSprites.Length] != null)
                symImg.sprite = symbolSprites[i % symbolSprites.Length];
            var srt = sym.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(18, 18);
            srt.offsetMax = new Vector2(-18, -18);
            reelSymbolImages[i] = symImg;
        }

        // Result text (above reels)
        resultText = MakeText(canvasObj.transform, "Result", "Press SPACE to spin!", 42,
            new Vector2(0.5f, 0.7f), new Color(1f, 1f, 0.4f), FontStyles.Bold);
        resultText.rectTransform.sizeDelta = new Vector2(1200, 80);

        // Bet controls
        float ctlY = 0.28f;
        betDownButton = MakeButton(canvasObj.transform, "BetDown", "◀",
            new Vector2(0.38f, ctlY), new Vector2(80, 80), new Color(0.3f, 0.15f, 0.4f));
        betDownButton.onClick.AddListener(() => ChangeBet(-1));

        var betPanel = MakePanel(canvasObj.transform, "BetPanel", new Color(0.15f, 0.05f, 0.2f, 1f));
        var bpRt = betPanel.GetComponent<RectTransform>();
        bpRt.anchorMin = new Vector2(0.5f, ctlY);
        bpRt.anchorMax = new Vector2(0.5f, ctlY);
        bpRt.pivot = new Vector2(0.5f, 0.5f);
        bpRt.sizeDelta = new Vector2(280, 80);
        bpRt.anchoredPosition = Vector2.zero;

        var betLabel = new GameObject("Label");
        betLabel.transform.SetParent(betPanel.transform);
        var blt = betLabel.AddComponent<TextMeshProUGUI>();
        blt.text = "BET";
        blt.fontSize = 18;
        blt.color = new Color(0.7f, 0.7f, 0.8f);
        blt.alignment = TextAlignmentOptions.Top;
        if (font != null) blt.font = font;
        var blRt = betLabel.GetComponent<RectTransform>();
        blRt.anchorMin = Vector2.zero; blRt.anchorMax = Vector2.one;
        blRt.offsetMin = new Vector2(0, 0); blRt.offsetMax = new Vector2(0, -5);

        var bv = new GameObject("Value");
        bv.transform.SetParent(betPanel.transform);
        betText = bv.AddComponent<TextMeshProUGUI>();
        betText.text = "10 DC";
        betText.fontSize = 40;
        betText.color = new Color(1f, 0.85f, 0.2f);
        betText.fontStyle = FontStyles.Bold;
        betText.alignment = TextAlignmentOptions.Center;
        if (font != null) betText.font = font;
        var bvRt = bv.GetComponent<RectTransform>();
        bvRt.anchorMin = Vector2.zero; bvRt.anchorMax = Vector2.one;
        bvRt.offsetMin = new Vector2(0, 0); bvRt.offsetMax = new Vector2(0, 0);

        betUpButton = MakeButton(canvasObj.transform, "BetUp", "▶",
            new Vector2(0.62f, ctlY), new Vector2(80, 80), new Color(0.3f, 0.15f, 0.4f));
        betUpButton.onClick.AddListener(() => ChangeBet(1));

        // Spin button
        spinButton = MakeButton(canvasObj.transform, "Spin", "SPIN  🎲",
            new Vector2(0.5f, 0.14f), new Vector2(320, 90), new Color(0.9f, 0.2f, 0.5f));
        spinButton.onClick.AddListener(OnSpin);

        // Balance
        balanceText = MakeText(canvasObj.transform, "Balance", "Balance: 0 DC", 36,
            new Vector2(0.5f, 0.06f), new Color(1f, 0.85f, 0.2f), FontStyles.Bold);

        // Info
        infoText = MakeText(canvasObj.transform, "Info",
            "SPACE — spin  |  ←/→ — change bet  |  E/ESC — exit  |  3 match = 10x  |  2 match = 2x",
            22, new Vector2(0.5f, 0.015f), new Color(1, 1, 1, 0.5f), FontStyles.Normal);
        infoText.rectTransform.sizeDelta = new Vector2(1600, 40);
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (isSpinning) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame) OnSpin();
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) ChangeBet(-1);
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) ChangeBet(1);
    }

    void ChangeBet(int delta)
    {
        currentBetIndex = Mathf.Clamp(currentBetIndex + delta, 0, betOptions.Length - 1);
        UpdateBet();
    }

    void UpdateBet()
    {
        if (betText != null) betText.text = currentBet + " DC";
    }

    void UpdateBalance()
    {
        if (balanceText != null && hud != null)
            balanceText.text = "Balance: " + hud.GetCoins() + " DC";
    }

    void OnSpin()
    {
        if (isSpinning) return;

        var sm = SoundManager.Instance;
        if (sm != null) sm.PlayMenuClick();

        if (hud == null) { StartCoroutine(DoSpin(false, false)); return; }

        if (!hud.SpendCoins(currentBet))
        {
            ShowResult("Not enough DC!", new Color(1f, 0.3f, 0.3f));
            UpdateBalance();
            return;
        }
        UpdateBalance();

        // Decide outcome upfront:
        // 5%  -> all 3 match  (10x)
        // 10% -> 2 match      (2x)
        // 85% -> nothing
        float r = Random.value;
        bool three = r < 0.05f;
        bool two = !three && r < 0.15f;

        StartCoroutine(DoSpin(three, two));
    }

    IEnumerator DoSpin(bool matchThree, bool matchTwo)
    {
        isSpinning = true;
        spinButton.interactable = false;
        ShowResult("Spinning...", new Color(0.9f, 0.9f, 1f));

        // Pick final symbols
        int N = symbolSprites.Length;
        int[] finalSymbols = new int[3];
        if (matchThree)
        {
            int s = Random.Range(0, N);
            finalSymbols[0] = finalSymbols[1] = finalSymbols[2] = s;
        }
        else if (matchTwo)
        {
            int s = Random.Range(0, N);
            int other;
            do { other = Random.Range(0, N); } while (other == s);
            int matchIdx = Random.Range(0, 3);
            int oddIdx;
            do { oddIdx = Random.Range(0, 3); } while (oddIdx == matchIdx);
            for (int i = 0; i < 3; i++) finalSymbols[i] = s;
            finalSymbols[oddIdx] = other;
        }
        else
        {
            // No match — ensure all different
            finalSymbols[0] = Random.Range(0, N);
            do { finalSymbols[1] = Random.Range(0, N); } while (finalSymbols[1] == finalSymbols[0]);
            do { finalSymbols[2] = Random.Range(0, N); }
            while (finalSymbols[2] == finalSymbols[0] || finalSymbols[2] == finalSymbols[1]);
        }

        // Each reel spins with different duration
        float[] durations = { 1.2f, 1.7f, 2.2f };
        float[] timers = { 0f, 0f, 0f };
        bool[] stopped = { false, false, false };
        float symbolSwapInterval = 0.08f;
        float[] nextSwap = { 0f, 0f, 0f };

        while (!stopped[0] || !stopped[1] || !stopped[2])
        {
            for (int i = 0; i < 3; i++)
            {
                if (stopped[i]) continue;
                timers[i] += Time.deltaTime;

                if (timers[i] >= durations[i])
                {
                    stopped[i] = true;
                    SetReelSymbol(i, finalSymbols[i]);
                    reelBGs[i].color = reelColors[finalSymbols[i]];
                    // Flash
                    yield return StartCoroutine(FlashReel(i));
                    continue;
                }

                if (timers[i] >= nextSwap[i])
                {
                    int s = Random.Range(0, symbolSprites.Length);
                    SetReelSymbol(i, s);
                    reelBGs[i].color = Color.Lerp(reelColors[s], new Color(0.3f, 0.2f, 0.4f), 0.4f);
                    nextSwap[i] = timers[i] + symbolSwapInterval;
                }
            }
            yield return null;
        }

        // Evaluate
        int winAmount = 0;
        if (matchThree) winAmount = currentBet * 10;
        else if (matchTwo) winAmount = currentBet * 2;

        if (winAmount > 0)
        {
            if (hud != null) hud.AddCoins(winAmount);
            ShowResult($"🎉 WIN! +{winAmount} DC 🎉", new Color(0.3f, 1f, 0.4f));
            yield return StartCoroutine(CelebrateReels(matchThree));
        }
        else
        {
            ShowResult("No match. Try again!", new Color(1f, 0.5f, 0.5f));
        }

        UpdateBalance();
        isSpinning = false;
        spinButton.interactable = true;
    }

    IEnumerator FlashReel(int idx)
    {
        Color orig = reelBGs[idx].color;
        Color flash = Color.white;
        float t = 0f, dur = 0.2f;
        while (t < dur)
        {
            t += Time.deltaTime;
            reelBGs[idx].color = Color.Lerp(flash, orig, t / dur);
            yield return null;
        }
        reelBGs[idx].color = orig;
    }

    IEnumerator CelebrateReels(bool big)
    {
        int cycles = big ? 6 : 3;
        for (int c = 0; c < cycles; c++)
        {
            for (int i = 0; i < 3; i++)
            {
                Color orig = reelBGs[i].color;
                reelBGs[i].color = Color.Lerp(orig, new Color(1f, 0.9f, 0.3f), 0.7f);
                if (reelSymbolImages[i] != null)
                    reelSymbolImages[i].transform.localScale = Vector3.one * 1.15f;
                yield return new WaitForSeconds(0.08f);
                reelBGs[i].color = orig;
                if (reelSymbolImages[i] != null)
                    reelSymbolImages[i].transform.localScale = Vector3.one;
            }
        }
    }

    void SetReelSymbol(int reel, int symbolIdx)
    {
        if (reelSymbolImages[reel] == null) return;
        if (symbolSprites != null && symbolIdx >= 0 && symbolIdx < symbolSprites.Length)
            reelSymbolImages[reel].sprite = symbolSprites[symbolIdx];
    }

    void ShowResult(string text, Color color)
    {
        if (resultText == null) return;
        resultText.text = text;
        resultText.color = color;
    }

    public void Cleanup()
    {
        // UI is children of root — destroyed with root
    }

    // ---- UI Helpers ----

    Image MakePanel(Transform parent, string name, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var img = obj.AddComponent<Image>();
        img.color = color;
        return img;
    }

    void SetFullStretch(Image img)
    {
        var rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    TMP_Text MakeText(Transform parent, string name, string text, float size,
        Vector2 anchor, Color color, FontStyles style)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = style;
        if (font != null) tmp.font = font;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(1800, 120);
        return tmp;
    }

    Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size, Color bg)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var img = obj.AddComponent<Image>();
        img.color = bg;
        var btn = obj.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = bg;
        cb.highlightedColor = new Color(bg.r * 1.3f, bg.g * 1.3f, bg.b * 1.3f, 1f);
        cb.pressedColor = new Color(bg.r * 0.7f, bg.g * 0.7f, bg.b * 0.7f, 1f);
        btn.colors = cb;

        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = Vector2.zero;

        var txt = new GameObject("Text");
        txt.transform.SetParent(obj.transform);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 40;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        return btn;
    }
}
