using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct Day2DragArrowOffsets
{
    public Vector2 start;
    public Vector2 end;

    public Day2DragArrowOffsets(Vector2 startOffset, Vector2 endOffset)
    {
        start = startOffset;
        end = endOffset;
    }
}

public sealed class Day2DragArrowOverlay : MonoBehaviour
{
    private Camera targetCamera;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Text startArrow;
    private Text endArrow;
    private Vector3 startWorldPosition;
    private Vector3 endWorldPosition;
    private Vector2 startScreenOffset;
    private Vector2 endScreenOffset;
    private bool visible;

    public static Day2DragArrowOverlay Ensure(GameObject owner, Camera camera)
    {
        Day2DragArrowOverlay overlay = owner.GetComponent<Day2DragArrowOverlay>();
        if (overlay == null)
        {
            overlay = owner.AddComponent<Day2DragArrowOverlay>();
        }

        overlay.targetCamera = camera != null ? camera : Camera.main;
        overlay.EnsureCanvas();
        return overlay;
    }

    public void Show(
        Vector3 startPosition,
        Vector3 endPosition,
        Vector2 startOffset,
        Vector2 endOffset)
    {
        EnsureCanvas();
        startWorldPosition = startPosition;
        endWorldPosition = endPosition;
        startScreenOffset = startOffset;
        endScreenOffset = endOffset;
        visible = true;
    }

    public void Hide()
    {
        visible = false;
        SetArrowActive(startArrow, false);
        SetArrowActive(endArrow, false);
    }

    public static Vector3 GetVisualCenter(Transform target)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
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

    private void LateUpdate()
    {
        if (!visible || targetCamera == null || canvasRect == null)
        {
            Hide();
            return;
        }

        float bob = Mathf.Sin(Time.unscaledTime * 5f) * 4f;
        UpdateArrow(startArrow, startWorldPosition, startScreenOffset + new Vector2(0f, 34f + bob));
        UpdateArrow(endArrow, endWorldPosition, endScreenOffset + new Vector2(0f, 34f - bob));
    }

    private void EnsureCanvas()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("Day2DragArrowCanvas", typeof(RectTransform));
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        canvasRect = canvasObject.GetComponent<RectTransform>();
        Font font = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "Arial" },
            36);
        startArrow = CreateArrow(canvasObject.transform, "DragStartArrow", font);
        endArrow = CreateArrow(canvasObject.transform, "DragEndArrow", font);
        Hide();
    }

    private static Text CreateArrow(Transform parent, string objectName, Font font)
    {
        GameObject arrowObject = new GameObject(objectName, typeof(RectTransform));
        arrowObject.transform.SetParent(parent, false);

        Text arrow = arrowObject.AddComponent<Text>();
        arrow.text = "▼";
        arrow.font = font;
        arrow.fontSize = 36;
        arrow.fontStyle = FontStyle.Bold;
        arrow.alignment = TextAnchor.MiddleCenter;
        arrow.color = new Color(1f, 0.78f, 0.12f, 1f);
        arrow.raycastTarget = false;

        Outline outline = arrowObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.12f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform rect = arrow.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(56f, 56f);
        return arrow;
    }

    private void UpdateArrow(Text arrow, Vector3 worldPosition, Vector2 screenOffset)
    {
        Vector3 screenPosition = targetCamera.WorldToScreenPoint(worldPosition);
        bool onScreen = screenPosition.z > 0f
            && screenPosition.x >= -30f
            && screenPosition.x <= Screen.width + 30f
            && screenPosition.y >= -30f
            && screenPosition.y <= Screen.height + 30f;
        SetArrowActive(arrow, onScreen);
        if (!onScreen)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out Vector2 localPosition))
        {
            arrow.rectTransform.anchoredPosition = localPosition + screenOffset;
        }
    }

    private static void SetArrowActive(Text arrow, bool active)
    {
        if (arrow != null && arrow.gameObject.activeSelf != active)
        {
            arrow.gameObject.SetActive(active);
        }
    }
}
