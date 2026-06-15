using System.Collections;
using UnityEngine;

public sealed class Day2StagePlaceholderController : MonoBehaviour
{
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

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.EnsureInstanceForDemo();
        }

        gameManager.MarkCurrentDay(2);
        ResolveRuntimeReferences();
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameManager.ReturnToMainMenu();
            return;
        }

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
        Rect panel = new Rect(12f, 10f, 760f, 156f);
        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.Label("Day2 采血流程（四阶段场景拆分）");
        GUILayout.Label("当前场景：第 " + stageIndex + " 阶段");
        GUILayout.Label(stageTitle);
        GUILayout.Label(stageDescription);

        if (IsStage3())
        {
            if (swabAnimating)
            {
                GUILayout.Label("棉签消毒中：正在自动上下擦拭...");
            }
            else if (!swabStepCompleted)
            {
                GUILayout.Label("操作：左键拖动棉签到中指附近，触发自动上下擦拭 2~3 次。");
            }
            else if (penAnimating)
            {
                GUILayout.Label("血糖笔归位中...");
            }
            else if (!stage3InteractionCompleted)
            {
                if (penPlacedAtTarget)
                {
                    GUILayout.Label("血糖笔已到位，按 Space 让血糖笔回到原处。");
                }
                else
                {
                    GUILayout.Label("操作：拖动血糖笔到目标位置（中指），按 Space 让血糖笔回到原处。");
                }
            }
            else
            {
                GUILayout.Label("本阶段操作已完成。");
            }
        }

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
