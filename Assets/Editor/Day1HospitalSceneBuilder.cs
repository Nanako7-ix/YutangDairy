using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Day1HospitalSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Day1_Hospital.unity";
    private const string BuildKey = "YutangDiary.Day1HospitalScene.Version";
    private const int BuildVersion = 1;

    static Day1HospitalSceneBuilder()
    {
        EditorApplication.delayCall += BuildOnce;
    }

    [MenuItem("Tools/Yutang Diary/Rebuild Day 1 Hospital")]
    public static void RebuildFromMenu()
    {
        BuildScene(true);
    }

    private static void BuildOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorPrefs.GetInt(BuildKey, 0) >= BuildVersion)
        {
            return;
        }

        BuildScene(false);
        EditorPrefs.SetInt(BuildKey, BuildVersion);
    }

    private static void BuildScene(bool showDialog)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(root);
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.36f, 0.39f, 0.42f, 1f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        Material floorMaterial = CreateMaterial("Day1_Floor", new Color(0.48f, 0.51f, 0.52f));
        Material wallMaterial = CreateMaterial("Day1_Wall", new Color(0.74f, 0.78f, 0.78f));
        Material accentMaterial = CreateMaterial("Day1_Accent", new Color(0.25f, 0.58f, 0.60f));
        Material seatMaterial = CreateMaterial("Day1_Seat", new Color(0.24f, 0.37f, 0.48f));
        Material playerMaterial = CreateMaterial("Day1_Player", new Color(0.91f, 0.67f, 0.24f));
        Material doctorMaterial = CreateMaterial("Day1_Doctor", new Color(0.86f, 0.92f, 0.92f));
        Material markerMaterial = CreateMaterial("Day1_Marker", new Color(0.25f, 0.78f, 0.48f));

        GameObject environment = new GameObject("HospitalEnvironment");
        CreateCube(environment.transform, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(18f, 0.2f, 13f), floorMaterial);
        CreateCube(environment.transform, "NorthWall", new Vector3(0f, 1.5f, 6.45f), new Vector3(18f, 3f, 0.2f), wallMaterial);
        CreateCube(environment.transform, "SouthWall_Left", new Vector3(-5.5f, 1.5f, -6.45f), new Vector3(7f, 3f, 0.2f), wallMaterial);
        CreateCube(environment.transform, "SouthWall_Right", new Vector3(5.5f, 1.5f, -6.45f), new Vector3(7f, 3f, 0.2f), wallMaterial);
        CreateCube(environment.transform, "WestWall", new Vector3(-8.95f, 1.5f, 0f), new Vector3(0.2f, 3f, 13f), wallMaterial);
        CreateCube(environment.transform, "EastWall", new Vector3(8.95f, 1.5f, 0f), new Vector3(0.2f, 3f, 13f), wallMaterial);
        CreateCube(environment.transform, "ClinicDividerSouth", new Vector3(3.1f, 1.5f, -1.35f), new Vector3(0.2f, 3f, 1.1f), wallMaterial);
        CreateCube(environment.transform, "ClinicDividerNorth", new Vector3(3.1f, 1.5f, 3.62f), new Vector3(0.2f, 3f, 5.65f), wallMaterial);
        CreateCube(environment.transform, "ClinicTopWall", new Vector3(6f, 1.5f, -1.9f), new Vector3(6f, 3f, 0.2f), wallMaterial);
        CreateCube(environment.transform, "ReceptionDesk", new Vector3(-4.8f, 0.65f, 2.8f), new Vector3(4.4f, 1.3f, 1.1f), accentMaterial);
        CreateCube(environment.transform, "ReceptionCounter", new Vector3(-4.8f, 1.35f, 2.8f), new Vector3(4.8f, 0.15f, 1.3f), wallMaterial);

        GameObject waitingArea = new GameObject("WaitingArea");
        waitingArea.transform.SetParent(environment.transform, false);
        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                CreateCube(
                    waitingArea.transform,
                    "Seat_" + row + "_" + column,
                    new Vector3(-4.8f + column * 1.45f, 0.35f, -0.5f - row * 1.45f),
                    new Vector3(1.05f, 0.7f, 0.75f),
                    seatMaterial);
            }
        }

        GameObject clinic = new GameObject("ClinicRoom");
        CreateCube(clinic.transform, "DoctorDesk", new Vector3(5.9f, 0.65f, 3.9f), new Vector3(3.2f, 1.3f, 1.2f), accentMaterial);
        CreateCube(clinic.transform, "DoctorDeskTop", new Vector3(5.9f, 1.35f, 3.9f), new Vector3(3.5f, 0.15f, 1.4f), wallMaterial);
        CreateCube(clinic.transform, "ClinicMarker", new Vector3(3.7f, 0.03f, 0f), new Vector3(1.1f, 0.06f, 2.2f), markerMaterial);

        GameObject doctor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        doctor.name = "Doctor";
        doctor.transform.SetParent(clinic.transform, false);
        doctor.transform.position = new Vector3(6f, 1f, 2.6f);
        doctor.transform.localScale = new Vector3(0.72f, 1f, 0.72f);
        doctor.GetComponent<Renderer>().sharedMaterial = doctorMaterial;
        Object.DestroyImmediate(doctor.GetComponent<Collider>());
        GameObject doctorHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        doctorHead.name = "Head";
        doctorHead.transform.SetParent(doctor.transform, false);
        doctorHead.transform.localPosition = new Vector3(0f, 1.18f, 0f);
        doctorHead.transform.localScale = Vector3.one * 0.62f;
        doctorHead.GetComponent<Renderer>().sharedMaterial = doctorMaterial;
        Object.DestroyImmediate(doctorHead.GetComponent<Collider>());

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, -4.8f);
        player.GetComponent<Renderer>().sharedMaterial = playerMaterial;
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.48f;
        characterController.center = Vector3.zero;
        Day1PlayerController playerController = player.AddComponent<Day1PlayerController>();

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.14f, 0.17f, 0.18f);
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        cameraObject.AddComponent<AudioListener>();
        Day1CameraFollow cameraFollow = cameraObject.AddComponent<Day1CameraFollow>();
        cameraFollow.SetTarget(player.transform);

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        light.color = new Color(1f, 0.96f, 0.90f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

        GameObject canvasObject = new GameObject("Day1UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        CreatePanel(canvasObject.transform, "TopBar", new Color(0.04f, 0.07f, 0.08f, 0.88f),
            new Vector2(0f, 0.86f), Vector2.one, Vector2.zero, Vector2.zero);
        CreateText(canvasObject.transform, "DayTitle", "Day 1  医院探索", 28, TextAnchor.MiddleLeft, Color.white,
            new Vector2(0.035f, 0.90f), new Vector2(0.45f, 0.98f), Vector2.zero, Vector2.zero, font, FontStyle.Bold);
        CreateText(canvasObject.transform, "ObjectiveText", "目标：前往诊室，与医生交谈", 22, TextAnchor.MiddleRight,
            new Color(0.83f, 0.93f, 0.89f), new Vector2(0.48f, 0.90f), new Vector2(0.96f, 0.98f),
            Vector2.zero, Vector2.zero, font, FontStyle.Normal);
        CreateText(canvasObject.transform, "ControlsText", "WASD / 方向键移动    移动鼠标调整相机    滚轮缩放    E 交互", 18,
            TextAnchor.MiddleLeft, new Color(0.85f, 0.90f, 0.90f), new Vector2(0.035f, 0.02f),
            new Vector2(0.58f, 0.085f), Vector2.zero, Vector2.zero, font, FontStyle.Normal);

        GameObject prompt = CreatePanel(canvasObject.transform, "InteractionPrompt",
            new Color(0.05f, 0.09f, 0.10f, 0.94f), new Vector2(0.38f, 0.12f),
            new Vector2(0.62f, 0.20f), Vector2.zero, Vector2.zero);
        CreateText(prompt.transform, "PromptText", "按 E 与医生交谈", 23, TextAnchor.MiddleCenter, Color.white,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, font, FontStyle.Bold);

        GameObject dialoguePanel = CreatePanel(canvasObject.transform, "DialoguePanel",
            new Color(0.05f, 0.07f, 0.08f, 0.97f), new Vector2(0.12f, 0.10f),
            new Vector2(0.88f, 0.39f), Vector2.zero, Vector2.zero);
        CreateText(dialoguePanel.transform, "SpeakerText", "医生", 24, TextAnchor.MiddleLeft,
            new Color(0.45f, 0.85f, 0.78f), new Vector2(0.045f, 0.70f), new Vector2(0.25f, 0.92f),
            Vector2.zero, Vector2.zero, font, FontStyle.Bold);
        Text dialogueText = CreateText(dialoguePanel.transform, "DialogueText", string.Empty, 22,
            TextAnchor.UpperLeft, Color.white, new Vector2(0.045f, 0.27f), new Vector2(0.82f, 0.70f),
            Vector2.zero, Vector2.zero, font, FontStyle.Normal);
        GameObject continueButtonObject = CreatePanel(dialoguePanel.transform, "ContinueButton",
            new Color(0.16f, 0.50f, 0.48f, 1f), new Vector2(0.76f, 0.08f), new Vector2(0.95f, 0.28f),
            Vector2.zero, Vector2.zero);
        Button continueButton = continueButtonObject.AddComponent<Button>();
        Text continueText = CreateText(continueButtonObject.transform, "ContinueButtonText", "继续", 20,
            TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            font, FontStyle.Bold);

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
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        prompt.SetActive(false);
        dialoguePanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = player;
        SceneView.lastActiveSceneView?.FrameSelected();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Day 1 Hospital", "Day 1 hospital scene rebuilt.", "OK");
        }
    }

    private static GameObject CreateCube(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Material material)
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
