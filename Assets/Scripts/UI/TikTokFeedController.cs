using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the TikTok-style feed with scrollable posts.
/// Each post fills the screen, swipe/scroll to see next.
/// </summary>
public class TikTokFeedController : MonoBehaviour
{
    [Header("Feed Container")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private GameObject postPrefabTemplate;

    [System.Serializable]
    public class TikTokPost
    {
        public string username = "@user";
        public string description = "Post text here...";
        public int likes = 0;
        public int comments = 0;
        public Color bgColor = new Color(0.08f, 0.08f, 0.12f);
    }

    [SerializeField] private List<TikTokPost> posts = new List<TikTokPost>();

    void Start()
    {
        if (posts.Count == 0)
            GenerateDefaultPosts();

        BuildFeed();
    }

    private void GenerateDefaultPosts()
    {
        posts.Add(new TikTokPost
        {
            username = "@doomscroller42",
            description = "POV: ты говоришь 'ещё один видос и спать' уже 3 часа подряд...",
            likes = 12300,
            comments = 567,
            bgColor = new Color(0.06f, 0.04f, 0.12f)
        });
        posts.Add(new TikTokPost
        {
            username = "@nightowl",
            description = "Когда будильник через 2 часа, но лента слишком интересная",
            likes = 8900,
            comments = 234,
            bgColor = new Color(0.12f, 0.04f, 0.06f)
        });
        posts.Add(new TikTokPost
        {
            username = "@insomnia_king",
            description = "Мой мозг в 3 часа ночи: 'А давай посмотрим как делают карандаши'",
            likes = 45600,
            comments = 1200,
            bgColor = new Color(0.04f, 0.08f, 0.12f)
        });
        posts.Add(new TikTokPost
        {
            username = "@scroll_addict",
            description = "Допамин закончился, но палец всё ещё листает...",
            likes = 23400,
            comments = 890,
            bgColor = new Color(0.1f, 0.06f, 0.1f)
        });
        posts.Add(new TikTokPost
        {
            username = "@3am_thoughts",
            description = "Сон -- это DLC которое я не могу себе позволить",
            likes = 67800,
            comments = 2100,
            bgColor = new Color(0.05f, 0.05f, 0.1f)
        });
    }

    private void BuildFeed()
    {
        if (contentContainer == null || postPrefabTemplate == null) return;

        // Clear existing
        foreach (Transform child in contentContainer)
        {
            if (child.gameObject != postPrefabTemplate)
                Destroy(child.gameObject);
        }

        postPrefabTemplate.SetActive(false);

        float postHeight = scrollRect != null ? scrollRect.GetComponent<RectTransform>().rect.height : 500f;
        if (postHeight <= 0) postHeight = 500f;

        for (int i = 0; i < posts.Count; i++)
        {
            GameObject postObj = Instantiate(postPrefabTemplate, contentContainer);
            postObj.name = $"Post_{i}";
            postObj.SetActive(true);

            // Set post size
            RectTransform postRT = postObj.GetComponent<RectTransform>();
            LayoutElement le = postObj.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredHeight = postHeight;
                le.minHeight = postHeight;
            }

            // Background color
            Image bg = postObj.GetComponent<Image>();
            if (bg != null) bg.color = posts[i].bgColor;

            // Find and set text elements
            TMP_Text[] texts = postObj.GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in texts)
            {
                if (txt.gameObject.name == "Username")
                    txt.text = posts[i].username;
                else if (txt.gameObject.name == "Description")
                    txt.text = posts[i].description;
                else if (txt.gameObject.name == "LikeCount")
                    txt.text = FormatNumber(posts[i].likes);
                else if (txt.gameObject.name == "CommentCount")
                    txt.text = FormatNumber(posts[i].comments);
            }
        }
    }

    private string FormatNumber(int num)
    {
        if (num >= 1000000) return (num / 1000000f).ToString("0.#") + "M";
        if (num >= 1000) return (num / 1000f).ToString("0.#") + "K";
        return num.ToString();
    }
}
