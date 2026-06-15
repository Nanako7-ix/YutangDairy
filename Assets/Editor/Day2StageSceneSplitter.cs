using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Day2StageSceneSplitter
{
    private static bool RebuildEnabled => false;
    private const string SourceScenePath = "Assets/Scenes/Day2_LancingStep.unity";
    private const string Stage1Path = "Assets/Scenes/Day2_Stage1_PenAssembly.unity";
    private const string Stage2Path = "Assets/Scenes/Day2_Stage2_InsertStrip.unity";
    private const string Stage3Path = "Assets/Scenes/Day2_Stage3_Puncture.unity";
    private const string Stage4Path = "Assets/Scenes/Day2_Stage4_SpaceMiniGame.unity";
    private const string AutoBuildKey = "YutangDiary_Day2StageSceneSplit_Version";
    private const string AutoBuildVersion = "day2-stage-split-v7";
    private const int Stage1CompleteStep = 5;
    private const int Stage2CompleteStep = 6;
    private const float Day2SceneScaleFactor = 10f;
    private const string NiproTextureFolder = "Assets/External/NiproKit/textures";
    private const string NiproMaterialFolder = "Assets/Materials/NiproAuto";
    private const string DisinfectionTrayObjectName = "DisinfectionTray";
    private const string DisinfectionTraySurfaceObjectName = "DisinfectionTraySurface";
    private const string DisinfectionSwabObjectName = "DisinfectionCottonSwab";
    private const string DisinfectionTrayMaterialPath = NiproMaterialFolder + "/DisinfectionTray.mat";
    private const string DisinfectionSwabMaterialPath = NiproMaterialFolder + "/DisinfectionSwab.mat";
    private const string CartoonHandMaterialPath = NiproMaterialFolder + "/CartoonHandSkin.mat";
    private static readonly Vector3 Stage3TrayOffsetFromHand = new Vector3(0.7f, -0.55f, 0.25f);
    private static readonly Vector3 Stage3PenBodyOffsetFromTray = new Vector3(0.6f, 0.16f, 0.1f);
    private static readonly Vector3 Stage3PenCapOffsetFromBody = new Vector3(-3.669f, -0.2819f, -0.0007f);
    private static readonly Vector3 Stage3PenScale = new Vector3(11f, 11f, 11f);

    [MenuItem("Tools/Yutang Diary/Rebuild Day2 Split Stage Scenes")]
    public static void RebuildFromMenu()
    {
        if (!RebuildEnabled)
        {
            Debug.Log("[Day2StageSceneSplitter] Rebuild is disabled. Edit stage scenes manually.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        RebuildAllScenes();
        EditorPrefs.SetString(AutoBuildKey, AutoBuildVersion);
    }

    [MenuItem("Tools/Yutang Diary/Rebuild Day2 Split Stage Scenes", true)]
    private static bool ValidateRebuildFromMenu()
    {
        return RebuildEnabled;
    }

    // For Unity batch mode: -executeMethod Day2StageSceneSplitter.RebuildFromCommandLine
    public static void RebuildFromCommandLine()
    {
        if (!RebuildEnabled)
        {
            Debug.Log("[Day2StageSceneSplitter] Rebuild is disabled for command line.");
            return;
        }

        RebuildAllScenes();
        EditorPrefs.SetString(AutoBuildKey, AutoBuildVersion);
    }

    [InitializeOnLoadMethod]
    private static void AutoBuildOnceAfterCompile()
    {
        if (!RebuildEnabled)
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorPrefs.GetString(AutoBuildKey, string.Empty) == AutoBuildVersion)
            {
                return;
            }

            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
            {
                return;
            }

            RebuildAllScenes();
            EditorPrefs.SetString(AutoBuildKey, AutoBuildVersion);
        };
    }

    private static void RebuildAllScenes()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
        {
            Debug.LogWarning("[Day2StageSceneSplitter] Source scene missing: " + SourceScenePath);
            return;
        }

        string originalScenePath = EditorSceneManager.GetActiveScene().path;

        BuildStage1();
        BuildStage2();
        BuildStage3();
        BuildStage4();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        Debug.Log("[Day2StageSceneSplitter] Day2 stage scenes rebuilt.");
    }

    private static void BuildStage1()
    {
        Scene scene = PrepareSceneCopy(Stage1Path);
        Day2LancingController controller = FindStageController();
        if (controller == null)
        {
            SaveScene(scene);
            return;
        }

        controller.enabled = true;
        SetControllerStage(controller, 0, Stage1CompleteStep, "Day2_Stage2_InsertStrip");
        SetControllerBool(controller, "autoCreateDisinfectionProps", false);
        SetControllerBool(controller, "enableHints", false);

        SetActive(scene, "LancingSetup", true);
        SetActive(scene, "TrayA", true);
        SetActive(scene, "TrayB", true);
        PruneObjects(
            scene,
            "NiproPreview",
            "NiproMeterProp",
            "NiproTestStripProp",
            "GlucoseMeterProp",
            "TestStripProp",
            "CartoonHandProp",
            "AlcoholSwabProp",
            "AlcoholBottleProp",
            "FingerDisinfectPoint",
            "LancetDockHint",
            "Day2StagePlaceholder");

        RemovePlaceholderControllers();
        Vector3 stageScalePivot = ScaleSceneLayout(scene, Day2SceneScaleFactor);
        ScaleControllerTuning(controller, Day2SceneScaleFactor, stageScalePivot);
        SaveScene(scene);
    }

    private static void BuildStage2()
    {
        Scene scene = PrepareSceneCopy(Stage2Path);
        Day2LancingController controller = FindStageController();
        if (controller == null)
        {
            SaveScene(scene);
            return;
        }

        controller.enabled = true;
        SetControllerStage(controller, Stage1CompleteStep, Stage2CompleteStep, "Day2_Stage3_Puncture");
        SetControllerBool(controller, "autoCreateDisinfectionProps", false);
        SetControllerBool(controller, "enableHints", false);

        DetachToRoot(scene, "NiproPreview");
        SetActive(scene, "NiproPreview", true);
        SetActive(scene, "NiproMeterProp", true);
        SetActive(scene, "NiproTestStripProp", true);
        PruneObjects(
            scene,
            "LancingSetup",
            "TrayA",
            "TrayB",
            "BloodLancetSet",
            "GlucoseMeterProp",
            "TestStripProp",
            "OriginalLancingDevice",
            "OriginalLancingDevice_Cap",
            "OriginalLancingDevice_Body",
            "Process",
            "CartoonHandProp",
            "AlcoholSwabProp",
            "AlcoholBottleProp",
            "FingerDisinfectPoint",
            "LancetDockHint",
            "Day2StagePlaceholder");

        ApplyNiproMeterTextures(scene);
        ApplyNiproStripTextures(scene);
        RemovePlaceholderControllers();
        Vector3 stageScalePivot = ScaleSceneLayout(scene, Day2SceneScaleFactor);
        ScaleControllerTuning(controller, Day2SceneScaleFactor, stageScalePivot);
        SaveScene(scene);
    }

    private static void BuildStage3()
    {
        Scene scene = PrepareSceneCopy(Stage3Path);
        RemoveStageController();

        DetachToRoot(scene, "NiproPreview");
        SetActive(scene, "NiproPreview", true);
        SetActive(scene, "NiproMeterProp", true);
        SetActive(scene, "NiproTestStripProp", true);
        SetActive(scene, "CartoonHandProp", true);
        PruneObjects(
            scene,
            "LancingSetup",
            "TrayA",
            "TrayB",
            "BloodLancetSet",
            "GlucoseMeterProp",
            "TestStripProp",
            "OriginalLancingDevice",
            "Process",
            "AlcoholSwabProp",
            "AlcoholBottleProp",
            "FingerDisinfectPoint",
            "LancetDockHint",
            "Day2StagePlaceholder");

        ApplyNiproMeterTextures(scene);
        ApplyNiproStripTextures(scene);
        EnsureDisinfectionTrayAndSwab(scene);
        ApplyCartoonHandMaterial(scene);
        RemovePlaceholderControllers();
        ConfigurePlaceholder(
            stageIndex: 3,
            title: "阶段3：扎手指测血糖",
            description: "该阶段将实现扎手指、出血与测血糖读数。当前版本先完成了前两个阶段。",
            nextScene: "Day2_Stage4_SpaceMiniGame");

        ScaleSceneLayout(scene, Day2SceneScaleFactor);
        ArrangeStage3Props(scene);
        SaveScene(scene);
    }

    private static void BuildStage4()
    {
        Scene scene = PrepareSceneCopy(Stage4Path);
        RemoveStageController();

        PruneObjects(
            scene,
            "LancingSetup",
            "TrayA",
            "TrayB",
            "BloodLancetSet",
            "NiproPreview",
            "NiproMeterProp",
            "NiproTestStripProp",
            "GlucoseMeterProp",
            "TestStripProp",
            "OriginalLancingDevice",
            "OriginalLancingDevice_Cap",
            "OriginalLancingDevice_Body",
            "Process",
            "CartoonHandProp",
            "AlcoholSwabProp",
            "AlcoholBottleProp",
            "FingerDisinfectPoint",
            "LancetDockHint",
            "Day2StagePlaceholder");

        RemovePlaceholderControllers();
        ConfigurePlaceholder(
            stageIndex: 4,
            title: "阶段4：空格小游戏",
            description: "该阶段将接入按空格的节奏/稳定控制小游戏，用于完成最终采血读数确认。",
            nextScene: "MainMenu");

        ScaleSceneLayout(scene, Day2SceneScaleFactor);
        SaveScene(scene);
    }

    private static Scene PrepareSceneCopy(string stagePath)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(stagePath) != null)
        {
            AssetDatabase.DeleteAsset(stagePath);
        }

        AssetDatabase.CopyAsset(SourceScenePath, stagePath);
        return EditorSceneManager.OpenScene(stagePath, OpenSceneMode.Single);
    }

    private static Day2LancingController FindStageController()
    {
        return Object.FindObjectOfType<Day2LancingController>(true);
    }

    private static void SetControllerStage(Day2LancingController controller, int entryStep, int completeStep, string nextScene)
    {
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("stageEntryStep").intValue = entryStep;
        serialized.FindProperty("stageCompleteStep").intValue = completeStep;
        serialized.FindProperty("allowEnterToLoadNextStage").boolValue = true;
        serialized.FindProperty("nextSceneName").stringValue = nextScene;
        serialized.FindProperty("currentStep").intValue = entryStep;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void SetControllerBool(Day2LancingController controller, string fieldName, bool value)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            return;
        }

        property.boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void ScaleControllerTuning(Day2LancingController controller, float scaleFactor, Vector3 pivot)
    {
        if (controller == null || scaleFactor <= 0f || Mathf.Approximately(scaleFactor, 1f))
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(controller);
        ScaleFloatProperty(serialized, "dockThreshold", scaleFactor);
        ScaleFloatProperty(serialized, "undockThreshold", scaleFactor);
        ScaleFloatProperty(serialized, "trayDropThreshold", scaleFactor);
        ScaleFloatProperty(serialized, "punctureDistance", scaleFactor);
        ScaleFloatProperty(serialized, "stripInsertThreshold", scaleFactor);
        ScaleFloatProperty(serialized, "stripInsertSnapDepth", scaleFactor);
        ScaleFloatProperty(serialized, "stripExposedLength", scaleFactor);
        ScaleFloatProperty(serialized, "swabDipThreshold", scaleFactor);
        ScaleFloatProperty(serialized, "swabDisinfectThreshold", scaleFactor);
        ScaleFloatPropertyAroundPivot(serialized, "dragPlaneHeight", pivot.y, scaleFactor);
        ScaleVector3PropertyAroundPivot(serialized, "recordedStripSnapWorldPos", pivot, scaleFactor);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void ScaleFloatProperty(SerializedObject serialized, string propertyName, float scaleFactor)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.Float)
        {
            return;
        }

        property.floatValue *= scaleFactor;
    }

    private static void ScaleFloatPropertyAroundPivot(SerializedObject serialized, string propertyName, float pivot, float scaleFactor)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.Float)
        {
            return;
        }

        property.floatValue = pivot + ((property.floatValue - pivot) * scaleFactor);
    }

    private static void ScaleVector3PropertyAroundPivot(SerializedObject serialized, string propertyName, Vector3 pivot, float scaleFactor)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.Vector3)
        {
            return;
        }

        Vector3 value = property.vector3Value;
        property.vector3Value = pivot + ((value - pivot) * scaleFactor);
    }

    private static void RemoveStageController()
    {
        Day2LancingController controller = FindStageController();
        if (controller != null)
        {
            Object.DestroyImmediate(controller.gameObject);
        }
    }

    private static void PruneObjects(Scene scene, params string[] objectNames)
    {
        if (objectNames == null)
        {
            return;
        }

        for (int i = 0; i < objectNames.Length; i++)
        {
            DestroyByName(scene, objectNames[i]);
        }
    }

    private static void DestroyByName(Scene scene, string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return;
        }

        while (true)
        {
            GameObject go = FindByName(scene, objectName);
            if (go == null)
            {
                return;
            }

            Object.DestroyImmediate(go);
        }
    }

    private static void SetActive(Scene scene, string objectName, bool active)
    {
        GameObject go = FindByName(scene, objectName);
        if (go != null && go.activeSelf != active)
        {
            go.SetActive(active);
            EditorUtility.SetDirty(go);
        }
    }

    private static void DetachToRoot(Scene scene, string objectName)
    {
        GameObject go = FindByName(scene, objectName);
        if (go == null || go.transform.parent == null)
        {
            return;
        }

        go.transform.SetParent(null, true);
        EditorUtility.SetDirty(go);
    }

    private static void ApplyNiproMeterTextures(Scene scene)
    {
        GameObject meter = FindByName(scene, "NiproMeterProp");
        if (meter == null)
        {
            return;
        }

        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null)
        {
            return;
        }

        EnsureMaterialFolders();
        Renderer[] renderers = meter.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            string textureName = SelectMeterTextureAssetName(renderer.gameObject.name);
            if (string.IsNullOrEmpty(textureName))
            {
                continue;
            }

            ApplyTextureMaterial(renderer, textureName, standardShader);
        }
    }

    private static void ApplyNiproStripTextures(Scene scene)
    {
        GameObject strip = FindByName(scene, "NiproTestStripProp");
        if (strip == null)
        {
            return;
        }

        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null)
        {
            return;
        }

        EnsureMaterialFolders();
        Renderer[] renderers = strip.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            string textureName = SelectStripTextureAssetName(renderer.gameObject.name);
            if (string.IsNullOrEmpty(textureName))
            {
                continue;
            }

            ApplyTextureMaterial(renderer, textureName, standardShader);
        }
    }

    private static void ApplyTextureMaterial(Renderer renderer, string textureName, Shader standardShader)
    {
        string texturePath = NiproTextureFolder + "/" + textureName + ".png";
        Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (baseTexture == null)
        {
            return;
        }

        string materialPath = NiproMaterialFolder + "/" + textureName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(standardShader) { name = textureName };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.mainTexture = baseTexture;
        material.color = Color.white;
        EditorUtility.SetDirty(material);

        renderer.sharedMaterial = material;
        EditorUtility.SetDirty(renderer);
    }

    private static void EnsureMaterialFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(NiproMaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "NiproAuto");
        }
    }

    private static string SelectMeterTextureAssetName(string rendererName)
    {
        string normalized = rendererName.ToLowerInvariant();
        if (normalized == "top body")
        {
            return "top_body_BaseColor";
        }

        if (normalized == "bottom body")
        {
            return "bottom_body_BaseColor";
        }

        if (normalized == "main button")
        {
            return "main_botton_BaseColor";
        }

        if (normalized == "thumb spot")
        {
            return "thumb_spot_BaseColor";
        }

        if (normalized == "lcd")
        {
            return "lcd_BaseColor";
        }

        if (normalized == "foot cover")
        {
            return "foot_cover_BaseColor";
        }

        if (normalized == "edge body")
        {
            return "main_body_BaseColor";
        }

        if (normalized == "glass")
        {
            return "lcd_BaseColor";
        }

        if (normalized.StartsWith("back."))
        {
            return "mainbody_BaseColor";
        }

        return "mainbody_BaseColor";
    }

    private static string SelectStripTextureAssetName(string rendererName)
    {
        string normalized = rendererName.ToLowerInvariant();
        if (normalized == "reader")
        {
            return "reader_BaseColor";
        }

        if (normalized == "bloodreceiver")
        {
            return "bloodreceiver_BaseColor";
        }

        if (normalized == "mainbody")
        {
            return "mainbody_BaseColor";
        }

        return string.Empty;
    }

    private static Vector3 ScaleSceneLayout(Scene scene, float scaleFactor)
    {
        if (scaleFactor <= 0f || Mathf.Approximately(scaleFactor, 1f))
        {
            return Vector3.zero;
        }

        Vector3 pivot = CalculateScenePivot(scene);
        GameObject[] roots = scene.GetRootGameObjects();
        Vector3 scaleVector = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null || root.GetComponentInChildren<Camera>(true) != null)
            {
                continue;
            }

            Transform transform = root.transform;
            transform.position = pivot + ((transform.position - pivot) * scaleFactor);
            transform.localScale = Vector3.Scale(transform.localScale, scaleVector);
            EditorUtility.SetDirty(root);
        }

        AdjustSceneCamerasForScaledLayout(scene, pivot, scaleFactor);
        return pivot;
    }

    private static void AdjustSceneCamerasForScaledLayout(Scene scene, Vector3 pivot, float scaleFactor)
    {
        Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || camera.gameObject.scene != scene)
            {
                continue;
            }

            Transform transform = camera.transform;
            transform.position = pivot + ((transform.position - pivot) * scaleFactor);
            if (camera.orthographic)
            {
                camera.orthographicSize = Mathf.Max(0.2f, camera.orthographicSize * scaleFactor);
            }

            if (scaleFactor > 1f)
            {
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, camera.farClipPlane * scaleFactor);
            }

            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(camera.gameObject);
        }
    }

    private static Vector3 CalculateScenePivot(Scene scene)
    {
        Renderer[] renderers = Object.FindObjectsOfType<Renderer>(true);
        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.gameObject.scene != scene)
            {
                continue;
            }

            sum += renderer.bounds.center;
            count++;
        }

        if (count > 0)
        {
            return sum / count;
        }

        return Vector3.zero;
    }

    private static void EnsureDisinfectionTrayAndSwab(Scene scene)
    {
        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null)
        {
            return;
        }

        EnsureMaterialFolders();

        GameObject tray = FindByName(scene, DisinfectionTrayObjectName);
        if (tray == null)
        {
            tray = new GameObject(DisinfectionTrayObjectName);
            SceneManager.MoveGameObjectToScene(tray, scene);
        }

        tray.transform.position = new Vector3(0.24f, 0.83f, 0.20f);
        tray.transform.rotation = Quaternion.identity;
        tray.transform.localScale = Vector3.one;

        // Upgrade old primitive root to an empty parent so child props are not distorted by scale.
        MeshRenderer legacyRenderer = tray.GetComponent<MeshRenderer>();
        if (legacyRenderer != null)
        {
            Object.DestroyImmediate(legacyRenderer);
        }

        MeshFilter legacyFilter = tray.GetComponent<MeshFilter>();
        if (legacyFilter != null)
        {
            Object.DestroyImmediate(legacyFilter);
        }

        Collider legacyCollider = tray.GetComponent<Collider>();
        if (legacyCollider != null)
        {
            Object.DestroyImmediate(legacyCollider);
        }

        GameObject traySurface = FindByName(scene, DisinfectionTraySurfaceObjectName);
        if (traySurface == null)
        {
            traySurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            traySurface.name = DisinfectionTraySurfaceObjectName;
            SceneManager.MoveGameObjectToScene(traySurface, scene);
        }

        traySurface.transform.SetParent(tray.transform, false);
        traySurface.transform.localPosition = Vector3.zero;
        traySurface.transform.localRotation = Quaternion.identity;
        traySurface.transform.localScale = new Vector3(0.24f, 0.02f, 0.14f);
        AssignSimpleColorMaterial(traySurface.GetComponent<Renderer>(), DisinfectionTrayMaterialPath, new Color(0.18f, 0.36f, 0.68f), standardShader);
        EditorUtility.SetDirty(traySurface);
        EditorUtility.SetDirty(tray);

        GameObject swab = FindByName(scene, DisinfectionSwabObjectName);
        if (swab == null)
        {
            swab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            swab.name = DisinfectionSwabObjectName;
            SceneManager.MoveGameObjectToScene(swab, scene);
        }

        swab.transform.SetParent(tray.transform, false);
        swab.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        swab.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        swab.transform.localScale = new Vector3(0.012f, 0.05f, 0.012f);
        AssignSimpleColorMaterial(swab.GetComponent<Renderer>(), DisinfectionSwabMaterialPath, new Color(0.94f, 0.95f, 0.97f), standardShader);
        EditorUtility.SetDirty(swab);
    }

    private static void ApplyCartoonHandMaterial(Scene scene)
    {
        GameObject hand = FindByName(scene, "CartoonHandProp");
        if (hand == null)
        {
            return;
        }

        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null)
        {
            return;
        }

        EnsureMaterialFolders();
        Renderer[] renderers = hand.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            AssignSimpleColorMaterial(renderers[i], CartoonHandMaterialPath, new Color(0.93f, 0.74f, 0.63f), standardShader);
            EditorUtility.SetDirty(renderers[i]);
        }
    }

    private static void ArrangeStage3Props(Scene scene)
    {
        GameObject hand = FindByName(scene, "CartoonHandProp");
        GameObject tray = FindByName(scene, DisinfectionTrayObjectName);
        if (hand != null && tray != null)
        {
            tray.transform.SetParent(null, true);
            tray.transform.position = hand.transform.position + Stage3TrayOffsetFromHand;
            tray.transform.rotation = Quaternion.identity;
            tray.transform.localScale = new Vector3(10f, 10f, 10f);
            EditorUtility.SetDirty(tray);
        }

        GameObject swab = FindByName(scene, DisinfectionSwabObjectName);
        if (tray != null && swab != null)
        {
            swab.transform.SetParent(tray.transform, true);
            swab.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            swab.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            EditorUtility.SetDirty(swab);
        }

        GameObject penBody = FindByName(scene, "OriginalLancingDevice_Body");
        GameObject penCap = FindByName(scene, "OriginalLancingDevice_Cap");
        if (penBody == null || penCap == null)
        {
            return;
        }

        Vector3 bodyTarget = tray != null
            ? tray.transform.position + Stage3PenBodyOffsetFromTray
            : (hand != null ? hand.transform.position + new Vector3(1.1f, -0.25f, 0.35f) : penBody.transform.position);

        penBody.SetActive(true);
        penBody.transform.SetParent(null, true);
        penBody.transform.position = bodyTarget;
        penBody.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
        penBody.transform.localScale = Stage3PenScale;
        EditorUtility.SetDirty(penBody);

        penCap.SetActive(true);
        penCap.transform.SetParent(null, true);
        penCap.transform.position = bodyTarget + Stage3PenCapOffsetFromBody;
        penCap.transform.rotation = penBody.transform.rotation;
        penCap.transform.localScale = Stage3PenScale;
        EditorUtility.SetDirty(penCap);
    }

    private static void AssignSimpleColorMaterial(Renderer renderer, string materialPath, Color color, Shader standardShader)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(standardShader)
            {
                name = Path.GetFileNameWithoutExtension(materialPath)
            };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        renderer.sharedMaterial = material;
        EditorUtility.SetDirty(renderer);
    }

    private static GameObject FindByName(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindInChildren(roots[i].transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindInChildren(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindInChildren(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void RemovePlaceholderControllers()
    {
        Day2StagePlaceholderController[] placeholders = Object.FindObjectsOfType<Day2StagePlaceholderController>(true);
        for (int i = 0; i < placeholders.Length; i++)
        {
            Object.DestroyImmediate(placeholders[i]);
        }
    }

    private static void ConfigurePlaceholder(int stageIndex, string title, string description, string nextScene)
    {
        Day2StagePlaceholderController placeholder = Object.FindObjectOfType<Day2StagePlaceholderController>(true);
        if (placeholder == null)
        {
            GameObject host = new GameObject("Day2StagePlaceholder");
            placeholder = host.AddComponent<Day2StagePlaceholderController>();
        }

        SerializedObject serialized = new SerializedObject(placeholder);
        serialized.FindProperty("stageIndex").intValue = stageIndex;
        serialized.FindProperty("stageTitle").stringValue = title;
        serialized.FindProperty("stageDescription").stringValue = description;
        serialized.FindProperty("nextSceneName").stringValue = nextScene;
        serialized.FindProperty("allowEnterToNextScene").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(placeholder);
    }

    private static void UpdateBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        EnsureSceneInBuildSettings(scenes, Stage1Path);
        EnsureSceneInBuildSettings(scenes, Stage2Path);
        EnsureSceneInBuildSettings(scenes, Stage3Path);
        EnsureSceneInBuildSettings(scenes, Stage4Path);
        SetSceneEnabled(scenes, SourceScenePath, false);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureSceneInBuildSettings(List<EditorBuildSettingsScene> scenes, string scenePath)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                scenes[i].enabled = true;
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
    }

    private static void SetSceneEnabled(List<EditorBuildSettingsScene> scenes, string scenePath, bool enabled)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                scenes[i].enabled = enabled;
                return;
            }
        }
    }

    private static void SaveScene(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
