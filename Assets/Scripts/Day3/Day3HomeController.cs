using System.Collections.Generic;
using UnityEngine;

public sealed class Day3HomeController : MonoBehaviour
{
    private enum Phase
    {
        MealCards,
        Runner,
        RunnerFailed,
        Completed
    }

    private sealed class FoodCard
    {
        public FoodCard(string name, string category, int healthDelta, int scoreDelta)
        {
            Name = name;
            Category = category;
            HealthDelta = healthDelta;
            ScoreDelta = scoreDelta;
        }

        public string Name;
        public string Category;
        public int HealthDelta;
        public int ScoreDelta;
    }

    private sealed class LaneEvent
    {
        public int Lane;
        public float TimeToImpact;
        public bool IsPickup;
    }

    private const float RunnerEventTravelSeconds = 2.2f;

    [Header("Flow")]
    [SerializeField] private string nextSceneName = "Day4_Home";
    [SerializeField] private float runnerDuration = 28f;

    private readonly List<FoodCard> mealCards = new List<FoodCard>
    {
        new FoodCard("糙米饭", "主食", 4, 8),
        new FoodCard("鸡胸肉", "蛋白", 6, 10),
        new FoodCard("西兰花", "蔬菜", 5, 9),
        new FoodCard("番茄蛋汤", "蔬菜", 4, 7),
        new FoodCard("三文鱼", "蛋白", 6, 11),
        new FoodCard("全麦面", "主食", 4, 8),
        new FoodCard("奶茶", "高糖", -8, -6),
        new FoodCard("炸鸡", "高油", -10, -8)
    };

    private readonly HashSet<int> selectedCards = new HashSet<int>();
    private readonly List<LaneEvent> laneEvents = new List<LaneEvent>();

    private GameManager gameManager;
    private Phase phase = Phase.MealCards;
    private string cardHint = "选择 4 张午/晚餐卡牌（数字键1~8，Enter确认）。";
    private string runnerHint = "跑酷：A/D 或 ←/→ 切换赛道，躲避红块，吃绿块。";
    private float runnerTimeLeft;
    private float spawnTimer;
    private int playerLane = 1;

    private void Awake()
    {
        gameManager = GameManager.EnsureInstanceForDemo();
        gameManager.MarkCurrentDay(3);
        ResetRunnerState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameManager.ReturnToMainMenu();
            return;
        }

        switch (phase)
        {
            case Phase.MealCards:
                UpdateMealCards();
                break;
            case Phase.Runner:
                UpdateRunner();
                break;
            case Phase.RunnerFailed:
                if (Input.GetKeyDown(KeyCode.R))
                {
                    gameManager.RegisterRetry();
                    gameManager.AddHealth(20);
                    ResetRunnerState();
                    phase = Phase.Runner;
                }
                break;
            case Phase.Completed:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    gameManager.LoadScene(nextSceneName);
                }
                break;
        }
    }

    private void UpdateMealCards()
    {
        for (int i = 0; i < mealCards.Count && i < 9; i++)
        {
            KeyCode alpha = (KeyCode)((int)KeyCode.Alpha1 + i);
            KeyCode keypad = (KeyCode)((int)KeyCode.Keypad1 + i);
            if (Input.GetKeyDown(alpha) || Input.GetKeyDown(keypad))
            {
                ToggleCard(i);
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            TryConfirmCards();
        }
    }

    private void ToggleCard(int index)
    {
        if (selectedCards.Contains(index))
        {
            selectedCards.Remove(index);
            return;
        }

        if (selectedCards.Count >= 4)
        {
            cardHint = "已选满 4 张，先取消一张。";
            return;
        }

        selectedCards.Add(index);
        cardHint = "已选择 " + selectedCards.Count + "/4。";
    }

    private void TryConfirmCards()
    {
        if (selectedCards.Count != 4)
        {
            cardHint = "请先选满 4 张卡牌。";
            return;
        }

        int main = 0;
        int protein = 0;
        int veggie = 0;
        int unhealthy = 0;
        int healthDelta = 0;
        int scoreDelta = 0;

        foreach (int cardIndex in selectedCards)
        {
            FoodCard card = mealCards[cardIndex];
            healthDelta += card.HealthDelta;
            scoreDelta += card.ScoreDelta;

            switch (card.Category)
            {
                case "主食":
                    main++;
                    break;
                case "蛋白":
                    protein++;
                    break;
                case "蔬菜":
                    veggie++;
                    break;
                default:
                    unhealthy++;
                    break;
            }
        }

        if (main == 0 || protein == 0 || veggie == 0)
        {
            cardHint = "类型不平衡：主食/蛋白/蔬菜至少各 1 张。";
            return;
        }

        if (unhealthy > 1)
        {
            cardHint = "高糖/高油卡牌最多 1 张。";
            return;
        }

        gameManager.AddHealth(healthDelta);
        gameManager.AddScore(scoreDelta);
        phase = Phase.Runner;
        cardHint = string.Format("饮食结算：健康{0:+#;-#;0}，积分{1:+#;-#;0}。进入跑酷！", healthDelta, scoreDelta);
    }

    private void UpdateRunner()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            playerLane = Mathf.Max(0, playerLane - 1);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            playerLane = Mathf.Min(2, playerLane + 1);
        }

        runnerTimeLeft -= Time.deltaTime;
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnLaneEvent();
            spawnTimer = Random.Range(0.55f, 1.0f);
        }

        for (int i = laneEvents.Count - 1; i >= 0; i--)
        {
            LaneEvent laneEvent = laneEvents[i];
            laneEvent.TimeToImpact -= Time.deltaTime;
            if (laneEvent.TimeToImpact > 0f)
            {
                continue;
            }

            if (laneEvent.Lane == playerLane)
            {
                if (laneEvent.IsPickup)
                {
                    gameManager.AddHealth(8);
                    gameManager.AddScore(16);
                    runnerHint = "拾取到健康道具 +8 健康 +16 积分";
                }
                else
                {
                    gameManager.AddHealth(-18);
                    gameManager.AddScore(-6);
                    runnerHint = "撞到障碍 -18 健康 -6 积分";
                }
            }

            laneEvents.RemoveAt(i);
        }

        if (gameManager.CurrentHealth <= 0)
        {
            phase = Phase.RunnerFailed;
            laneEvents.Clear();
            runnerHint = "跑酷失败：健康值归零。按 R 重试该阶段。";
            return;
        }

        if (runnerTimeLeft <= 0f)
        {
            phase = Phase.Completed;
            laneEvents.Clear();
            gameManager.AddScore(40);
            runnerHint = "Day3 完成！按 Enter 进入 Day4。";
        }
    }

    private void ResetRunnerState()
    {
        runnerTimeLeft = runnerDuration;
        spawnTimer = 0.4f;
        playerLane = 1;
        laneEvents.Clear();
        runnerHint = "跑酷：A/D 或 ←/→ 切换赛道，躲避红块，吃绿块。";
    }

    private void SpawnLaneEvent()
    {
        LaneEvent laneEvent = new LaneEvent
        {
            Lane = Random.Range(0, 3),
            TimeToImpact = RunnerEventTravelSeconds,
            IsPickup = Random.value < 0.28f
        };
        laneEvents.Add(laneEvent);
    }

    private void OnGUI()
    {
        DrawSessionHeader();

        switch (phase)
        {
            case Phase.MealCards:
                DrawMealCardsUI();
                break;
            case Phase.Runner:
                DrawRunnerUI();
                break;
            case Phase.RunnerFailed:
                DrawRunnerFailedUI();
                break;
            case Phase.Completed:
                DrawCompletedUI();
                break;
        }
    }

    private void DrawSessionHeader()
    {
        GUILayout.BeginArea(new Rect(12f, 10f, 560f, 40f), GUI.skin.box);
        GUILayout.Label(
            string.Format(
                "Day {0} | 健康值: {1} | 健康积分: {2} | 重试: {3}",
                gameManager.CurrentDay,
                gameManager.CurrentHealth,
                gameManager.CurrentScore,
                gameManager.RetryCount));
        GUILayout.EndArea();
    }

    private void DrawMealCardsUI()
    {
        GUILayout.BeginArea(new Rect(12f, 56f, 690f, 360f), GUI.skin.box);
        GUILayout.Label("Day3 · 午餐/晚餐卡牌");
        GUILayout.Label(cardHint);
        GUILayout.Space(6f);

        for (int i = 0; i < mealCards.Count; i++)
        {
            FoodCard card = mealCards[i];
            bool selected = selectedCards.Contains(i);
            string marker = selected ? "[已选]" : "[  ]";
            GUILayout.Label(
                string.Format(
                    "{0}. {1} {2}  类型:{3}  健康{4:+#;-#;0}  积分{5:+#;-#;0}",
                    i + 1,
                    marker,
                    card.Name,
                    card.Category,
                    card.HealthDelta,
                    card.ScoreDelta));
        }

        GUILayout.Space(8f);
        GUILayout.Label("平衡要求：主食/蛋白/蔬菜至少各 1，且高糖/高油最多 1。");
        GUILayout.EndArea();
    }

    private void DrawRunnerUI()
    {
        GUILayout.BeginArea(new Rect(12f, 56f, 620f, 116f), GUI.skin.box);
        GUILayout.Label("Day3 · 血糖平衡跑酷");
        GUILayout.Label(runnerHint);
        GUILayout.Label(string.Format("剩余时间: {0:F1}s", runnerTimeLeft));
        GUILayout.EndArea();

        Rect track = new Rect(40f, 190f, 560f, 290f);
        DrawTrack(track);
    }

    private void DrawTrack(Rect trackRect)
    {
        Color oldColor = GUI.color;
        GUI.Box(trackRect, "三轨跑酷道");

        float laneWidth = trackRect.width / 3f;
        for (int i = 1; i <= 2; i++)
        {
            Rect divider = new Rect(trackRect.x + laneWidth * i - 1f, trackRect.y + 2f, 2f, trackRect.height - 4f);
            GUI.color = new Color(0.75f, 0.75f, 0.75f, 0.9f);
            GUI.DrawTexture(divider, Texture2D.whiteTexture);
        }

        float playerY = trackRect.y + trackRect.height - 36f;
        float playerX = trackRect.x + laneWidth * playerLane + laneWidth * 0.5f - 22f;
        GUI.color = new Color(0.25f, 0.65f, 1f, 0.95f);
        GUI.DrawTexture(new Rect(playerX, playerY, 44f, 22f), Texture2D.whiteTexture);

        for (int i = 0; i < laneEvents.Count; i++)
        {
            LaneEvent laneEvent = laneEvents[i];
            float progress = 1f - Mathf.Clamp01(laneEvent.TimeToImpact / RunnerEventTravelSeconds);
            float itemY = trackRect.y + 22f + progress * (trackRect.height - 70f);
            float itemX = trackRect.x + laneWidth * laneEvent.Lane + laneWidth * 0.5f - 18f;

            GUI.color = laneEvent.IsPickup
                ? new Color(0.3f, 0.9f, 0.35f, 0.95f)
                : new Color(0.95f, 0.25f, 0.25f, 0.95f);
            GUI.DrawTexture(new Rect(itemX, itemY, 36f, 18f), Texture2D.whiteTexture);
        }

        GUI.color = oldColor;
    }

    private void DrawRunnerFailedUI()
    {
        GUILayout.BeginArea(new Rect(12f, 56f, 620f, 120f), GUI.skin.box);
        GUILayout.Label("Day3 · 跑酷失败");
        GUILayout.Label(runnerHint);
        GUILayout.Label("按 R 重试跑酷阶段（保留卡牌结算）。");
        GUILayout.EndArea();
    }

    private void DrawCompletedUI()
    {
        GUILayout.BeginArea(new Rect(12f, 56f, 620f, 120f), GUI.skin.box);
        GUILayout.Label("Day3 完成");
        GUILayout.Label(runnerHint);
        GUILayout.Label("按 Enter 进入 Day4。");
        GUILayout.EndArea();
    }
}
