using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class Day1PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 16f;
    [SerializeField] private bool rotateTowardMoveDirection = true;

    private CharacterController characterController;

    public bool InputLocked { get; set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
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
    }
}
