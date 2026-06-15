using UnityEngine;

public sealed class Day2Stage4SpaceGameController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private bool allowAutoLoadOnWin = true;
    [SerializeField] private float winAutoLoadDelaySeconds = 1.2f;
    [SerializeField] private int scoreReward = 20;
    [SerializeField] private int healthReward = 5;

    [Header("Gauge Tuning")]
    [SerializeField] private float riseSpeed = 0.85f;
    [SerializeField] private float fallSpeed = 0.7f;

    [Header("Fixed Target Zone (do not move)")]
    [Range(0f, 1f)]
    [SerializeField] private float zoneMin = 0.55f;
    [Range(0f, 1f)]
    [SerializeField] private float zoneMax = 0.8f;

    [Header("Win Condition")]
    [SerializeField] private float requiredHoldSeconds = 2f;
    [SerializeField] private bool resetHoldWhenOutOfZone = false;

    [SerializeField] private GameManager gameManager;

    private float value;
    private float holdTimer;
    private bool completed;
    private bool rewardGranted;
    private bool loadQueued;
    private float loadTimer;

    private Texture2D solidTexture;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.EnsureInstanceForDemo();
        }

        gameManager.MarkCurrentDay(2);

        if (zoneMax < zoneMin)
        {
            float tmp = zoneMin;
            zoneMin = zoneMax;
            zoneMax = tmp;
        }
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
            HandleWinAutoLoad();
            return;
        }

        bool holding = Input.GetKey(KeyCode.Space);
        value += (holding ? riseSpeed : -fallSpeed) * Time.deltaTime;
        value = Mathf.Clamp01(value);

        bool inZone = value >= zoneMin && value <= zoneMax;
        if (inZone)
        {
            holdTimer += Time.deltaTime;
        }
        else if (resetHoldWhenOutOfZone)
        {
            holdTimer = 0f;
        }

        if (holdTimer >= requiredHoldSeconds)
        {
            holdTimer = requiredHoldSeconds;
            completed = true;
            GrantRewardOnce();
        }
    }

    private void HandleWinAutoLoad()
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

    private void GrantRewardOnce()
    {
        if (rewardGranted || gameManager == null)
        {
            return;
        }

        rewardGranted = true;
        gameManager.AddScore(scoreReward);
        gameManager.AddHealth(healthReward);
    }

    private void OnGUI()
    {
        Rect panel = new Rect(12f, 10f, 760f, 132f);
        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.Label("Day2 阶段4：按空格保持小游戏");
        GUILayout.Label("操作：长按 空格 让指针向右移动，松开向左回落。把指针保持在固定的绿色区间内。");
        GUILayout.Label("目标：累计保持 " + requiredHoldSeconds.ToString("F0") + " 秒即可完成。按 Esc 返回主菜单。");
        if (completed)
        {
            GUILayout.Label("完成！稳定保持达标。");
        }
        else
        {
            GUILayout.Label("当前保持：" + holdTimer.ToString("F1") + " / " + requiredHoldSeconds.ToString("F1") + " s");
        }
        GUILayout.EndArea();

        DrawGauge();
    }

    private void DrawGauge()
    {
        EnsureSolidTexture();

        float barWidth = 420f;
        float barHeight = 70f;
        float barX = (Screen.width * 0.5f) - (barWidth * 0.5f);
        float barY = (Screen.height * 0.7f) - (barHeight * 0.5f);
        Rect bar = new Rect(barX, barY, barWidth, barHeight);

        Color prev = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(bar.x - 4f, bar.y - 4f, bar.width + 8f, bar.height + 8f), solidTexture);

        GUI.color = new Color(0.18f, 0.18f, 0.2f, 1f);
        GUI.DrawTexture(bar, solidTexture);

        float zoneLeftX = bar.x + (zoneMin * bar.width);
        float zoneRightX = bar.x + (zoneMax * bar.width);
        Rect zoneRect = new Rect(zoneLeftX, bar.y, zoneRightX - zoneLeftX, bar.height);
        bool inZone = value >= zoneMin && value <= zoneMax;
        GUI.color = inZone ? new Color(0.35f, 0.85f, 0.4f, 0.9f) : new Color(0.35f, 0.7f, 0.4f, 0.55f);
        GUI.DrawTexture(zoneRect, solidTexture);

        float indicatorX = bar.x + (value * bar.width);
        GUI.color = new Color(1f, 0.95f, 0.4f, 1f);
        GUI.DrawTexture(new Rect(indicatorX - 3f, bar.y - 8f, 6f, bar.height + 16f), solidTexture);

        if (completed)
        {
            GUI.color = new Color(0.4f, 1f, 0.5f, 1f);
            GUI.Label(new Rect(bar.x, bar.y - 26f, bar.width, 24f), "完成");
        }

        GUI.color = prev;
    }

    private void EnsureSolidTexture()
    {
        if (solidTexture != null)
        {
            return;
        }

        solidTexture = new Texture2D(1, 1);
        solidTexture.SetPixel(0, 0, Color.white);
        solidTexture.Apply();
        solidTexture.hideFlags = HideFlags.HideAndDontSave;
    }
}
