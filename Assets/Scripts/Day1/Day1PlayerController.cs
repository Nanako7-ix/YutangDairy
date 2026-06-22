using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class Day1PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 16f;
    [SerializeField] private bool rotateTowardMoveDirection = true;
    [SerializeField] private Animator movementAnimator;
    [SerializeField] private float animationDampSeconds = 0.08f;
    [SerializeField] private bool lockVerticalPosition = true;

    private CharacterController characterController;
    private float lockedY;
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");

    public bool InputLocked { get; set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        lockedY = transform.position.y;
        if (lockVerticalPosition)
        {
            characterController.stepOffset = 0f;
        }

        if (movementAnimator == null)
        {
            movementAnimator = GetComponentInChildren<Animator>(true);
        }
    }

    private void Update()
    {
        Vector3 move = Vector3.zero;
        if (!InputLocked)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;

            move = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);
            if (rotateTowardMoveDirection && move.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        characterController.Move(move * (moveSpeed * Time.deltaTime));
        KeepVerticalPositionLocked();

        if (movementAnimator != null)
        {
            movementAnimator.SetFloat(
                SpeedParameter,
                move.magnitude,
                Mathf.Max(0f, animationDampSeconds),
                Time.deltaTime);
        }
    }

    private void KeepVerticalPositionLocked()
    {
        if (!lockVerticalPosition || Mathf.Abs(transform.position.y - lockedY) < 0.0001f)
        {
            return;
        }

        Vector3 position = transform.position;
        position.y = lockedY;

        bool wasEnabled = characterController.enabled;
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = wasEnabled;
    }
}
