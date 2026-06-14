using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class Day1HospitalController : MonoBehaviour
{
    private const string LegacyDay2Scene = "Day2_LancingStep";
    private const string SplitDay2EntryScene = "Day2_Stage1_PenAssembly";
    private const string Day2FallbackScene = "Day2_Home";

    private static readonly string[] DialogueLines =
    {
        "医生：你好，先不用紧张。今天我们从记录日常状态和生活选择开始。",
        "医生：接下来会通过游戏化任务练习饮食规划、检测操作和运动节奏。",
        "第一天完成。准备进入下一阶段。"
    };

    [Header("Scene References")]
    [SerializeField] private Day1PlayerController player;
    [SerializeField] private Transform doctor;
    [SerializeField] private Collider interactionZone;

    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Text continueButtonText;
    [SerializeField] private Button continueButton;

    [Header("Flow")]
    [SerializeField] private float interactionDistance = 2.2f;
    [SerializeField] private bool zoneTriggerUsesXZOnly = true;
    [SerializeField] private string nextSceneName = "Day2_Stage1_PenAssembly";

    private Day1CameraFollow cameraFollow;
    private int dialogueIndex;
    private bool dialogueOpen;
    private bool playerInRange;

    private void Awake()
    {
        nextSceneName = NormalizeNextSceneName(nextSceneName);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.36f, 0.39f, 0.42f, 1f);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraFollow = mainCamera.GetComponent<Day1CameraFollow>();
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(AdvanceDialogue);
            continueButton.onClick.AddListener(AdvanceDialogue);
        }

        // Auto-bind to the green marker zone when available.
        if (interactionZone == null)
        {
            GameObject markerObject = GameObject.Find("DoctorZoneMarker");
            if (markerObject != null)
            {
                interactionZone = markerObject.GetComponent<Collider>();
            }
        }
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        playerInRange = IsPlayerInRange();
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(playerInRange && !dialogueOpen);
        }

        if (playerInRange && !dialogueOpen && Input.GetKeyDown(KeyCode.E))
        {
            BeginDialogue();
        }

        if (dialogueOpen && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            AdvanceDialogue();
        }

        if (!dialogueOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private bool IsPlayerInRange()
    {
        if (interactionZone != null && interactionZone.enabled && interactionZone.gameObject.activeInHierarchy)
        {
            Bounds bounds = interactionZone.bounds;
            Vector3 playerPosition = player.transform.position;
            bool insideXZ =
                playerPosition.x >= bounds.min.x &&
                playerPosition.x <= bounds.max.x &&
                playerPosition.z >= bounds.min.z &&
                playerPosition.z <= bounds.max.z;

            if (!insideXZ)
            {
                return false;
            }

            if (zoneTriggerUsesXZOnly)
            {
                return true;
            }

            return playerPosition.y >= bounds.min.y && playerPosition.y <= bounds.max.y;
        }

        if (doctor == null)
        {
            return false;
        }

        return Vector3.Distance(player.transform.position, doctor.position) <= interactionDistance;
    }

    public void BeginDialogue()
    {
        dialogueOpen = true;
        dialogueIndex = 0;
        player.InputLocked = true;

        if (cameraFollow != null)
        {
            cameraFollow.SetLookInputEnabled(false);
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        ShowCurrentLine();
    }

    public void AdvanceDialogue()
    {
        if (!dialogueOpen)
        {
            return;
        }

        dialogueIndex++;
        if (dialogueIndex >= DialogueLines.Length)
        {
            LoadNextDayScene();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (dialogueText != null)
        {
            dialogueText.text = DialogueLines[dialogueIndex];
        }

        if (continueButtonText != null)
        {
            continueButtonText.text = dialogueIndex == DialogueLines.Length - 1
                ? "进入下一阶段"
                : "继续";
        }
    }

    private static string NormalizeNextSceneName(string requestedScene)
    {
        if (string.IsNullOrWhiteSpace(requestedScene))
        {
            return SplitDay2EntryScene;
        }

        return requestedScene == LegacyDay2Scene ? SplitDay2EntryScene : requestedScene;
    }

    private void LoadNextDayScene()
    {
        string normalizedRequested = NormalizeNextSceneName(nextSceneName);
        string[] candidates =
        {
            normalizedRequested,
            SplitDay2EntryScene,
            Day2FallbackScene
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string sceneName = candidates[i];
            if (string.IsNullOrWhiteSpace(sceneName) || IsDuplicateCandidate(candidates, i, sceneName))
            {
                continue;
            }

            if (!IsSceneInBuildSettings(sceneName))
            {
                continue;
            }

            SceneManager.LoadScene(sceneName);
            return;
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            string sceneName = candidates[i];
            if (string.IsNullOrWhiteSpace(sceneName) || IsDuplicateCandidate(candidates, i, sceneName))
            {
                continue;
            }

            try
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[Day1HospitalController] LoadScene failed: " + sceneName + " -> " + exception.Message);
            }
        }

        Debug.LogError("[Day1HospitalController] Unable to load a valid Day2 scene. Checked: " + normalizedRequested + ", " + SplitDay2EntryScene + ", " + Day2FallbackScene);
    }

    private static bool IsDuplicateCandidate(string[] candidates, int currentIndex, string sceneName)
    {
        for (int i = 0; i < currentIndex; i++)
        {
            if (candidates[i] == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSceneInBuildSettings(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                continue;
            }

            string scenePathName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (scenePathName == sceneName)
            {
                return true;
            }
        }

        return false;
    }
}
