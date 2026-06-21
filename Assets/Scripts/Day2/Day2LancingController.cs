using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class Day2LancingController : MonoBehaviour
{
    private const int StepMoveCapToTrayA = 0;
    private const int StepInstallLancet = 1;
    private const int StepLancetInstalled = 2;
    private const int StepSafetyCapOnTrayB = 3;
    private const int StepPenCapReattached = 4;
    private const int StepDepthLocked = 5;
    private const int StepStripInserted = 6;
    private const int MinDepthLevel = 1;
    private const int MaxDepthLevel = 5;
    private const float DefaultPickRadiusPixels = 90f;

    [Header("Flow")]
    [SerializeField] private int currentDay = 2;
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private int scoreReward = 20;
    [SerializeField] private int healthReward = 5;
    [SerializeField] private int stageEntryStep = StepMoveCapToTrayA;
    [SerializeField] private int stageCompleteStep = StepStripInserted;
    [SerializeField] private bool allowEnterToLoadNextStage = true;
    [SerializeField] private float autoLoadDelaySeconds = 0.5f;

    [Header("Scene References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform penBody;
    [SerializeField] private Transform penCap;
    [SerializeField] private Transform lancetNeedle;
    [SerializeField] private Transform safetyCap;
    [SerializeField] private Transform penCapDock;
    [SerializeField] private Transform lancetDock;
    [SerializeField] private Transform trayPointA;
    [SerializeField] private Transform trayPointB;
    [SerializeField] private Transform triggerButton;
    [SerializeField] private Transform punctureNeedleTip;
    [SerializeField] private Transform glucoseMeter;
    [SerializeField] private Transform testStrip;
    [SerializeField] private Transform testStripDock;
    [SerializeField] private Transform testStripFinalPose;
    [SerializeField] private Transform testStripInsertProbe;
    [SerializeField] private Transform testStripAssembly;
    [SerializeField] private Transform testStripAssemblyFinalPose;
    [SerializeField] private Transform cartoonHand;
    [SerializeField] private Transform fingerDisinfectPoint;
    [SerializeField] private Transform alcoholSwab;
    [SerializeField] private Transform swabTip;
    [SerializeField] private Transform alcoholBottle;
    [SerializeField] private Transform alcoholDipPoint;

    [Header("Tuning")]
    [SerializeField] private float dragPlaneHeight = 0.82f;
    [SerializeField] private float dockThreshold = 0.16f;
    [SerializeField] private float undockThreshold = 0.05f;
    [SerializeField] private float trayDropThreshold = 0.12f;
    [SerializeField] private float punctureDistance = 0.018f;
    [SerializeField] private float punctureDuration = 0.3f;
    [SerializeField] private bool keepLancetInitialPoseOnStart = true;
    [SerializeField] private int targetDepthLevel = 3;
    [SerializeField] private bool requireTargetDepthLevel = true;
    [SerializeField] private string depthAdjustHintText = "调整到合适的挡位";
    [SerializeField] private float stripInsertThreshold = 0.09f;
    [SerializeField] private float stripInsertSnapDepth = 0.008f;
    [SerializeField] private float stripExposedLength = 0.03f;
    [SerializeField] private float swabDipThreshold = 0.1f;
    [SerializeField] private float swabDisinfectThreshold = 0.1f;
    [SerializeField] private bool autoCreateDisinfectionProps = true;
    [SerializeField] private Color swabDryColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color swabWetColor = new Color(0.67f, 0.85f, 1f, 1f);
    [SerializeField] private bool useRecordedLancetSnapPose = true;
    [SerializeField] private Vector3 recordedLancetSnapWorldPos = new Vector3(4.952f, 3.606f, 3.251f);
    [SerializeField] private Vector3 recordedLancetSnapWorldEuler = new Vector3(90f, 0f, 0f);
    [SerializeField] private Vector3 recordedLancetSnapLocalScale = new Vector3(10f, 10f, 10f);
    [SerializeField] private bool useRecordedStripSnapPose = true;
    [SerializeField] private Vector3 recordedStripSnapWorldPos = new Vector3(0.842f, 0.825f, -0.141f);
    [SerializeField] private Vector3 recordedStripSnapWorldEuler = new Vector3(0f, 251.2858f, 0f);
    [SerializeField] private Vector3 recordedStripSnapLocalScale = Vector3.one;
    [SerializeField] private bool autoResolveNiproProps = true;

    [Header("Runtime State")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private int currentStep;
    [SerializeField] private int currentDepthLevel = 1;
    [SerializeField] private bool completionRewardGranted;
    [SerializeField] private Transform draggingPart;
    [SerializeField] private Vector3 dragOffset;
    [SerializeField] private float pulseT;
    [SerializeField] private bool punctureAnimating;
    [SerializeField] private float punctureT;
    [SerializeField] private Vector3 punctureNeedleStartLocalPos;
    [SerializeField] private Vector3 punctureNeedleLocalDir = Vector3.forward;
    [SerializeField] private Vector3 triggerButtonStartLocalPos;
    [SerializeField] private Transform safetyCapOriginalParent;
    [SerializeField] private Vector3 safetyCapAttachedWorldPos;
    [SerializeField] private Quaternion safetyCapAttachedWorldRot;
    [SerializeField] private Vector3 lancetInitialWorldPos;
    [SerializeField] private Quaternion lancetInitialWorldRot;
    [SerializeField] private Vector3 lancetInitialLocalScale = Vector3.one;
    [SerializeField] private bool lancetInitialPoseCaptured;
    [SerializeField] private Vector3 stripInitialWorldPos;
    [SerializeField] private Quaternion stripInitialWorldRot;
    [SerializeField] private Vector3 stripInitialLocalScale = Vector3.one;
    [SerializeField] private bool stripInitialPoseCaptured;
    [SerializeField] private Vector3 stripAssemblyInitialLocalPosition;
    [SerializeField] private Quaternion stripAssemblyInitialLocalRotation;
    [SerializeField] private Vector3 stripAssemblyInitialLocalScale = Vector3.one;
    [SerializeField] private bool stripAssemblyInitialPoseCaptured;
    [SerializeField] private Vector3 swabInitialWorldPos;
    [SerializeField] private Quaternion swabInitialWorldRot;
    [SerializeField] private Vector3 swabInitialLocalScale = Vector3.one;
    [SerializeField] private bool swabInitialPoseCaptured;
    [SerializeField] private bool swabDipped;

    [Header("Optional UI References")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Object titleText;
    [SerializeField] private Object stepText;
    [SerializeField] private Object hintText;
    [SerializeField] private Object progressText;
    [SerializeField] private GameObject depthPanel;
    [SerializeField] private Object depthValueText;
    [SerializeField] private Object depthMinusButton;
    [SerializeField] private Object depthPlusButton;
    [SerializeField] private Object depthLockButton;
    [SerializeField] private Object completeHintText;

    private Transform lancetAssembly;
    private Transform lancetDockProbe;
    private Transform dragReferenceTransform;
    private Vector3 penCapAttachedWorldPos;
    private Quaternion penCapAttachedWorldRot = Quaternion.identity;
    private Vector3 penCapAttachedLocalScale = Vector3.one;
    private bool penCapAttachedPoseCaptured;
    private string statusText = string.Empty;
    private bool nextSceneLoadScheduled;
    private float nextSceneLoadTimer;
    private bool nextSceneLoadTriggered;
    private Texture2D uiSolidTexture;
    private GUIStyle uiTitleStyle;
    private GUIStyle uiLineStyle;
    private GUIStyle uiPrimaryStyle;
    private GUIStyle uiHintStyle;
    private GUIStyle uiButtonStyle;

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        AlignLancetColliderToVisual();
    }

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (gameManager == null)
        {
            gameManager = GameManager.EnsureInstanceForDemo();
        }

        gameManager.MarkCurrentDay(Mathf.Max(1, currentDay));
        ResolveLancetRuntimeReferences();
        ResolveMeterAndStripReferences();
        CaptureInitialPosesIfNeeded();
        TouchSerializedFieldsForWip();
        targetDepthLevel = Mathf.Clamp(targetDepthLevel, MinDepthLevel, MaxDepthLevel);
        currentDepthLevel = Mathf.Clamp(currentDepthLevel, MinDepthLevel, MaxDepthLevel);
        stageEntryStep = Mathf.Clamp(stageEntryStep, StepMoveCapToTrayA, StepStripInserted);
        stageCompleteStep = Mathf.Clamp(stageCompleteStep, stageEntryStep, StepStripInserted);

        if (currentStep < stageEntryStep)
        {
            currentStep = stageEntryStep;
        }
        else if (currentStep > stageCompleteStep)
        {
            currentStep = stageCompleteStep;
        }

        UpdateStepStatusText();
        UpdateHintVisibility();
    }

    private void Update()
    {
        if (IsCurrentStageComplete())
        {
            TryAutoLoadNextScene();
            return;
        }

        nextSceneLoadScheduled = false;
        nextSceneLoadTimer = 0f;
        nextSceneLoadTriggered = false;

        if (currentStep == StepMoveCapToTrayA)
        {
            HandleCapDragInput();
            return;
        }

        if (currentStep == StepInstallLancet)
        {
            HandleLancetDragInput();
            return;
        }

        if (currentStep == StepLancetInstalled)
        {
            HandleSafetyCapDragInput();
            return;
        }

        if (currentStep == StepSafetyCapOnTrayB)
        {
            HandleReattachPenCapInput();
            return;
        }

        if (currentStep == StepPenCapReattached)
        {
            HandleDepthAdjustInput();
            return;
        }

        if (currentStep == StepDepthLocked)
        {
            HandleTestStripInsertInput();
        }
    }

    private void OnGUI()
    {
        EnsureUiStyles();

        string operation = GetOperationText();
        if (!string.IsNullOrEmpty(operation))
        {
            float width = Mathf.Clamp(Screen.width - 140f, 720f, 980f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, 32f, width, 64f);
            DrawUiPanel(panel);

            GUILayout.BeginArea(GetPaddedRect(panel, 24f, 14f));
            GUILayout.Label(operation, uiPrimaryStyle);
            GUILayout.EndArea();
        }

        DrawDepthAdjustPanel();
    }

    private bool IsCurrentStageComplete()
    {
        return currentStep >= stageCompleteStep;
    }

    private void LoadNextSceneIfConfigured()
    {
        if (gameManager == null || string.IsNullOrWhiteSpace(nextSceneName))
        {
            return;
        }

        gameManager.LoadScene(nextSceneName);
    }

    private void TryAutoLoadNextScene()
    {
        if (!allowEnterToLoadNextStage || nextSceneLoadTriggered)
        {
            return;
        }

        if (gameManager == null || string.IsNullOrWhiteSpace(nextSceneName))
        {
            return;
        }

        if (!nextSceneLoadScheduled)
        {
            nextSceneLoadScheduled = true;
            nextSceneLoadTimer = Mathf.Max(0f, autoLoadDelaySeconds);
            return;
        }

        nextSceneLoadTimer -= Time.deltaTime;
        if (nextSceneLoadTimer > 0f)
        {
            return;
        }

        nextSceneLoadTriggered = true;
        LoadNextSceneIfConfigured();
    }

    private void HandleCapDragInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDraggingCap();
        }

        if (draggingPart != penCap)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            UpdateDraggingPart();
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishDraggingCap();
        }
    }

    private void HandleLancetDragInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDraggingLancet();
        }

        if (draggingPart != GetLancetDragTarget())
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            UpdateDraggingPart();
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishDraggingLancet();
        }
    }

    private void HandleSafetyCapDragInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDraggingSafetyCap();
        }

        if (draggingPart != safetyCap)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            UpdateDraggingPart();
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishDraggingSafetyCap();
        }
    }

    private void HandleReattachPenCapInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDraggingCap();
        }

        if (draggingPart != penCap)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            UpdateDraggingPart();
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishReattachPenCap();
        }
    }

    private void HandleDepthAdjustInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            AdjustDepthLevel(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            AdjustDepthLevel(1);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            TryLockDepthLevel();
        }
    }

    private void HandleTestStripInsertInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDraggingTestStrip();
        }

        if (draggingPart != testStrip)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            UpdateDraggingPart();
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishDraggingTestStrip();
        }
    }

    private void HandleAlcoholDisinfectionInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDraggingSwab();
        }

        if (draggingPart != alcoholSwab)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            UpdateDraggingPart();

            if (!swabDipped)
            {
                TryDipSwabWithAlcohol();
            }

            if (swabDipped)
            {
                TryDisinfectFinger();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishDraggingSwab();
        }
    }

    private void DrawDepthAdjustPanel()
    {
        if (currentStep != StepPenCapReattached)
        {
            return;
        }

        EnsureUiStyles();

        float width = Mathf.Clamp(Screen.width - 140f, 520f, 620f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, 188f, width, 240f);
        DrawUiPanel(panel);

        GUILayout.BeginArea(GetPaddedRect(panel, 24f, 18f));
        GUILayout.Label("采血深度调节", uiTitleStyle);
        GUILayout.Space(6f);
        if (requireTargetDepthLevel)
        {
            GUILayout.Label("当前 " + currentDepthLevel + " 档    目标 " + targetDepthLevel + " 档", uiPrimaryStyle);
        }
        else
        {
            GUILayout.Label(GetDepthAdjustInstructionText() + "    当前 " + currentDepthLevel + " 档", uiPrimaryStyle);
        }
        GUILayout.Space(12f);

        GUI.enabled = currentStep == StepPenCapReattached && currentDepthLevel > MinDepthLevel;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("降低", uiButtonStyle, GUILayout.Height(48f)))
        {
            AdjustDepthLevel(-1);
        }

        GUI.enabled = currentStep == StepPenCapReattached && currentDepthLevel < MaxDepthLevel;
        if (GUILayout.Button("提高", uiButtonStyle, GUILayout.Height(48f)))
        {
            AdjustDepthLevel(1);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10f);
        GUI.enabled = currentStep == StepPenCapReattached && CanLockCurrentDepthLevel();
        if (GUILayout.Button("锁定挡位", uiButtonStyle, GUILayout.Height(48f)))
        {
            TryLockDepthLevel();
        }

        GUI.enabled = true;
        GUILayout.EndArea();
    }

    private void TryBeginDraggingCap()
    {
        if (penCap == null)
        {
            return;
        }

        if (!IsPointerNearTransform(penCap, DefaultPickRadiusPixels))
        {
            return;
        }

        draggingPart = penCap;
        dragReferenceTransform = penCap;
        if (TryGetMouseWorldOnDragPlane(out Vector3 worldOnPlane))
        {
            dragOffset = dragReferenceTransform.position - worldOnPlane;
        }
        else
        {
            dragOffset = Vector3.zero;
        }
    }

    private void TryBeginDraggingLancet()
    {
        Transform dragTarget = GetLancetDragTarget();
        if (dragTarget == null)
        {
            return;
        }

        if (!IsPointerNearLancet(DefaultPickRadiusPixels))
        {
            return;
        }

        draggingPart = dragTarget;
        Transform probe = GetLancetDockProbe();
        dragReferenceTransform = probe != null ? probe : draggingPart;
        if (TryGetMouseWorldOnDragPlane(out Vector3 worldOnPlane))
        {
            dragOffset = dragReferenceTransform.position - worldOnPlane;
        }
        else
        {
            dragOffset = Vector3.zero;
        }
    }

    private void TryBeginDraggingSafetyCap()
    {
        if (safetyCap == null)
        {
            return;
        }

        if (!IsPointerNearTransform(safetyCap, DefaultPickRadiusPixels))
        {
            return;
        }

        PrepareSafetyCapForDrag();
        draggingPart = safetyCap;
        dragReferenceTransform = safetyCap;
        if (TryGetMouseWorldOnDragPlane(out Vector3 worldOnPlane))
        {
            dragOffset = dragReferenceTransform.position - worldOnPlane;
        }
        else
        {
            dragOffset = Vector3.zero;
        }
    }

    private void TryBeginDraggingTestStrip()
    {
        if (testStrip == null)
        {
            return;
        }

        if (!IsPointerNearTestStrip(DefaultPickRadiusPixels))
        {
            return;
        }

        draggingPart = testStrip;
        Transform probe = GetTestStripProbe();
        dragReferenceTransform = probe != null ? probe : testStrip;
        if (TryGetMouseWorldOnDragPlane(out Vector3 worldOnPlane))
        {
            dragOffset = dragReferenceTransform.position - worldOnPlane;
        }
        else
        {
            dragOffset = Vector3.zero;
        }
    }

    private void TryBeginDraggingSwab()
    {
        if (alcoholSwab == null)
        {
            return;
        }

        if (!IsPointerNearSwab(DefaultPickRadiusPixels))
        {
            return;
        }

        draggingPart = alcoholSwab;
        Transform probe = GetSwabProbe();
        dragReferenceTransform = probe != null ? probe : alcoholSwab;
        if (TryGetMouseWorldOnDragPlane(out Vector3 worldOnPlane))
        {
            dragOffset = dragReferenceTransform.position - worldOnPlane;
        }
        else
        {
            dragOffset = Vector3.zero;
        }
    }

    private void UpdateDraggingPart()
    {
        if (draggingPart == null)
        {
            return;
        }

        if (!TryGetMouseWorldOnDragPlane(out Vector3 worldOnPlane))
        {
            return;
        }

        Transform dragReference = dragReferenceTransform != null ? dragReferenceTransform : draggingPart;
        Vector3 targetReferencePos = worldOnPlane + dragOffset;
        draggingPart.position += targetReferencePos - dragReference.position;
    }

    private void FinishDraggingCap()
    {
        if (draggingPart != penCap || penCap == null)
        {
            ClearDraggingState();
            return;
        }

        ClearDraggingState();

        if (trayPointA != null && Vector3.Distance(penCap.position, trayPointA.position) <= trayDropThreshold)
        {
            SnapTransform(penCap, trayPointA);
            currentStep = StepInstallLancet;
            UpdateStepStatusText();
            UpdateHintVisibility();
            return;
        }

        ReturnCapToDock();
        statusText = "笔帽还没有放到左侧托盘，请对准托盘后再松开。";
    }

    private void FinishDraggingLancet()
    {
        Transform dragTarget = GetLancetDragTarget();
        if (draggingPart != dragTarget || dragTarget == null)
        {
            ClearDraggingState();
            return;
        }

        ClearDraggingState();

        Transform dockProbe = GetLancetDockProbe();
        if (lancetDock != null && dockProbe != null && Vector3.Distance(dockProbe.position, lancetDock.position) <= dockThreshold)
        {
            SnapLancetToDock();
            currentStep = StepLancetInstalled;
            UpdateStepStatusText();
            UpdateHintVisibility();
            return;
        }

        ReturnLancetToInitialPose();
        statusText = "采血针还没有对准笔身，请拖到采血笔前端的安装位。";
    }

    private void FinishDraggingSafetyCap()
    {
        if (draggingPart != safetyCap || safetyCap == null)
        {
            ClearDraggingState();
            return;
        }

        ClearDraggingState();
        if (trayPointB != null && Vector3.Distance(safetyCap.position, trayPointB.position) <= trayDropThreshold)
        {
            SnapTransform(safetyCap, trayPointB);
            currentStep = StepSafetyCapOnTrayB;
            UpdateStepStatusText();
            UpdateHintVisibility();
            return;
        }

        ReturnSafetyCapToAttachedPose();
        statusText = "保护帽还没有放到右侧托盘，请先取下并放好。";
    }

    private void FinishReattachPenCap()
    {
        if (draggingPart != penCap || penCap == null)
        {
            ClearDraggingState();
            return;
        }

        ClearDraggingState();
        if (IsNearPenCapAttachPose())
        {
            SnapPenCapToAttachPose();
            currentStep = StepPenCapReattached;
            UpdateStepStatusText();
            UpdateHintVisibility();
            return;
        }

        ReturnCapToTrayA();
        statusText = "笔帽还没有对准笔身，请从左侧托盘拖回采血笔前端。";
    }

    private void FinishDraggingTestStrip()
    {
        if (draggingPart != testStrip || testStrip == null)
        {
            ClearDraggingState();
            return;
        }

        ClearDraggingState();
        if (TrySnapTestStripIntoMeter())
        {
            currentStep = StepStripInserted;
            UpdateStepStatusText();
            UpdateHintVisibility();
            GrantCompletionRewardOnce();
            return;
        }

        ReturnTestStripToInitialPose();
        statusText = "试纸还没有插准，请把试纸前端对准血糖仪插入口。";
    }

    private void FinishDraggingSwab()
    {
        if (draggingPart != alcoholSwab || alcoholSwab == null)
        {
            ClearDraggingState();
            return;
        }

        ClearDraggingState();
        if (!swabDipped)
        {
            if (TryDipSwabWithAlcohol())
            {
                ReturnSwabToInitialPose();
                return;
            }

            ReturnSwabToInitialPose();
            statusText = "先让棉签蘸取酒精，再擦拭手指。";
            return;
        }

        if (TryDisinfectFinger())
        {
            return;
        }

        ReturnSwabToInitialPose();
        statusText = "请用蘸过酒精的棉签擦拭指尖采血位置。";
    }

    private void AdjustDepthLevel(int delta)
    {
        if (currentStep != StepPenCapReattached)
        {
            return;
        }

        int nextLevel = Mathf.Clamp(currentDepthLevel + delta, MinDepthLevel, MaxDepthLevel);
        if (nextLevel == currentDepthLevel)
        {
            return;
        }

        currentDepthLevel = nextLevel;
        statusText = string.Empty;
    }

    private string GetDepthAdjustInstructionText()
    {
        return string.IsNullOrWhiteSpace(depthAdjustHintText)
            ? "调整到合适的挡位"
            : depthAdjustHintText;
    }

    private bool CanLockCurrentDepthLevel()
    {
        if (!requireTargetDepthLevel)
        {
            return true;
        }

        int clampedTarget = Mathf.Clamp(targetDepthLevel, MinDepthLevel, MaxDepthLevel);
        return currentDepthLevel == clampedTarget;
    }

    private void TryLockDepthLevel()
    {
        if (currentStep != StepPenCapReattached)
        {
            return;
        }

        if (requireTargetDepthLevel)
        {
            int clampedTarget = Mathf.Clamp(targetDepthLevel, MinDepthLevel, MaxDepthLevel);
            if (currentDepthLevel != clampedTarget)
            {
                statusText = "还没有调到目标挡位，请先调到 " + clampedTarget + " 档。";
                return;
            }

            targetDepthLevel = clampedTarget;
        }

        currentStep = StepDepthLocked;
        UpdateStepStatusText();
        UpdateHintVisibility();
    }

    private void ReturnCapToDock()
    {
        if (penCap == null)
        {
            return;
        }

        if (penCapAttachedPoseCaptured)
        {
            float distanceToAttachPose = Vector3.Distance(penCap.position, penCapAttachedWorldPos);
            if (distanceToAttachPose > undockThreshold || distanceToAttachPose <= dockThreshold)
            {
                SnapPenCapToAttachPose();
            }
            return;
        }

        if (penCapDock == null)
        {
            return;
        }

        if (Vector3.Distance(penCap.position, penCapDock.position) > undockThreshold)
        {
            SnapTransform(penCap, penCapDock);
            return;
        }

        if (Vector3.Distance(penCap.position, penCapDock.position) <= dockThreshold)
        {
            SnapTransform(penCap, penCapDock);
        }
    }

    private void ReturnCapToTrayA()
    {
        if (penCap == null || trayPointA == null)
        {
            return;
        }

        SnapTransform(penCap, trayPointA);
    }

    private void ReturnLancetToInitialPose()
    {
        Transform dragTarget = GetLancetDragTarget();
        if (dragTarget == null)
        {
            return;
        }

        if (lancetInitialPoseCaptured)
        {
            dragTarget.SetPositionAndRotation(lancetInitialWorldPos, lancetInitialWorldRot);
            dragTarget.localScale = lancetInitialLocalScale;
            return;
        }

        if (trayPointB != null)
        {
            SnapTransform(dragTarget, trayPointB);
        }
    }

    private void PrepareSafetyCapForDrag()
    {
        if (safetyCap == null)
        {
            return;
        }

        if (safetyCapOriginalParent == null)
        {
            safetyCapOriginalParent = safetyCap.parent;
        }

        safetyCap.SetParent(null, true);
    }

    private void ReturnSafetyCapToAttachedPose()
    {
        if (safetyCap == null)
        {
            return;
        }

        safetyCap.SetPositionAndRotation(safetyCapAttachedWorldPos, safetyCapAttachedWorldRot);
        if (safetyCapOriginalParent != null)
        {
            safetyCap.SetParent(safetyCapOriginalParent, true);
        }
    }

    private void ReturnTestStripToInitialPose()
    {
        if (testStrip == null || !stripInitialPoseCaptured)
        {
            return;
        }

        testStrip.SetPositionAndRotation(stripInitialWorldPos, stripInitialWorldRot);
        testStrip.localScale = stripInitialLocalScale;

        if (testStripAssembly != null && stripAssemblyInitialPoseCaptured)
        {
            testStripAssembly.localPosition = stripAssemblyInitialLocalPosition;
            testStripAssembly.localRotation = stripAssemblyInitialLocalRotation;
            testStripAssembly.localScale = stripAssemblyInitialLocalScale;
        }
    }

    private void ReturnSwabToInitialPose()
    {
        if (alcoholSwab == null || !swabInitialPoseCaptured)
        {
            return;
        }

        alcoholSwab.SetPositionAndRotation(swabInitialWorldPos, swabInitialWorldRot);
        alcoholSwab.localScale = swabInitialLocalScale;
    }

    private bool TryDipSwabWithAlcohol()
    {
        if (swabDipped)
        {
            return true;
        }

        Transform probe = GetSwabProbe();
        if (probe == null)
        {
            return false;
        }

        float threshold = Mathf.Max(0.01f, swabDipThreshold);
        bool nearDipPoint = alcoholDipPoint != null && Vector3.Distance(probe.position, alcoholDipPoint.position) <= threshold;
        bool nearBottle = alcoholBottle != null && Vector3.Distance(probe.position, alcoholBottle.position) <= threshold * 1.35f;
        if (!nearDipPoint && !nearBottle)
        {
            return false;
        }

        swabDipped = true;
        ApplySwabTipColor(swabWetColor);
        statusText = "棉签已蘸取酒精，请擦拭指尖采血位置。";
        return true;
    }

    private bool TryDisinfectFinger()
    {
        if (currentStep != StepStripInserted || !swabDipped)
        {
            return false;
        }

        Transform probe = GetSwabProbe();
        if (probe == null || fingerDisinfectPoint == null)
        {
            return false;
        }

        float threshold = Mathf.Max(0.01f, swabDisinfectThreshold);
        if (Vector3.Distance(probe.position, fingerDisinfectPoint.position) > threshold)
        {
            return false;
        }

        currentStep = StepStripInserted;
        UpdateStepStatusText();
        UpdateHintVisibility();
        ReturnSwabToInitialPose();
        GrantCompletionRewardOnce();
        return true;
    }

    private bool TrySnapTestStripIntoMeter()
    {
        if (testStrip == null)
        {
            return false;
        }

        Transform probe = GetTestStripProbe();
        if (probe == null)
        {
            return false;
        }

        float threshold = Mathf.Max(0.01f, stripInsertThreshold);
        if (testStripDock != null)
        {
            if (Vector3.Distance(probe.position, testStripDock.position) > threshold)
            {
                return false;
            }

            if (TryApplyRecordedStripSnapPose())
            {
                return true;
            }

            ApplyTestStripExposeOffset();
            return true;
        }

        if (!TryGetMeterBounds(out Bounds meterBounds))
        {
            return false;
        }

        Vector3 closestPoint = meterBounds.ClosestPoint(probe.position);
        float distanceToSurface = Vector3.Distance(probe.position, closestPoint);
        if (distanceToSurface > threshold)
        {
            return false;
        }

        Vector3 probeToRoot = testStrip.position - probe.position;
        Vector3 fallbackDirection = glucoseMeter != null ? -glucoseMeter.forward : Vector3.back;
        Vector3 insertDirection = probeToRoot.sqrMagnitude > 0.000001f ? probeToRoot.normalized : fallbackDirection;
        Vector3 targetProbePosition = closestPoint + insertDirection * Mathf.Max(0f, stripInsertSnapDepth);
        testStrip.position += targetProbePosition - probe.position;

        if (TryApplyRecordedStripSnapPose())
        {
            return true;
        }

        ApplyTestStripExposeOffset();
        return true;
    }

    private void GrantCompletionRewardOnce()
    {
        if (completionRewardGranted || gameManager == null)
        {
            return;
        }

        completionRewardGranted = true;
        gameManager.AddScore(scoreReward);
        gameManager.AddHealth(healthReward);
    }

    private bool IsPointerNearTransform(Transform target, float radiusPixels)
    {
        Camera cam = ResolveCamera();
        if (cam == null || target == null)
        {
            return false;
        }

        Vector3 screenPoint = cam.WorldToScreenPoint(GetPointerAnchorPosition(target));
        if (screenPoint.z <= 0f)
        {
            return false;
        }

        Vector2 targetScreen = new Vector2(screenPoint.x, screenPoint.y);
        Vector2 mouseScreen = Input.mousePosition;
        return Vector2.Distance(targetScreen, mouseScreen) <= radiusPixels;
    }

    private static Vector3 GetPointerAnchorPosition(Transform target)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return target.position;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }

    private bool TryGetMouseWorldOnDragPlane(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        Camera cam = ResolveCamera();
        if (cam == null)
        {
            return false;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, dragPlaneHeight, 0f));
        if (!dragPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        worldPoint = ray.GetPoint(enter);
        return true;
    }

    private Camera ResolveCamera()
    {
        if (mainCamera != null)
        {
            return mainCamera;
        }

        mainCamera = Camera.main;
        return mainCamera;
    }

    private static void SnapTransform(Transform source, Transform target)
    {
        if (source == null || target == null)
        {
            return;
        }

        source.SetPositionAndRotation(target.position, target.rotation);
    }

    private void ClearDraggingState()
    {
        draggingPart = null;
        dragReferenceTransform = null;
    }

    private static void TintPrimitive(GameObject target, Color color)
    {
        if (target == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;
        if (material != null && material.HasProperty("_Color"))
        {
            material.color = color;
        }
    }

    private Transform GetTestStripProbe()
    {
        return testStripInsertProbe != null ? testStripInsertProbe : testStrip;
    }

    private Transform GetSwabProbe()
    {
        return swabTip != null ? swabTip : alcoholSwab;
    }

    private void ApplySwabTipColor(Color color)
    {
        Transform colorTarget = swabTip != null ? swabTip : alcoholSwab;
        if (colorTarget == null)
        {
            return;
        }

        Renderer[] renderers = colorTarget.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material mat = renderers[i].material;
            if (mat != null && mat.HasProperty("_Color"))
            {
                mat.color = color;
            }
        }
    }

    private bool TryApplyRecordedStripSnapPose()
    {
        if (!useRecordedStripSnapPose || testStrip == null)
        {
            return false;
        }

        if (testStripFinalPose != null)
        {
            CopyTransformPose(testStrip, testStripFinalPose);
            ApplyTestStripAssemblyFinalPose();
            return true;
        }

        testStrip.SetPositionAndRotation(recordedStripSnapWorldPos, Quaternion.Euler(recordedStripSnapWorldEuler));
        testStrip.localScale = recordedStripSnapLocalScale;
        ApplyTestStripAssemblyFinalPose();
        return true;
    }

    private void ApplyTestStripAssemblyFinalPose()
    {
        if (testStripAssembly != null && testStripAssemblyFinalPose != null)
        {
            CopyTransformPose(testStripAssembly, testStripAssemblyFinalPose);
        }
    }

    private static void CopyTransformPose(Transform target, Transform source)
    {
        if (target.parent == source.parent && target.parent != null)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            return;
        }

        target.SetPositionAndRotation(source.position, source.rotation);
        target.localScale = source.localScale;
    }

    private void ApplyTestStripExposeOffset()
    {
        float exposedLength = Mathf.Max(0f, stripExposedLength);
        if (exposedLength <= 0f || testStrip == null)
        {
            return;
        }

        Transform probe = GetTestStripProbe();
        if (probe == null)
        {
            return;
        }

        testStrip.position += GetTestStripExposeDirection(probe) * exposedLength;
    }

    private Vector3 GetTestStripExposeDirection(Transform probe)
    {
        if (testStrip != null && probe != null)
        {
            Vector3 probeToRoot = testStrip.position - probe.position;
            if (probeToRoot.sqrMagnitude > 0.000001f)
            {
                return probeToRoot.normalized;
            }
        }

        if (glucoseMeter != null)
        {
            return -glucoseMeter.forward;
        }

        return Vector3.back;
    }

    private bool TryGetMeterBounds(out Bounds meterBounds)
    {
        meterBounds = new Bounds();
        if (glucoseMeter == null)
        {
            return false;
        }

        Renderer[] renderers = glucoseMeter.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!hasBounds)
            {
                meterBounds = renderers[i].bounds;
                hasBounds = true;
                continue;
            }

            meterBounds.Encapsulate(renderers[i].bounds);
        }

        return hasBounds;
    }

    private bool IsNearPenCapAttachPose()
    {
        if (penCap == null)
        {
            return false;
        }

        if (penCapAttachedPoseCaptured)
        {
            return Vector3.Distance(penCap.position, penCapAttachedWorldPos) <= dockThreshold;
        }

        return penCapDock != null && Vector3.Distance(penCap.position, penCapDock.position) <= dockThreshold;
    }

    private void SnapPenCapToAttachPose()
    {
        if (penCap == null)
        {
            return;
        }

        if (penCapAttachedPoseCaptured)
        {
            penCap.SetPositionAndRotation(penCapAttachedWorldPos, penCapAttachedWorldRot);
            penCap.localScale = penCapAttachedLocalScale;
            return;
        }

        if (penCapDock != null)
        {
            SnapTransform(penCap, penCapDock);
        }
    }

    private void ResolveLancetRuntimeReferences()
    {
        lancetDockProbe = lancetNeedle;
        lancetAssembly = lancetNeedle;

        if (lancetNeedle == null || lancetNeedle.parent == null)
        {
            return;
        }

        Transform parent = lancetNeedle.parent;
        bool parentLooksLikeProcess = parent.name.Contains("Process");
        bool parentContainsSafetyCap = safetyCap != null && safetyCap.parent == parent;
        if (parentLooksLikeProcess || parentContainsSafetyCap)
        {
            lancetAssembly = parent;
        }

        Transform visualNeedle = FindChildByNameContains(lancetNeedle, "needle");
        if (visualNeedle == null)
        {
            visualNeedle = FindChildByNameContains(lancetNeedle, "tip");
        }

        if (visualNeedle != null)
        {
            lancetDockProbe = visualNeedle;
        }

        AlignLancetColliderToVisual();
    }

    private void AlignLancetColliderToVisual()
    {
        if (lancetNeedle == null)
        {
            return;
        }

        BoxCollider lancetCollider = lancetNeedle.GetComponent<BoxCollider>();
        if (lancetCollider == null)
        {
            return;
        }

        Renderer[] renderers = lancetNeedle.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        bool hasPoint = false;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            // Use renderer local bounds and transform corners into lancet local space.
            // Converting world-space AABB back to local space causes offset under rotation/non-uniform scaling.
            Bounds rendererBounds = renderer.localBounds;
            Vector3 ext = rendererBounds.extents;
            Vector3 center = rendererBounds.center;
            Vector3[] corners =
            {
                center + new Vector3( ext.x,  ext.y,  ext.z),
                center + new Vector3( ext.x,  ext.y, -ext.z),
                center + new Vector3( ext.x, -ext.y,  ext.z),
                center + new Vector3( ext.x, -ext.y, -ext.z),
                center + new Vector3(-ext.x,  ext.y,  ext.z),
                center + new Vector3(-ext.x,  ext.y, -ext.z),
                center + new Vector3(-ext.x, -ext.y,  ext.z),
                center + new Vector3(-ext.x, -ext.y, -ext.z)
            };

            for (int j = 0; j < corners.Length; j++)
            {
                Vector3 worldPoint = renderer.transform.TransformPoint(corners[j]);
                Vector3 localPoint = lancetNeedle.InverseTransformPoint(worldPoint);
                if (!hasPoint)
                {
                    min = localPoint;
                    max = localPoint;
                    hasPoint = true;
                    continue;
                }

                min = Vector3.Min(min, localPoint);
                max = Vector3.Max(max, localPoint);
            }
        }

        if (!hasPoint)
        {
            return;
        }

        Vector3 colliderSize = max - min;
        if (colliderSize.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        // Keep a tiny padding so clicks near edge still register.
        colliderSize += Vector3.one * 0.002f;
        lancetCollider.center = (min + max) * 0.5f;
        lancetCollider.size = colliderSize;
    }

    private void ResolveMeterAndStripReferences()
    {
        if (autoResolveNiproProps)
        {
            if (glucoseMeter == null)
            {
                GameObject meterGo = GameObject.Find("NiproMeterProp");
                if (meterGo == null)
                {
                    meterGo = GameObject.Find("GlucoseMeterProp");
                }

                if (meterGo != null)
                {
                    glucoseMeter = meterGo.transform;
                }
            }

            if (testStrip == null)
            {
                GameObject stripGo = GameObject.Find("NiproTestStripProp");
                if (stripGo == null)
                {
                    stripGo = GameObject.Find("TestStripProp");
                }

                if (stripGo != null)
                {
                    testStrip = stripGo.transform;
                }
            }

            if (testStripDock == null)
            {
                GameObject dockGo = GameObject.Find("NiproStripDock");
                if (dockGo == null)
                {
                    dockGo = GameObject.Find("TestStripDock");
                }

                if (dockGo == null)
                {
                    dockGo = GameObject.Find("StripDock");
                }

                if (dockGo != null)
                {
                    testStripDock = dockGo.transform;
                }
            }

            if (testStripFinalPose == null)
            {
                GameObject finalPoseGo = GameObject.Find("TestStripFinalPose");
                if (finalPoseGo != null)
                {
                    testStripFinalPose = finalPoseGo.transform;
                }
            }
        }

        if (testStripInsertProbe == null && testStrip != null)
        {
            testStripInsertProbe = FindChildByNameContains(testStrip, "TestStripInsertProbe");
            if (testStripInsertProbe == null)
            {
                testStripInsertProbe = FindChildByNameContains(testStrip, "reader");
            }
            if (testStripInsertProbe == null)
            {
                testStripInsertProbe = FindChildByNameContains(testStrip, "bloodreceiver");
            }
        }

        if (testStripAssembly == null && testStrip != null)
        {
            testStripAssembly = FindChildByNameContains(testStrip, "TestStripAssembly");
        }

        if (testStripAssemblyFinalPose == null && testStrip != null)
        {
            testStripAssemblyFinalPose = FindChildByNameContains(testStrip, "TestStripAssemblyFinalPose");
        }
    }

    private void ResolveDisinfectionRuntimeReferences()
    {
        if (autoResolveNiproProps)
        {
            if (cartoonHand == null)
            {
                GameObject handGo = GameObject.Find("CartoonHandProp");
                if (handGo == null)
                {
                    handGo = GameObject.Find("HandProp");
                }

                if (handGo != null)
                {
                    cartoonHand = handGo.transform;
                }
            }

            if (fingerDisinfectPoint == null)
            {
                GameObject fingerPointGo = GameObject.Find("FingerDisinfectPoint");
                if (fingerPointGo == null)
                {
                    fingerPointGo = GameObject.Find("FingerTipPoint");
                }

                if (fingerPointGo != null)
                {
                    fingerDisinfectPoint = fingerPointGo.transform;
                }
            }

            if (alcoholSwab == null)
            {
                GameObject swabGo = GameObject.Find("AlcoholSwabProp");
                if (swabGo == null)
                {
                    swabGo = GameObject.Find("CottonSwabProp");
                }

                if (swabGo == null)
                {
                    swabGo = GameObject.Find("SwabProp");
                }

                if (swabGo != null)
                {
                    alcoholSwab = swabGo.transform;
                }
            }

            if (alcoholBottle == null)
            {
                GameObject bottleGo = GameObject.Find("AlcoholBottleProp");
                if (bottleGo == null)
                {
                    bottleGo = GameObject.Find("AlcoholCupProp");
                }

                if (bottleGo != null)
                {
                    alcoholBottle = bottleGo.transform;
                }
            }

            if (alcoholDipPoint == null)
            {
                GameObject dipPointGo = GameObject.Find("AlcoholDipPoint");
                if (dipPointGo != null)
                {
                    alcoholDipPoint = dipPointGo.transform;
                }
            }
        }

        if (autoCreateDisinfectionProps)
        {
            EnsureDisinfectionDemoObjects();
        }

        if (swabTip == null && alcoholSwab != null)
        {
            swabTip = FindChildByNameContains(alcoholSwab, "tip");
            if (swabTip == null)
            {
                swabTip = FindChildByNameContains(alcoholSwab, "cotton");
            }
        }

        if (fingerDisinfectPoint == null && cartoonHand != null)
        {
            fingerDisinfectPoint = FindChildByNameContains(cartoonHand, "disinfect");
            if (fingerDisinfectPoint == null)
            {
                fingerDisinfectPoint = FindChildByNameContains(cartoonHand, "finger");
            }
        }

        if (alcoholDipPoint == null && alcoholBottle != null)
        {
            alcoholDipPoint = FindChildByNameContains(alcoholBottle, "dip");
        }
    }

    private void EnsureDisinfectionDemoObjects()
    {
        float tableY = trayPointA != null ? trayPointA.position.y : dragPlaneHeight;

        if (cartoonHand == null)
        {
            GameObject handRoot = new GameObject("CartoonHandProp");
            handRoot.transform.position = new Vector3(0.30f, tableY + 0.025f, 0.12f);
            handRoot.transform.rotation = Quaternion.Euler(0f, -20f, 0f);

            GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            palm.name = "Palm";
            palm.transform.SetParent(handRoot.transform, false);
            palm.transform.localPosition = Vector3.zero;
            palm.transform.localScale = new Vector3(0.16f, 0.06f, 0.14f);
            TintPrimitive(palm, new Color(1f, 0.84f, 0.72f, 1f));

            GameObject index = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            index.name = "IndexFinger";
            index.transform.SetParent(handRoot.transform, false);
            index.transform.localPosition = new Vector3(0.075f, 0.03f, 0.025f);
            index.transform.localRotation = Quaternion.Euler(0f, 0f, -60f);
            index.transform.localScale = new Vector3(0.028f, 0.08f, 0.028f);
            TintPrimitive(index, new Color(1f, 0.84f, 0.72f, 1f));

            GameObject thumb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            thumb.name = "Thumb";
            thumb.transform.SetParent(handRoot.transform, false);
            thumb.transform.localPosition = new Vector3(0.02f, 0.01f, -0.05f);
            thumb.transform.localRotation = Quaternion.Euler(10f, 0f, -5f);
            thumb.transform.localScale = new Vector3(0.026f, 0.065f, 0.026f);
            TintPrimitive(thumb, new Color(1f, 0.84f, 0.72f, 1f));

            GameObject fingerPoint = new GameObject("FingerDisinfectPoint");
            fingerPoint.transform.SetParent(handRoot.transform, false);
            fingerPoint.transform.localPosition = new Vector3(0.115f, 0.058f, 0.025f);

            cartoonHand = handRoot.transform;
            fingerDisinfectPoint = fingerPoint.transform;
        }

        if (alcoholBottle == null)
        {
            GameObject bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottle.name = "AlcoholBottleProp";
            bottle.transform.position = new Vector3(0.08f, tableY + 0.036f, 0.13f);
            bottle.transform.localScale = new Vector3(0.03f, 0.07f, 0.03f);
            TintPrimitive(bottle, new Color(0.55f, 0.8f, 1f, 1f));

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Cap";
            cap.transform.SetParent(bottle.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            cap.transform.localScale = new Vector3(0.45f, 0.18f, 0.45f);
            TintPrimitive(cap, new Color(0.85f, 0.95f, 1f, 1f));

            GameObject dipPoint = new GameObject("AlcoholDipPoint");
            dipPoint.transform.SetParent(bottle.transform, false);
            dipPoint.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            alcoholBottle = bottle.transform;
            alcoholDipPoint = dipPoint.transform;
        }
        else if (alcoholDipPoint == null)
        {
            GameObject dipPoint = new GameObject("AlcoholDipPoint");
            dipPoint.transform.SetParent(alcoholBottle, false);
            dipPoint.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            alcoholDipPoint = dipPoint.transform;
        }

        if (alcoholSwab == null)
        {
            GameObject swabRoot = new GameObject("AlcoholSwabProp");
            Vector3 startPos = trayPointB != null
                ? trayPointB.position + new Vector3(0.02f, 0.015f, 0.01f)
                : new Vector3(-0.48f, tableY + 0.015f, -0.03f);
            swabRoot.transform.position = startPos;
            swabRoot.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(swabRoot.transform, false);
            stem.transform.localPosition = Vector3.zero;
            stem.transform.localScale = new Vector3(0.006f, 0.07f, 0.006f);
            TintPrimitive(stem, new Color(0.92f, 0.92f, 0.92f, 1f));

            GameObject tipA = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tipA.name = "SwabTip";
            tipA.transform.SetParent(swabRoot.transform, false);
            tipA.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            tipA.transform.localScale = new Vector3(0.018f, 0.018f, 0.018f);
            TintPrimitive(tipA, swabDryColor);

            GameObject tipB = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tipB.name = "SwabTail";
            tipB.transform.SetParent(swabRoot.transform, false);
            tipB.transform.localPosition = new Vector3(0f, -0.07f, 0f);
            tipB.transform.localScale = new Vector3(0.018f, 0.018f, 0.018f);
            TintPrimitive(tipB, swabDryColor);

            alcoholSwab = swabRoot.transform;
            swabTip = tipA.transform;
        }
    }

    private Transform GetLancetDragTarget()
    {
        return lancetAssembly != null ? lancetAssembly : lancetNeedle;
    }

    private Transform GetLancetDockProbe()
    {
        return lancetDockProbe != null ? lancetDockProbe : GetLancetDragTarget();
    }

    private bool IsPointerNearLancet(float radiusPixels)
    {
        Transform dragTarget = GetLancetDragTarget();
        if (IsPointerNearTransform(dragTarget, radiusPixels))
        {
            return true;
        }

        Transform probe = GetLancetDockProbe();
        return probe != null && probe != dragTarget && IsPointerNearTransform(probe, radiusPixels);
    }

    private bool IsPointerNearTestStrip(float radiusPixels)
    {
        if (IsPointerNearTransform(testStrip, radiusPixels))
        {
            return true;
        }

        Transform probe = GetTestStripProbe();
        return probe != null && probe != testStrip && IsPointerNearTransform(probe, radiusPixels);
    }

    private bool IsPointerNearSwab(float radiusPixels)
    {
        if (IsPointerNearTransform(alcoholSwab, radiusPixels))
        {
            return true;
        }

        Transform probe = GetSwabProbe();
        return probe != null && probe != alcoholSwab && IsPointerNearTransform(probe, radiusPixels);
    }

    private void SnapLancetToDock()
    {
        Transform dragTarget = GetLancetDragTarget();
        Transform probe = GetLancetDockProbe();
        if (dragTarget == null || probe == null || lancetDock == null)
        {
            return;
        }

        if (TryApplyRecordedLancetSnapPose(dragTarget))
        {
            return;
        }

        if (dragTarget == probe)
        {
            SnapTransform(dragTarget, lancetDock);
            return;
        }

        dragTarget.rotation = lancetDock.rotation * Quaternion.Inverse(probe.localRotation);
        Vector3 delta = lancetDock.position - probe.position;
        dragTarget.position += delta;
    }

    private bool TryApplyRecordedLancetSnapPose(Transform dragTarget)
    {
        if (!useRecordedLancetSnapPose || dragTarget == null)
        {
            return false;
        }

        Transform snapRoot = ResolveLancetSnapRoot(dragTarget);
        if (snapRoot == null)
        {
            return false;
        }

        snapRoot.SetPositionAndRotation(recordedLancetSnapWorldPos, Quaternion.Euler(recordedLancetSnapWorldEuler));
        snapRoot.localScale = recordedLancetSnapLocalScale;
        return true;
    }

    private Transform ResolveLancetSnapRoot(Transform dragTarget)
    {
        if (lancetAssembly != null)
        {
            return lancetAssembly;
        }

        if (lancetNeedle != null && lancetNeedle.parent != null)
        {
            return lancetNeedle.parent;
        }

        return dragTarget;
    }

    private static Transform FindChildByNameContains(Transform root, string keyword)
    {
        if (root == null || string.IsNullOrEmpty(keyword))
        {
            return null;
        }

        string loweredKeyword = keyword.ToLowerInvariant();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.ToLowerInvariant().Contains(loweredKeyword))
            {
                return child;
            }

            Transform nestedMatch = FindChildByNameContains(child, keyword);
            if (nestedMatch != null)
            {
                return nestedMatch;
            }
        }

        return null;
    }

    private void UpdateStepStatusText()
    {
        switch (currentStep)
        {
            case StepMoveCapToTrayA:
            case StepInstallLancet:
            case StepLancetInstalled:
            case StepSafetyCapOnTrayB:
            case StepPenCapReattached:
                statusText = string.Empty;
                break;
            case StepDepthLocked:
                statusText = stageCompleteStep <= StepDepthLocked
                    ? "采血笔已准备好，正在进入下一阶段。"
                    : string.Empty;
                break;
            case StepStripInserted:
                statusText = "试纸已插入，正在进入下一阶段。";
                break;
            default:
                statusText = "当前流程状态异常，请重新进入场景。";
                break;
        }
    }

    private string GetOperationText()
    {
        switch (currentStep)
        {
            case StepMoveCapToTrayA:
                return "拖动笔帽到左侧托盘";
            case StepInstallLancet:
                return "拖动采血针装入采血笔";
            case StepLancetInstalled:
                return "拖动保护帽到右侧托盘";
            case StepSafetyCapOnTrayB:
                return "拖动笔帽装回采血笔";
            case StepPenCapReattached:
                return string.Empty;
            case StepDepthLocked:
                return stageCompleteStep <= StepDepthLocked
                    ? string.Empty
                    : "拖动试纸插入血糖仪";
            case StepStripInserted:
                return string.Empty;
            default:
                return string.Empty;
        }
    }

    private void EnsureUiStyles()
    {
        if (uiSolidTexture == null)
        {
            uiSolidTexture = new Texture2D(1, 1);
            uiSolidTexture.SetPixel(0, 0, Color.white);
            uiSolidTexture.Apply();
            uiSolidTexture.hideFlags = HideFlags.HideAndDontSave;
        }

        if (uiTitleStyle != null)
        {
            return;
        }

        uiTitleStyle = CreateLabelStyle(28, FontStyle.Bold, Color.white);
        uiLineStyle = CreateLabelStyle(18, FontStyle.Normal, new Color(0.84f, 0.91f, 0.96f));
        uiPrimaryStyle = CreateLabelStyle(21, FontStyle.Bold, Color.white);
        uiHintStyle = CreateLabelStyle(18, FontStyle.Normal, new Color(0.73f, 0.84f, 0.92f));
        uiButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            hover = { textColor = Color.white },
            active = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        };
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
        EnsureUiStyles();

        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.22f);
        GUI.DrawTexture(new Rect(rect.x + 5f, rect.y + 5f, rect.width, rect.height), uiSolidTexture);
        GUI.color = new Color(0.04f, 0.08f, 0.12f, 0.88f);
        GUI.DrawTexture(rect, uiSolidTexture);
        GUI.color = oldColor;
    }

    private static Rect GetPaddedRect(Rect rect, float horizontal, float vertical)
    {
        return new Rect(rect.x + horizontal, rect.y + vertical, rect.width - (horizontal * 2f), rect.height - (vertical * 2f));
    }

    private void UpdateHintVisibility()
    {
        // Dock hint object has been removed from the flow.
    }

    private void CaptureInitialPosesIfNeeded()
    {
        if (penCap != null)
        {
            penCapAttachedWorldPos = penCap.position;
            penCapAttachedWorldRot = penCap.rotation;
            penCapAttachedLocalScale = penCap.localScale;
            penCapAttachedPoseCaptured = true;
        }

        if (safetyCap != null)
        {
            safetyCapOriginalParent = safetyCap.parent;
            safetyCapAttachedWorldPos = safetyCap.position;
            safetyCapAttachedWorldRot = safetyCap.rotation;
        }

        if (testStrip != null)
        {
            stripInitialWorldPos = testStrip.position;
            stripInitialWorldRot = testStrip.rotation;
            stripInitialLocalScale = testStrip.localScale;
            stripInitialPoseCaptured = true;
        }

        if (testStripAssembly != null)
        {
            stripAssemblyInitialLocalPosition = testStripAssembly.localPosition;
            stripAssemblyInitialLocalRotation = testStripAssembly.localRotation;
            stripAssemblyInitialLocalScale = testStripAssembly.localScale;
            stripAssemblyInitialPoseCaptured = true;
        }

        if (alcoholSwab != null)
        {
            swabInitialWorldPos = alcoholSwab.position;
            swabInitialWorldRot = alcoholSwab.rotation;
            swabInitialLocalScale = alcoholSwab.localScale;
            swabInitialPoseCaptured = true;
        }

        Transform dragTarget = GetLancetDragTarget();
        if (!keepLancetInitialPoseOnStart || dragTarget == null)
        {
            return;
        }

        lancetInitialWorldPos = dragTarget.position;
        lancetInitialWorldRot = dragTarget.rotation;
        lancetInitialLocalScale = dragTarget.localScale;
        lancetInitialPoseCaptured = true;
    }

    // Keep inspector-serialized WIP fields warning-free while remaining forward-compatible.
    private void TouchSerializedFieldsForWip()
    {
        _ = nextSceneName;
        _ = scoreReward;
        _ = healthReward;
        _ = stageEntryStep;
        _ = stageCompleteStep;
        _ = allowEnterToLoadNextStage;
        _ = autoLoadDelaySeconds;
        _ = punctureDistance;
        _ = punctureDuration;
        _ = targetDepthLevel;
        _ = currentDepthLevel;
        _ = stripInsertThreshold;
        _ = stripInsertSnapDepth;
        _ = stripExposedLength;
        _ = swabDipThreshold;
        _ = swabDisinfectThreshold;
        _ = autoCreateDisinfectionProps;
        _ = swabDryColor;
        _ = swabWetColor;
        _ = useRecordedLancetSnapPose;
        _ = recordedLancetSnapWorldPos;
        _ = recordedLancetSnapWorldEuler;
        _ = recordedLancetSnapLocalScale;
        _ = useRecordedStripSnapPose;
        _ = recordedStripSnapWorldPos;
        _ = recordedStripSnapWorldEuler;
        _ = recordedStripSnapLocalScale;
    }

#if UNITY_EDITOR
    [ContextMenu("Sync Test Strip Final Pose From Prop")]
    private void SyncTestStripFinalPoseFromProp()
    {
        if (testStrip == null)
        {
            GameObject stripGo = GameObject.Find("NiproTestStripProp");
            if (stripGo != null)
            {
                testStrip = stripGo.transform;
            }
        }

        if (testStrip == null || testStripFinalPose == null)
        {
            Debug.LogWarning("[Day2LancingController] Need NiproTestStripProp and TestStripFinalPose.");
            return;
        }

        testStripFinalPose.localPosition = testStrip.localPosition;
        testStripFinalPose.localRotation = testStrip.localRotation;
        testStripFinalPose.localScale = testStrip.localScale;
        EditorUtility.SetDirty(testStripFinalPose);
        Debug.Log("[Day2LancingController] Copied strip pose to TestStripFinalPose: "
            + testStripFinalPose.localPosition + " / " + testStripFinalPose.localEulerAngles);
    }
#endif
}
