using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TikTokFeedController : MonoBehaviour
{
    [SerializeField] private RectTransform postParent;
    [SerializeField] private GameObject postPrefabTemplate;
    [SerializeField] private bool generateMorePostsWhenEnded = true;
    [SerializeField] private float imagePostChance = 0.6f;

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
    private Sprite[] loadedPostSprites;

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
        "POV:",
        "Me when",
        "Nobody talks about how",
        "Why does it feel like",
        "At this point",
        "It is crazy how",
        "Sometimes I think",
        "You ever notice how",
        "That moment when",
        "I swear"
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

        if (posts.Count == 0)
            GenerateInitialPosts(5);

        ShowCurrentPost();
    }

    public void NextPost()
    {
        if (currentPostIndex < posts.Count - 1)
        {
            currentPostIndex++;
            ShowCurrentPost();
            return;
        }

        if (generateMorePostsWhenEnded)
        {
            AddGeneratedPost();
            currentPostIndex++;
            ShowCurrentPost();
        }
    }

    private void ShowCurrentPost()
    {
        if (postParent == null || postPrefabTemplate == null || posts.Count == 0)
            return;

        if (currentPostObject != null)
            Destroy(currentPostObject);

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
            bg.color = post.bgColor;

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
                }
                else
                {
                    postImage.gameObject.SetActive(false);
                }
            }
        }
    }

    private void GenerateInitialPosts(int count)
    {
        posts.Clear();

        for (int i = 0; i < count; i++)
            posts.Add(GeneratePost(i));
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

        if (roll < 0.35f)
            return "@" + left + "_" + right;

        if (roll < 0.6f)
            return "@" + left + right + Random.Range(7, 9999).ToString();

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
        if (num >= 1000000)
            return (num / 1000000f).ToString("0.#") + "M";

        if (num >= 1000)
            return (num / 1000f).ToString("0.#") + "K";

        return num.ToString();
    }
}