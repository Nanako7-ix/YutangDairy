using System.Collections.Generic;
using UnityEngine;

public sealed class Day4HomeController : MonoBehaviour
{
    private enum Phase
    {
        PlanCards,
        AdvancedMonitor,
        MonitorFailed,
        Completed
    }

    private struct FoodCard
    {
        public FoodCard(string name, string foodType, int healthDelta, int scoreDelta)
        {
            Name = name;
            FoodType = foodType;
            HealthDelta = healthDelta;
            ScoreDelta = scoreDelta;
        }

        public string Name { get; }
        public string FoodType { get; }
        public int HealthDelta { get; }
        public int ScoreDelta { get; }
    }

    [Header("Flow")]
    [SerializeField] private string nextSceneName = "Day5_Rhythm";

    [Header("Advanced Monitor")]
    [SerializeField] private float glucoseFallPerSecond = 0.18f;
    [SerializeField] private float glucoseRisePerSecond = 0.35f;
    [SerializeField] private float monitorRequiredSeconds = 6f;
    [SerializeField] private float targetCenterAmplitude = 0.22f;
    [SerializeField] private float targetMoveSpeed = 1.1f;
    [SerializeField] private float targetWidth = 0.16f;

    private readonly List<FoodCard> dayPlanCards = new List<FoodCard>
    {
        new FoodCard("杂粮早餐", "主食", 5, 8),
        new FoodCard("蔬菜沙拉", "蔬菜", 6, 9),
        new FoodCard("清蒸鱼", "蛋白", 6, 10),
        new FoodCard("豆腐汤", "蛋白", 5, 8),
        new FoodCard("糙米饭", "主食", 4, 7),
        new FoodCard("水果拼盘", "水果", 5, 8),
        new FoodCard("酸奶", "乳制品", 4, 7),
        new FoodCard("薯片", "高盐", -8, -5),
        new FoodCard("甜甜圈", "高糖", -10, -6),
        new FoodCard("炸鸡块", "高油", -12, -8)
    };

    private readonly HashSet<int> selectedCards = new HashSet<int>();

    private GameManager gameManager;
    private Phase phase = Phase.PlanCards;
    private string cardHint = "选择 5 张一日饮食卡牌（数字键 1~0，Enter 确认）。";
    private string monitorHint = "进阶监测：移动窗口更窄，按住 Space 保持数值。";
    private float glucoseValue = 0.52f;
    private float monitorHoldTimer;
    private float monitorStartTime;

    private void Awake()
    {
        gameManager = GameManager.EnsureInstanceForDemo();
        gameManager.MarkCurrentDay(4);
        monitorStartTime = Time.time;
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
            case Phase.PlanCards:
                UpdateCardsPhase();
                break;
            case Phase.AdvancedMonitor:
                UpdateAdvancedMonitor();
                break;
            case Phase.MonitorFailed:
                if (Input.GetKeyDown(KeyCode.R))
                {
                    gameManager.RegisterRetry();
                    gameManager.AddHealth(18);
                    ResetMonitor();
                    phase = Phase.AdvancedMonitor;
                }
                break;
            case Phase.Completed:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    gameManager.CompleteRun();
                    gameManager.LoadScene(nextSceneName);
                }
                break;
        }
    }

    private void UpdateCardsPhase()
    {
        for (int i = 0; i < dayPlanCards.Count && i < 10; i++)
        {
            KeyCode key;
            if (i <= 8)
            {
                key = (KeyCode)((int)KeyCode.Alpha1 + i);
            }
            else
            {
                key = KeyCode.Alpha0;
            }

            if (Input.GetKeyDown(key))
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

        if (selectedCards.Count >= 5)
        {
            cardHint = "已选满 5 张，先取消一张。";
            return;
        }

        selectedCards.Add(index);
        cardHint = "已选择 " + selectedCards.Count + "/5。";
    }

    private void TryConfirmCards()
    {
        if (selectedCards.Count != 5)
        {
            cardHint = "请先选满 5 张卡牌。";
            return;
        }

        int healthyTypes = 0;
        int unhealthyCount = 0;
        int healthDelta = 0;
        int scoreDelta = 0;
        HashSet<string> seenTypes = new HashSet<string>();

        foreach (int index in selectedCards)
        {
            FoodCard card = dayPlanCards[index];
            healthDelta += card.HealthDelta;
            scoreDelta += card.ScoreDelta;
            seenTypes.Add(card.FoodType);

            if (card.FoodType == "高盐" || card.FoodType == "高糖" || card.FoodType == "高油")
            {
                unhealthyCount++;
            }
        }

        healthyTypes = seenTypes.Count;
        if (healthyTypes < 4)
        {
            cardHint = "类型太单一：至少覆盖 4 类食物。";
            return;
        }

        if (unhealthyCount > 1)
        {
            cardHint = "高盐/高糖/高油最多 1 张。";
            return;
        }

        gameManager.AddHealth(healthDelta);
        gameManager.AddScore(scoreDelta);
        phase = Phase.AdvancedMonitor;
        cardHint = string.Format("饮食规划完成：健康{0:+#;-#;0}，积分{1:+#;-#;0}。", healthDelta, scoreDelta);
        ResetMonitor();
    }

    private void UpdateAdvancedMonitor()
    {
        glucoseValue -= glucoseFallPerSecond * Time.deltaTime;
        if (Input.GetKey(KeyCode.Space))
        {
            glucoseValue += glucoseRisePerSecond * Time.deltaTime;
        }

        glucoseValue = Mathf.Clamp01(glucoseValue);

        float center = CurrentTargetCenter();
        float halfWidth = targetWidth * 0.5f;
        float min = Mathf.Clamp01(center - halfWidth);
        float max = Mathf.Clamp01(center + halfWidth);
        bool inTarget = glucoseValue >= min && glucoseValue <= max;

        if (inTarget)
        {
            monitorHoldTimer += Time.deltaTime;
        }
        else
        {
            monitorHoldTimer = Mathf.Max(0f, monitorHoldTimer - Time.deltaTime * 0.9f);
        }

        if (glucoseValue <= 0.02f || glucoseValue >= 0.98f)
        {
            phase = Phase.MonitorFailed;
            monitorHint = "进阶监测失败：数值偏离过大。按 R 重试。";
            return;
        }

        if (monitorHoldTimer >= monitorRequiredSeconds)
        {
            phase = Phase.Completed;
            gameManager.AddScore(55);
            gameManager.AddHealth(8);
            monitorHint = "Day4 完成！按 Enter 进入下一天。";
        }
    }

    private float CurrentTargetCenter()
    {
        float t = (Time.time - monitorStartTime) * targetMoveSpeed;
        return 0.5f + Mathf.Sin(t) * targetCenterAmplitude;
    }

    private void ResetMonitor()
    {
        glucoseValue = 0.52f;
        monitorHoldTimer = 0f;
        monitorStartTime = Time.time;
        monitorHint = "进阶监测：移动窗口更窄，按住 Space 保持数值。";
    }

    private void OnGUI()
    {
        DrawSessionHeader();

        switch (phase)
        {
            case Phase.PlanCards:
                DrawPlanCardsUI();
                break;
            case Phase.AdvancedMonitor:
                DrawAdvancedMonitorUI();
                break;
            case Phase.MonitorFailed:
                DrawFailedUI();
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
                "Day {0} | 健康值: {1} | 得分: {2} | 重试: {3}",
                gameManager.CurrentDay,
                gameManager.CurrentHealth,
                gameManager.CurrentScore,
                gameManager.RetryCount));
        GUILayout.EndArea();
    }

    private void DrawPlanCardsUI()
    {
        GUILayout.BeginArea(new Rect(12f, 56f, 730f, 390f), GUI.skin.box);
        GUILayout.Label("Day4 · 一日饮食规划");
        GUILayout.Label(cardHint);
        GUILayout.Space(6f);

        for (int i = 0; i < dayPlanCards.Count; i++)
        {
            FoodCard card = dayPlanCards[i];
            bool selected = selectedCards.Contains(i);
            string marker = selected ? "[已选]" : "[  ]";
            string keyLabel = i < 9 ? (i + 1).ToString() : "0";
            GUILayout.Label(
                string.Format(
                    "{0}. {1} {2}  类型:{3}  健康{4:+#;-#;0}  积分{5:+#;-#;0}",
                    keyLabel,
                    marker,
                    card.Name,
                    card.FoodType,
                    card.HealthDelta,
                    card.ScoreDelta));
        }

        GUILayout.Space(8f);
        GUILayout.Label("要求：覆盖至少 4 类食物；高盐/高糖/高油最多 1 张。");
        GUILayout.EndArea();
    }

    private void DrawAdvancedMonitorUI()
    {
        GUILayout.BeginArea(new Rect(12f, 56f, 600f, 230f), GUI.skin.box);
        GUILayout.Label("Day4 · 监测进阶（移动窄窗口）");
        GUILayout.Label(monitorHint);
        GUILayout.Space(8f);

        Rect barRect = new Rect(20f, 96f, 540f, 30f);
        DrawMovingTargetBar(barRect);
        GUILayout.Space(76f);
        GUILayout.Label(
            string.Format("窗口内累计: {0:F1}/{1:F1}s", monitorHoldTimer, monitorRequiredSeconds));
        GUILayout.EndArea();
    }

    private void DrawMovingTargetBar(Rect rect)
    {
        float center = CurrentTargetCenter();
        float half = targetWidth * 0.5f;
        float min = Mathf.Clamp01(center - half);
        float max = Mathf.Clamp01(center + half);

        Color old = GUI.color;
        GUI.Box(rect, GUIContent.none);

        Rect targetRect = new Rect(
            rect.x + rect.width * min,
            rect.y + 2f,
            rect.width * (max - min),
            rect.height - 4f);
        GUI.color = new Color(0.25f, 0.8f, 0.3f, 0.52f);
        GUI.DrawTexture(targetRect, Texture2D.whiteTexture);

        Rect fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * glucoseValue, rect.height - 4f);
        GUI.color = new Color(0.3f, 0.55f, 0.95f, 0.86f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

        Rect marker = new Rect(rect.x + rect.width * glucoseValue - 2f, rect.y - 4f, 4f, rect.height + 8f);
        GUI.color = new Color(1f, 0.25f, 0.25f, 0.95f);
        GUI.DrawTexture(marker, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void DrawFailedUI()
    {
        GUILayout.BeginArea(new Rect(12f, 56f, 620f, 120f), GUI.skin.box);
        GUILayout.Label("Day4 · 进阶监测失败");
        GUILayout.Label(monitorHint);
        GUILayout.Label("按 R 重试该阶段。");
        GUILayout.EndArea();
    }

    private void DrawCompletedUI()
    {
        GUILayout.BeginArea(new Rect(12f, 56f, 620f, 120f), GUI.skin.box);
        GUILayout.Label("Day4 完成");
        GUILayout.Label(monitorHint);
        GUILayout.Label("按 Enter 进入下一天。");
        GUILayout.EndArea();
    }
}
