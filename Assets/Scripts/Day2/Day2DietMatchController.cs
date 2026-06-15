using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class Day2DietMatchController : MonoBehaviour
{
    [System.Serializable]
    public sealed class DietCard
    {
        public string displayName = "食物";
        public float sugarPerPortion = 10f;
        public float basePreference = 1f;
        public Sprite sprite;
        [HideInInspector] public int portions;
    }

    [Header("Flow")]
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private bool allowAutoLoadOnWin = true;
    [SerializeField] private float winAutoLoadDelaySeconds = 1.8f;

    [Header("Scoring")]
    [Tooltip("满意度(喜好和) 乘以该系数后计入全局得分(Score)。")]
    [SerializeField] private int satisfactionScoreMultiplier = 10;
    [Tooltip("达标(糖在区间内)时是否额外给全局健康值加奖励。")]
    [SerializeField] private bool grantHealthBonusOnValidMeal = true;
    [SerializeField] private int healthBonus = 5;

    [Header("Sugar Range (fixed)")]
    [SerializeField] private float sugarMin = 30f;
    [SerializeField] private float sugarMax = 50f;

    [Header("Anti-spam")]
    [Tooltip("同一张卡每多吃一份，喜好乘以该系数（递减）。")]
    [Range(0.1f, 1f)]
    [SerializeField] private float repeatPreferenceFactor = 0.5f;

    [Header("Cards")]
    [SerializeField] private List<DietCard> cards = new List<DietCard>();

    [SerializeField] private GameManager gameManager;

    private bool completed;
    private bool rewardGranted;
    private bool loadQueued;
    private float loadTimer;
    private int awardedScore;

    private Font uiFont;
    private Text sugarText;
    private Text satisfactionText;
    private Text messageText;
    private readonly List<Text> countTexts = new List<Text>();
    private readonly List<Button> stepButtons = new List<Button>();
    private Button confirmButton;
    private Text confirmLabel;

    private static readonly Color ColTable = new Color(0.42f, 0.28f, 0.19f, 1f);
    private static readonly Color ColPanel = new Color(0f, 0f, 0f, 0.32f);
    private static readonly Color ColCard = new Color(0.98f, 0.96f, 0.90f, 1f);
    private static readonly Color ColCardText = new Color(0.20f, 0.17f, 0.14f, 1f);
    private static readonly Color ColMuted = new Color(0.45f, 0.40f, 0.35f, 1f);
    private static readonly Color ColGood = new Color(0.36f, 0.70f, 0.40f, 1f);
    private static readonly Color ColBad = new Color(0.86f, 0.45f, 0.36f, 1f);
    private static readonly Color ColBtn = new Color(0.93f, 0.90f, 0.83f, 1f);
    private static readonly Color ColBtnText = new Color(0.20f, 0.17f, 0.14f, 1f);

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.EnsureInstanceForDemo();
        }

        gameManager.MarkCurrentDay(2);

        if (cards == null || cards.Count == 0)
        {
            cards = BuildDefaultCards();
        }

        if (sugarMax < sugarMin)
        {
            float tmp = sugarMin;
            sugarMin = sugarMax;
            sugarMax = tmp;
        }

        uiFont = Font.CreateDynamicFontFromOSFont(
            new string[] { "Microsoft YaHei", "SimHei", "PingFang SC", "Heiti SC", "Arial" }, 22);
    }

    private void Start()
    {
        EnsureEventSystem();
        BuildUI();
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameManager.ReturnToMainMenu();
            return;
        }

        if (completed)
        {
            HandleAutoLoad();
        }
    }

    private static List<DietCard> BuildDefaultCards()
    {
        return new List<DietCard>
        {
            new DietCard { displayName = "田园沙拉", sugarPerPortion = 9f, basePreference = 2f },
            new DietCard { displayName = "杂粮饭", sugarPerPortion = 32f, basePreference = 3f },
            new DietCard { displayName = "香煎鸡胸", sugarPerPortion = 6f, basePreference = 3f },
            new DietCard { displayName = "鲜榨果汁", sugarPerPortion = 26f, basePreference = 4f },
            new DietCard { displayName = "草莓奶油蛋糕", sugarPerPortion = 64f, basePreference = 6f }
        };
    }

    // ---------- gameplay calc ----------

    private float GetTotalSugar()
    {
        float total = 0f;
        for (int i = 0; i < cards.Count; i++)
        {
            total += cards[i].sugarPerPortion * cards[i].portions;
        }
        return total;
    }

    private float GetCardSatisfaction(DietCard card)
    {
        float sum = 0f;
        float weight = 1f;
        for (int n = 0; n < card.portions; n++)
        {
            sum += card.basePreference * weight;
            weight *= repeatPreferenceFactor;
        }
        return sum;
    }

    private float GetTotalSatisfaction()
    {
        float total = 0f;
        for (int i = 0; i < cards.Count; i++)
        {
            total += GetCardSatisfaction(cards[i]);
        }
        return total;
    }

    private bool IsInRange(float sugar)
    {
        return sugar >= sugarMin && sugar <= sugarMax;
    }

    private int GetSelectedPortionCount()
    {
        int count = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            count += cards[i].portions;
        }
        return count;
    }

    private void ChangePortion(int index, int delta)
    {
        if (completed)
        {
            return;
        }

        DietCard card = cards[index];
        card.portions = Mathf.Max(0, card.portions + delta);
        RefreshUI();
    }

    private void TrySubmitMeal()
    {
        if (completed)
        {
            return;
        }

        float sugar = GetTotalSugar();
        if (!IsInRange(sugar))
        {
            if (messageText != null)
            {
                messageText.text = sugar < sugarMin ? "吃得太少，含糖量未达下限。" : "含糖量超标了，减少高糖食物。";
                messageText.color = ColBad;
            }
            return;
        }

        completed = true;
        awardedScore = Mathf.RoundToInt(GetTotalSatisfaction() * satisfactionScoreMultiplier);

        if (!rewardGranted)
        {
            rewardGranted = true;
            gameManager.AddScore(awardedScore);
            if (grantHealthBonusOnValidMeal)
            {
                gameManager.AddHealth(healthBonus);
            }
        }

        RefreshUI();
    }

    private void HandleAutoLoad()
    {
        if (!allowAutoLoadOnWin || string.IsNullOrWhiteSpace(nextSceneName))
        {
            return;
        }

        if (!loadQueued)
        {
            loadQueued = true;
            loadTimer = Mathf.Max(0f, winAutoLoadDelaySeconds);
            return;
        }

        loadTimer -= Time.deltaTime;
        if (loadTimer > 0f)
        {
            return;
        }

        loadQueued = false;
        gameManager.LoadScene(nextSceneName);
    }

    // ---------- UI ----------

    private void RefreshUI()
    {
        float sugar = GetTotalSugar();
        bool inRange = IsInRange(sugar);

        if (sugarText != null)
        {
            sugarText.text = "含糖量  " + sugar.ToString("F0") + " / " + sugarMin.ToString("F0") + "~" + sugarMax.ToString("F0");
            sugarText.color = inRange ? ColGood : new Color(1f, 0.85f, 0.5f, 1f);
        }

        if (satisfactionText != null)
        {
            satisfactionText.text = "满意度  " + GetTotalSatisfaction().ToString("F1");
        }

        for (int i = 0; i < countTexts.Count && i < cards.Count; i++)
        {
            countTexts[i].text = "x" + cards[i].portions;
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = !completed && inRange && GetSelectedPortionCount() > 0;
        }

        if (confirmLabel != null)
        {
            confirmLabel.text = completed ? "已完成" : "确定这一餐";
        }

        for (int i = 0; i < stepButtons.Count; i++)
        {
            stepButtons[i].interactable = !completed;
        }

        if (messageText != null)
        {
            if (completed)
            {
                messageText.text = "完成！得分 +" + awardedScore + (grantHealthBonusOnValidMeal ? ("   健康 +" + healthBonus) : string.Empty);
                messageText.color = ColGood;
            }
            else if (!inRange)
            {
                messageText.text = string.Empty;
            }
        }
    }

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("DietMatchCanvas", typeof(RectTransform));
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject bg = NewUI("Background", canvasGo.transform);
        Stretch(bg.GetComponent<RectTransform>());
        AddImage(bg, ColTable);

        BuildHeader(canvasGo.transform);
        BuildCards(canvasGo.transform);
        BuildFooter(canvasGo.transform);
    }

    private void BuildHeader(Transform parent)
    {
        GameObject header = NewUI("Header", parent);
        RectTransform rt = header.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -28f);
        rt.sizeDelta = new Vector2(1300f, 150f);
        AddImage(header, ColPanel);

        GameObject title = NewUI("Title", header.transform);
        PlaceTop(title.GetComponent<RectTransform>(), -14f, 40f, 1240f);
        AddText(title, "第二天 · 午餐搭配", 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

        GameObject rule = NewUI("Rule", header.transform);
        PlaceTop(rule.GetComponent<RectTransform>(), -58f, 30f, 1240f);
        AddText(rule, "搭配午餐，让总含糖量落在 " + sugarMin.ToString("F0") + " ~ " + sugarMax.ToString("F0") + " 克之间；同种吃多了喜好会递减。", 19, new Color(1f, 1f, 1f, 0.82f), TextAnchor.MiddleCenter);

        GameObject sugarGo = NewUI("Sugar", header.transform);
        RectTransform sgr = sugarGo.GetComponent<RectTransform>();
        sgr.anchorMin = new Vector2(0.5f, 1f);
        sgr.anchorMax = new Vector2(0.5f, 1f);
        sgr.pivot = new Vector2(1f, 1f);
        sgr.anchoredPosition = new Vector2(-30f, -96f);
        sgr.sizeDelta = new Vector2(500f, 36f);
        sugarText = AddText(sugarGo, string.Empty, 24, ColGood, TextAnchor.MiddleRight, FontStyle.Bold);

        GameObject satGo = NewUI("Satisfaction", header.transform);
        RectTransform satr = satGo.GetComponent<RectTransform>();
        satr.anchorMin = new Vector2(0.5f, 1f);
        satr.anchorMax = new Vector2(0.5f, 1f);
        satr.pivot = new Vector2(0f, 1f);
        satr.anchoredPosition = new Vector2(30f, -96f);
        satr.sizeDelta = new Vector2(500f, 36f);
        satisfactionText = AddText(satGo, string.Empty, 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
    }

    private void BuildCards(Transform parent)
    {
        float cardW = 240f;
        float gap = 26f;
        int n = cards.Count;
        float totalW = (n * cardW) + ((n - 1) * gap);
        float startX = -(totalW * 0.5f) + (cardW * 0.5f);

        for (int i = 0; i < n; i++)
        {
            float centerX = startX + i * (cardW + gap);
            BuildCard(parent, i, centerX, cardW);
        }
    }

    private void BuildCard(Transform parent, int index, float centerX, float cardW)
    {
        DietCard card = cards[index];

        GameObject cardGo = NewUI("Card_" + index, parent);
        RectTransform rt = cardGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(centerX, 36f);
        rt.sizeDelta = new Vector2(cardW, 340f);
        AddImage(cardGo, ColCard);

        GameObject imgGo = NewUI("Food", cardGo.transform);
        RectTransform imr = imgGo.GetComponent<RectTransform>();
        imr.anchorMin = new Vector2(0.5f, 0.5f);
        imr.anchorMax = new Vector2(0.5f, 0.5f);
        imr.pivot = new Vector2(0.5f, 0.5f);
        imr.anchoredPosition = new Vector2(0f, 78f);
        imr.sizeDelta = new Vector2(cardW - 28f, 170f);
        Image foodImg = AddImage(imgGo, Color.white);
        foodImg.sprite = card.sprite;
        foodImg.preserveAspect = true;
        if (card.sprite == null)
        {
            foodImg.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        }

        GameObject nameGo = NewUI("Name", cardGo.transform);
        PlaceCenter(nameGo.GetComponent<RectTransform>(), -38f, cardW - 20f, 32f);
        AddText(nameGo, card.displayName, 23, ColCardText, TextAnchor.MiddleCenter, FontStyle.Bold);

        GameObject statGo = NewUI("Stat", cardGo.transform);
        PlaceCenter(statGo.GetComponent<RectTransform>(), -70f, cardW - 20f, 26f);
        AddText(statGo, "糖 " + card.sugarPerPortion.ToString("F0") + "   喜好 " + card.basePreference.ToString("F0"), 18, ColMuted, TextAnchor.MiddleCenter);

        int captured = index;

        Button minus = BuildStepButton(cardGo.transform, "−", new Vector2(-72f, -118f));
        minus.onClick.AddListener(() => ChangePortion(captured, -1));
        stepButtons.Add(minus);

        GameObject countGo = NewUI("Count", cardGo.transform);
        PlaceCenter(countGo.GetComponent<RectTransform>(), -118f, 90f, 44f);
        Text countText = AddText(countGo, "x0", 26, ColCardText, TextAnchor.MiddleCenter, FontStyle.Bold);
        countTexts.Add(countText);

        Button plus = BuildStepButton(cardGo.transform, "+", new Vector2(72f, -118f));
        plus.onClick.AddListener(() => ChangePortion(captured, 1));
        stepButtons.Add(plus);
    }

    private Button BuildStepButton(Transform parent, string label, Vector2 anchoredPos)
    {
        GameObject go = NewUI("Step", parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(52f, 52f);
        Image img = AddImage(go, ColBtn);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject txt = NewUI("Label", go.transform);
        Stretch(txt.GetComponent<RectTransform>());
        AddText(txt, label, 30, ColBtnText, TextAnchor.MiddleCenter, FontStyle.Bold);
        return btn;
    }

    private void BuildFooter(Transform parent)
    {
        GameObject msgGo = NewUI("Message", parent);
        RectTransform mr = msgGo.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(0.5f, 0.5f);
        mr.anchorMax = new Vector2(0.5f, 0.5f);
        mr.pivot = new Vector2(0.5f, 0.5f);
        mr.anchoredPosition = new Vector2(0f, -210f);
        mr.sizeDelta = new Vector2(900f, 34f);
        messageText = AddText(msgGo, "按 Esc 返回主菜单", 20, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter);

        GameObject btnGo = NewUI("Confirm", parent);
        RectTransform br = btnGo.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.5f, 0.5f);
        br.anchorMax = new Vector2(0.5f, 0.5f);
        br.pivot = new Vector2(0.5f, 0.5f);
        br.anchoredPosition = new Vector2(0f, -270f);
        br.sizeDelta = new Vector2(280f, 64f);
        Image img = AddImage(btnGo, ColGood);
        confirmButton = btnGo.AddComponent<Button>();
        confirmButton.targetGraphic = img;
        confirmButton.onClick.AddListener(TrySubmitMeal);

        GameObject txt = NewUI("Label", btnGo.transform);
        Stretch(txt.GetComponent<RectTransform>());
        confirmLabel = AddText(txt, "确定这一餐", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
    }

    // ---------- UI helpers ----------

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }
        return go;
    }

    private static Image AddImage(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private Text AddText(GameObject go, string content, int size, Color color, TextAnchor anchor, FontStyle style = FontStyle.Normal)
    {
        Text t = go.AddComponent<Text>();
        t.font = uiFont;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void PlaceTop(RectTransform rt, float y, float height, float width)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(width, height);
    }

    private static void PlaceCenter(RectTransform rt, float y, float width, float height)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(width, height);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        es.hideFlags = HideFlags.DontSave;
    }
}
