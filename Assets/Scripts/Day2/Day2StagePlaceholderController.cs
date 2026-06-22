using System.Collections;
using UnityEngine;

public sealed class Day2StagePlaceholderController : MonoBehaviour
{
    [SerializeField] private int currentDay = 2;
    [SerializeField] private int stageIndex = 3;
    [SerializeField] private string stageTitle = "阶段3：扎手指测血糖";
    [SerializeField] [TextArea(2, 4)] private string stageDescription = "将棉签拖到中指附近，完成消毒动作。";
    [SerializeField] private string nextSceneName = "Day2_Stage4_SpaceMiniGame";
    [SerializeField] private bool allowEnterToNextScene = true;
    [SerializeField] private float autoLoadDelaySeconds = 0.5f;

    [Header("Stage3 Swab Interaction")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform cottonSwab;
    [SerializeField] private Transform handModel;
    [SerializeField] private Transform middleFingerPoint;
    [SerializeField] private float swabPickRadiusPixels = 100f;
    [SerializeField] private float swabTriggerDistance = 0.18f;
    [SerializeField] private float swabWipeAmplitude = 0.03f;
    [SerializeField] private float swabWipeHalfCycleSeconds = 0.14f;
    [SerializeField] private float swabReturnSeconds = 0.22f;
    [SerializeField] private Transform lancingPen;
    [SerializeField] private Transform penTargetPoint;
    [SerializeField] private float penPickRadiusPixels = 120f;
    [SerializeField] private float penSnapDistance = 0.6f;
    [SerializeField] private float penReturnSeconds = 0.3f;

    [Header("Drag Arrow UI Offsets (Pixels)")]
    [SerializeField] private Day2DragArrowOffsets swabArrows =
        new Day2DragArrowOffsets(Vector2.zero, new Vector2(-22f, 0f));
    [SerializeField] private Day2DragArrowOffsets penArrows =
        new Day2DragArrowOffsets(Vector2.zero, new Vector2(-22f, 0f));

    [SerializeField] private GameManager gameManager;

    private bool autoLoadQueued;
    private float autoLoadTimer;
    private bool stage3InteractionCompleted;
    private bool draggingSwab;
    private bool swabAnimating;
    private Vector3 dragOffset;
    private float dragPlaneHeight;
    private Vector3 swabInitialWorldPos;
    private Quaternion swabInitialWorldRot = Quaternion.identity;
    private Vector3 swabInitialLocalScale = Vector3.one;
    private bool swabInitialPoseCaptured;
    private bool swabStepCompleted;
    private bool draggingPen;
    private bool penPlacedAtTarget;
    private bool penAnimating;
    private Vector3 penInitialWorldPos;
    private Quaternion penInitialWorldRot = Quaternion.identity;
    private Vector3 penInitialLocalScale = Vector3.one;
    private bool penInitialPoseCaptured;
    private Coroutine swabRoutine;
    private Coroutine penRoutine;
    private Texture2D uiSolidTexture;
    private GUIStyle uiTitleStyle;
    private GUIStyle uiLineStyle;
    private GUIStyle uiPrimaryStyle;
    private GUIStyle uiHintStyle;
    private Day2DragArrowOverlay dragArrowOverlay;
    private Day4StageHintGate hintGate;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.EnsureInstanceForDemo();
        }

        gameManager.MarkCurrentDay(Mathf.Max(1, currentDay));
        hintGate = Day4StageHintGate.Ensure(gameObject, currentDay, gameManager);
        ResolveRuntimeReferences();
        dragArrowOverlay = Day2DragArrowOverlay.Ensure(gameObject, mainCamera);
        CaptureSwabInitialPose();
        CapturePenInitialPose();
        swabStepCompleted = false;
        draggingPen = false;
        penPlacedAtTarget = false;
        penAnimating = false;

        if (!IsStage3())
        {
            stage3InteractionCompleted = true;
        }
        else
        {
            stage3InteractionCompleted = CanRunStage3Interaction() == false;
        }
    }

    private void Update()
    {
        UpdateDragArrowTargets();

        if (IsStage3())
        {
            if (!swabStepCompleted)
            {
                HandleStage3SwabInteraction();
            }
            else
            {
                HandleStage3PenStep();
            }

            if (!stage3InteractionCompleted)
            {
                autoLoadQueued = false;
                autoLoadTimer = Mathf.Max(0f, autoLoadDelaySeconds);
                return;
            }
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

    private void UpdateDragArrowTargets()
    {
        if (dragArrowOverlay == null || !IsStage3() || stage3InteractionCompleted || !IsGuidanceVisible())
        {
            dragArrowOverlay?.Hide();
            return;
        }

        if (!swabStepCompleted && cottonSwab != null && TryGetMiddleFingerAnchor(out Vector3 fingerAnchor))
        {
            dragArrowOverlay.Show(
                Day2DragArrowOverlay.GetVisualCenter(cottonSwab),
                fingerAnchor,
                swabArrows.start,
                swabArrows.end);
            return;
        }

        if (swabStepCompleted && !penPlacedAtTarget && lancingPen != null && penTargetPoint != null)
        {
            dragArrowOverlay.Show(
                Day2DragArrowOverlay.GetVisualCenter(lancingPen),
                penTargetPoint.position,
                penArrows.start,
                penArrows.end);
            return;
        }

        dragArrowOverlay.Hide();
    }

    private void HandleStage3SwabInteraction()
    {
        if (stage3InteractionCompleted || swabAnimating || swabStepCompleted)
        {
            return;
        }

        if (!CanRunStage3Interaction())
        {
            // Fall back to legacy placeholder behavior if required objects are missing.
            stage3InteractionCompleted = true;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDraggingSwab();
        }

        if (draggingSwab && Input.GetMouseButton(0))
        {
            UpdateDraggingSwab();
        }

        if (draggingSwab && Input.GetMouseButtonUp(0))
        {
            FinishDraggingSwab();
        }
    }

    private void TryBeginDraggingSwab()
    {
        if (cottonSwab == null || !IsPointerNearTransform(cottonSwab, swabPickRadiusPixels))
        {
            return;
        }

        draggingSwab = true;
        dragPlaneHeight = cottonSwab.position.y;
        if (TryGetMouseWorldOnPlane(dragPlaneHeight, out Vector3 mouseOnPlane))
        {
            dragOffset = cottonSwab.position - mouseOnPlane;
        }
        else
        {
            dragOffset = Vector3.zero;
        }
    }

    private void UpdateDraggingSwab()
    {
        if (cottonSwab == null)
        {
            draggingSwab = false;
            return;
        }

        if (!TryGetMouseWorldOnPlane(dragPlaneHeight, out Vector3 mouseOnPlane))
        {
            return;
        }

        cottonSwab.position = mouseOnPlane + dragOffset;
    }

    private void FinishDraggingSwab()
    {
        draggingSwab = false;
        if (cottonSwab == null)
        {
            return;
        }

        if (!TryGetMiddleFingerAnchor(out Vector3 middleFingerAnchor))
        {
            ReturnSwabToInitialPose();
            return;
        }

        float triggerDistance = Mathf.Max(0.02f, swabTriggerDistance);
        if (Vector3.Distance(cottonSwab.position, middleFingerAnchor) > triggerDistance)
        {
            ReturnSwabToInitialPose();
            return;
        }

        if (swabRoutine != null)
        {
            StopCoroutine(swabRoutine);
        }

        swabRoutine = StartCoroutine(PlaySwabWipeRoutine(middleFingerAnchor));
    }

    private IEnumerator PlaySwabWipeRoutine(Vector3 middleFingerAnchor)
    {
        swabAnimating = true;
        Vector3 basePos = middleFingerAnchor + new Vector3(0f, 0.015f, 0f);
        cottonSwab.position = basePos;

        int cycleCount = Random.Range(2, 4);
        float halfCycle = Mathf.Max(0.05f, swabWipeHalfCycleSeconds);
        float amplitude = Mathf.Max(0.005f, swabWipeAmplitude);

        for (int i = 0; i < cycleCount; i++)
        {
            yield return MoveSwabTo(basePos + (Vector3.up * amplitude), halfCycle);
            yield return MoveSwabTo(basePos - (Vector3.up * amplitude), halfCycle);
        }

        if (swabInitialPoseCaptured)
        {
            yield return MoveSwabTo(swabInitialWorldPos, Mathf.Max(0.05f, swabReturnSeconds));
            cottonSwab.rotation = swabInitialWorldRot;
            cottonSwab.localScale = swabInitialLocalScale;
        }

        swabAnimating = false;
        swabStepCompleted = true;
        swabRoutine = null;
    }

    private void HandleStage3PenStep()
    {
        if (stage3InteractionCompleted || !swabStepCompleted || penAnimating)
        {
            return;
        }

        if (lancingPen == null || !penInitialPoseCaptured)
        {
            stage3InteractionCompleted = true;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDraggingPen();
        }

        if (draggingPen && Input.GetMouseButton(0))
        {
            UpdateDraggingPen();
        }

        if (draggingPen && Input.GetMouseButtonUp(0))
        {
            draggingPen = false;
            TrySnapPenToTarget();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            draggingPen = false;
            if (penRoutine != null)
            {
                StopCoroutine(penRoutine);
            }

            penRoutine = StartCoroutine(ReturnPenToInitialRoutine());
        }
    }

    private void TrySnapPenToTarget()
    {
        if (lancingPen == null || penTargetPoint == null)
        {
            return;
        }

        Vector3 penPos = lancingPen.position;
        Vector3 targetPos = penTargetPoint.position;
        Vector2 penFlat = new Vector2(penPos.x, penPos.z);
        Vector2 targetFlat = new Vector2(targetPos.x, targetPos.z);
        if (Vector2.Distance(penFlat, targetFlat) > Mathf.Max(0.05f, penSnapDistance))
        {
            return;
        }

        lancingPen.position = targetPos;
        penPlacedAtTarget = true;
    }

    private void TryBeginDraggingPen()
    {
        if (lancingPen == null || !IsPointerNearTransform(lancingPen, penPickRadiusPixels))
        {
            return;
        }

        draggingPen = true;
        dragPlaneHeight = lancingPen.position.y;
        if (TryGetMouseWorldOnPlane(dragPlaneHeight, out Vector3 mouseOnPlane))
        {
            dragOffset = lancingPen.position - mouseOnPlane;
        }
        else
        {
            dragOffset = Vector3.zero;
        }
    }

    private void UpdateDraggingPen()
    {
        if (lancingPen == null)
        {
            draggingPen = false;
            return;
        }

        if (!TryGetMouseWorldOnPlane(dragPlaneHeight, out Vector3 mouseOnPlane))
        {
            return;
        }

        lancingPen.position = mouseOnPlane + dragOffset;
    }

    private IEnumerator ReturnPenToInitialRoutine()
    {
        if (lancingPen == null || !penInitialPoseCaptured)
        {
            penAnimating = false;
            stage3InteractionCompleted = true;
            penRoutine = null;
            yield break;
        }

        penAnimating = true;
        Vector3 startPos = lancingPen.position;
        Quaternion startRot = lancingPen.rotation;
        float duration = Mathf.Max(0.05f, penReturnSeconds);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            lancingPen.position = Vector3.Lerp(startPos, penInitialWorldPos, p);
            lancingPen.rotation = Quaternion.Slerp(startRot, penInitialWorldRot, p);
            yield return null;
        }

        lancingPen.SetPositionAndRotation(penInitialWorldPos, penInitialWorldRot);
        lancingPen.localScale = penInitialLocalScale;
        penAnimating = false;
        stage3InteractionCompleted = true;
        penRoutine = null;
    }

    private IEnumerator MoveSwabTo(Vector3 targetPos, float duration)
    {
        if (cottonSwab == null)
        {
            yield break;
        }

        Vector3 startPos = cottonSwab.position;
        if (duration <= 0.0001f)
        {
            cottonSwab.position = targetPos;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            cottonSwab.position = Vector3.Lerp(startPos, targetPos, p);
            yield return null;
        }
    }

    private void ResolveRuntimeReferences()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (cottonSwab == null)
        {
            GameObject swabGo = GameObject.Find("CottonSwabProp");
            if (swabGo == null)
            {
                swabGo = GameObject.Find("DisinfectionCottonSwab");
            }

            if (swabGo == null)
            {
                swabGo = GameObject.Find("AlcoholSwabProp");
            }

            if (swabGo != null)
            {
                cottonSwab = swabGo.transform;
            }
        }

        if (handModel == null)
        {
            GameObject handGo = GameObject.Find("CartoonHandProp");
            if (handGo != null)
            {
                handModel = handGo.transform;
            }
        }

        if (middleFingerPoint == null)
        {
            GameObject pointGo = GameObject.Find("MiddleFingerPoint");
            if (pointGo != null)
            {
                middleFingerPoint = pointGo.transform;
            }
        }

        if (middleFingerPoint == null && handModel != null && TryGetRendererBounds(handModel, out Bounds handBounds))
        {
            GameObject autoPoint = new GameObject("MiddleFingerPoint");
            autoPoint.transform.position = handBounds.center + (Vector3.up * handBounds.extents.y * 0.86f);
            middleFingerPoint = autoPoint.transform;
        }

        if (lancingPen == null)
        {
            GameObject penGo = GameObject.Find("OriginalLancingDevice");
            if (penGo == null)
            {
                penGo = GameObject.Find("OriginalLancingDevice_Body");
            }

            if (penGo != null)
            {
                lancingPen = penGo.transform;
            }
        }

        if (penTargetPoint == null)
        {
            GameObject penTargetGo = GameObject.Find("PenTargetPoint");
            if (penTargetGo != null)
            {
                penTargetPoint = penTargetGo.transform;
            }
        }
    }

    private void CaptureSwabInitialPose()
    {
        if (cottonSwab == null)
        {
            swabInitialPoseCaptured = false;
            return;
        }

        swabInitialPoseCaptured = true;
        swabInitialWorldPos = cottonSwab.position;
        swabInitialWorldRot = cottonSwab.rotation;
        swabInitialLocalScale = cottonSwab.localScale;
    }

    private void ReturnSwabToInitialPose()
    {
        if (cottonSwab == null || !swabInitialPoseCaptured)
        {
            return;
        }

        cottonSwab.SetPositionAndRotation(swabInitialWorldPos, swabInitialWorldRot);
        cottonSwab.localScale = swabInitialLocalScale;
    }

    private void CapturePenInitialPose()
    {
        if (lancingPen == null)
        {
            penInitialPoseCaptured = false;
            return;
        }

        penInitialPoseCaptured = true;
        penInitialWorldPos = lancingPen.position;
        penInitialWorldRot = lancingPen.rotation;
        penInitialLocalScale = lancingPen.localScale;
    }

    private bool CanRunStage3Interaction()
    {
        if (cottonSwab == null)
        {
            return false;
        }

        return TryGetMiddleFingerAnchor(out _);
    }

    private bool TryGetMiddleFingerAnchor(out Vector3 anchor)
    {
        if (middleFingerPoint != null)
        {
            anchor = middleFingerPoint.position;
            return true;
        }

        if (handModel != null && TryGetRendererBounds(handModel, out Bounds bounds))
        {
            anchor = bounds.center + (Vector3.up * bounds.extents.y * 0.86f);
            return true;
        }

        anchor = Vector3.zero;
        return false;
    }

    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderers[i].bounds);
        }

        return hasBounds;
    }

    private bool IsPointerNearTransform(Transform target, float radiusPixels)
    {
        Camera cam = ResolveCamera();
        if (cam == null || target == null)
        {
            return false;
        }

        Vector3 anchorPos = GetPointerAnchorPosition(target);
        Vector3 screenPoint = cam.WorldToScreenPoint(anchorPos);
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

    private bool TryGetMouseWorldOnPlane(float planeHeight, out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        Camera cam = ResolveCamera();
        if (cam == null)
        {
            return false;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
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

    private bool IsStage3()
    {
        return stageIndex == 3;
    }

    private void OnGUI()
    {
        if (!IsStage3() || !IsGuidanceVisible())
        {
            return;
        }

        string operation = GetStageOperationText();
        if (string.IsNullOrEmpty(operation))
        {
            return;
        }

        EnsureUiStyles();

        float width = Mathf.Clamp(Screen.width - 140f, 720f, 980f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, 32f, width, 64f);
        DrawUiPanel(panel);

        GUILayout.BeginArea(GetPaddedRect(panel, 24f, 14f));
        GUILayout.Label(operation, uiPrimaryStyle);
        GUILayout.EndArea();
    }

    private string GetStageOperationText()
    {
        if (swabAnimating || penAnimating)
        {
            return string.Empty;
        }

        if (!swabStepCompleted)
        {
            return "拖动酒精棉到中指指尖";
        }

        if (!stage3InteractionCompleted)
        {
            return penPlacedAtTarget
                ? "按 Space 按压采血笔"
                : "拖动采血笔到中指指尖";
        }

        return string.Empty;
    }

    private bool IsGuidanceVisible()
    {
        return hintGate == null || hintGate.GuidanceVisible;
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
}
