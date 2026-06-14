using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class Day234DemoSceneBuilder
{
    private const string Day2Path = "Assets/Scenes/Day2_Home.unity";
    private const string Day3Path = "Assets/Scenes/Day3_Home.unity";
    private const string Day4Path = "Assets/Scenes/Day4_Home.unity";

    [MenuItem("Tools/Yutang Diary/Rebuild Day2-Day4 Demo Scenes")]
    public static void RebuildDay2ToDay4()
    {
        BuildDay2();
        BuildDay3();
        BuildDay4();
        AssetDatabase.SaveAssets();
    }

    private static void BuildDay2()
    {
        Scene scene = EditorSceneManager.OpenScene(Day2Path, OpenSceneMode.Single);
        ClearScene(scene);

        GameObject environment = CreateCommonEnvironment("Day2Environment", new Color(0.82f, 0.85f, 0.88f));
        CreateCube(environment.transform, "DiningTable", new Vector3(0f, 0.8f, 2.8f), new Vector3(5.0f, 1.6f, 2.0f), new Color(0.38f, 0.48f, 0.55f));
        CreateCube(environment.transform, "GlucoseMeterZone", new Vector3(0f, 0.2f, -2.8f), new Vector3(4.2f, 0.3f, 2.2f), new Color(0.35f, 0.72f, 0.56f));

        GameObject root = new GameObject("Day2Root");
        root.AddComponent<Day2HomeController>();

        FinalizeScene(scene, root);
    }

    private static void BuildDay3()
    {
        Scene scene = EditorSceneManager.OpenScene(Day3Path, OpenSceneMode.Single);
        ClearScene(scene);

        GameObject environment = CreateCommonEnvironment("Day3Environment", new Color(0.80f, 0.84f, 0.88f));
        CreateCube(environment.transform, "MealTable", new Vector3(-5.2f, 0.8f, 2.3f), new Vector3(3.8f, 1.6f, 2.2f), new Color(0.33f, 0.50f, 0.60f));
        CreateCube(environment.transform, "RunnerTrack", new Vector3(2.4f, 0.05f, 0f), new Vector3(10.5f, 0.1f, 7.2f), new Color(0.72f, 0.76f, 0.81f));
        CreateCube(environment.transform, "LaneDividerLeft", new Vector3(0.7f, 0.12f, 0f), new Vector3(0.1f, 0.24f, 7.2f), new Color(0.90f, 0.92f, 0.94f));
        CreateCube(environment.transform, "LaneDividerRight", new Vector3(4.1f, 0.12f, 0f), new Vector3(0.1f, 0.24f, 7.2f), new Color(0.90f, 0.92f, 0.94f));

        GameObject root = new GameObject("Day3Root");
        root.AddComponent<Day3HomeController>();

        FinalizeScene(scene, root);
    }

    private static void BuildDay4()
    {
        Scene scene = EditorSceneManager.OpenScene(Day4Path, OpenSceneMode.Single);
        ClearScene(scene);

        GameObject environment = CreateCommonEnvironment("Day4Environment", new Color(0.84f, 0.87f, 0.90f));
        CreateCube(environment.transform, "PlanBoard", new Vector3(-4.8f, 1.6f, 2.8f), new Vector3(0.2f, 3.2f, 4.0f), new Color(0.27f, 0.52f, 0.62f));
        CreateCube(environment.transform, "MonitorDesk", new Vector3(2.8f, 0.7f, -2.6f), new Vector3(4.4f, 1.4f, 2.2f), new Color(0.38f, 0.48f, 0.55f));
        CreateCube(environment.transform, "MonitorZone", new Vector3(2.8f, 0.16f, -5.0f), new Vector3(4.8f, 0.32f, 1.8f), new Color(0.35f, 0.72f, 0.56f));

        GameObject root = new GameObject("Day4Root");
        root.AddComponent<Day4HomeController>();

        FinalizeScene(scene, root);
    }

    private static void ClearScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateCommonEnvironment(string name, Color floorColor)
    {
        GameObject environment = new GameObject(name);

        CreateCube(environment.transform, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(24f, 0.2f, 16f), floorColor);
        CreateCube(environment.transform, "NorthWall", new Vector3(0f, 1.5f, 7.9f), new Vector3(24f, 3f, 0.3f), Color.white);
        CreateCube(environment.transform, "SouthWall", new Vector3(0f, 1.5f, -7.9f), new Vector3(24f, 3f, 0.3f), Color.white);
        CreateCube(environment.transform, "WestWall", new Vector3(-11.9f, 1.5f, 0f), new Vector3(0.3f, 3f, 16f), Color.white);
        CreateCube(environment.transform, "EastWall", new Vector3(11.9f, 1.5f, 0f), new Vector3(0.3f, 3f, 16f), Color.white);

        return environment;
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }

        return cube;
    }

    private static void FinalizeScene(Scene scene, GameObject focusTarget)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 9.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.14f, 0.17f, 0.20f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 120f;
        cameraObject.transform.position = new Vector3(0f, 20f, 0f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        light.color = new Color(1f, 0.97f, 0.92f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(65f, -20f, 0f);

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = focusTarget;
    }
}
