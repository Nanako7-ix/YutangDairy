using UnityEngine;

public sealed class EnemyWanderer : MonoBehaviour
{
    [Header("Wander Area (XZ world space)")]
    [SerializeField] private Vector3 areaCenter = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector2 areaSize = new Vector2(34f, 34f);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float arriveDistance = 0.6f;
    [SerializeField] private float repathInterval = 3.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Catch Player")]
    [SerializeField] private float catchDistance = 1.4f;

    [Header("Appearance")]
    [SerializeField] private Color enemyColor = new Color(0.9f, 0.15f, 0.15f, 1f);

    private Day1PlayerController player;
    private CharacterController playerController;
    private CharacterController enemyController;
    private Vector3 playerStartPosition;
    private Vector3 targetPoint;
    private float repathTimer;
    private float fixedY;
    private float verticalVelocity;

    private void Awake()
    {
        enemyController = GetComponent<CharacterController>();
        if (enemyController == null)
        {
            enemyController = gameObject.AddComponent<CharacterController>();
            enemyController.height = 2f;
            enemyController.radius = 0.45f;
            enemyController.center = Vector3.zero;
        }

        ApplyColor();
        fixedY = transform.position.y;
    }

    private void Start()
    {
        player = FindObjectOfType<Day1PlayerController>();
        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
            playerStartPosition = player.transform.position;
        }

        PickNewTarget();
    }

    private void Update()
    {
        Wander();
        CheckCatch();
    }

    private void Wander()
    {
        repathTimer -= Time.deltaTime;

        Vector3 flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTarget = new Vector3(targetPoint.x, 0f, targetPoint.z);

        if (repathTimer <= 0f || Vector3.Distance(flatPos, flatTarget) <= arriveDistance)
        {
            PickNewTarget();
        }

        Vector3 direction = flatTarget - flatPos;
        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            Vector3 velocity = direction * moveSpeed;
            if (enemyController != null)
            {
                if (enemyController.isGrounded && verticalVelocity < 0f)
                {
                    verticalVelocity = -1f;
                }

                verticalVelocity += gravity * Time.deltaTime;
                velocity.y = verticalVelocity;
                CollisionFlags flags = enemyController.Move(velocity * Time.deltaTime);

                if ((flags & CollisionFlags.Sides) != 0)
                {
                    PickNewTarget();
                }

                if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
                {
                    verticalVelocity = -1f;
                }
            }
            else
            {
                Vector3 next = transform.position + direction * moveSpeed * Time.deltaTime;
                next.y = fixedY;
                transform.position = next;
            }
        }
    }

    private void PickNewTarget()
    {
        float halfX = areaSize.x * 0.5f;
        float halfZ = areaSize.y * 0.5f;
        float x = areaCenter.x + Random.Range(-halfX, halfX);
        float z = areaCenter.z + Random.Range(-halfZ, halfZ);
        targetPoint = new Vector3(x, fixedY, z);
        repathTimer = repathInterval;
    }

    private void CheckCatch()
    {
        if (player == null)
        {
            return;
        }

        Vector3 a = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 b = new Vector3(player.transform.position.x, 0f, player.transform.position.z);

        if (Vector3.Distance(a, b) <= catchDistance)
        {
            ResetPlayer();
        }
    }

    private void ResetPlayer()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
            player.transform.position = playerStartPosition;
            playerController.enabled = true;
        }
        else if (player != null)
        {
            player.transform.position = playerStartPosition;
        }
    }

    private void ApplyColor()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            rend.material.color = enemyColor;
        }
    }
}
