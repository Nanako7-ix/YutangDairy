using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    private const string MainMenuScene = "MainMenu";
    private const string Day1Scene = "Day1_Hospital";
    private const int DefaultHealth = 100;
    private const int MaxHealth = 100;
    private const string BestScoreKey = "YutangDiary_BestHealthScore";

    public static GameManager Instance { get; private set; }
    public static int BestHealthScore => PlayerPrefs.GetInt(BestScoreKey, 0);
    public static GameManager EnsureInstanceForDemo()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("GameManager");
        return managerObject.AddComponent<GameManager>();
    }

    public event Action SessionChanged;

    public int CurrentHealth { get; private set; } = DefaultHealth;
    public int CurrentScore { get; private set; }
    public int RetryCount { get; private set; }
    public int CurrentDay { get; private set; } = 1;
    public bool RunCompleted { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void StartNewGame()
    {
        ResetSession();
        SceneManager.LoadScene(Day1Scene);
    }

    public void ReturnToMainMenu()
    {
        SaveBestScoreIfNeeded();
        SceneManager.LoadScene(MainMenuScene);
    }

    public void MarkCurrentDay(int day)
    {
        CurrentDay = Mathf.Max(1, day);
        NotifySessionChanged();
    }

    public void AddScore(int amount)
    {
        CurrentScore = Mathf.Max(0, CurrentScore + amount);
        NotifySessionChanged();
    }

    public void AddHealth(int amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
        NotifySessionChanged();
    }

    public void RegisterRetry()
    {
        RetryCount++;
        NotifySessionChanged();
    }

    public void CompleteRun()
    {
        RunCompleted = true;
        SaveBestScoreIfNeeded();
        NotifySessionChanged();
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "Day1_Hospital":
                CurrentDay = 1;
                break;
            case "Day2_Home":
            case "Day2_LancingStep":
            case "Day2_Stage1_PenAssembly":
            case "Day2_Stage2_InsertStrip":
            case "Day2_Stage3_Puncture":
            case "Day2_Stage4_SpaceMiniGame":
                CurrentDay = 2;
                break;
            case "Day3_Home":
                CurrentDay = 3;
                break;
            case "Day4_Home":
                CurrentDay = 4;
                break;
            case "Day5_Rhythm":
                CurrentDay = 5;
                break;
            default:
                break;
        }

        NotifySessionChanged();
    }

    private void ResetSession()
    {
        CurrentHealth = DefaultHealth;
        CurrentScore = 0;
        RetryCount = 0;
        CurrentDay = 1;
        RunCompleted = false;
        NotifySessionChanged();
    }

    private void SaveBestScoreIfNeeded()
    {
        if (CurrentScore <= BestHealthScore)
        {
            return;
        }

        PlayerPrefs.SetInt(BestScoreKey, CurrentScore);
        PlayerPrefs.Save();
    }

    private void NotifySessionChanged()
    {
        SessionChanged?.Invoke();
    }
}
