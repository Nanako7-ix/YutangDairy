using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class YutangWelcomeScreen : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Text bestScoreText;
    [SerializeField] private Text versionText;

    private void Awake()
    {
        Bind();
    }

    private void OnEnable()
    {
        Bind();
        Refresh();
    }

public void Refresh()
    {
        if (bestScoreText != null)
        {
            bestScoreText.text = "历史最高健康积分：" + GameManager.BestHealthScore;
        }

        if (versionText != null)
        {
            versionText.text = "灰盒 Demo · Unity 场景化欢迎界面";
        }
    }

public void StartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
    }

    private void Bind()
    {
        if (startButton == null)
        {
            startButton = GetComponentInChildren<Button>(true);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
