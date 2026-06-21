using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class TestStripEndpointEditor
{
    private const string MenuRoot = "Tools/Yutang Diary/Test Strip Endpoint/";
    private const string SessionKey = "GlucoseDiary.TestStripEndpointEditState";

    [Serializable]
    private sealed class EditState
    {
        public string scenePath;
        public Vector3 startLocalPosition;
        public Quaternion startLocalRotation;
        public Vector3 startLocalScale;
        public Vector3 assemblyStartLocalPosition;
        public Quaternion assemblyStartLocalRotation;
        public Vector3 assemblyStartLocalScale;
    }

    [MenuItem(MenuRoot + "1 Begin Edit")]
    private static void BeginEdit()
    {
        if (!EnsureEditMode()
            || !TryResolveSceneObjects(
                out _,
                out Transform strip,
                out Transform finalPose,
                out Transform assembly,
                out Transform assemblyFinalPose))
        {
            return;
        }

        if (!string.IsNullOrEmpty(SessionState.GetString(SessionKey, string.Empty)))
        {
            Debug.LogWarning("[TestStripEndpointEditor] An endpoint edit is already active. Save or cancel it first.");
            return;
        }

        var state = new EditState
        {
            scenePath = SceneManager.GetActiveScene().path,
            startLocalPosition = strip.localPosition,
            startLocalRotation = strip.localRotation,
            startLocalScale = strip.localScale,
            assemblyStartLocalPosition = assembly.localPosition,
            assemblyStartLocalRotation = assembly.localRotation,
            assemblyStartLocalScale = assembly.localScale
        };
        SessionState.SetString(SessionKey, JsonUtility.ToJson(state));

        Undo.RecordObjects(new UnityEngine.Object[] { strip, assembly }, "Begin test strip endpoint edit");
        CopyLocalPose(strip, finalPose);
        CopyLocalPose(assembly, assemblyFinalPose);
        EditorUtility.SetDirty(strip);
        EditorUtility.SetDirty(assembly);

        Selection.activeGameObject = strip.gameObject;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("[TestStripEndpointEditor] Endpoint edit started. Move/rotate NiproTestStripProp, then choose '2 Save Endpoint'.");
    }

    [MenuItem(MenuRoot + "2 Save Endpoint")]
    private static void SaveEndpoint()
    {
        if (!EnsureEditMode()
            || !TryGetState(out EditState state)
            || !TryResolveSceneObjects(
                out Day2LancingController controller,
                out Transform strip,
                out Transform finalPose,
                out Transform assembly,
                out Transform assemblyFinalPose))
        {
            return;
        }

        if (state.scenePath != SceneManager.GetActiveScene().path)
        {
            Debug.LogWarning("[TestStripEndpointEditor] Return to the scene where endpoint editing started before saving.");
            return;
        }

        Undo.RecordObjects(
            new UnityEngine.Object[] { controller, strip, finalPose, assembly, assemblyFinalPose },
            "Save test strip endpoint");
        CopyLocalPose(finalPose, strip);
        CopyLocalPose(assemblyFinalPose, assembly);

        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("useRecordedStripSnapPose").boolValue = true;
        serializedController.FindProperty("recordedStripSnapWorldPos").vector3Value = finalPose.position;
        serializedController.FindProperty("recordedStripSnapWorldEuler").vector3Value = finalPose.eulerAngles;
        serializedController.FindProperty("recordedStripSnapLocalScale").vector3Value = finalPose.localScale;
        serializedController.ApplyModifiedProperties();

        RestoreStartPose(strip, assembly, state);
        SessionState.EraseString(SessionKey);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(strip);
        EditorUtility.SetDirty(finalPose);
        EditorUtility.SetDirty(assembly);
        EditorUtility.SetDirty(assemblyFinalPose);
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = controller.gameObject;
        Debug.Log("[TestStripEndpointEditor] Endpoint saved and the strip start pose restored: "
            + finalPose.localPosition + " / " + finalPose.localEulerAngles);
    }

    [MenuItem(MenuRoot + "3 Cancel Edit")]
    private static void CancelEdit()
    {
        if (!EnsureEditMode() || !TryGetState(out EditState state))
        {
            return;
        }

        Transform strip = FindSceneTransform("NiproTestStripProp");
        Transform assembly = FindSceneTransform("TestStripAssembly");
        if (strip == null || assembly == null || state.scenePath != SceneManager.GetActiveScene().path)
        {
            Debug.LogWarning("[TestStripEndpointEditor] Return to the scene where endpoint editing started before cancelling.");
            return;
        }

        Undo.RecordObjects(new UnityEngine.Object[] { strip, assembly }, "Cancel test strip endpoint edit");
        RestoreStartPose(strip, assembly, state);
        SessionState.EraseString(SessionKey);
        EditorUtility.SetDirty(strip);
        EditorUtility.SetDirty(assembly);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[TestStripEndpointEditor] Endpoint edit cancelled and the strip start pose restored.");
    }

    private static bool EnsureEditMode()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return true;
        }

        Debug.LogWarning("[TestStripEndpointEditor] Exit Play mode before editing the endpoint.");
        return false;
    }

    private static bool TryGetState(out EditState state)
    {
        string json = SessionState.GetString(SessionKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            state = null;
            Debug.LogWarning("[TestStripEndpointEditor] No endpoint edit is active. Choose '1 Begin Edit' first.");
            return false;
        }

        state = JsonUtility.FromJson<EditState>(json);
        return state != null;
    }

    private static bool TryResolveSceneObjects(
        out Day2LancingController controller,
        out Transform strip,
        out Transform finalPose,
        out Transform assembly,
        out Transform assemblyFinalPose)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        controller = Resources.FindObjectsOfTypeAll<Day2LancingController>()
            .FirstOrDefault(item => item.gameObject.scene == activeScene);
        strip = FindSceneTransform("NiproTestStripProp");
        finalPose = FindSceneTransform("TestStripFinalPose");
        assembly = FindSceneTransform("TestStripAssembly");
        assemblyFinalPose = FindSceneTransform("TestStripAssemblyFinalPose");

        if (controller != null
            && strip != null
            && finalPose != null
            && assembly != null
            && assemblyFinalPose != null)
        {
            return true;
        }

        Debug.LogWarning("[TestStripEndpointEditor] Open the insert-strip scene before using this tool.");
        return false;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(item => item.gameObject.scene == activeScene && item.name == objectName);
    }

    private static void CopyLocalPose(Transform target, Transform source)
    {
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    private static void RestoreStartPose(Transform strip, Transform assembly, EditState state)
    {
        strip.localPosition = state.startLocalPosition;
        strip.localRotation = state.startLocalRotation;
        strip.localScale = state.startLocalScale;
        assembly.localPosition = state.assemblyStartLocalPosition;
        assembly.localRotation = state.assemblyStartLocalRotation;
        assembly.localScale = state.assemblyStartLocalScale;
    }
}
