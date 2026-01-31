using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 14f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundAcceleration = 80f;
    [SerializeField] private float groundDeceleration = 100f;
    [SerializeField] private float airAcceleration = 45f;
    [SerializeField] private float airDeceleration = 35f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayers;

    private Rigidbody2D rb;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }

        if (jumpAction != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJump;
        }
    }

    private void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.action.performed -= OnJump;
            jumpAction.action.Disable();
        }

        if (moveAction != null)
        {
            moveAction.action.Disable();
        }
    }

    private void FixedUpdate()
    {
        UpdateGrounded();

        float moveX = 0f;
        if (moveAction != null)
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();
            moveX = input.x;
        }

        float targetSpeed = moveX * moveSpeed;
        float accel = GetHorizontalAcceleration(moveX);
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    private float GetHorizontalAcceleration(float moveX)
    {
        bool hasInput = Mathf.Abs(moveX) > 0.01f;
        if (isGrounded)
        {
            return hasInput ? groundAcceleration : groundDeceleration;
        }

        return hasInput ? airAcceleration : airDeceleration;
    }

    private void UpdateGrounded()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayers);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (!isGrounded)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
