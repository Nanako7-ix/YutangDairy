using System.Collections.Generic;
using UnityEngine;

public sealed class Day2HomeController : MonoBehaviour
{
    private enum Phase
    {
        AssemblePen,
        InsertStrip,
        Disinfect,
        Lancing,
        AbsorbBlood,
        StabilizeValue,
        Failed,
        Completed
    }

    private enum DragItem
    {
        None,
        PenTip,
        LancetNeedle,
        LancetCap,
        PenBody,
        TestStrip,
        AlcoholSwab
    }

    [Header("Flow")]
    [SerializeField] private string nextSceneName = "MainMenu";

    [Header("Final Step (Old Space Mini Game)")]
    [SerializeField] private float glucoseFallPerSecond = 0.14f;
    [SerializeField] private float glucoseRisePerSecond = 0.36f;
    [SerializeField] private float targetMin = 0.42f;
    [SerializeField] private float targetMax = 0.70f;
    [SerializeField] private float requiredHoldSeconds = 5f;
    [SerializeField] private float maxFinalStepSeconds = 40f;
    [SerializeField] private KeyCode boostKey = KeyCode.Space;

    [Header("Scoring")]
    [SerializeField] private int finalStepScoreReward = 40;
    [SerializeField] private int finalStepHealthReward = 8;
    [SerializeField] private int finalStepFailPenalty = 12;

    private GameManager gameManager;
    private Phase phase = Phase.AssemblePen;
    private DragItem draggingItem = DragItem.None;
    private Vector2 dragOffsetN;

    // Pen assembly states.
    private bool tipRemoved;
    private bool lancetInstalled;
    private bool lancetCapRemoved;
    private bool tipReattached;
    private int depthLevel = 1;
    private const int TargetDepthLevel = 3;

    // Mid states.
    private bool stripInserted;
    private float disinfectProgress;
    private bool bloodReady;
    private bool bloodAbsorbed;

    // Final mini game states.
    private float glucoseValue;
    private float holdTimer;
    private float finalTimer;
    private bool completionRewardGranted;

    private string statusText;

    // Workspace & item positions (normalized to workspace rect).
    private Vector2 penHomeN = new Vector2(0.28f, 0.54f);
    private Vector2 penPosN;
    private Vector2 tipDetachedHomeN = new Vector2(0.44f, 0.54f);
    private Vector2 tipPosN;
    private Vector2 lancetTrayN = new Vector2(0.18f, 0.25f);
    private Vector2 lancetPosN;
    private Vector2 capDetachedHomeN = new Vector2(0.53f, 0.25f);
    private Vector2 lancetCapPosN;
    private Vector2 stripTrayN = new Vector2(0.18f, 0.80f);
    private Vector2 stripPosN;
    private Vector2 swabHomeN = new Vector2(0.50f, 0.80f);
    private Vector2 swabPosN;
    private Vector2 meterBodyN = new Vector2(0.78f, 0.35f);
    private Vector2 fingerN = new Vector2(0.76f, 0.68f);
    private Vector2 bloodN = new Vector2(0.78f, 0.66f);

    private Texture2D whiteTexture;
    private Texture2D woodTexture;

    private void Awake()
    {
        gameManager = GameManager.EnsureInstanceForDemo();
        gameManager.MarkCurrentDay(2);
        InitializeTextures();
        ResetEntireFlow();
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
            case Phase.Disinfect:
                UpdateDisinfectStep();
                break;
            case Phase.StabilizeValue:
                UpdateFinalMiniGame();
                break;
            case Phase.Failed:
                if (Input.GetKeyDown(KeyCode.R))
                {
                    gameManager.RegisterRetry();
                    ResetEntireFlow();
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

    private void OnGUI()
    {
        DrawWoodenTableBackground();

        Rect workspace = GetWorkspaceRect();
        DrawWorkspacePanel(workspace);
        DrawSessionHeader();
        DrawStepList(workspace);
        DrawInstructionPanel(workspace);
        DrawObjects(workspace);

        if (phase == Phase.AssemblePen && tipReattached)
        {
            DrawDepthControl(workspace);
        }

        if (phase == Phase.StabilizeValue || phase == Phase.Failed || phase == Phase.Completed)
        {
            DrawFinalMiniGamePanel(workspace);
        }

        HandleDragEvents(workspace, Event.current);
    }

    private void InitializeTextures()
    {
        if (whiteTexture == null)
        {
            whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
        }

        if (woodTexture == null)
        {
            woodTexture = BuildWoodTexture(256, 256);
        }
    }

    private void ResetEntireFlow()
    {
        phase = Phase.AssemblePen;
        draggingItem = DragItem.None;

        tipRemoved = false;
        lancetInstalled = false;
        lancetCapRemoved = false;
        tipReattached = false;
        depthLevel = 1;

        stripInserted = false;
        disinfectProgress = 0f;
        bloodReady = false;
        bloodAbsorbed = false;

        glucoseValue = 0.55f;
        holdTimer = 0f;
        finalTimer = 0f;
        completionRewardGranted = false;

        penPosN = penHomeN;
        tipPosN = PenSocketN();
        lancetPosN = lancetTrayN;
        lancetCapPosN = LancetCapAttachedN();
        stripPosN = stripTrayN;
        swabPosN = swabHomeN;

        statusText = "步骤1：先拖动采血笔笔头，将它从采血笔上取下。";
    }

    private void UpdateDisinfectStep()
    {
        Rect workspace = GetWorkspaceRect();
        Rect fingerRect = GetRect(workspace, fingerN, new Vector2(0.10f, 0.10f));
        Rect swabRect = GetRect(workspace, swabPosN, new Vector2(0.09f, 0.055f));

        if (swabRect.Overlaps(fingerRect))
        {
            disinfectProgress += Time.deltaTime;
            statusText = "步骤6：持续消毒中...";
        }
        else
        {
            disinfectProgress = Mathf.Max(0f, disinfectProgress - Time.deltaTime * 0.35f);
        }

        if (disinfectProgress >= 1.4f)
        {
            phase = Phase.Lancing;
            statusText = "步骤7：将采血笔拖到手指处并松开，完成采血。";
        }
    }

    private void UpdateFinalMiniGame()
    {
        finalTimer += Time.deltaTime;

        glucoseValue -= glucoseFallPerSecond * Time.deltaTime;
        if (Input.GetKey(boostKey))
        {
            glucoseValue += glucoseRisePerSecond * Time.deltaTime;
        }

        glucoseValue = Mathf.Clamp01(glucoseValue);

        bool insideWindow = glucoseValue >= targetMin && glucoseValue <= targetMax;
        if (insideWindow)
        {
            holdTimer += Time.deltaTime;
        }
        else
        {
            holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 0.8f);
        }

        if (glucoseValue <= 0.02f || glucoseValue >= 0.98f || finalTimer > maxFinalStepSeconds)
        {
            phase = Phase.Failed;
            gameManager.AddHealth(-finalStepFailPenalty);
            statusText = finalTimer > maxFinalStepSeconds
                ? "最终步骤超时：未能稳定读数。按 R 从头重来。"
                : "最终步骤失败：数值偏离过大。按 R 从头重来。";
            return;
        }

        if (holdTimer >= requiredHoldSeconds)
        {
            phase = Phase.Completed;
            statusText = "Day2 血糖监测全流程完成！按 Enter 继续。";
            if (!completionRewardGranted)
            {
                gameManager.AddScore(finalStepScoreReward);
                gameManager.AddHealth(finalStepHealthReward);
                completionRewardGranted = true;
            }
        }
    }

    private void HandleDragEvents(Rect workspace, Event evt)
    {
        if (evt == null)
        {
            return;
        }

        if (evt.type == EventType.MouseDown && evt.button == 0)
        {
            DragItem hitItem = HitTestDraggableItem(workspace, evt.mousePosition);
            if (hitItem != DragItem.None)
            {
                draggingItem = hitItem;
                Vector2 mouseN = ScreenToWorkspaceNormalized(workspace, evt.mousePosition);
                dragOffsetN = GetItemPosition(hitItem) - mouseN;
                evt.Use();
            }
        }
        else if (evt.type == EventType.MouseDrag && draggingItem != DragItem.None)
        {
            Vector2 mouseN = ScreenToWorkspaceNormalized(workspace, evt.mousePosition);
            SetItemPosition(draggingItem, mouseN + dragOffsetN);
            evt.Use();
        }
        else if (evt.type == EventType.MouseUp && draggingItem != DragItem.None)
        {
            ResolveDrop(draggingItem);
            draggingItem = DragItem.None;
            evt.Use();
        }
    }

    private DragItem HitTestDraggableItem(Rect workspace, Vector2 mousePosition)
    {
        DragItem allowed = AllowedDragItemForCurrentStep();
        if (allowed == DragItem.None)
        {
            return DragItem.None;
        }

        Rect itemRect = GetRectForItem(workspace, allowed);
        return itemRect.Contains(mousePosition) ? allowed : DragItem.None;
    }

    private DragItem AllowedDragItemForCurrentStep()
    {
        switch (phase)
        {
            case Phase.AssemblePen:
                if (!tipRemoved)
                {
                    return DragItem.PenTip;
                }

                if (!lancetInstalled)
                {
                    return DragItem.LancetNeedle;
                }

                if (!lancetCapRemoved)
                {
                    return DragItem.LancetCap;
                }

                if (!tipReattached)
                {
                    return DragItem.PenTip;
                }

                return DragItem.None;
            case Phase.InsertStrip:
                return DragItem.TestStrip;
            case Phase.Disinfect:
                return DragItem.AlcoholSwab;
            case Phase.Lancing:
                return DragItem.PenBody;
            case Phase.AbsorbBlood:
                return DragItem.TestStrip;
            default:
                return DragItem.None;
        }
    }

    private void ResolveDrop(DragItem item)
    {
        switch (phase)
        {
            case Phase.AssemblePen:
                ResolveAssembleDrop(item);
                break;
            case Phase.InsertStrip:
                ResolveInsertStripDrop(item);
                break;
            case Phase.Lancing:
                ResolveLancingDrop(item);
                break;
            case Phase.AbsorbBlood:
                ResolveAbsorbDrop(item);
                break;
            default:
                break;
        }
    }

    private void ResolveAssembleDrop(DragItem item)
    {
        if (!tipRemoved && item == DragItem.PenTip)
        {
            if (!IsNear(tipPosN, PenSocketN(), 0.07f))
            {
                tipRemoved = true;
                tipPosN = ClampWorkspaceN(tipPosN);
                statusText = "步骤2：将采血针拖到采血笔内，安装采血针。";
            }
            else
            {
                tipPosN = PenSocketN();
            }

            return;
        }

        if (!lancetInstalled && item == DragItem.LancetNeedle)
        {
            if (IsNear(lancetPosN, LancetSlotN(), 0.08f))
            {
                lancetInstalled = true;
                lancetPosN = LancetSlotN();
                lancetCapPosN = LancetCapAttachedN();
                statusText = "步骤3：拖走采血针的保护头。";
            }
            else
            {
                lancetPosN = lancetTrayN;
            }

            return;
        }

        if (!lancetCapRemoved && item == DragItem.LancetCap)
        {
            if (!IsNear(lancetCapPosN, LancetCapAttachedN(), 0.08f))
            {
                lancetCapRemoved = true;
                lancetCapPosN = capDetachedHomeN;
                statusText = "步骤4：将笔头装回采血笔。";
            }
            else
            {
                lancetCapPosN = LancetCapAttachedN();
            }

            return;
        }

        if (!tipReattached && item == DragItem.PenTip)
        {
            if (IsNear(tipPosN, PenSocketN(), 0.07f))
            {
                tipReattached = true;
                tipPosN = PenSocketN();
                statusText = "步骤5：调节采血笔挡位到 3，并点击“锁定挡位”。";
            }
            else
            {
                tipPosN = tipDetachedHomeN;
            }
        }
    }

    private void ResolveInsertStripDrop(DragItem item)
    {
        if (item != DragItem.TestStrip)
        {
            return;
        }

        if (IsNear(stripPosN, MeterSlotN(), 0.08f))
        {
            stripInserted = true;
            stripPosN = StripInsertedN();
            phase = Phase.Disinfect;
            statusText = "步骤6：用酒精棉消毒手指区域。";
        }
        else
        {
            stripPosN = stripTrayN;
        }
    }

    private void ResolveLancingDrop(DragItem item)
    {
        if (item != DragItem.PenBody)
        {
            return;
        }

        if (IsNear(penPosN, fingerN, 0.11f))
        {
            bloodReady = true;
            phase = Phase.AbsorbBlood;
            statusText = "步骤8：将试纸前端拖到血滴处吸血。";
            penPosN = penHomeN;
        }
        else
        {
            penPosN = penHomeN;
        }
    }

    private void ResolveAbsorbDrop(DragItem item)
    {
        if (item != DragItem.TestStrip)
        {
            return;
        }

        Vector2 stripTipN = stripPosN + new Vector2(0.09f, 0f);
        if (IsNear(stripTipN, bloodN, 0.08f))
        {
            bloodAbsorbed = true;
            phase = Phase.StabilizeValue;
            glucoseValue = 0.55f;
            holdTimer = 0f;
            finalTimer = 0f;
            statusText = "最后一步：按住 Space 稳定读数（原空格玩法）。";
            stripPosN = StripInsertedN();
        }
        else
        {
            stripPosN = StripInsertedN();
        }
    }

    private void DrawWoodenTableBackground()
    {
        if (woodTexture != null)
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), woodTexture, ScaleMode.StretchToFill);
        }
    }

    private void DrawWorkspacePanel(Rect workspace)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.18f);
        GUI.DrawTexture(new Rect(workspace.x - 6f, workspace.y - 6f, workspace.width + 12f, workspace.height + 12f), whiteTexture);
        GUI.color = new Color(0.96f, 0.96f, 0.96f, 0.96f);
        GUI.DrawTexture(workspace, whiteTexture);
        GUI.color = oldColor;
    }

    private void DrawSessionHeader()
    {
        GUILayout.BeginArea(new Rect(12f, 10f, 700f, 42f), GUI.skin.box);
        GUILayout.Label(
            string.Format(
                "Day {0} | 健康值: {1} | 健康积分: {2} | 重试: {3}",
                gameManager.CurrentDay,
                gameManager.CurrentHealth,
                gameManager.CurrentScore,
                gameManager.RetryCount));
        GUILayout.EndArea();
    }

    private void DrawStepList(Rect workspace)
    {
        GUILayout.BeginArea(new Rect(workspace.x, workspace.y - 116f, workspace.width, 106f), GUI.skin.box);
        GUILayout.Label("Day2 血糖监测流程（Demo）");
        GUILayout.Label(GetStepStatusLine());
        GUILayout.EndArea();
    }

    private void DrawInstructionPanel(Rect workspace)
    {
        GUILayout.BeginArea(new Rect(workspace.x, workspace.yMax + 8f, workspace.width, 72f), GUI.skin.box);
        GUILayout.Label(statusText);

        if (phase == Phase.Failed)
        {
            GUILayout.Label("按 R 重新开始 Day2 全流程。");
        }
        else if (phase == Phase.Completed)
        {
            GUILayout.Label("按 Enter 继续。");
        }
        else
        {
            GUILayout.Label("Esc 返回主菜单。");
        }

        GUILayout.EndArea();
    }

    private void DrawObjects(Rect workspace)
    {
        // Static objects.
        DrawItem(workspace, meterBodyN, new Vector2(0.24f, 0.16f), new Color(0.80f, 0.85f, 0.90f), "血糖仪");
        DrawItem(workspace, fingerN, new Vector2(0.10f, 0.10f), new Color(0.95f, 0.73f, 0.66f), "手指");
        DrawItem(workspace, penPosN, new Vector2(0.24f, 0.08f), new Color(0.30f, 0.45f, 0.62f), "采血笔");

        // Dynamic objects.
        DrawItem(workspace, tipPosN, new Vector2(0.06f, 0.05f), new Color(0.24f, 0.34f, 0.46f), "笔头");
        DrawItem(workspace, lancetPosN, new Vector2(0.10f, 0.024f), new Color(0.78f, 0.78f, 0.78f), "采血针");
        if (!lancetCapRemoved)
        {
            DrawItem(workspace, lancetCapPosN, new Vector2(0.06f, 0.04f), new Color(0.96f, 0.90f, 0.70f), "保护帽");
        }

        DrawItem(workspace, stripPosN, new Vector2(0.18f, 0.032f), new Color(0.97f, 0.97f, 0.97f), "试纸");
        DrawItem(workspace, swabPosN, new Vector2(0.09f, 0.055f), new Color(0.62f, 0.92f, 1f), "酒精棉");

        // Meter slot hint.
        Rect slotRect = GetRect(workspace, MeterSlotN(), new Vector2(0.08f, 0.03f));
        Color oldColor = GUI.color;
        GUI.color = new Color(0.26f, 0.26f, 0.30f, 0.9f);
        GUI.DrawTexture(slotRect, whiteTexture);
        GUI.color = oldColor;
        GUI.Label(new Rect(slotRect.x, slotRect.y - 16f, 80f, 18f), "试纸口");

        if (bloodReady && !bloodAbsorbed)
        {
            DrawItem(workspace, bloodN, new Vector2(0.04f, 0.04f), new Color(0.88f, 0.14f, 0.18f), "血滴");
        }
    }

    private void DrawDepthControl(Rect workspace)
    {
        Rect panel = new Rect(workspace.xMax - 220f, workspace.y + 12f, 200f, 110f);
        GUI.Box(panel, "挡位调节");
        GUI.Label(new Rect(panel.x + 12f, panel.y + 28f, 180f, 22f), "当前挡位: " + depthLevel);
        GUI.Label(new Rect(panel.x + 12f, panel.y + 46f, 180f, 22f), "目标挡位: " + TargetDepthLevel);

        if (GUI.Button(new Rect(panel.x + 12f, panel.y + 74f, 36f, 24f), "-"))
        {
            depthLevel = Mathf.Max(1, depthLevel - 1);
        }

        if (GUI.Button(new Rect(panel.x + 54f, panel.y + 74f, 36f, 24f), "+"))
        {
            depthLevel = Mathf.Min(5, depthLevel + 1);
        }

        GUI.enabled = depthLevel == TargetDepthLevel;
        if (GUI.Button(new Rect(panel.x + 96f, panel.y + 74f, 92f, 24f), "锁定挡位"))
        {
            phase = Phase.InsertStrip;
            statusText = "步骤5：将试纸插入血糖仪。";
        }

        GUI.enabled = true;
    }

    private void DrawFinalMiniGamePanel(Rect workspace)
    {
        Rect panel = new Rect(workspace.x + 16f, workspace.y + 14f, workspace.width - 32f, 120f);
        GUI.Box(panel, "最后一步：稳定读数（原空格玩法）");
        GUI.Label(new Rect(panel.x + 12f, panel.y + 26f, panel.width - 24f, 20f), "按住 Space 提升数值，稳定在绿色窗口内。");

        Rect bar = new Rect(panel.x + 12f, panel.y + 54f, panel.width - 24f, 26f);
        DrawMonitorBar(bar);

        GUI.Label(
            new Rect(panel.x + 12f, panel.y + 84f, panel.width - 24f, 20f),
            string.Format("窗口内累计: {0:F1}/{1:F1}s    剩余: {2:F1}s", holdTimer, requiredHoldSeconds, Mathf.Max(0f, maxFinalStepSeconds - finalTimer)));
    }

    private void DrawMonitorBar(Rect rect)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.35f);
        GUI.DrawTexture(rect, whiteTexture);

        Rect targetRect = new Rect(
            rect.x + rect.width * targetMin,
            rect.y + 2f,
            rect.width * (targetMax - targetMin),
            rect.height - 4f);
        GUI.color = new Color(0.2f, 0.75f, 0.3f, 0.65f);
        GUI.DrawTexture(targetRect, whiteTexture);

        Rect fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * glucoseValue, rect.height - 4f);
        GUI.color = new Color(0.25f, 0.55f, 0.9f, 0.88f);
        GUI.DrawTexture(fillRect, whiteTexture);

        Rect markerRect = new Rect(rect.x + rect.width * glucoseValue - 2f, rect.y - 3f, 4f, rect.height + 6f);
        GUI.color = new Color(0.95f, 0.2f, 0.2f, 0.95f);
        GUI.DrawTexture(markerRect, whiteTexture);
        GUI.color = oldColor;
    }

    private void DrawItem(Rect workspace, Vector2 centerN, Vector2 sizeN, Color color, string label)
    {
        Rect rect = GetRect(workspace, centerN, sizeN);
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = Color.black;
        GUI.Label(new Rect(rect.x + 2f, rect.y + 1f, rect.width - 4f, rect.height - 2f), label);
        GUI.color = oldColor;
    }

    private Rect GetWorkspaceRect()
    {
        float width = Mathf.Min(980f, Screen.width - 40f);
        float height = Mathf.Min(560f, Screen.height - 210f);
        float x = (Screen.width - width) * 0.5f;
        float y = 86f;
        return new Rect(x, y, width, height);
    }

    private Rect GetRect(Rect workspace, Vector2 centerN, Vector2 sizeN)
    {
        float width = workspace.width * sizeN.x;
        float height = workspace.height * sizeN.y;
        float x = workspace.x + workspace.width * centerN.x - width * 0.5f;
        float y = workspace.y + workspace.height * centerN.y - height * 0.5f;
        return new Rect(x, y, width, height);
    }

    private Rect GetRectForItem(Rect workspace, DragItem item)
    {
        switch (item)
        {
            case DragItem.PenTip:
                return GetRect(workspace, tipPosN, new Vector2(0.06f, 0.05f));
            case DragItem.LancetNeedle:
                return GetRect(workspace, lancetPosN, new Vector2(0.10f, 0.024f));
            case DragItem.LancetCap:
                return GetRect(workspace, lancetCapPosN, new Vector2(0.06f, 0.04f));
            case DragItem.PenBody:
                return GetRect(workspace, penPosN, new Vector2(0.24f, 0.08f));
            case DragItem.TestStrip:
                return GetRect(workspace, stripPosN, new Vector2(0.18f, 0.032f));
            case DragItem.AlcoholSwab:
                return GetRect(workspace, swabPosN, new Vector2(0.09f, 0.055f));
            default:
                return Rect.zero;
        }
    }

    private Vector2 GetItemPosition(DragItem item)
    {
        switch (item)
        {
            case DragItem.PenTip:
                return tipPosN;
            case DragItem.LancetNeedle:
                return lancetPosN;
            case DragItem.LancetCap:
                return lancetCapPosN;
            case DragItem.PenBody:
                return penPosN;
            case DragItem.TestStrip:
                return stripPosN;
            case DragItem.AlcoholSwab:
                return swabPosN;
            default:
                return Vector2.zero;
        }
    }

    private void SetItemPosition(DragItem item, Vector2 valueN)
    {
        valueN = ClampWorkspaceN(valueN);
        switch (item)
        {
            case DragItem.PenTip:
                tipPosN = valueN;
                break;
            case DragItem.LancetNeedle:
                lancetPosN = valueN;
                break;
            case DragItem.LancetCap:
                lancetCapPosN = valueN;
                break;
            case DragItem.PenBody:
                penPosN = valueN;
                break;
            case DragItem.TestStrip:
                stripPosN = valueN;
                break;
            case DragItem.AlcoholSwab:
                swabPosN = valueN;
                break;
        }
    }

    private Vector2 ScreenToWorkspaceNormalized(Rect workspace, Vector2 screen)
    {
        float x = (screen.x - workspace.x) / Mathf.Max(1f, workspace.width);
        float y = (screen.y - workspace.y) / Mathf.Max(1f, workspace.height);
        return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    private Vector2 ClampWorkspaceN(Vector2 posN)
    {
        return new Vector2(Mathf.Clamp(posN.x, 0.02f, 0.98f), Mathf.Clamp(posN.y, 0.04f, 0.96f));
    }

    private bool IsNear(Vector2 a, Vector2 b, float threshold)
    {
        return Vector2.Distance(a, b) <= threshold;
    }

    private Vector2 PenSocketN()
    {
        return penPosN + new Vector2(0.12f, 0f);
    }

    private Vector2 LancetSlotN()
    {
        return penPosN + new Vector2(0.03f, 0f);
    }

    private Vector2 LancetCapAttachedN()
    {
        return LancetSlotN() + new Vector2(0.04f, 0f);
    }

    private Vector2 MeterSlotN()
    {
        return meterBodyN + new Vector2(0.08f, -0.01f);
    }

    private Vector2 StripInsertedN()
    {
        return MeterSlotN() - new Vector2(0.06f, 0f);
    }

    private string GetStepStatusLine()
    {
        string[] labels =
        {
            "1拆笔头",
            "2装采血针",
            "3去保护帽",
            "4装回笔头+挡位",
            "5插试纸",
            "6消毒",
            "7采血",
            "8试纸吸血",
            "9稳定读数"
        };

        int doneCount = 0;
        if (tipRemoved) doneCount = 1;
        if (lancetInstalled) doneCount = 2;
        if (lancetCapRemoved) doneCount = 3;
        if (tipReattached && phase != Phase.AssemblePen) doneCount = 4;
        if (stripInserted) doneCount = 5;
        if (phase == Phase.Lancing || phase == Phase.AbsorbBlood || phase == Phase.StabilizeValue || phase == Phase.Failed || phase == Phase.Completed) doneCount = 6;
        if (bloodReady) doneCount = 7;
        if (bloodAbsorbed) doneCount = 8;
        if (phase == Phase.Completed) doneCount = 9;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < labels.Length; i++)
        {
            sb.Append(i < doneCount ? "✓ " : "· ");
            sb.Append(labels[i]);
            if (i < labels.Length - 1)
            {
                sb.Append("   ");
            }
        }

        return sb.ToString();
    }

    private Texture2D BuildWoodTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color baseColor = new Color(0.50f, 0.31f, 0.18f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float n1 = Mathf.PerlinNoise(x * 0.035f, y * 0.01f);
                float n2 = Mathf.PerlinNoise((x + 100) * 0.08f, (y + 40) * 0.015f);
                float ring = Mathf.Sin((x * 0.09f) + (y * 0.02f) + n2 * 2.2f) * 0.12f;
                float brightness = Mathf.Clamp01(0.55f + n1 * 0.35f + ring);
                Color c = new Color(
                    baseColor.r * (0.75f + brightness * 0.4f),
                    baseColor.g * (0.75f + brightness * 0.35f),
                    baseColor.b * (0.75f + brightness * 0.25f));
                texture.SetPixel(x, y, c);
            }
        }

        texture.Apply();
        return texture;
    }
}
