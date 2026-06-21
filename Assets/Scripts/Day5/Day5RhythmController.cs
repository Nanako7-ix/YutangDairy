using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Day5RhythmController : MonoBehaviour
{
    private enum NoteState
    {
        Pending,
        Hit,
        Missed
    }

    private sealed class RhythmNote
    {
        public RhythmNote(int lane, float hitTime)
        {
            Lane = lane;
            HitTime = hitTime;
        }

        public int Lane;
        public float HitTime;
        public NoteState State;
    }

    [Header("Flow")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private float introDelaySeconds = 1f;
    [SerializeField] private int hitScore = 10;
    [SerializeField] private int perfectBonus = 5;
    [SerializeField] private int completionScore = 80;
    [SerializeField] private int completionHealthReward = 5;

    [Header("Rhythm")]
    [SerializeField] private float noteTravelSeconds = 2.2f;
    [SerializeField] private float perfectWindowSeconds = 0.14f;
    [SerializeField] private float goodWindowSeconds = 0.30f;

    private readonly KeyCode[] laneKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
    private readonly string[] laneLabels = { "W", "A", "S", "D" };
    private readonly Color[] laneColors =
    {
        new Color(0.35f, 0.72f, 1f),
        new Color(0.20f, 0.78f, 0.52f),
        new Color(0.98f, 0.76f, 0.25f),
        new Color(1f, 0.44f, 0.48f)
    };

    private readonly List<RhythmNote> notes = new List<RhythmNote>();

    private GameManager gameManager;
    private float songStartTime;
    private float songDuration;
    private bool completed;
    private bool completionRewardGranted;
    private int combo;
    private int bestCombo;
    private int perfectCount;
    private int goodCount;
    private int missCount;
    private string feedbackText = "准备";
    private float feedbackTimer;

    private Texture2D solidTexture;
    private GUIStyle titleStyle;
    private GUIStyle lineStyle;
    private GUIStyle primaryStyle;
    private GUIStyle hintStyle;
    private GUIStyle centerTitleStyle;
    private GUIStyle centerHintStyle;

    private void Awake()
    {
        gameManager = GameManager.EnsureInstanceForDemo();
        gameManager.MarkCurrentDay(5);
        BuildEasyChart();
        RestartSong();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
            return;
        }

        if (completed)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartSong();
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ReturnToMenu();
            }
            return;
        }

        float songTime = GetSongTime();
        if (songTime >= 0f)
        {
            HandleLaneInput(songTime);
            UpdateMissedNotes(songTime);
            TryCompleteSong(songTime);
        }

        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0f)
            {
                feedbackText = "从左到右：W  A  S  D";
            }
        }
    }

    private void BuildEasyChart()
    {
        notes.Clear();

        int[] pattern =
        {
            0, 1, 2, 3,
            0, 2, 1, 3,
            1, 0, 2, 3,
            0, 1, 3, 2,
            1, 2, 3, 0,
            0, 1, 2, 3,
            1, 3, 2, 0,
            0, 2, 1, 3
        };

        float t = noteTravelSeconds + 0.6f;
        for (int i = 0; i < pattern.Length; i++)
        {
            notes.Add(new RhythmNote(pattern[i], t));
            t += i % 8 == 7 ? 1.12f : 0.82f;
        }

        songDuration = notes[notes.Count - 1].HitTime + 1.3f;
    }

    private void RestartSong()
    {
        songStartTime = Time.time + introDelaySeconds;
        completed = false;
        completionRewardGranted = false;
        combo = 0;
        bestCombo = 0;
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
        feedbackText = "准备开始：看准判定线，按 W A S D";
        feedbackTimer = introDelaySeconds + 1.1f;

        for (int i = 0; i < notes.Count; i++)
        {
            notes[i].State = NoteState.Pending;
        }
    }

    private void HandleLaneInput(float songTime)
    {
        for (int lane = 0; lane < laneKeys.Length; lane++)
        {
            if (Input.GetKeyDown(laneKeys[lane]))
            {
                TryHitLane(lane, songTime);
            }
        }
    }

    private void TryHitLane(int lane, float songTime)
    {
        RhythmNote bestNote = null;
        float bestAbsDelta = float.MaxValue;
        float signedDelta = 0f;

        for (int i = 0; i < notes.Count; i++)
        {
            RhythmNote note = notes[i];
            if (note.State != NoteState.Pending || note.Lane != lane)
            {
                continue;
            }

            float delta = songTime - note.HitTime;
            float absDelta = Mathf.Abs(delta);
            if (absDelta < bestAbsDelta)
            {
                bestNote = note;
                bestAbsDelta = absDelta;
                signedDelta = delta;
            }
        }

        if (bestNote == null || bestAbsDelta > goodWindowSeconds)
        {
            ShowFeedback(signedDelta < 0f ? "稍早一点，等音符靠近判定线" : "稍晚一点，提前按键");
            return;
        }

        bestNote.State = NoteState.Hit;
        combo++;
        bestCombo = Mathf.Max(bestCombo, combo);

        if (bestAbsDelta <= perfectWindowSeconds)
        {
            perfectCount++;
            gameManager.AddScore(hitScore + perfectBonus);
            ShowFeedback("Perfect  连击 " + combo);
        }
        else
        {
            goodCount++;
            gameManager.AddScore(hitScore);
            ShowFeedback("Good  连击 " + combo);
        }
    }

    private void UpdateMissedNotes(float songTime)
    {
        for (int i = 0; i < notes.Count; i++)
        {
            RhythmNote note = notes[i];
            if (note.State != NoteState.Pending)
            {
                continue;
            }

            if (songTime > note.HitTime + goodWindowSeconds)
            {
                note.State = NoteState.Missed;
                missCount++;
                combo = 0;
                ShowFeedback("Miss  放轻松，下一颗继续");
            }
        }
    }

    private void TryCompleteSong(float songTime)
    {
        if (songTime < songDuration)
        {
            return;
        }

        completed = true;
        feedbackText = "节奏复盘完成";
        feedbackTimer = 0f;

        if (!completionRewardGranted)
        {
            completionRewardGranted = true;
            gameManager.AddScore(completionScore);
            gameManager.AddHealth(completionHealthReward);
            gameManager.CompleteRun();
        }
    }

    private void ReturnToMenu()
    {
        if (gameManager != null)
        {
            gameManager.ReturnToMainMenu();
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    private float GetSongTime()
    {
        return Time.time - songStartTime;
    }

    private void ShowFeedback(string message)
    {
        feedbackText = message;
        feedbackTimer = 1.0f;
    }

    private float GetAccuracy()
    {
        if (notes.Count == 0)
        {
            return 1f;
        }

        return (perfectCount + goodCount) / (float)notes.Count;
    }

    private void OnGUI()
    {
        EnsureUiStyles();
        DrawBackground();
        DrawHud();
        DrawRhythmTrack();

        if (completed)
        {
            DrawResultPanel();
        }
    }

    private void DrawBackground()
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0.53f, 0.73f, 0.84f, 1f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), solidTexture);
        GUI.color = new Color(0.05f, 0.10f, 0.14f, 0.30f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), solidTexture);
        GUI.color = oldColor;
    }

    private void DrawHud()
    {
        float width = Mathf.Clamp(Screen.width - 140f, 780f, 1080f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, 32f, width, 136f);
        DrawUiPanel(panel, 0.88f);

        GUILayout.BeginArea(GetPaddedRect(panel, 24f, 14f));
        GUILayout.Label("Day 5 · 节奏复盘", titleStyle);
        GUILayout.Space(4f);
        GUILayout.Label("4K 下落式音游    从左到右：W  A  S  D", lineStyle);

        float progress = Mathf.Clamp01(Mathf.Max(0f, GetSongTime()) / Mathf.Max(1f, songDuration));
        string scoreLine = "得分 " + gameManager.CurrentScore
            + "   |   连击 " + combo
            + "   |   最高连击 " + bestCombo
            + "   |   准确率 " + Mathf.RoundToInt(GetAccuracy() * 100f) + "%";
        GUILayout.Label(scoreLine, primaryStyle);
        GUILayout.EndArea();

        Rect progressBack = new Rect(panel.x + 24f, panel.yMax - 18f, panel.width - 48f, 8f);
        DrawSolid(progressBack, new Color(0.12f, 0.18f, 0.22f, 0.95f));
        DrawSolid(new Rect(progressBack.x, progressBack.y, progressBack.width * progress, progressBack.height), new Color(0.20f, 0.78f, 0.52f, 1f));
    }

    private void DrawRhythmTrack()
    {
        float trackWidth = Mathf.Clamp(Screen.width - 220f, 620f, 760f);
        float trackHeight = Mathf.Clamp(Screen.height - 250f, 430f, 560f);
        Rect track = new Rect((Screen.width - trackWidth) * 0.5f, 190f, trackWidth, trackHeight);
        DrawUiPanel(track, 0.82f);

        float laneGap = 10f;
        float laneWidth = (track.width - (laneGap * 5f)) / 4f;
        float topY = track.y + 22f;
        float hitLineY = track.yMax - 86f;
        float bottomY = track.yMax - 28f;
        float songTime = GetSongTime();

        DrawSolid(new Rect(track.x + 18f, hitLineY - 3f, track.width - 36f, 6f), new Color(0.98f, 0.76f, 0.25f, 0.95f));

        for (int lane = 0; lane < 4; lane++)
        {
            Rect laneRect = new Rect(track.x + laneGap + lane * (laneWidth + laneGap), track.y + 16f, laneWidth, track.height - 32f);
            Color laneBase = Input.GetKey(laneKeys[lane])
                ? new Color(laneColors[lane].r, laneColors[lane].g, laneColors[lane].b, 0.34f)
                : new Color(0.09f, 0.15f, 0.19f, 0.72f);
            DrawSolid(laneRect, laneBase);

            Rect receptor = new Rect(laneRect.x + 12f, hitLineY - 18f, laneRect.width - 24f, 36f);
            DrawSolid(receptor, new Color(laneColors[lane].r, laneColors[lane].g, laneColors[lane].b, 0.70f));
            GUI.Label(new Rect(laneRect.x, bottomY - 28f, laneRect.width, 34f), laneLabels[lane], centerTitleStyle);
        }

        for (int i = 0; i < notes.Count; i++)
        {
            RhythmNote note = notes[i];
            if (note.State != NoteState.Pending)
            {
                continue;
            }

            float remaining = note.HitTime - songTime;
            if (remaining > noteTravelSeconds || remaining < -goodWindowSeconds)
            {
                continue;
            }

            float progress = 1f - (remaining / noteTravelSeconds);
            float y = Mathf.Lerp(topY, hitLineY, progress);
            float x = track.x + laneGap + note.Lane * (laneWidth + laneGap);
            Rect noteRect = new Rect(x + 18f, y - 15f, laneWidth - 36f, 30f);
            DrawSolid(new Rect(noteRect.x + 4f, noteRect.y + 4f, noteRect.width, noteRect.height), new Color(0f, 0f, 0f, 0.25f));
            DrawSolid(noteRect, laneColors[note.Lane]);
            GUI.Label(noteRect, laneLabels[note.Lane], centerHintStyle);
        }

        Rect feedbackRect = new Rect(track.x, track.yMax + 10f, track.width, 34f);
        GUI.Label(feedbackRect, feedbackText, centerHintStyle);
    }

    private void DrawResultPanel()
    {
        Rect panel = new Rect((Screen.width - 620f) * 0.5f, (Screen.height - 320f) * 0.5f, 620f, 320f);
        DrawUiPanel(panel, 0.94f);

        GUILayout.BeginArea(GetPaddedRect(panel, 34f, 28f));
        GUILayout.Label("节奏复盘完成", centerTitleStyle);
        GUILayout.Space(18f);
        GUILayout.Label("Perfect " + perfectCount + "    Good " + goodCount + "    Miss " + missCount, primaryStyle);
        GUILayout.Label("最高连击 " + bestCombo + "    准确率 " + Mathf.RoundToInt(GetAccuracy() * 100f) + "%", primaryStyle);
        GUILayout.Space(12f);
        GUILayout.Label("完成奖励：健康 +" + completionHealthReward + "，积分 +" + completionScore, hintStyle);
        GUILayout.Label("按 Enter 返回首页，按 R 再玩一次。", hintStyle);
        GUILayout.EndArea();
    }

    private void EnsureUiStyles()
    {
        if (solidTexture == null)
        {
            solidTexture = new Texture2D(1, 1);
            solidTexture.SetPixel(0, 0, Color.white);
            solidTexture.Apply();
            solidTexture.hideFlags = HideFlags.HideAndDontSave;
        }

        if (titleStyle != null)
        {
            return;
        }

        titleStyle = CreateLabelStyle(30, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        lineStyle = CreateLabelStyle(18, FontStyle.Normal, new Color(0.84f, 0.91f, 0.96f), TextAnchor.MiddleLeft);
        primaryStyle = CreateLabelStyle(21, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        hintStyle = CreateLabelStyle(18, FontStyle.Normal, new Color(0.73f, 0.84f, 0.92f), TextAnchor.MiddleLeft);
        centerTitleStyle = CreateLabelStyle(32, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        centerHintStyle = CreateLabelStyle(20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    private static GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = fontStyle,
            normal = { textColor = color },
            alignment = alignment,
            wordWrap = true
        };
    }

    private void DrawUiPanel(Rect rect, float alpha)
    {
        DrawSolid(new Rect(rect.x + 5f, rect.y + 5f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.22f));
        DrawSolid(rect, new Color(0.04f, 0.08f, 0.12f, alpha));
    }

    private void DrawSolid(Rect rect, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, solidTexture);
        GUI.color = oldColor;
    }

    private static Rect GetPaddedRect(Rect rect, float horizontal, float vertical)
    {
        return new Rect(rect.x + horizontal, rect.y + vertical, rect.width - horizontal * 2f, rect.height - vertical * 2f);
    }
}
