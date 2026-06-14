using UnityEngine;
using UnityEngine.Serialization;

public sealed class Day1CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.65f, 0f);
    [SerializeField] private float distance = 6.2f;
    [SerializeField] private float minDistance = 2.2f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float yaw;
    [SerializeField] private float pitch = 22f;
    [SerializeField] private float minPitch = 5f;
    [SerializeField] private float maxPitch = 75f;
    [SerializeField] private float horizontalLookSensitivity = 500f;
    [FormerlySerializedAs("lookSensitivity")]
    [SerializeField] private float verticalLookSensitivity = 200f;
    [SerializeField] private float zoomSensitivity = 1.2f;
    [SerializeField] private float positionSmooth = 10f;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private float collisionPadding = 0.15f;

    private readonly RaycastHit[] collisionHits = new RaycastHit[12];
    private bool lookInputEnabled = true;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }

    public void SetLookInputEnabled(bool enabled)
    {
        lookInputEnabled = enabled;
        SetCursorLocked(enabled);
    }

    private void Start()
    {
        SetCursorLocked(true);
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateOrbitInput();

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + targetOffset;
        Vector3 desiredPosition = focusPoint + orbitRotation * new Vector3(0f, 0f, -distance);
        desiredPosition = ResolveCameraCollision(focusPoint, desiredPosition);
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            1f - Mathf.Exp(-positionSmooth * Time.deltaTime));
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + targetOffset;
        Vector3 desiredPosition = focusPoint + orbitRotation * new Vector3(0f, 0f, -distance);
        transform.position = ResolveCameraCollision(focusPoint, desiredPosition);
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
    }

    private void UpdateOrbitInput()
    {
        if (!lookInputEnabled)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                SetCursorLocked(true);
            }

            return;
        }

        yaw += Input.GetAxisRaw("Mouse X") * horizontalLookSensitivity * Time.deltaTime;
        pitch -= Input.GetAxisRaw("Mouse Y") * verticalLookSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance = Mathf.Clamp(
                distance - scroll * zoomSensitivity,
                minDistance,
                maxDistance);
        }
    }

    private Vector3 ResolveCameraCollision(Vector3 focusPoint, Vector3 desiredPosition)
    {
        Vector3 cameraOffset = desiredPosition - focusPoint;
        float desiredDistance = cameraOffset.magnitude;
        if (desiredDistance <= 0.001f)
        {
            return desiredPosition;
        }

        Vector3 direction = cameraOffset / desiredDistance;
        int hitCount = Physics.SphereCastNonAlloc(
            focusPoint,
            collisionRadius,
            direction,
            collisionHits,
            desiredDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = desiredDistance;
        for (int index = 0; index < hitCount; index++)
        {
            Collider hitCollider = collisionHits[index].collider;
            if (hitCollider == null ||
                (target != null && hitCollider.transform.IsChildOf(target)))
            {
                continue;
            }

            nearestDistance = Mathf.Min(nearestDistance, collisionHits[index].distance);
        }

        if (nearestDistance >= desiredDistance)
        {
            return desiredPosition;
        }

        float safeDistance = Mathf.Max(0.8f, nearestDistance - collisionPadding);
        return focusPoint + direction * safeDistance;
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
