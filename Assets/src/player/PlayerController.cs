using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference attackAction;

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

    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.2f, 0.8f);
    [SerializeField] private LayerMask attackLayers;
    [SerializeField] private float attackDuration = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isAttacking;
    private bool facingLeft;
    private Vector3 attackPointLocal;
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (attackPoint == null)
        {
            var found = transform.Find("Visuals/AttackPoint");
            if (found != null)
                attackPoint = found;
        }

        if (attackPoint != null)
            attackPointLocal = attackPoint.localPosition;
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

        if (attackAction != null)
        {
            attackAction.action.Enable();
            attackAction.action.performed += OnAttack;
        }
    }

    private void OnDisable()
    {
        if (attackAction != null)
        {
            attackAction.action.performed -= OnAttack;
            attackAction.action.Disable();
        }

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

        UpdateFacing(moveX);
        UpdateAnimator(moveX);
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

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (isAttacking)
        {
            return;
        }

        StartCoroutine(AttackRoutine());
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        isAttacking = true;
        if (animator != null)
            animator.SetTrigger(AnimAttack);

        DoAttackHit();
        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
    }

    private void DoAttackHit()
    {
        if (attackPoint == null)
            return;

        var hits = Physics2D.OverlapBoxAll(attackPoint.position, attackBoxSize, 0f, attackLayers);
        foreach (var hit in hits)
        {
            if (hit == null || hit.transform == transform)
                continue;

            var meep = hit.GetComponent<Meep>();
            if (meep != null)
            {
                meep.TakeHit(1);
                continue;
            }

            var boss = hit.GetComponent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(1);
            }
        }
    }

    private void UpdateFacing(float moveX)
    {
        if (moveX > 0.01f)
            facingLeft = false;
        else if (moveX < -0.01f)
            facingLeft = true;

        if (spriteRenderer != null)
            spriteRenderer.flipX = facingLeft;

        if (attackPoint != null)
        {
            var local = attackPointLocal;
            local.x = Mathf.Abs(local.x) * (facingLeft ? -1f : 1f);
            attackPoint.localPosition = local;
        }
    }

    private void UpdateAnimator(float moveX)
    {
        if (animator == null)
            return;

        float speed = Mathf.Abs(moveX);
        animator.SetBool(AnimIsMoving, speed > 0.01f);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);

        if (attackPoint != null)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(attackPoint.position, attackBoxSize);
        }
    }
}
