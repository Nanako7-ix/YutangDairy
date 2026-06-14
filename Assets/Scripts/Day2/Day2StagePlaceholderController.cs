using UnityEngine;

public sealed class Day2StagePlaceholderController : MonoBehaviour
{
    [SerializeField] private int stageIndex = 3;
    [SerializeField] private string stageTitle = "阶段3：扎手指测血糖";
    [SerializeField] [TextArea(2, 4)] private string stageDescription = "该阶段还未接入交互逻辑。";
    [SerializeField] private string nextSceneName = "Day2_Stage4_SpaceMiniGame";
    [SerializeField] private bool allowEnterToNextScene = true;
    [SerializeField] private float autoLoadDelaySeconds = 0.5f;
    [SerializeField] private GameManager gameManager;
    private bool autoLoadQueued;
    private float autoLoadTimer;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.EnsureInstanceForDemo();
        }

        gameManager.MarkCurrentDay(2);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameManager.ReturnToMainMenu();
            return;
        }

        if (!allowEnterToNextScene)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            return;
        }

        if (!autoLoadQueued)
        {
            autoLoadQueued = true;
            autoLoadTimer = Mathf.Max(0f, autoLoadDelaySeconds);
            return;
        }

        autoLoadTimer -= Time.deltaTime;
        if (autoLoadTimer > 0f)
        {
            return;
        }

        autoLoadQueued = false;
        gameManager.LoadScene(nextSceneName);
    }

    private void OnGUI()
    {
        Rect panel = new Rect(12f, 10f, 720f, 132f);
        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.Label("Day2 采血流程（四阶段场景拆分）");
        GUILayout.Label("当前场景：第 " + stageIndex + " 阶段");
        GUILayout.Label(stageTitle);
        GUILayout.Label(stageDescription);
        if (allowEnterToNextScene && !string.IsNullOrWhiteSpace(nextSceneName))
        {
            float remain = autoLoadQueued ? Mathf.Max(0f, autoLoadTimer) : Mathf.Max(0f, autoLoadDelaySeconds);
            GUILayout.Label("即将自动进入下一阶段（" + remain.ToString("F1") + "s），按 Esc 返回主菜单。");
        }
        else
        {
            GUILayout.Label("按 Esc 返回主菜单。");
        }
        GUILayout.EndArea();
    }
}
