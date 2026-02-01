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
    [SerializeField] private GroundCheck groundCheck;
    [SerializeField] private float legacyGroundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayers;

    [Header("Jump Animation")]
    [SerializeField] private float midAirRangeWidth = 0.5f;

    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.2f, 0.8f);
    [SerializeField] private LayerMask attackLayers;
    [SerializeField] private float attackDuration = 0.2f;
    [Tooltip("Impulse applied to enemies when hit (x=horizontal, y=up).")]
    [SerializeField] private Vector2 attackKnockback = new Vector2(7f, 3f);
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField] private PlayerAttackHitbox attackHitbox;
    [SerializeField] private PlayerSlashVfx slashVfx;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField, Range(0f, 2f)] private float attackVolume = 0.8f;
    [SerializeField, Range(0f, 2f)] private float jumpVolume = 1f;
    [SerializeField, Range(0f, 2f)] private float landVolume = 0.5f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isAttacking;
    [HideInInspector] public bool controlsLocked;
    private bool facingLeft;
    private int jumpState;
    private Vector3 attackPointLocal;
    private Vector3 baseScale;
    private float knockbackLockTimer;
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimJumpState = Animator.StringToHash("JumpState");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
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
        if (attackHitbox == null)
        {
            var hitboxTransform = transform.Find("AttackHitbox");
            if (hitboxTransform != null)
                attackHitbox = hitboxTransform.GetComponent<PlayerAttackHitbox>();
        }
        if (slashVfx == null)
            slashVfx = GetComponentInChildren<PlayerSlashVfx>(true);

        if (attackPoint != null)
            attackPointLocal = attackPoint.localPosition;

        baseScale = transform.localScale;
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
        UpdateJumpState();
        HandleLandingSound();

        if (controlsLocked)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateAnimator(0f);
            return;
        }

        if (knockbackLockTimer > 0f)
        {
            knockbackLockTimer -= Time.fixedDeltaTime;
            UpdateAnimator(rb.linearVelocity.x);
            return;
        }

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
        if (groundCheck != null)
        {
            isGrounded = groundCheck.IsGrounded;
        }
        else
        {
            // fallback to old overlap method if not wired
            isGrounded = false;
        }
    }

    private void HandleLandingSound()
    {
        if (!wasGrounded && isGrounded && landClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(landClip, landVolume);
        }
        wasGrounded = isGrounded;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (controlsLocked || knockbackLockTimer > 0f || !isGrounded)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        if (audioSource != null && jumpClip != null)
            audioSource.PlayOneShot(jumpClip, jumpVolume);
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (controlsLocked || knockbackLockTimer > 0f || isAttacking)
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
        PlayRandomAttackSfx();
        if (slashVfx != null)
            slashVfx.Play();

        DoAttackHit();
        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
    }

    private void PlayRandomAttackSfx()
    {
        if (audioSource == null || attackClips == null || attackClips.Length == 0) return;
        var clip = attackClips[Random.Range(0, attackClips.Length)];
        if (clip != null) audioSource.PlayOneShot(clip, attackVolume);
    }

    private void DoAttackHit()
    {
        if (attackHitbox != null)
        {
            attackHitbox.Activate(attackDuration, attackKnockback, transform.position);
            return;
        }

        if (attackPoint == null)
            return;

        var hits = Physics2D.OverlapBoxAll(attackPoint.position, attackBoxSize, 0f, attackLayers);
        foreach (var hit in hits)
        {
            if (hit == null || hit.transform == transform)
                continue;

            var hurtbox = hit.GetComponent<MeepHurtbox>();
            if (hurtbox != null)
            {
                hurtbox.ApplyHit(1, transform.position, attackKnockback);
                continue;
            }

            var meep = hit.GetComponent<Meep>();
            if (meep != null)
            {
                if (meep.HasHurtbox)
                    continue;
                meep.TakeHit(1, transform.position, attackKnockback);
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
            spriteRenderer.flipX = false;

        var scale = baseScale;
        scale.x = Mathf.Abs(scale.x) * (facingLeft ? -1f : 1f);
        transform.localScale = scale;

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
        animator.SetBool(AnimIsGrounded, isGrounded);
        animator.SetInteger(AnimJumpState, jumpState);
    }

    private void UpdateJumpState()
    {
        if (isGrounded)
        {
            jumpState = -1;
            return;
        }

        float vy = rb.linearVelocity.y;
        if (vy > midAirRangeWidth)
        {
            jumpState = 0;
        }
        else if (vy < -midAirRangeWidth)
        {
            jumpState = 2;
        }
        else
        {
            jumpState = 1;
        }
    }

    public void StartKnockbackLock(float duration)
    {
        if (duration <= 0f) return;
        knockbackLockTimer = Mathf.Max(knockbackLockTimer, duration);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = groundCheck.IsGrounded ? Color.green : Color.yellow;
            var col = groundCheck.GetComponent<Collider2D>() as BoxCollider2D;
            if (col != null)
            {
                Gizmos.matrix = groundCheck.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(col.offset, col.size);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
        if (attackPoint != null)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(attackPoint.position, attackBoxSize);
        }
    }
}
