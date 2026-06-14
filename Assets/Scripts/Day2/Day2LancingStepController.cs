using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class Day2LancingStepController : MonoBehaviour
{
    private enum Step
    {
        RemovePenCap,
        InsertLancet,
        RemoveSafetyCap,
        ReattachPenCap,
        SetDepth,
        PressTrigger,
        Completed
    }

    [Header("Flow")]
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private int scoreReward = 20;
    [SerializeField] private int healthReward = 5;

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
    [SerializeField] private Transform lancetDockHint;
    [SerializeField] private Transform triggerButton;
    [SerializeField] private Transform punctureNeedleTip;

    [Header("Tuning")]
    [SerializeField] private float dragPlaneHeight = 0.82f;
    [SerializeField] private float dockThreshold = 0.11f;
    [SerializeField] private float undockThreshold = 0.07f;
    [SerializeField] private float punctureDistance = 0.02f;
    [SerializeField] private float punctureDuration = 0.35f;
    [SerializeField] private bool enableHints;

    [Header("Depth Dial")]
    [SerializeField] private int targetDepthLevel = 3;

    private GameManager gameManager;
    private Step currentStep;
    private int currentDepthLevel = 1;
    private bool completionRewardGranted;

    private Transform draggingPart;
    private Vector3 dragOffset;
    private Plane dragPlane;

    private readonly Dictionary<Transform, Vector3> originScale = new Dictionary<Transform, Vector3>();
    private float pulseT;

    private bool punctureAnimating;
    private float punctureT;
    private Vector3 punctureNeedleStartLocalPos;
    private Vector3 punctureNeedleLocalDir = Vector3.forward;
    private Vector3 triggerButtonStartLocalPos;

    // Runtime UI.
    private Canvas uiCanvas;
    private Text titleText;
    private Text stepText;
    private Text hintText;
    private Text progressText;
    private GameObject depthPanel;
    private Text depthValueText;
    private Button depthMinusButton;
    private Button depthPlusButton;
    private Button depthLockButton;
    private Text completeHintText;

    private void Awake()
    {
        gameManager = GameManager.EnsureInstanceForDemo();
        gameManager.MarkCurrentDay(2);

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError("Day2LancingStepController: mainCamera 未绑定且场景中无 MainCamera。");
            enabled = false;
            return;
        }

        dragPlane = new Plane(Vector3.up, new Vector3(0f, dragPlaneHeight, 0f));

        CacheOriginScale(penCap);
        CacheOriginScale(lancetNeedle);
        CacheOriginScale(safetyCap);
        CacheOriginScale(lancetDockHint);
        CacheOriginScale(triggerButton);

        if (triggerButton != null)
        {
            triggerButtonStartLocalPos = triggerButton.localPosition;
        }

        if (punctureNeedleTip != null)
        {
            punctureNeedleStartLocalPos = punctureNeedleTip.localPosition;
            punctureNeedleLocalDir = ResolvePunctureLocalDir();
            punctureNeedleTip.gameObject.SetActive(false);
        }

        SyncTrayPlacementPoints();
        AlignVisualsAtStartup();
        EnsurePreciseInteractionColliders();
        EnsureRuntimeUi();
        BindUiEvents();
        SetStep(Step.RemovePenCap);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameManager.ReturnToMainMenu();
            return;
        }

        if (currentStep == Step.Completed && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            gameManager.LoadScene(nextSceneName);
            return;
        }

        HandleInput();
        if (enableHints)
        {
            UpdatePulseHighlight();
        }
        else
        {
            ResetHintVisualState();
        }
        UpdatePunctureAnimation();
    }

    private void HandleInput()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (currentStep == Step.PressTrigger)
        {
            Transform triggerTarget = GetTriggerInteractable();
            if (Input.GetMouseButtonDown(0) && triggerTarget != null && TryRaycastPart(triggerTarget, out _))
            {
                StartPuncture();
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Transform candidate = GetActiveInteractable();
            if (candidate != null && TryRaycastPart(candidate, out _))
            {
                draggingPart = candidate;
                Vector3 pointer = GetPointerOnPlane(draggingPart);
                dragOffset = draggingPart.position - pointer;
            }
        }

        if (Input.GetMouseButton(0) && draggingPart != null)
        {
            Vector3 pointer = GetPointerOnPlane(draggingPart);
            draggingPart.position = pointer + dragOffset;
        }

        if (Input.GetMouseButtonUp(0) && draggingPart != null)
        {
            ResolveDrop(draggingPart);
            draggingPart = null;
        }
    }

    private Vector3 GetPointerOnPlane(Transform reference)
    {
        float planeHeight = dragPlaneHeight;
        if (reference != null)
        {
            Renderer renderer = reference.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                planeHeight = Mathf.Max(planeHeight, renderer.bounds.center.y);
            }
        }

        dragPlane.SetNormalAndPosition(Vector3.up, new Vector3(0f, planeHeight, 0f));
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        return reference != null ? reference.position : Vector3.zero;
    }

    private bool TryRaycastPart(Transform target, out RaycastHit hit)
    {
        hit = default;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit candidate = hits[i];
            if (candidate.transform == target || candidate.transform.IsChildOf(target))
            {
                hit = candidate;
                return true;
            }

            // 某些模型把碰撞体挂在父级节点，补一个祖先关系兜底。
            if (candidate.collider != null && target.IsChildOf(candidate.collider.transform))
            {
                hit = candidate;
                return true;
            }
        }

        return false;
    }

    private void ResolveDrop(Transform part)
    {
        switch (currentStep)
        {
            case Step.RemovePenCap:
                if (part == penCap)
                {
                    bool removed = Vector3.Distance(penCap.position, penCapDock.position) > undockThreshold;
                    if (removed)
                    {
                        MoveTransformPreciseCenterTo(penCap, GetTrayPlacementCenter(trayPointA, "TrayA"));
                    }
                    else
                    {
                        penCap.position = penCapDock.position;
                    }
                    if (removed)
                    {
                        SetStep(Step.InsertLancet);
                    }
                }
                break;

            case Step.InsertLancet:
                if (part == lancetNeedle)
                {
                    bool inserted = IsWithinDockRange(lancetNeedle, lancetDock, dockThreshold);
                    if (inserted)
                    {
                        lancetNeedle.position = lancetDock.position;
                        if (safetyCap != null)
                        {
                            safetyCap.gameObject.SetActive(true);
                            Vector3 rightDir = penBody != null ? penBody.right : Vector3.right;
                            safetyCap.position = lancetDock.position + (rightDir * 0.018f);
                            safetyCap.rotation = lancetNeedle.rotation;
                        }

                        SetStep(Step.RemoveSafetyCap);
                    }
                    else
                    {
                        MoveTransformPreciseCenterTo(lancetNeedle, GetTrayPlacementCenter(trayPointB, "TrayB") + new Vector3(0f, 0.01f, 0f));

                        if (enableHints && hintText != null)
                        {
                            hintText.text = "还差一点：把细采血针拖到绿色圈中心再松手。";
                        }
                    }
                }
                break;

            case Step.RemoveSafetyCap:
                if (part == safetyCap)
                {
                    Vector3 dockLikePos = lancetDock.position + ((penBody != null ? penBody.right : Vector3.right) * 0.018f);
                    bool removed = Vector3.Distance(safetyCap.position, dockLikePos) > undockThreshold;
                    if (removed)
                    {
                        MoveTransformPreciseCenterTo(safetyCap, GetTrayPlacementCenter(trayPointA, "TrayA") + (Vector3.forward * 0.08f));
                    }
                    else
                    {
                        safetyCap.position = dockLikePos;
                    }
                    if (removed)
                    {
                        SetStep(Step.ReattachPenCap);
                    }
                }
                break;

            case Step.ReattachPenCap:
                if (part == penCap)
                {
                    bool attached = Vector3.Distance(penCap.position, penCapDock.position) <= dockThreshold;
                    if (attached)
                    {
                        penCap.position = penCapDock.position;
                    }
                    else
                    {
                        MoveTransformPreciseCenterTo(penCap, GetTrayPlacementCenter(trayPointA, "TrayA"));
                    }
                    if (attached)
                    {
                        SetStep(Step.SetDepth);
                    }
                }
                break;
        }
    }

    private Transform GetActiveInteractable()
    {
        switch (currentStep)
        {
            case Step.RemovePenCap:
                return penCap;
            case Step.InsertLancet:
                return lancetNeedle;
            case Step.RemoveSafetyCap:
                return safetyCap;
            case Step.ReattachPenCap:
                return penCap;
            case Step.PressTrigger:
                return GetTriggerInteractable();
            default:
                return null;
        }
    }

    private Transform GetTriggerInteractable()
    {
        return triggerButton != null ? triggerButton : penBody;
    }

    private void UpdatePulseHighlight()
    {
        pulseT += Time.deltaTime * 5f;
        float scaleMultiplier = 1f + (Mathf.Sin(pulseT) * 0.025f);
        Transform active = GetActiveInteractable();

        ApplyScalePulse(penCap, active == penCap ? scaleMultiplier : 1f);
        ApplyScalePulse(lancetNeedle, active == lancetNeedle ? scaleMultiplier : 1f);
        ApplyScalePulse(safetyCap, active == safetyCap ? scaleMultiplier : 1f);
        ApplyScalePulse(triggerButton, active == triggerButton ? 1f + (Mathf.Sin(pulseT * 1.4f) * 0.05f) : 1f);
        ApplyScalePulse(lancetDockHint, currentStep == Step.InsertLancet ? 1f + (Mathf.Sin(pulseT * 1.3f) * 0.06f) : 1f);
    }

    private void ResetHintVisualState()
    {
        ApplyScalePulse(penCap, 1f);
        ApplyScalePulse(lancetNeedle, 1f);
        ApplyScalePulse(safetyCap, 1f);
        ApplyScalePulse(triggerButton, 1f);
        ApplyScalePulse(lancetDockHint, 1f);
    }

    private void StartPuncture()
    {
        if (punctureAnimating)
        {
            return;
        }

        punctureAnimating = true;
        punctureT = 0f;

        if (triggerButton != null)
        {
            triggerButton.localPosition = triggerButtonStartLocalPos + (Vector3.down * 0.004f);
        }

        if (punctureNeedleTip != null)
        {
            punctureNeedleTip.localPosition = punctureNeedleStartLocalPos;
            punctureNeedleTip.gameObject.SetActive(true);
        }

        if (hintText != null)
        {
            hintText.text = "已按下按钮：针尖刺出并回收中...";
        }
    }

    private void UpdatePunctureAnimation()
    {
        if (!punctureAnimating)
        {
            return;
        }

        punctureT += Time.deltaTime / Mathf.Max(0.05f, punctureDuration);
        float normalized = Mathf.Clamp01(punctureT);
        float phase = Mathf.Sin(normalized * Mathf.PI);

        if (punctureNeedleTip != null)
        {
            punctureNeedleTip.localPosition = punctureNeedleStartLocalPos + (punctureNeedleLocalDir * (punctureDistance * phase));
        }

        if (normalized >= 1f)
        {
            punctureAnimating = false;
            if (triggerButton != null)
            {
                triggerButton.localPosition = triggerButtonStartLocalPos;
            }

            if (punctureNeedleTip != null)
            {
                punctureNeedleTip.localPosition = punctureNeedleStartLocalPos;
                punctureNeedleTip.gameObject.SetActive(false);
            }

            SetStep(Step.Completed);
        }
    }

    private void ApplyScalePulse(Transform target, float multiplier)
    {
        if (target == null || !originScale.TryGetValue(target, out Vector3 baseScale))
        {
            return;
        }

        target.localScale = baseScale * multiplier;
    }

    private void CacheOriginScale(Transform target)
    {
        if (target != null && !originScale.ContainsKey(target))
        {
            originScale[target] = target.localScale;
        }
    }

    private void SetStep(Step step)
    {
        currentStep = step;
        string stepLine;
        string hintLine;
        string progressLine;

        switch (currentStep)
        {
            case Step.RemovePenCap:
                stepLine = "步骤1/6：先取下采血笔笔帽";
                hintLine = "把笔帽从笔尖方向拖开，放到左侧托盘。";
                progressLine = "进度：0%";
                SetOptionalHintVisible(lancetDockHint, false);
                SetOptionalHintVisible(triggerButton, false);
                SetOptionalHintVisible(punctureNeedleTip, false);
                if (depthPanel != null) depthPanel.SetActive(false);
                if (completeHintText != null) completeHintText.gameObject.SetActive(false);
                break;

            case Step.InsertLancet:
                stepLine = "步骤2/6：安装细采血针";
                hintLine = "拖动右侧细采血针，对准绿色安装孔后松手。";
                progressLine = "进度：20%";
                SetOptionalHintVisible(lancetDockHint, enableHints);
                SetOptionalHintVisible(triggerButton, false);
                SetOptionalHintVisible(punctureNeedleTip, false);
                if (depthPanel != null) depthPanel.SetActive(false);
                if (completeHintText != null) completeHintText.gameObject.SetActive(false);
                break;

            case Step.RemoveSafetyCap:
                stepLine = "步骤3/6：拔掉针头保护帽";
                hintLine = "将保护帽拖离针头并放到托盘。";
                progressLine = "进度：40%";
                SetOptionalHintVisible(lancetDockHint, false);
                SetOptionalHintVisible(triggerButton, false);
                SetOptionalHintVisible(punctureNeedleTip, false);
                if (depthPanel != null) depthPanel.SetActive(false);
                if (completeHintText != null) completeHintText.gameObject.SetActive(false);
                break;

            case Step.ReattachPenCap:
                stepLine = "步骤4/6：把笔帽装回去";
                hintLine = "将笔帽拖回笔尖，贴合后会自动吸附。";
                progressLine = "进度：60%";
                SetOptionalHintVisible(lancetDockHint, false);
                SetOptionalHintVisible(triggerButton, false);
                SetOptionalHintVisible(punctureNeedleTip, false);
                if (depthPanel != null) depthPanel.SetActive(false);
                if (completeHintText != null) completeHintText.gameObject.SetActive(false);
                break;

            case Step.SetDepth:
                stepLine = "步骤5/6：调节采血深度到 3 档";
                hintLine = "使用 +/- 调节档位，达到 3 后点击“锁定档位”。";
                progressLine = "进度：80%";
                SetOptionalHintVisible(lancetDockHint, false);
                SetOptionalHintVisible(triggerButton, false);
                SetOptionalHintVisible(punctureNeedleTip, false);
                if (depthPanel != null) depthPanel.SetActive(true);
                if (completeHintText != null) completeHintText.gameObject.SetActive(false);
                RefreshDepthUi();
                break;

            case Step.PressTrigger:
                stepLine = "步骤6/6：按下采血按钮";
                hintLine = triggerButton != null
                    ? "点击笔身绿色按钮，针尖会快速刺出并回收。"
                    : "点击采血笔本体按钮区域，针尖会快速刺出并回收。";
                progressLine = "进度：95%";
                SetOptionalHintVisible(lancetDockHint, false);
                SetOptionalHintVisible(triggerButton, enableHints);
                SetOptionalHintVisible(punctureNeedleTip, false);
                if (triggerButton != null)
                {
                    triggerButton.localPosition = triggerButtonStartLocalPos;
                }

                if (depthPanel != null) depthPanel.SetActive(false);
                if (completeHintText != null) completeHintText.gameObject.SetActive(false);
                break;

            case Step.Completed:
                stepLine = "采血笔步骤完成";
                hintLine = "流程正确：拆帽、装针、拔保护、回帽、调档、按键刺出。";
                progressLine = "进度：100%";
                SetOptionalHintVisible(lancetDockHint, false);
                SetOptionalHintVisible(triggerButton, false);
                SetOptionalHintVisible(punctureNeedleTip, false);
                if (depthPanel != null) depthPanel.SetActive(false);
                if (completeHintText != null) completeHintText.gameObject.SetActive(enableHints);
                if (!completionRewardGranted)
                {
                    gameManager.AddScore(scoreReward);
                    gameManager.AddHealth(healthReward);
                    completionRewardGranted = true;
                }

                break;

            default:
                return;
        }

        if (titleText != null) titleText.text = "Day2 - 采血笔准备";
        if (stepText != null) stepText.text = stepLine;
        if (hintText != null)
        {
            hintText.gameObject.SetActive(enableHints);
            if (enableHints)
            {
                hintText.text = hintLine;
            }
        }
        if (progressText != null) progressText.text = progressLine + "   |   按 Esc 返回主菜单";
    }

    private Vector3 ResolvePunctureLocalDir()
    {
        Vector3 worldDir = penBody != null ? penBody.forward : (punctureNeedleTip != null ? punctureNeedleTip.forward : Vector3.forward);
        if (punctureNeedleTip != null && punctureNeedleTip.parent != null)
        {
            Vector3 localDir = punctureNeedleTip.parent.InverseTransformDirection(worldDir).normalized;
            if (localDir.sqrMagnitude > 0.001f)
            {
                return localDir;
            }
        }

        return Vector3.forward;
    }

    private void EnsurePreciseInteractionColliders()
    {
        EnsureMeshColliders(penBody);
        EnsureMeshColliders(penCap);
        EnsureMeshColliders(lancetNeedle);
        EnsureMeshColliders(safetyCap);
    }

    private static void EnsureMeshColliders(Transform root)
    {
        if (root == null)
        {
            return;
        }

        bool hasMeshCollider = false;
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter mf = meshFilters[i];
            if (mf == null || mf.sharedMesh == null)
            {
                continue;
            }

            MeshCollider mc = mf.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = mf.gameObject.AddComponent<MeshCollider>();
            }

            if (mc.sharedMesh != mf.sharedMesh)
            {
                mc.sharedMesh = mf.sharedMesh;
            }

            mc.convex = false;
            mc.isTrigger = false;
            hasMeshCollider = true;
        }

        // 优先使用贴模碰撞，避免粗盒碰撞导致“点中了却判定不准”。
        if (hasMeshCollider)
        {
            BoxCollider rootBox = root.GetComponent<BoxCollider>();
            if (rootBox != null)
            {
                rootBox.enabled = false;
            }
        }
    }

    private void AlignVisualsAtStartup()
    {
        if (penCap != null && penCapDock != null)
        {
            penCap.position = penCapDock.position;
        }

        if (safetyCap != null)
        {
            safetyCap.gameObject.SetActive(false);
        }
    }

    private static void SetOptionalHintVisible(Transform hint, bool visible)
    {
        if (hint != null)
        {
            hint.gameObject.SetActive(visible);
        }
    }

    private static bool IsWithinDockRange(Transform moving, Transform dock, float threshold)
    {
        if (moving == null || dock == null)
        {
            return false;
        }

        Vector3 dockPos = dock.position;
        float bestDistance = Vector3.Distance(moving.position, dockPos);
        Vector2 dockXZ = new Vector2(dockPos.x, dockPos.z);
        bestDistance = Mathf.Min(bestDistance, Vector2.Distance(new Vector2(moving.position.x, moving.position.z), dockXZ));

        Renderer renderer = moving.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Vector3 closest = renderer.bounds.ClosestPoint(dockPos);
            bestDistance = Mathf.Min(bestDistance, Vector3.Distance(closest, dockPos));
            bestDistance = Mathf.Min(bestDistance, Vector2.Distance(new Vector2(closest.x, closest.z), dockXZ));
        }

        return bestDistance <= threshold;
    }

    private static void MoveTransformBoundsCenterTo(Transform target, Vector3 center)
    {
        if (target == null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            target.position = center;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        target.position += center - bounds.center;
    }

    private static void MoveTransformPreciseCenterTo(Transform target, Vector3 center)
    {
        if (target == null)
        {
            return;
        }

        if (TryGetSurfaceCentroid(target, out Vector3 centroid))
        {
            target.position += center - centroid;
            return;
        }

        MoveTransformBoundsCenterTo(target, center);
    }

    private static bool TryGetSurfaceCentroid(Transform root, out Vector3 centroid)
    {
        centroid = Vector3.zero;
        if (root == null)
        {
            return false;
        }

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        if (filters == null || filters.Length == 0)
        {
            return false;
        }

        double totalArea = 0.0;
        Vector3 weighted = Vector3.zero;
        const float areaEpsilon = 0.0000001f;

        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            if (vertices == null || triangles == null || triangles.Length < 3)
            {
                continue;
            }

            Matrix4x4 localToWorld = filter.transform.localToWorldMatrix;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 a = localToWorld.MultiplyPoint3x4(vertices[triangles[t]]);
                Vector3 b = localToWorld.MultiplyPoint3x4(vertices[triangles[t + 1]]);
                Vector3 c = localToWorld.MultiplyPoint3x4(vertices[triangles[t + 2]]);

                float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                if (area <= areaEpsilon)
                {
                    continue;
                }

                Vector3 triCenter = (a + b + c) / 3f;
                weighted += triCenter * area;
                totalArea += area;
            }
        }

        if (totalArea <= areaEpsilon)
        {
            return false;
        }

        centroid = weighted / (float)totalArea;
        return true;
    }

    private void SyncTrayPlacementPoints()
    {
        if (trayPointA != null)
        {
            trayPointA.position = GetTrayPlacementCenter(trayPointA, "TrayA");
        }

        if (trayPointB != null)
        {
            trayPointB.position = GetTrayPlacementCenter(trayPointB, "TrayB");
        }
    }

    private static Vector3 GetTrayPlacementCenter(Transform fallbackPoint, string trayName)
    {
        GameObject tray = GameObject.Find(trayName);
        if (tray != null)
        {
            Renderer trayRenderer = tray.GetComponent<Renderer>();
            if (trayRenderer != null)
            {
                return trayRenderer.bounds.center + new Vector3(0f, 0.03f, 0f);
            }
        }

        return fallbackPoint != null ? fallbackPoint.position : Vector3.zero;
    }

    private void BindUiEvents()
    {
        if (depthMinusButton != null)
        {
            depthMinusButton.onClick.AddListener(() =>
            {
                currentDepthLevel = Mathf.Max(1, currentDepthLevel - 1);
                RefreshDepthUi();
            });
        }

        if (depthPlusButton != null)
        {
            depthPlusButton.onClick.AddListener(() =>
            {
                currentDepthLevel = Mathf.Min(5, currentDepthLevel + 1);
                RefreshDepthUi();
            });
        }

        if (depthLockButton != null)
        {
            depthLockButton.onClick.AddListener(() =>
            {
                if (currentDepthLevel == targetDepthLevel)
                {
                    SetStep(Step.PressTrigger);
                }
                else if (hintText != null)
                {
                    hintText.text = "请先把档位调到 " + targetDepthLevel + "。";
                }
            });
        }
    }

    private void RefreshDepthUi()
    {
        if (depthValueText != null)
        {
            depthValueText.text = "当前档位: " + currentDepthLevel + "   (目标: " + targetDepthLevel + ")";
        }

        if (depthLockButton != null)
        {
            depthLockButton.interactable = currentDepthLevel == targetDepthLevel;
        }
    }

    private void EnsureRuntimeUi()
    {
        GameObject existing = GameObject.Find("Day2LancingUI");
        if (existing != null)
        {
            uiCanvas = existing.GetComponent<Canvas>();
        }

        if (uiCanvas == null)
        {
            GameObject canvasGo = new GameObject("Day2LancingUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            uiCanvas = canvasGo.GetComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        titleText = CreateText("TitleText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(840f, 46f), 32, FontStyle.Bold);
        stepText = CreateText("StepText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(980f, 40f), 28, FontStyle.Bold);
        hintText = CreateText("HintText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1100f, 34f), 24, FontStyle.Normal);
        hintText.gameObject.SetActive(enableHints);
        progressText = CreateText("ProgressText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(1200f, 30f), 22, FontStyle.Normal);

        depthPanel = CreatePanel("DepthPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-210f, 0f), new Vector2(360f, 220f), new Color(0.08f, 0.12f, 0.18f, 0.8f));
        CreateText("DepthTitle", depthPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(320f, 28f), 24, FontStyle.Bold, "采血深度设置");
        depthValueText = CreateText("DepthValue", depthPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(320f, 26f), 20, FontStyle.Normal);

        depthMinusButton = CreateButton("DepthMinus", depthPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-100f, -126f), new Vector2(70f, 42f), "-");
        depthPlusButton = CreateButton("DepthPlus", depthPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(70f, 42f), "+");
        depthLockButton = CreateButton("DepthLock", depthPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(96f, -126f), new Vector2(120f, 42f), "锁定档位");

        completeHintText = CreateText("CompleteHint", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(960f, 34f), 24, FontStyle.Bold);
        completeHintText.text = "按 Enter 继续下一场景";
        completeHintText.gameObject.SetActive(false);
        depthPanel.SetActive(false);
    }

    private Text CreateText(
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos,
        Vector2 size,
        int fontSize,
        FontStyle style)
    {
        return CreateText(name, uiCanvas.transform, anchorMin, anchorMax, anchoredPos, size, fontSize, style, string.Empty);
    }

    private Text CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos,
        Vector2 size,
        int fontSize,
        FontStyle style,
        string initialText = "")
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Text txt = go.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = initialText;
        return txt;
    }

    private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(uiCanvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;
        return go;
    }

    private Button CreateButton(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos,
        Vector2 size,
        string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.23f, 0.41f, 0.66f, 0.95f);

        Button btn = go.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = image.color;
        cb.highlightedColor = new Color(0.32f, 0.55f, 0.84f, 1f);
        cb.pressedColor = new Color(0.16f, 0.33f, 0.52f, 1f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;

        Text txt = CreateText(name + "_Label", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22, FontStyle.Bold, label);
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 14;
        txt.resizeTextMaxSize = 24;
        return btn;
    }
}
