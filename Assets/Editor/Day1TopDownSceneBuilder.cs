using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Day1TopDownSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Day1_Hospital.unity";

    [MenuItem("Tools/Yutang Diary/Rebuild Day 1 As Topdown 2D")]
    public static void RebuildFromMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(root);
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.40f, 0.44f, 0.48f, 1f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Material floorMaterial = CreateMaterial("Day1Simple_Floor", new Color(0.83f, 0.86f, 0.88f));
        Material wallMaterial = CreateMaterial("Day1Simple_Wall", new Color(0.95f, 0.97f, 0.98f));
        Material zoneReceptionMaterial = CreateMaterial("Day1Simple_ZoneReception", new Color(0.74f, 0.86f, 0.90f));
        Material zoneWaitingMaterial = CreateMaterial("Day1Simple_ZoneWaiting", new Color(0.77f, 0.82f, 0.91f));
        Material zoneCorridorMaterial = CreateMaterial("Day1Simple_ZoneCorridor", new Color(0.88f, 0.89f, 0.90f));
        Material zoneClinicMaterial = CreateMaterial("Day1Simple_ZoneClinic", new Color(0.80f, 0.90f, 0.82f));
        Material accentMaterial = CreateMaterial("Day1Simple_Accent", new Color(0.22f, 0.56f, 0.63f));
        Material seatMaterial = CreateMaterial("Day1Simple_Seat", new Color(0.28f, 0.42f, 0.57f));
        Material signMaterial = CreateMaterial("Day1Simple_Sign", new Color(0.85f, 0.18f, 0.18f));
        Material doctorMaterial = CreateMaterial("Day1Simple_Doctor", new Color(0.86f, 0.92f, 0.92f));
        Material playerMaterial = CreateMaterial("Day1Simple_Player", new Color(0.92f, 0.70f, 0.28f));
        Material enemyMaterial = CreateMaterial("Day1Simple_Enemy", new Color(0.90f, 0.16f, 0.16f));
        Material markerMaterial = CreateMaterial("Day1Simple_Marker", new Color(0.23f, 0.72f, 0.51f));

        GameObject environment = new GameObject("HospitalEnvironment");
        CreateCube(environment.transform, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(28f, 0.2f, 20f), floorMaterial);
        CreateCube(environment.transform, "OuterNorth", new Vector3(0f, 1.5f, 9.9f), new Vector3(28f, 3f, 0.35f), wallMaterial);
        CreateCube(environment.transform, "OuterSouth_Left", new Vector3(-8.3f, 1.5f, -9.9f), new Vector3(11.4f, 3f, 0.35f), wallMaterial);
        CreateCube(environment.transform, "OuterSouth_Right", new Vector3(8.3f, 1.5f, -9.9f), new Vector3(11.4f, 3f, 0.35f), wallMaterial);
        CreateCube(environment.transform, "OuterWest", new Vector3(-13.9f, 1.5f, 0f), new Vector3(0.35f, 3f, 20f), wallMaterial);
        CreateCube(environment.transform, "OuterEast", new Vector3(13.9f, 1.5f, 0f), new Vector3(0.35f, 3f, 20f), wallMaterial);

        // Floor zones for recognizable hospital areas.
        CreateCube(environment.transform, "Zone_Reception", new Vector3(-9.0f, -0.04f, -5.5f), new Vector3(8.5f, 0.06f, 5.4f), zoneReceptionMaterial);
        CreateCube(environment.transform, "Zone_Waiting", new Vector3(-8.8f, -0.04f, 1.6f), new Vector3(8.0f, 0.06f, 8.0f), zoneWaitingMaterial);
        CreateCube(environment.transform, "Zone_Corridor", new Vector3(0.1f, -0.03f, -0.2f), new Vector3(5.0f, 0.05f, 18.0f), zoneCorridorMaterial);
        CreateCube(environment.transform, "Zone_Clinic", new Vector3(8.4f, -0.04f, 2.5f), new Vector3(9.2f, 0.06f, 12.0f), zoneClinicMaterial);

        // Room walls and corridor segmentation.
        CreateCube(environment.transform, "CorridorLeftWall", new Vector3(-2.8f, 1.5f, -0.5f), new Vector3(0.35f, 3f, 14.5f), wallMaterial);
        CreateCube(environment.transform, "CorridorRightWall_South", new Vector3(2.8f, 1.5f, -4.8f), new Vector3(0.35f, 3f, 7.6f), wallMaterial);
        CreateCube(environment.transform, "CorridorRightWall_North", new Vector3(2.8f, 1.5f, 4.7f), new Vector3(0.35f, 3f, 7.8f), wallMaterial);

        CreateCube(environment.transform, "ReceptionBackWall", new Vector3(-8.2f, 1.5f, -3.0f), new Vector3(7.8f, 3f, 0.35f), wallMaterial);
        CreateCube(environment.transform, "ReceptionSideWall", new Vector3(-11.9f, 1.5f, -1.8f), new Vector3(0.35f, 3f, 4.2f), wallMaterial);
        CreateCube(environment.transform, "WaitingPartition", new Vector3(-5.4f, 1.5f, 5.7f), new Vector3(4.8f, 3f, 0.35f), wallMaterial);

        CreateCube(environment.transform, "ClinicSouthWall", new Vector3(8.2f, 1.5f, -1.2f), new Vector3(10.0f, 3f, 0.35f), wallMaterial);
        CreateCube(environment.transform, "ClinicEastWall", new Vector3(11.8f, 1.5f, 2.9f), new Vector3(0.35f, 3f, 8.2f), wallMaterial);
        CreateCube(environment.transform, "ClinicNorthWall_Left", new Vector3(7.2f, 1.5f, 7.0f), new Vector3(4.8f, 3f, 0.35f), wallMaterial);
        CreateCube(environment.transform, "ClinicNorthWall_Right", new Vector3(12.0f, 1.5f, 7.0f), new Vector3(4.6f, 3f, 0.35f), wallMaterial);
        CreateCube(environment.transform, "ClinicInnerDivider", new Vector3(6.6f, 1.5f, 2.7f), new Vector3(0.35f, 3f, 5.0f), wallMaterial);
        CreateCube(environment.transform, "NurseStationCounter", new Vector3(3.9f, 1.0f, 3.4f), new Vector3(2.0f, 2.0f, 0.8f), accentMaterial);

        // Reception and waiting furniture.
        CreateCube(environment.transform, "ReceptionDesk_Main", new Vector3(-9.6f, 0.65f, -7.1f), new Vector3(4.8f, 1.3f, 1.1f), accentMaterial);
        CreateCube(environment.transform, "ReceptionDesk_Side", new Vector3(-11.5f, 0.65f, -5.1f), new Vector3(1.0f, 1.3f, 3.4f), accentMaterial);
        CreateCube(environment.transform, "ReceptionDesk_Top", new Vector3(-9.6f, 1.35f, -7.1f), new Vector3(5.2f, 0.15f, 1.3f), wallMaterial);
        CreateCube(environment.transform, "QueuePost_01", new Vector3(-6.7f, 0.55f, -6.0f), new Vector3(0.22f, 1.1f, 1.8f), accentMaterial);
        CreateCube(environment.transform, "QueuePost_02", new Vector3(-6.0f, 0.55f, -6.0f), new Vector3(0.22f, 1.1f, 1.8f), accentMaterial);
        CreateCube(environment.transform, "SelfCheckIn", new Vector3(-12.4f, 1.0f, -2.5f), new Vector3(0.8f, 2.0f, 0.8f), accentMaterial);

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                CreateCube(
                    environment.transform,
                    "WaitingSeat_" + row + "_" + col,
                    new Vector3(-10.9f + col * 1.6f, 0.4f, 0.6f + row * 1.9f),
                    new Vector3(1.0f, 0.8f, 0.68f),
                    seatMaterial);
            }
        }
        CreateCube(environment.transform, "WaterDispenser", new Vector3(-12.5f, 0.9f, 5.4f), new Vector3(0.8f, 1.8f, 0.8f), accentMaterial);
        CreateCube(environment.transform, "PosterBoard", new Vector3(-11.8f, 1.2f, 7.3f), new Vector3(1.1f, 2.4f, 0.2f), accentMaterial);

        // Clinic room furniture and medical symbol.
        CreateCube(environment.transform, "DoctorDesk", new Vector3(8.0f, 0.65f, 5.2f), new Vector3(2.8f, 1.3f, 1.2f), accentMaterial);
        CreateCube(environment.transform, "DoctorDeskTop", new Vector3(8.0f, 1.35f, 5.2f), new Vector3(3.2f, 0.15f, 1.4f), wallMaterial);
        CreateCube(environment.transform, "Computer", new Vector3(8.8f, 1.55f, 5.2f), new Vector3(0.8f, 0.4f, 0.5f), accentMaterial);
        CreateCube(environment.transform, "ExamBed", new Vector3(10.6f, 0.55f, 2.8f), new Vector3(2.8f, 1.1f, 1.2f), seatMaterial);
        CreateCube(environment.transform, "MedicineCabinet", new Vector3(12.7f, 1.1f, 5.3f), new Vector3(0.9f, 2.2f, 1.4f), wallMaterial);
        CreateCube(environment.transform, "PrivacyScreen", new Vector3(9.3f, 1.1f, 1.8f), new Vector3(1.3f, 2.2f, 0.2f), accentMaterial);
        CreateCube(environment.transform, "ConsultationChair", new Vector3(4.7f, 0.5f, 5.7f), new Vector3(0.9f, 1.0f, 0.9f), seatMaterial);

        CreateCube(environment.transform, "ClinicCross_Vert", new Vector3(4.1f, 1.9f, 1.9f), new Vector3(0.35f, 1.3f, 0.2f), signMaterial);
        CreateCube(environment.transform, "ClinicCross_Horz", new Vector3(4.1f, 1.9f, 1.9f), new Vector3(1.1f, 0.35f, 0.2f), signMaterial);
        CreateCube(environment.transform, "DoctorZoneMarker", new Vector3(4.7f, 0.03f, 5.7f), new Vector3(2.2f, 0.06f, 1.8f), markerMaterial);

        GameObject doctor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        doctor.name = "Doctor";
        doctor.transform.position = new Vector3(8.0f, 1f, 2.5f);
        doctor.transform.localScale = new Vector3(0.75f, 1f, 0.75f);
        doctor.GetComponent<Renderer>().sharedMaterial = doctorMaterial;
        Object.DestroyImmediate(doctor.GetComponent<Collider>());

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, -8.0f);
        player.GetComponent<Renderer>().sharedMaterial = playerMaterial;
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.48f;
        characterController.center = Vector3.zero;
        Day1PlayerController playerController = player.AddComponent<Day1PlayerController>();

        CreateEnemy("Enemy_A", new Vector3(-8.8f, 1f, -0.5f), enemyMaterial, new Vector3(-8.8f, 1f, 0.8f), new Vector2(5.0f, 8.0f), 1.6f, 1.25f);
        CreateEnemy("Enemy_B", new Vector3(0.3f, 1f, -2.2f), enemyMaterial, new Vector3(0.3f, 1f, -0.5f), new Vector2(3.5f, 13.0f), 1.7f, 1.25f);
        CreateEnemy("Enemy_C", new Vector3(7.7f, 1f, -4.6f), enemyMaterial, new Vector3(8.5f, 1f, -4.5f), new Vector2(5.0f, 4.2f), 1.6f, 1.25f);
        CreateEnemy("Enemy_D", new Vector3(8.6f, 1f, 5.2f), enemyMaterial, new Vector3(8.5f, 1f, 5.0f), new Vector2(4.2f, 3.8f), 1.5f, 1.25f);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.16f, 0.19f);
        camera.orthographic = true;
        camera.orthographicSize = 10.5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraObject.transform.position = new Vector3(0f, 20f, 0f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.color = new Color(1f, 0.97f, 0.91f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(70f, -20f, 0f);

        GameObject canvasObject = new GameObject("Day1UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        CreatePanel(canvasObject.transform, "TopBar", new Color(0.04f, 0.07f, 0.08f, 0.62f),
            new Vector2(0f, 0.95f), Vector2.one, Vector2.zero, Vector2.zero);
        CreateText(canvasObject.transform, "DayTitle", "Day 1  俯视 2D 医院探索", 20, TextAnchor.MiddleLeft, Color.white,
            new Vector2(0.015f, 0.955f), new Vector2(0.36f, 0.995f), Vector2.zero, Vector2.zero, font, FontStyle.Bold);
        CreateText(canvasObject.transform, "ControlsText", "WASD/方向键移动   E交互   Esc主菜单", 16, TextAnchor.MiddleRight,
            new Color(0.85f, 0.90f, 0.90f), new Vector2(0.36f, 0.955f), new Vector2(0.985f, 0.995f), Vector2.zero, Vector2.zero, font, FontStyle.Normal);

        GameObject prompt = CreatePanel(canvasObject.transform, "InteractionPrompt",
            new Color(0.05f, 0.09f, 0.10f, 0.94f), new Vector2(0.40f, 0.12f), new Vector2(0.60f, 0.19f), Vector2.zero, Vector2.zero);
        CreateText(prompt.transform, "PromptText", "按 E 与医生交谈", 24, TextAnchor.MiddleCenter, Color.white,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, font, FontStyle.Bold);

        GameObject dialoguePanel = CreatePanel(canvasObject.transform, "DialoguePanel",
            new Color(0.05f, 0.07f, 0.08f, 0.97f), new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.39f), Vector2.zero, Vector2.zero);
        CreateText(dialoguePanel.transform, "SpeakerText", "医生", 24, TextAnchor.MiddleLeft, new Color(0.45f, 0.85f, 0.78f),
            new Vector2(0.045f, 0.70f), new Vector2(0.25f, 0.92f), Vector2.zero, Vector2.zero, font, FontStyle.Bold);
        Text dialogueText = CreateText(dialoguePanel.transform, "DialogueText", string.Empty, 22, TextAnchor.UpperLeft, Color.white,
            new Vector2(0.045f, 0.27f), new Vector2(0.82f, 0.70f), Vector2.zero, Vector2.zero, font, FontStyle.Normal);
        GameObject continueButtonObject = CreatePanel(dialoguePanel.transform, "ContinueButton",
            new Color(0.16f, 0.50f, 0.48f, 1f), new Vector2(0.76f, 0.08f), new Vector2(0.95f, 0.28f), Vector2.zero, Vector2.zero);
        Button continueButton = continueButtonObject.AddComponent<Button>();
        Text continueText = CreateText(continueButtonObject.transform, "ContinueButtonText", "继续", 20, TextAnchor.MiddleCenter, Color.white,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, font, FontStyle.Bold);

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        GameObject controllerObject = new GameObject("Day1HospitalController");
        Day1HospitalController controller = controllerObject.AddComponent<Day1HospitalController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("player").objectReferenceValue = playerController;
        serializedController.FindProperty("doctor").objectReferenceValue = doctor.transform;
        serializedController.FindProperty("interactionPrompt").objectReferenceValue = prompt;
        serializedController.FindProperty("dialoguePanel").objectReferenceValue = dialoguePanel;
        serializedController.FindProperty("dialogueText").objectReferenceValue = dialogueText;
        serializedController.FindProperty("continueButtonText").objectReferenceValue = continueText;
        serializedController.FindProperty("continueButton").objectReferenceValue = continueButton;
        Collider interactionZone = GameObject.Find("DoctorZoneMarker")?.GetComponent<Collider>();
        serializedController.FindProperty("interactionZone").objectReferenceValue = interactionZone;
        serializedController.FindProperty("zoneTriggerUsesXZOnly").boolValue = true;
        serializedController.FindProperty("interactionDistance").floatValue = 1.8f;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        prompt.SetActive(false);
        dialoguePanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = player;
    }

    private static void CreateEnemy(
        string name,
        Vector3 position,
        Material material,
        Vector3 areaCenter,
        Vector2 areaSize,
        float moveSpeed,
        float catchDistance)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = name;
        enemy.transform.position = position;
        enemy.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
        enemy.GetComponent<Renderer>().sharedMaterial = material;

        CapsuleCollider capsuleCollider = enemy.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            Object.DestroyImmediate(capsuleCollider);
        }

        CharacterController enemyController = enemy.AddComponent<CharacterController>();
        enemyController.height = 2f;
        enemyController.radius = 0.45f;
        enemyController.center = Vector3.zero;

        EnemyWanderer enemyWanderer = enemy.AddComponent<EnemyWanderer>();

        SerializedObject serializedEnemy = new SerializedObject(enemyWanderer);
        serializedEnemy.FindProperty("areaCenter").vector3Value = areaCenter;
        serializedEnemy.FindProperty("areaSize").vector2Value = areaSize;
        serializedEnemy.FindProperty("moveSpeed").floatValue = moveSpeed;
        serializedEnemy.FindProperty("catchDistance").floatValue = catchDistance;
        serializedEnemy.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        const string folder = "Assets/Materials/Day1";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Day1");
        }

        string path = folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreatePanel(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return panel;
    }

    private static Text CreateText(
        Transform parent,
        string name,
        string value,
        int fontSize,
        TextAnchor alignment,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Font font,
        FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return text;
    }
}
