using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class Day3HomeController : MonoBehaviour
{
    private enum RunState
    {
        Running,
        Failed,
        Completed
    }

    [Header("Runner")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform playerVisual;
    [SerializeField] private Transform followCamera;
    [SerializeField] private float forwardSpeed = 8.5f;
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private float laneChangeSpeed = 12f;
    [SerializeField] private float jumpHeight = 2.4f;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float finishZ = 260f;

    [Header("Flow")]
    [SerializeField] private int currentDay = 3;
    [SerializeField] private int maxObstacleHits = 3;
    [SerializeField] private string nextSceneName = "Day4_Stage1_PenAssembly";
    [SerializeField] private float completeLoadDelaySeconds = 0.8f;
    [SerializeField] private string replaySceneName = "Day3_Home";
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Score Pickups")]
    [SerializeField] private string scorePickupPrefix = "Heart_";
    [SerializeField] private string scorePickupDisplayName = "爱心";
    [SerializeField] private int scorePickupValue = 20;
    [SerializeField] private int completionScoreValue = 100;
    [SerializeField] private int retryScoreCost = 20;

    [Header("HUD")]
    [SerializeField] private Text healthText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text progressText;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image progressFill;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultTitle;
    [SerializeField] private Text resultBody;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    private GameManager gameManager;
    private RunState state = RunState.Running;
    private int targetLane = 1;
    private float verticalVelocity;
    private float invulnerabilityTimer;
    private float feedbackTimer;
    private int heartsCollected;
    private int pendingRunScore;
    private int obstacleHits;
    private float completeLoadTimer;
    private Vector3 visualStartLocalPosition;
    private Quaternion visualStartLocalRotation;
    private Vector3 cameraVelocity;

    private void Awake()
    {
        gameManager = GameManager.EnsureInstanceForDemo();
        maxObstacleHits = Mathf.Max(1, maxObstacleHits);
        gameManager.MarkCurrentDay(Mathf.Max(1, currentDay));

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (playerVisual != null)
        {
            visualStartLocalPosition = playerVisual.localPosition;
            visualStartLocalRotation = playerVisual.localRotation;
        }

        ApplyChineseFont();
    }

    private void Start()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryRun);
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(ReturnToMenu);
        }

        if (healthFill != null)
        {
            healthFill.transform.parent.gameObject.SetActive(false);
        }

        ShowFeedback("A / D 或方向键换道，Space 跳跃", 4f);
        RefreshHud();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
            return;
        }

        if (state == RunState.Failed)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RetryRun();
            }
            return;
        }

        if (state == RunState.Completed)
        {
            completeLoadTimer -= Time.deltaTime;
            if (completeLoadTimer <= 0f)
            {
                gameManager.LoadScene(nextSceneName);
            }
            return;
        }

        HandleLaneInput();
        MoveRunner();
        AnimateVisual();

        invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - Time.deltaTime);
        feedbackTimer = Mathf.Max(0f, feedbackTimer - Time.deltaTime);
        if (feedbackTimer <= 0f && feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }

        if (transform.position.z >= finishZ)
        {
            CompleteRun();
        }

        RefreshHud();
    }

    private void LateUpdate()
    {
        if (followCamera == null)
        {
            return;
        }

        Vector3 targetPosition = transform.position + new Vector3(0f, 6.2f, -12.5f);
        followCamera.position = Vector3.SmoothDamp(
            followCamera.position,
            targetPosition,
            ref cameraVelocity,
            0.12f);

        Vector3 lookTarget = transform.position + new Vector3(0f, 1.1f, 8f);
        followCamera.rotation = Quaternion.Slerp(
            followCamera.rotation,
            Quaternion.LookRotation(lookTarget - followCamera.position, Vector3.up),
            8f * Time.deltaTime);
    }

    private void HandleLaneInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            targetLane = Mathf.Max(0, targetLane - 1);
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            targetLane = Mathf.Min(2, targetLane + 1);
        }

        if (characterController != null
            && characterController.isGrounded
            && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void MoveRunner()
    {
        if (characterController == null)
        {
            return;
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        float targetX = (targetLane - 1) * laneWidth;
        float nextX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
        float horizontalSpeed = (nextX - transform.position.x) / Mathf.Max(Time.deltaTime, 0.0001f);

        Vector3 velocity = new Vector3(horizontalSpeed, verticalVelocity, forwardSpeed);
        characterController.Move(velocity * Time.deltaTime);
    }

    private void AnimateVisual()
    {
        if (playerVisual == null)
        {
            return;
        }

        float bob = characterController != null && characterController.isGrounded
            ? Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.08f
            : 0f;
        playerVisual.localPosition = visualStartLocalPosition + Vector3.up * bob;

        float targetX = (targetLane - 1) * laneWidth;
        float lean = Mathf.Clamp((targetX - transform.position.x) * -7f, -14f, 14f);
        playerVisual.localRotation = visualStartLocalRotation * Quaternion.Euler(0f, 0f, lean);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (state != RunState.Running || other == null)
        {
            return;
        }

        if (other.name.StartsWith(scorePickupPrefix))
        {
            heartsCollected++;
            pendingRunScore += Mathf.Max(0, scorePickupValue);
            ShowFeedback("收集" + scorePickupDisplayName + "  本局得分 +" + scorePickupValue, 1.6f);
            Destroy(other.gameObject);
            return;
        }

        if (other.name.StartsWith("Obstacle_"))
        {
            Destroy(other.gameObject);

            if (invulnerabilityTimer > 0f)
            {
                return;
            }

            invulnerabilityTimer = 1.1f;
            obstacleHits++;
            ShowFeedback("撞到高糖食物  爱心 -1  剩余 " + Mathf.Max(0, maxObstacleHits - obstacleHits) + "/" + maxObstacleHits, 1.8f);

            if (obstacleHits >= maxObstacleHits)
            {
                FailRun();
            }
            return;
        }

        if (other.name == "FinishTrigger")
        {
            CompleteRun();
        }
    }

    private void CompleteRun()
    {
        if (state != RunState.Running)
        {
            return;
        }

        state = RunState.Completed;
        gameManager.AddScore(pendingRunScore + Mathf.Max(0, completionScoreValue));
        pendingRunScore = 0;
        completeLoadTimer = completeLoadDelaySeconds;
    }

    private void FailRun()
    {
        state = RunState.Failed;
        ShowResult(
            "需要重来",
            "碰到高糖食物 " + maxObstacleHits + " 次\n收集" + scorePickupDisplayName + "：" + heartsCollected
            + "\n本局临时得分：" + pendingRunScore + "（未结算）"
            + "\n重试扣除 " + retryScoreCost + " 分"
            + "\n\n按 R 重新挑战");
    }

    private void ShowResult(string title, string body)
    {
        if (resultTitle != null)
        {
            resultTitle.text = title;
        }

        if (resultBody != null)
        {
            resultBody.text = body;
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (retryButton != null)
        {
            Text label = retryButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = state == RunState.Completed ? "再跑一次" : "重新挑战";
            }
        }
    }

    private void RetryRun()
    {
        int cost = Mathf.Min(Mathf.Max(0, retryScoreCost), gameManager.CurrentScore);
        gameManager.AddScore(-cost);
        gameManager.RegisterRetry();
        SceneManager.LoadScene(replaySceneName);
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    private void ShowFeedback(string message, float duration)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
        feedbackTimer = duration;
    }

    private void RefreshHud()
    {
        if (healthText != null)
        {
            healthText.text = BuildHeartHud();
        }

        if (scoreText != null)
        {
            scoreText.text = "得分  " + (gameManager.CurrentScore + pendingRunScore);
        }

        float progress = Mathf.Clamp01(transform.position.z / Mathf.Max(1f, finishZ));
        if (progressText != null)
        {
            progressText.text = "运动进度  " + Mathf.RoundToInt(progress * 100f) + "%";
        }

        if (healthFill != null)
        {
            healthFill.transform.parent.gameObject.SetActive(false);
        }

        if (progressFill != null)
        {
            progressFill.type = Image.Type.Simple;

            RectTransform fillRect = progressFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(progress, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }
    }

    private string BuildHeartHud()
    {
        int remaining = Mathf.Clamp(maxObstacleHits - obstacleHits, 0, maxObstacleHits);
        string hearts = string.Empty;
        for (int i = 0; i < remaining; i++)
        {
            if (i > 0)
            {
                hearts += "  ";
            }

            hearts += "♥";
        }

        return "健康  " + hearts;
    }

    private void ApplyChineseFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "PingFang SC", "Arial" },
            24);

        Text[] texts = FindObjectsOfType<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].font = font;
        }
    }
}
