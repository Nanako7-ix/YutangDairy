using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Day3RunnerSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Day3_Home.unity";
    private const string MaterialFolder = "Assets/Materials/Day3Runner";
    private const string RunnerModelPath = "Assets/External/Day3Runner/KenneyProtagonist/Model/characterMedium.fbx";
    private const string RunnerMaterialPath = MaterialFolder + "/KenneyRunner.mat";
    private const string RunnerAnimatorPath = MaterialFolder + "/KenneyRunner.controller";
    private const float TrackLength = 285f;
    private const float FinishZ = 260f;
    private static readonly float[] Lanes = { -3f, 0f, 3f };

    private static readonly string[] FoodPaths =
    {
        "Assets/External/Day3Runner/Food/cake.fbx",
        "Assets/External/Day3Runner/Food/cupcake.fbx",
        "Assets/External/Day3Runner/Food/donut-chocolate.fbx",
        "Assets/External/Day3Runner/Food/chocolate.fbx",
        "Assets/External/Day3Runner/Food/ice-cream.fbx"
    };

    [MenuItem("Tools/Yutang Diary/Rebuild Day3 Runner")]
    public static void Rebuild()
    {
        EnsureFolder("Assets/Materials", "Day3Runner");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(root);
        }

        Material road = GetMaterial("Road", new Color(0.16f, 0.19f, 0.23f));
        Material lane = GetMaterial("Lane", new Color(0.93f, 0.90f, 0.66f));
        Material curb = GetMaterial("Curb", new Color(0.67f, 0.73f, 0.77f));
        Material grass = GetMaterial("Grass", new Color(0.24f, 0.55f, 0.32f));
        Material tree = GetMaterial("Tree", new Color(0.16f, 0.42f, 0.24f));
        Material trunk = GetMaterial("Trunk", new Color(0.35f, 0.22f, 0.12f));
        Material buildingA = GetMaterial("BuildingA", new Color(0.38f, 0.54f, 0.65f));
        Material buildingB = GetMaterial("BuildingB", new Color(0.70f, 0.48f, 0.42f));
        Material accent = GetMaterial("Accent", new Color(0.98f, 0.78f, 0.25f));
        Material finish = GetMaterial("Finish", new Color(0.20f, 0.76f, 0.58f));

        BuildEnvironment(road, lane, curb, grass, tree, trunk, buildingA, buildingB, accent, finish);
        Day3HomeController controller = BuildPlayerAndCamera();
        BuildGameplayObjects();
        BuildHud(controller);
        BuildLighting();

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.61f, 0.68f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.62f, 0.78f, 0.86f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 150f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = controller.gameObject;
        Debug.Log("[Day3RunnerSceneBuilder] Day3 runner rebuilt.");
    }

    private static void BuildEnvironment(
        Material road,
        Material lane,
        Material curb,
        Material grass,
        Material tree,
        Material trunk,
        Material buildingA,
        Material buildingB,
        Material accent,
        Material finish)
    {
        GameObject root = new GameObject("RunnerEnvironment");

        CreateCube(root.transform, "Road", new Vector3(0f, -0.15f, TrackLength * 0.5f - 10f), new Vector3(11f, 0.3f, TrackLength), road);
        CreateCube(root.transform, "GrassLeft", new Vector3(-14f, -0.24f, TrackLength * 0.5f - 10f), new Vector3(17f, 0.2f, TrackLength), grass);
        CreateCube(root.transform, "GrassRight", new Vector3(14f, -0.24f, TrackLength * 0.5f - 10f), new Vector3(17f, 0.2f, TrackLength), grass);
        CreateCube(root.transform, "CurbLeft", new Vector3(-5.8f, 0.05f, TrackLength * 0.5f - 10f), new Vector3(0.6f, 0.4f, TrackLength), curb);
        CreateCube(root.transform, "CurbRight", new Vector3(5.8f, 0.05f, TrackLength * 0.5f - 10f), new Vector3(0.6f, 0.4f, TrackLength), curb);

        for (float z = 2f; z < TrackLength; z += 8f)
        {
            CreateCube(root.transform, "LaneMark", new Vector3(-1.5f, 0.03f, z), new Vector3(0.10f, 0.04f, 4f), lane);
            CreateCube(root.transform, "LaneMark", new Vector3(1.5f, 0.03f, z), new Vector3(0.10f, 0.04f, 4f), lane);
        }

        for (int i = 0; i < 18; i++)
        {
            float z = 8f + i * 15f;
            CreateTree(root.transform, new Vector3(-8.3f - (i % 2) * 1.4f, 0f, z), tree, trunk);
            CreateTree(root.transform, new Vector3(8.3f + (i % 2) * 1.4f, 0f, z + 6f), tree, trunk);

            if (i % 3 == 0)
            {
                float height = 5f + (i % 4) * 1.6f;
                CreateCube(root.transform, "Building", new Vector3(-17f, height * 0.5f, z + 4f), new Vector3(7f, height, 10f), i % 2 == 0 ? buildingA : buildingB);
                CreateCube(root.transform, "Building", new Vector3(17f, height * 0.5f, z + 11f), new Vector3(7f, height + 2f, 10f), i % 2 == 0 ? buildingB : buildingA);
            }
        }

        CreateArch(root.transform, "StartArch", 5f, accent);
        CreateArch(root.transform, "FinishArch", FinishZ, finish);

        GameObject finishTrigger = new GameObject("FinishTrigger");
        finishTrigger.transform.SetParent(root.transform, false);
        finishTrigger.transform.position = new Vector3(0f, 2f, FinishZ);
        BoxCollider finishCollider = finishTrigger.AddComponent<BoxCollider>();
        finishCollider.isTrigger = true;
        finishCollider.size = new Vector3(11f, 4f, 1f);
    }

    private static Day3HomeController BuildPlayerAndCamera()
    {
        GameObject player = new GameObject("RunnerPlayer");
        player.transform.position = new Vector3(0f, 0.05f, 0f);
        CharacterController character = player.AddComponent<CharacterController>();
        character.radius = 0.42f;
        character.height = 1.8f;
        character.center = new Vector3(0f, 0.9f, 0f);
        character.stepOffset = 0.25f;

        Day3HomeController controller = player.AddComponent<Day3HomeController>();

        GameObject playerVisual = new GameObject("PlayerVisual");
        playerVisual.transform.SetParent(player.transform, false);
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RunnerModelPath);
        if (modelAsset != null)
        {
            GameObject model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            model.name = "KenneyRunner";
            model.transform.SetParent(playerVisual.transform, false);
            NormalizeModel(model, 2.2f);
            model.transform.localRotation = Quaternion.identity;

            Material runnerMaterial = AssetDatabase.LoadAssetAtPath<Material>(RunnerMaterialPath);
            if (runnerMaterial != null)
            {
                AssignMaterial(model, runnerMaterial);
            }

            RuntimeAnimatorController animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(RunnerAnimatorPath);
            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
        }
        else
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "RunnerFallback";
            fallback.transform.SetParent(playerVisual.transform, false);
            fallback.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 62f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 220f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.55f, 0.77f, 0.88f);
        cameraObject.transform.position = new Vector3(0f, 6.2f, -12.5f);
        cameraObject.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
        cameraObject.AddComponent<AudioListener>();

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("characterController").objectReferenceValue = character;
        serialized.FindProperty("playerVisual").objectReferenceValue = playerVisual.transform;
        serialized.FindProperty("followCamera").objectReferenceValue = cameraObject.transform;
        serialized.FindProperty("finishZ").floatValue = FinishZ;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static void BuildGameplayObjects()
    {
        GameObject root = new GameObject("RunnerGameplay");
        int[][] obstaclePatterns =
        {
            new[] { 0 },
            new[] { 2 },
            new[] { 0, 2 },
            new[] { 1 },
            new[] { 0, 1 },
            new[] { 1, 2 },
            new[] { 0, 2 },
            new[] { 1 },
            new[] { 0, 1 },
            new[] { 2 },
            new[] { 0, 2 },
            new[] { 1, 2 },
            new[] { 0 },
            new[] { 0, 1 },
            new[] { 2 }
        };

        for (int i = 0; i < obstaclePatterns.Length; i++)
        {
            float z = 22f + i * 15f;
            HashSet<int> blocked = new HashSet<int>(obstaclePatterns[i]);
            foreach (int laneIndex in obstaclePatterns[i])
            {
                CreateObstacle(root.transform, laneIndex, z, i);
            }

            int safeLane = 0;
            while (blocked.Contains(safeLane))
            {
                safeLane++;
            }

            if (i % 2 == 0 || obstaclePatterns[i].Length == 2)
            {
                CreateHeart(root.transform, safeLane, z - 4.5f, i);
            }
        }

        CreateHeart(root.transform, 1, 12f, 100);
        CreateHeart(root.transform, 0, 246f, 101);
        CreateHeart(root.transform, 2, 252f, 102);
    }

    private static void CreateObstacle(Transform parent, int laneIndex, float z, int index)
    {
        GameObject root = new GameObject("Obstacle_" + index + "_" + laneIndex);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(Lanes[laneIndex], 0f, z);

        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(FoodPaths[(index + laneIndex) % FoodPaths.Length]);
        if (asset != null)
        {
            GameObject model = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            model.name = "HighSugarFood";
            model.transform.SetParent(root.transform, false);
            NormalizeModelByLargestDimension(model, index % 4 == 0 ? 1.8f : 2.2f);
            model.transform.localRotation = Quaternion.Euler(0f, (index * 37f) % 360f, 0f);
            Color[] colors =
            {
                new Color(0.94f, 0.48f, 0.58f),
                new Color(0.96f, 0.67f, 0.28f),
                new Color(0.52f, 0.30f, 0.22f),
                new Color(0.67f, 0.36f, 0.22f),
                new Color(0.82f, 0.50f, 0.88f)
            };
            AssignMaterial(model, GetMaterial("SugarFood" + ((index + laneIndex) % colors.Length), colors[(index + laneIndex) % colors.Length]));
        }

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = new Vector3(0f, 0.75f, 0f);
        collider.size = new Vector3(1.7f, index % 4 == 0 ? 1.1f : 1.55f, 1.4f);
    }

    private static void CreateHeart(Transform parent, int laneIndex, float z, int index)
    {
        GameObject root = new GameObject("Heart_" + index);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(Lanes[laneIndex], 1.15f, z);

        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External/Day3Runner/Runner/heart.fbx");
        if (asset != null)
        {
            GameObject model = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            model.name = "HeartModel";
            model.transform.SetParent(root.transform, false);
            NormalizeModel(model, 0.85f);
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            AssignMaterial(model, GetMaterial("Heart", new Color(1f, 0.20f, 0.38f)));
        }

        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.7f;
    }

    private static void BuildHud(Day3HomeController controller)
    {
        GameObject canvasObject = new GameObject("RunnerHUD", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Color panelColor = new Color(0.04f, 0.08f, 0.12f, 0.84f);
        Color green = new Color(0.20f, 0.78f, 0.52f);
        Color heartColor = new Color(1f, 0.28f, 0.34f);

        GameObject topPanel = CreateUiPanel(canvasObject.transform, "TopPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(1460f, 112f), panelColor);
        Text title = CreateText(topPanel.transform, "Title", "Day 3 · 晨间运动", 30, TextAnchor.MiddleLeft, font, Color.white);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -12f), new Vector2(430f, 44f), new Vector2(0f, 1f));

        Text healthText = CreateText(topPanel.transform, "HealthText", "健康  ♥  ♥  ♥", 24, TextAnchor.MiddleLeft, font, heartColor);
        SetRect(healthText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 14f), new Vector2(280f, 34f), Vector2.zero);

        Text scoreText = CreateText(topPanel.transform, "ScoreText", "得分 0", 22, TextAnchor.MiddleCenter, font, Color.white);
        SetRect(scoreText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(300f, 30f), new Vector2(0.5f, 0f));

        Text progressText = CreateText(topPanel.transform, "ProgressText", "运动进度 0%", 22, TextAnchor.MiddleRight, font, Color.white);
        SetRect(progressText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 16f), new Vector2(260f, 30f), new Vector2(1f, 0f));
        Image progressFill = CreateBar(topPanel.transform, "ProgressBar", new Vector2(-330f, 22f), new Vector2(300f, 20f), new Color(0.98f, 0.76f, 0.25f), true);

        Text feedback = CreateText(canvasObject.transform, "Feedback", string.Empty, 26, TextAnchor.MiddleCenter, font, Color.white);
        SetRect(feedback.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(900f, 54f), new Vector2(0.5f, 0f));
        feedback.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

        GameObject resultPanel = CreateUiPanel(canvasObject.transform, "ResultPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 420f), new Color(0.04f, 0.08f, 0.12f, 0.95f));
        Text resultTitle = CreateText(resultPanel.transform, "ResultTitle", "运动完成", 38, TextAnchor.MiddleCenter, font, Color.white);
        SetRect(resultTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(540f, 60f), new Vector2(0.5f, 1f));
        Text resultBody = CreateText(resultPanel.transform, "ResultBody", string.Empty, 24, TextAnchor.MiddleCenter, font, new Color(0.90f, 0.95f, 0.98f));
        SetRect(resultBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(540f, 190f), new Vector2(0.5f, 0.5f));
        Button retry = CreateButton(resultPanel.transform, "RetryButton", "再跑一次", new Vector2(-140f, -142f), green, font);
        Button menu = CreateButton(resultPanel.transform, "MenuButton", "返回首页", new Vector2(140f, -142f), new Color(0.35f, 0.45f, 0.55f), font);

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("healthText").objectReferenceValue = healthText;
        serialized.FindProperty("scoreText").objectReferenceValue = scoreText;
        serialized.FindProperty("progressText").objectReferenceValue = progressText;
        serialized.FindProperty("feedbackText").objectReferenceValue = feedback;
        serialized.FindProperty("healthFill").objectReferenceValue = null;
        serialized.FindProperty("progressFill").objectReferenceValue = progressFill;
        serialized.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        serialized.FindProperty("resultTitle").objectReferenceValue = resultTitle;
        serialized.FindProperty("resultBody").objectReferenceValue = resultBody;
        serialized.FindProperty("retryButton").objectReferenceValue = retry;
        serialized.FindProperty("menuButton").objectReferenceValue = menu;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(1f, 0.94f, 0.82f);
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

        GameObject fillObject = new GameObject("Fill Light");
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.35f;
        fill.color = new Color(0.56f, 0.75f, 1f);
        fill.shadows = LightShadows.None;
        fillObject.transform.rotation = Quaternion.Euler(35f, 145f, 0f);
    }

    private static void CreateTree(Transform parent, Vector3 position, Material leaves, Material trunk)
    {
        GameObject root = new GameObject("Tree");
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        GameObject trunkObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunkObject.name = "Trunk";
        trunkObject.transform.SetParent(root.transform, false);
        trunkObject.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        trunkObject.transform.localScale = new Vector3(0.35f, 1.1f, 0.35f);
        trunkObject.GetComponent<Renderer>().sharedMaterial = trunk;

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "Crown";
        crown.transform.SetParent(root.transform, false);
        crown.transform.localPosition = new Vector3(0f, 3f, 0f);
        crown.transform.localScale = new Vector3(2.2f, 2.5f, 2.2f);
        crown.GetComponent<Renderer>().sharedMaterial = leaves;
    }

    private static void CreateArch(Transform parent, string name, float z, Material material)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(0f, 0f, z);
        CreateCube(root.transform, "Left", new Vector3(-5.2f, 2.5f, 0f), new Vector3(0.5f, 5f, 0.6f), material, false);
        CreateCube(root.transform, "Right", new Vector3(5.2f, 2.5f, 0f), new Vector3(0.5f, 5f, 0.6f), material, false);
        CreateCube(root.transform, "Top", new Vector3(0f, 4.8f, 0f), new Vector3(10.8f, 0.6f, 0.6f), material, false);
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool worldPosition = true)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        if (worldPosition)
        {
            cube.transform.position = position;
        }
        else
        {
            cube.transform.localPosition = position;
        }
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static void NormalizeModel(GameObject model, float targetHeight)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float scale = targetHeight / Mathf.Max(0.01f, bounds.size.y);
        model.transform.localScale = Vector3.one * scale;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 parentPosition = model.transform.parent != null ? model.transform.parent.position : Vector3.zero;
        Vector3 offset = new Vector3(parentPosition.x - bounds.center.x, parentPosition.y - bounds.min.y, parentPosition.z - bounds.center.z);
        model.transform.position += offset;
    }

    private static void NormalizeModelByLargestDimension(GameObject model, float targetSize)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        model.transform.localScale = Vector3.one * (targetSize / Mathf.Max(0.01f, largest));

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 parentPosition = model.transform.parent != null ? model.transform.parent.position : Vector3.zero;
        Vector3 offset = new Vector3(parentPosition.x - bounds.center.x, parentPosition.y - bounds.min.y, parentPosition.z - bounds.center.z);
        model.transform.position += offset;
    }

    private static void AssignMaterial(GameObject model, Material material)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = new Material[Mathf.Max(1, renderers[i].sharedMaterials.Length)];
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = material;
            }
            renderers[i].sharedMaterials = materials;
        }
    }

    private static GameObject CreateUiPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor, Font font, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        return text;
    }

    private static Image CreateBar(Transform parent, string name, Vector2 position, Vector2 size, Color fillColor, bool rightAligned = false)
    {
        GameObject background = new GameObject(name + "Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(parent, false);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        Vector2 anchor = rightAligned ? new Vector2(1f, 0f) : Vector2.zero;
        bgRect.anchorMin = anchor;
        bgRect.anchorMax = anchor;
        bgRect.pivot = rightAligned ? new Vector2(1f, 0f) : Vector2.zero;
        bgRect.anchoredPosition = position;
        bgRect.sizeDelta = size;
        background.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.16f);

        GameObject fill = new GameObject(name, typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(background.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        Image image = fill.GetComponent<Image>();
        image.color = fillColor;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = 0;
        image.fillAmount = 1f;
        return image;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Color color, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(220f, 62f);
        Image image = go.GetComponent<Image>();
        image.color = color;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateText(go.transform, "Label", label, 24, TextAnchor.MiddleCenter, font, Color.white);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Material GetMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        material.SetFloat("_Glossiness", 0.2f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
