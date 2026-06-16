using UnityEngine;

public sealed class Day2Stage4SpaceGameController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private int currentDay = 2;
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private bool allowAutoLoadOnWin = true;
    [SerializeField] private float winAutoLoadDelaySeconds = 1.2f;
    [SerializeField] private int scoreReward = 20;
    [SerializeField] private int healthReward = 5;

    [Header("Gauge Tuning")]
    [SerializeField] private float riseSpeed = 0.85f;
    [SerializeField] private float fallSpeed = 0.7f;

    [Header("Target Zone")]
    [Range(0f, 1f)]
    [SerializeField] private float zoneMin = 0.55f;
    [Range(0f, 1f)]
    [SerializeField] private float zoneMax = 0.8f;
    [SerializeField] private bool useMovingZone = false;
    [Range(0.05f, 0.95f)]
    [SerializeField] private float movingZoneWidth = 0.12f;
    [SerializeField] private float zoneMoveSpeed = 0.42f;
    [SerializeField] private float zoneRetargetIntervalMin = 0.65f;
    [SerializeField] private float zoneRetargetIntervalMax = 1.35f;

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
    private float activeZoneMin;
    private float activeZoneMax;
    private float zoneCenter;
    private float zoneCenterTarget;
    private float zoneRetargetTimer;

    private Texture2D solidTexture;
    private GUIStyle uiTitleStyle;
    private GUIStyle uiLineStyle;
    private GUIStyle uiPrimaryStyle;
    private GUIStyle uiHintStyle;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.EnsureInstanceForDemo();
        }

        gameManager.MarkCurrentDay(Mathf.Max(1, currentDay));

        if (zoneMax < zoneMin)
        {
            float tmp = zoneMin;
            zoneMin = zoneMax;
            zoneMax = tmp;
        }

        InitializeActiveZone();
    }

    private void InitializeActiveZone()
    {
        if (useMovingZone)
        {
            float width = GetActiveZoneWidth();
            zoneCenter = Random.Range(width * 0.5f, 1f - (width * 0.5f));
            zoneCenterTarget = zoneCenter;
            ApplyZoneCenter(zoneCenter);
            ScheduleNextZoneRetarget();
            return;
        }

        activeZoneMin = zoneMin;
        activeZoneMax = zoneMax;
    }

    private float GetActiveZoneWidth()
    {
        if (useMovingZone)
        {
            return Mathf.Clamp(movingZoneWidth, 0.05f, 0.95f);
        }

        return Mathf.Clamp01(zoneMax - zoneMin);
    }

    private void ApplyZoneCenter(float center)
    {
        float half = GetActiveZoneWidth() * 0.5f;
        center = Mathf.Clamp(center, half, 1f - half);
        zoneCenter = center;
        activeZoneMin = center - half;
        activeZoneMax = center + half;
    }

    private void PickRandomZoneTarget()
    {
        float half = GetActiveZoneWidth() * 0.5f;
        zoneCenterTarget = Random.Range(half, 1f - half);
    }

    private void ScheduleNextZoneRetarget()
    {
        float minInterval = Mathf.Max(0.1f, zoneRetargetIntervalMin);
        float maxInterval = Mathf.Max(minInterval, zoneRetargetIntervalMax);
        zoneRetargetTimer = Random.Range(minInterval, maxInterval);
    }

    private void UpdateMovingZone()
    {
        zoneRetargetTimer -= Time.deltaTime;
        if (zoneRetargetTimer <= 0f)
        {
            PickRandomZoneTarget();
            ScheduleNextZoneRetarget();
        }

        if (Mathf.Approximately(zoneCenter, zoneCenterTarget))
        {
            return;
        }

        zoneCenter = Mathf.MoveTowards(zoneCenter, zoneCenterTarget, zoneMoveSpeed * Time.deltaTime);
        ApplyZoneCenter(zoneCenter);
    }

    private void Update()
    {
        if (completed)
        {
            HandleWinAutoLoad();
            return;
        }

        bool holding = Input.GetKey(KeyCode.Space);
        value += (holding ? riseSpeed : -fallSpeed) * Time.deltaTime;
        value = Mathf.Clamp01(value);

        if (useMovingZone)
        {
            UpdateMovingZone();
        }

        bool inZone = value >= activeZoneMin && value <= activeZoneMax;
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
        if (completed)
        {
            DrawGauge();
            return;
        }

        EnsureUiStyles();

        float width = Mathf.Clamp(Screen.width - 140f, 720f, 980f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, 32f, width, 64f);
        DrawUiPanel(panel);

        GUILayout.BeginArea(GetPaddedRect(panel, 24f, 14f));
        GUILayout.Label("保持光标在绿色区域", uiPrimaryStyle);
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

        GUI.color = new Color(0f, 0f, 0f, 0.22f);
        GUI.DrawTexture(new Rect(bar.x - 8f, bar.y - 8f, bar.width + 16f, bar.height + 16f), solidTexture);

        GUI.color = new Color(0.04f, 0.08f, 0.12f, 0.88f);
        GUI.DrawTexture(new Rect(bar.x - 4f, bar.y - 4f, bar.width + 8f, bar.height + 8f), solidTexture);

        GUI.color = new Color(0.12f, 0.18f, 0.22f, 1f);
        GUI.DrawTexture(bar, solidTexture);

        float zoneLeftX = bar.x + (activeZoneMin * bar.width);
        float zoneRightX = bar.x + (activeZoneMax * bar.width);
        Rect zoneRect = new Rect(zoneLeftX, bar.y, zoneRightX - zoneLeftX, bar.height);
        bool inZone = value >= activeZoneMin && value <= activeZoneMax;
        GUI.color = inZone ? new Color(0.20f, 0.78f, 0.52f, 0.95f) : new Color(0.20f, 0.78f, 0.52f, 0.50f);
        GUI.DrawTexture(zoneRect, solidTexture);

        float indicatorX = bar.x + (value * bar.width);
        GUI.color = new Color(0.98f, 0.76f, 0.25f, 1f);
        GUI.DrawTexture(new Rect(indicatorX - 3f, bar.y - 8f, 6f, bar.height + 16f), solidTexture);

        GUI.color = Color.white;
        GUI.Label(new Rect(bar.x, bar.y - 28f, bar.width, 24f), "稳定区");

        if (completed)
        {
            GUI.color = new Color(0.20f, 0.78f, 0.52f, 1f);
            GUI.Label(new Rect(bar.x, bar.y + bar.height + 10f, bar.width, 24f), "读数完成");
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

    private void EnsureUiStyles()
    {
        EnsureSolidTexture();

        if (uiTitleStyle != null)
        {
            return;
        }

        uiTitleStyle = CreateLabelStyle(28, FontStyle.Bold, Color.white);
        uiLineStyle = CreateLabelStyle(18, FontStyle.Normal, new Color(0.84f, 0.91f, 0.96f));
        uiPrimaryStyle = CreateLabelStyle(21, FontStyle.Bold, Color.white);
        uiHintStyle = CreateLabelStyle(18, FontStyle.Normal, new Color(0.73f, 0.84f, 0.92f));
    }

    private static GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, Color color)
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = fontStyle,
            normal = { textColor = color },
            wordWrap = true
        };
    }

    private void DrawUiPanel(Rect rect)
    {
        EnsureSolidTexture();

        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.22f);
        GUI.DrawTexture(new Rect(rect.x + 5f, rect.y + 5f, rect.width, rect.height), solidTexture);
        GUI.color = new Color(0.04f, 0.08f, 0.12f, 0.88f);
        GUI.DrawTexture(rect, solidTexture);
        GUI.color = oldColor;
    }

    private static Rect GetPaddedRect(Rect rect, float horizontal, float vertical)
    {
        return new Rect(rect.x + horizontal, rect.y + vertical, rect.width - (horizontal * 2f), rect.height - (vertical * 2f));
    }
}
