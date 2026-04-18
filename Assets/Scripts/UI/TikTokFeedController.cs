using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// TikTok-подобная лента. Пост на весь экран. Свайп вверх/вниз пальцем (drag-ом мыши)
/// переключает на следующий/предыдущий пост. Картинки берутся случайно из Resources/Posts.
/// </summary>
public class TikTokFeedController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform postParent;
    [SerializeField] private GameObject postPrefabTemplate;
    [SerializeField] private bool generateMorePostsWhenEnded = true;
    [SerializeField] private float imagePostChance = 0.6f;

    [Header("Swipe")]
    [Tooltip("Минимальное расстояние свайпа (в пикселях канваса) для перехода")]
    [SerializeField] private float swipeThreshold = 120f;
    [SerializeField] private float swipeAnimDuration = 0.28f;

    [System.Serializable]
    public class TikTokPost
    {
        public string username;
        public string description;
        public int likes;
        public int comments;
        public int shares;
        public Color bgColor;
        public bool hasImage;
        public Sprite postSprite;
    }

    [SerializeField] private List<TikTokPost> posts = new List<TikTokPost>();

    private int currentPostIndex = 0;
    private GameObject currentPostObject;
    private GameObject nextPostObject;      // используется во время анимации свайпа
    private Sprite[] loadedPostSprites;

    // Swipe state
    private bool isDragging;
    private bool isAnimating;
    private float dragStartY;
    private float currentDragDelta;
    private float parentHeight;

    private readonly string[] usernamesPart1 =
    {
        "doom", "night", "sleep", "brain", "lost", "void", "late", "digital", "sad", "glitch",
        "scroll", "loop", "feed", "core", "dark", "hyper", "cold", "tired", "broken", "ghost"
    };

    private readonly string[] usernamesPart2 =
    {
        "scroll", "feed", "mind", "loop", "core", "wave", "zone", "ghost", "vision", "drift",
        "soul", "thoughts", "static", "dream", "void", "hours", "glow", "signal", "vibe", "fall"
    };

    private readonly string[] postStarts =
    {
        "POV:", "Me when", "Nobody talks about how", "Why does it feel like", "At this point",
        "It is crazy how", "Sometimes I think", "You ever notice how", "That moment when", "I swear"
    };

    private readonly string[] postSubjects =
    {
        "you opened the app for 5 minutes",
        "your brain stopped working at 2AM",
        "you keep watching videos without blinking",
        "the algorithm reads you like a book",
        "your sleep schedule is completely destroyed",
        "you scroll instead of thinking",
        "one more video turns into an hour",
        "your thumb has more stamina than you do",
        "you are too tired to stop",
        "the feed gets more accurate every night",
        "you wanted to rest but got trapped",
        "everything online starts to feel the same",
        "you laugh once and then feel empty",
        "you do not even remember the last 10 posts",
        "you are still awake for no reason"
    };

    private readonly string[] postEndings =
    {
        "and somehow you still stay.",
        "but you keep going anyway.",
        "like that is normal now.",
        "and that is the scary part.",
        "but the next one might fix everything.",
        "and your brain just accepts it.",
        "like sleep is optional.",
        "and now it is basically a ritual.",
        "but stopping feels illegal.",
        "and the night disappears.",
        "but you pretend you are in control.",
        "and suddenly it is 4AM.",
        "but at least the content is relatable.",
        "and you do not even question it anymore.",
        "but leaving the app feels wrong."
    };

    private readonly string[] shortPosts =
    {
        "One more swipe and I am done",
        "The feed knows me too well",
        "This app is eating my life",
        "I should sleep but this is important somehow",
        "Every post feels made for my exact problem",
        "Dopamine is gone but we keep scrolling",
        "This is not entertainment anymore",
        "The algorithm is farming my soul",
        "I came here for one video",
        "Why is the worst content always the hardest to leave",
        "I do not control the scroll anymore",
        "This loop is actually insane",
        "I am too tired to enjoy this and too awake to stop",
        "The night belongs to the feed now",
        "My attention span is completely cooked"
    };

    private void Start()
    {
        loadedPostSprites = Resources.LoadAll<Sprite>("Posts");
        Debug.Log($"[TikTok] Loaded {loadedPostSprites?.Length ?? 0} post sprites from Resources/Posts");

        if (posts.Count == 0)
            GenerateInitialPosts();

        EnsureSwipeReceiver();
        ShowCurrentPost();
    }

    /// <summary>
    /// Гарантирует, что на postParent есть Image (raycastTarget=true) — иначе drag не ловится.
    /// </summary>
    private void EnsureSwipeReceiver()
    {
        if (postParent == null) return;
        var img = postParent.GetComponent<Image>();
        if (img == null)
        {
            img = postParent.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.001f); // почти невидим, но ловит raycast
        }
        img.raycastTarget = true;

        // Опционально маска, чтобы анимируемый соседний пост не вылазил за пределы экрана
        if (postParent.GetComponent<RectMask2D>() == null)
            postParent.gameObject.AddComponent<RectMask2D>();
    }

    public void NextPost()
    {
        if (isAnimating) return;
        if (currentPostIndex >= posts.Count - 1)
        {
            if (generateMorePostsWhenEnded) AddGeneratedPost();
            else return;
        }
        EnsurePreviewPost(+1);
        StartCoroutine(FinishSwipe(+1));
    }

    public void PrevPost()
    {
        if (isAnimating) return;
        if (currentPostIndex <= 0) return;
        EnsurePreviewPost(-1);
        StartCoroutine(FinishSwipe(-1));
    }

    // ---------------- Drag handlers ----------------

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isAnimating) return;
        isDragging = true;
        dragStartY = eventData.position.y;
        currentDragDelta = 0f;
        parentHeight = postParent != null ? postParent.rect.height : 800f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || currentPostObject == null) return;
        currentDragDelta = eventData.position.y - dragStartY;

        // Не даём тянуть назад (вверх), если мы на первом посте
        // Swipe down (delta > 0) = next, swipe up (delta < 0) = prev
        if (currentDragDelta < 0f && currentPostIndex <= 0)
            currentDragDelta = Mathf.Max(currentDragDelta * 0.25f, -60f); // резиновый эффект

        var rt = currentPostObject.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = new Vector2(0f, currentDragDelta);

        // Превью следующего/предыдущего "подкрадывается" снизу/сверху
        EnsurePreviewPost();
        if (nextPostObject != null)
        {
            var nrt = nextPostObject.GetComponent<RectTransform>();
            if (nrt != null)
            {
                float sign = currentDragDelta < 0f ? -1f : 1f;
                float offset = (currentDragDelta < 0f ? parentHeight : -parentHeight) + currentDragDelta;
                nrt.anchoredPosition = new Vector2(0f, offset);
                _ = sign;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        float delta = currentDragDelta;
        currentDragDelta = 0f;

        // Cleanup превью, восстановим если ниже порога
        if (Mathf.Abs(delta) < swipeThreshold)
        {
            StartCoroutine(SnapBack());
            return;
        }

        // Swipe down (delta > 0) = next post; swipe up (delta < 0) = prev post
        int dir = delta > 0f ? +1 : -1;
        if (dir < 0 && currentPostIndex <= 0)
        {
            StartCoroutine(SnapBack());
            return;
        }

        // Завершаем анимацию в ту же сторону
        StartCoroutine(FinishSwipe(dir));
    }

    private IEnumerator SnapBack()
    {
        isAnimating = true;
        float dur = 0.18f;
        float t = 0f;
        Vector2 curStart = currentPostObject != null
            ? currentPostObject.GetComponent<RectTransform>().anchoredPosition
            : Vector2.zero;
        Vector2 nextStart = nextPostObject != null
            ? nextPostObject.GetComponent<RectTransform>().anchoredPosition
            : Vector2.zero;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float s = 1f - Mathf.Pow(1f - k, 3f);
            if (currentPostObject != null)
                currentPostObject.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(curStart, Vector2.zero, s);
            if (nextPostObject != null)
            {
                Vector2 target = new Vector2(0f, nextStart.y > 0f ? parentHeight : -parentHeight);
                nextPostObject.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(nextStart, target, s);
            }
            yield return null;
        }

        if (nextPostObject != null)
        {
            Destroy(nextPostObject);
            nextPostObject = null;
        }
        isAnimating = false;
    }

    private IEnumerator FinishSwipe(int dir)
    {
        isAnimating = true;

        // Убедимся что есть следующий/предыдущий
        if (dir > 0)
        {
            if (currentPostIndex >= posts.Count - 1)
            {
                if (generateMorePostsWhenEnded) AddGeneratedPost();
                else { isAnimating = false; yield break; }
            }
        }

        EnsurePreviewPost(dir); // гарантия что nextPostObject есть

        var curRT = currentPostObject != null ? currentPostObject.GetComponent<RectTransform>() : null;
        var nxtRT = nextPostObject != null ? nextPostObject.GetComponent<RectTransform>() : null;

        Vector2 curStart = curRT != null ? curRT.anchoredPosition : Vector2.zero;
        Vector2 nxtStart = nxtRT != null ? nxtRT.anchoredPosition : Vector2.zero;

        Vector2 curEnd = new Vector2(0f, dir > 0 ? parentHeight : -parentHeight);
        Vector2 nxtEnd = Vector2.zero;

        float t = 0f;
        while (t < swipeAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / swipeAnimDuration);
            float s = 1f - Mathf.Pow(1f - k, 3f);
            if (curRT != null) curRT.anchoredPosition = Vector2.Lerp(curStart, curEnd, s);
            if (nxtRT != null) nxtRT.anchoredPosition = Vector2.Lerp(nxtStart, nxtEnd, s);
            yield return null;
        }

        // Удаляем старый, новый становится текущим
        if (currentPostObject != null) Destroy(currentPostObject);
        currentPostObject = nextPostObject;
        nextPostObject = null;
        if (curRT != null) curRT.anchoredPosition = Vector2.zero;
        if (currentPostObject != null)
        {
            var rt = currentPostObject.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;
        }

        // dir > 0 → свайп вверх → следующий пост (+1); dir < 0 → предыдущий (-1)
        currentPostIndex = Mathf.Clamp(currentPostIndex + dir, 0, posts.Count - 1);

        // Дофамин за свайп
        if (GameEconomy.Instance != null)
            GameEconomy.Instance.AwardDopamine(GameEconomy.ActTikTok);

        isAnimating = false;
    }

    private void EnsurePreviewPost(int dir = 0)
    {
        if (nextPostObject != null) return;
        if (postParent == null || postPrefabTemplate == null) return;

        // Определяем направление — если передан dir берём его, иначе по знаку currentDragDelta
        // Swipe down (delta > 0) = next (+1), swipe up (delta < 0) = prev (-1)
        int d = dir != 0 ? dir : (currentDragDelta > 0f ? +1 : -1);

        int targetIdx = currentPostIndex + (d > 0 ? 1 : -1);
        if (targetIdx < 0) return;
        if (targetIdx >= posts.Count)
        {
            if (!generateMorePostsWhenEnded) return;
            AddGeneratedPost();
            targetIdx = posts.Count - 1;
        }

        nextPostObject = Instantiate(postPrefabTemplate, postParent);
        nextPostObject.name = $"PostPreview_{targetIdx}";
        nextPostObject.SetActive(true);

        RectTransform prt = nextPostObject.GetComponent<RectTransform>();
        if (prt != null)
        {
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            prt.localScale = Vector3.one;
            prt.anchoredPosition = new Vector2(0f, d > 0 ? -parentHeight : parentHeight);
        }

        ApplyPostData(nextPostObject, posts[targetIdx]);
    }

    // ---------------- Show / Apply ----------------

    private void ShowCurrentPost()
    {
        if (postParent == null || postPrefabTemplate == null || posts.Count == 0)
            return;

        if (currentPostObject != null)
            Destroy(currentPostObject);

        postPrefabTemplate.SetActive(false);

        currentPostObject = Instantiate(postPrefabTemplate, postParent);
        currentPostObject.name = $"Post_{currentPostIndex}";
        currentPostObject.SetActive(true);

        RectTransform postRT = currentPostObject.GetComponent<RectTransform>();
        if (postRT != null)
        {
            postRT.anchorMin = Vector2.zero;
            postRT.anchorMax = Vector2.one;
            postRT.offsetMin = Vector2.zero;
            postRT.offsetMax = Vector2.zero;
            postRT.localScale = Vector3.one;
            postRT.anchoredPosition = Vector2.zero;
        }

        ApplyPostData(currentPostObject, posts[currentPostIndex]);
    }

    private void ApplyPostData(GameObject postObj, TikTokPost post)
    {
        Image bg = postObj.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = post.bgColor;
            bg.raycastTarget = false; // пропускаем raycast через пост, ловим на parent
        }

        TMP_Text[] texts = postObj.GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts)
        {
            if (txt.gameObject.name == "Username")
                txt.text = post.username;
            else if (txt.gameObject.name == "Description")
                txt.text = post.description;
            else if (txt.gameObject.name == "LikeCount")
                txt.text = FormatNumber(post.likes);
            else if (txt.gameObject.name == "CommentCount")
                txt.text = FormatNumber(post.comments);
            else if (txt.gameObject.name == "ShareCount")
                txt.text = FormatNumber(post.shares);
            txt.raycastTarget = false;
        }

        // Отключаем raycast на всех image внутри поста (чтобы drag работал поверх)
        var allImages = postObj.GetComponentsInChildren<Image>(true);
        foreach (var im in allImages)
        {
            if (im != bg) im.raycastTarget = false;
        }

        Transform imageTransform = postObj.transform.Find("PostImage");
        if (imageTransform != null)
        {
            Image postImage = imageTransform.GetComponent<Image>();
            if (postImage != null)
            {
                if (post.hasImage && post.postSprite != null)
                {
                    postImage.gameObject.SetActive(true);
                    postImage.sprite = post.postSprite;
                    postImage.preserveAspect = true;
                    postImage.raycastTarget = false;
                }
                else
                {
                    postImage.gameObject.SetActive(false);
                }
            }
        }
    }

    // ---------------- Generation ----------------

    private void GenerateInitialPosts()
    {
        posts.Clear();

        // Сначала создаём по одному посту на КАЖДОЕ загруженное изображение
        // в случайном порядке — чтобы все 13 картинок появились в ленте.
        if (loadedPostSprites != null && loadedPostSprites.Length > 0)
        {
            var shuffled = new List<Sprite>(loadedPostSprites);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            for (int i = 0; i < shuffled.Count; i++)
                posts.Add(GeneratePostFromSprite(i, shuffled[i]));
        }
        else
        {
            // fallback: хоть сгенерируем текстовые
            for (int i = 0; i < 5; i++)
                posts.Add(GeneratePost(i));
        }
    }

    /// <summary>Пост с конкретным спрайтом (каждое фото из Resources/Posts = отдельный tiktok).</summary>
    private TikTokPost GeneratePostFromSprite(int index, Sprite sprite)
    {
        var post = new TikTokPost
        {
            username = GenerateUsername(),
            likes = GenerateLikes(index),
            hasImage = true,
            postSprite = sprite,
            bgColor = Color.black,
            description = GenerateShortCaption()
        };
        post.comments = GenerateComments(post.likes);
        post.shares = GenerateShares(post.likes);
        return post;
    }

    private void AddGeneratedPost()
    {
        posts.Add(GeneratePost(posts.Count));
    }

    private TikTokPost GeneratePost(int index)
    {
        TikTokPost post = new TikTokPost();

        post.username = GenerateUsername();
        post.likes = GenerateLikes(index);
        post.comments = GenerateComments(post.likes);
        post.shares = GenerateShares(post.likes);

        bool canUseImage = loadedPostSprites != null && loadedPostSprites.Length > 0;
        post.hasImage = canUseImage && Random.value < imagePostChance;

        if (post.hasImage)
        {
            post.postSprite = loadedPostSprites[Random.Range(0, loadedPostSprites.Length)];
            post.description = GenerateShortCaption();
            post.bgColor = Color.black;
        }
        else
        {
            post.postSprite = null;
            post.description = GenerateDescription();
            post.bgColor = GenerateColor();
        }

        return post;
    }

    private string GenerateUsername()
    {
        string left = usernamesPart1[Random.Range(0, usernamesPart1.Length)];
        string right = usernamesPart2[Random.Range(0, usernamesPart2.Length)];

        float roll = Random.value;
        if (roll < 0.35f) return "@" + left + "_" + right;
        if (roll < 0.6f)  return "@" + left + right + Random.Range(7, 9999).ToString();
        return "@" + left + right;
    }

    private string GenerateDescription()
    {
        if (Random.value < 0.45f)
            return shortPosts[Random.Range(0, shortPosts.Length)];

        string start = postStarts[Random.Range(0, postStarts.Length)];
        string subject = postSubjects[Random.Range(0, postSubjects.Length)];
        string ending = postEndings[Random.Range(0, postEndings.Length)];
        return start + " " + subject + " " + ending;
    }

    private string GenerateShortCaption()
    {
        string[] captions =
        {
            "this one is actually insane",
            "why does this look so familiar",
            "could stare at this for hours",
            "found this at the worst possible time",
            "the algorithm cooked again",
            "this hit harder than it should",
            "saving this for no reason",
            "this feels illegal to scroll past",
            "the feed is locked in tonight",
            "this image has too much aura"
        };
        return captions[Random.Range(0, captions.Length)];
    }

    private int GenerateLikes(int index)
    {
        int baseMin = 300 + index * 120;
        int baseMax = 12000 + index * 850;
        if (Random.value < 0.15f)
            return Random.Range(25000 + index * 1000, 180000 + index * 3000);
        return Random.Range(baseMin, baseMax);
    }

    private int GenerateComments(int likes)
    {
        int min = Mathf.Max(5, likes / 80);
        int max = Mathf.Max(min + 1, likes / 12);
        return Random.Range(min, max);
    }

    private int GenerateShares(int likes)
    {
        int min = Mathf.Max(2, likes / 150);
        int max = Mathf.Max(min + 1, likes / 20);
        return Random.Range(min, max);
    }

    private Color GenerateColor()
    {
        Color[] colors =
        {
            new Color(0.06f, 0.04f, 0.12f),
            new Color(0.12f, 0.04f, 0.06f),
            new Color(0.04f, 0.08f, 0.12f),
            new Color(0.10f, 0.06f, 0.10f),
            new Color(0.05f, 0.05f, 0.10f),
            new Color(0.08f, 0.03f, 0.08f),
            new Color(0.03f, 0.07f, 0.09f),
            new Color(0.09f, 0.03f, 0.05f)
        };
        return colors[Random.Range(0, colors.Length)];
    }

    private string FormatNumber(int num)
    {
        if (num >= 1000000) return (num / 1000000f).ToString("0.#") + "M";
        if (num >= 1000)    return (num / 1000f).ToString("0.#") + "K";
        return num.ToString();
    }
}
