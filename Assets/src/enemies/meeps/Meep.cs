using System.Collections;
using UnityEngine;

/// Simplified enemy logic placeholder for jam MVP.
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Meep : MonoBehaviour
{
    public enum State { Falling, Dormant, Active, Dead }

    [Header("Config")]
    public int baseHitsToKill = 3;
    [Tooltip("Random +/- applied on spawn to vary hits to kill.")]
    public int hitVariance = 2;
    [Tooltip("Seconds to stay sin colisión tras aterrizar.")]
    public float dormantSeconds = 2f;
    [Tooltip("Horizontal speed when activo.")]
    public float moveSpeed = 3f;
    [Tooltip("Daño al tocar mientras está Falling o Active.")]
    public int touchDamage = 1;
    [Tooltip("Capas con las que rebotará (muros laterales).")]
    public LayerMask wallMask;
    [Header("Ground Check")]
    [Tooltip("Punto de chequeo de suelo (opcional).")]
    public Transform groundCheck;
    [Tooltip("Radio del chequeo de suelo.")]
    public float groundRadius = 0.15f;
    [Tooltip("Capas consideradas suelo.")]
    public LayerMask groundMask;

    [Header("Events (assign in editor)")]
    public UnityEngine.Events.UnityEvent onDeath;
    public UnityEngine.Events.UnityEvent onMaskAttached;
    public UnityEngine.Events.UnityEvent onMaskLost;

    [Header("Mask Drop")]
    [Tooltip("Optional mask transform to detach on death (will look for child named 'Mask' if empty).")]
    public Transform mask;
    [Tooltip("Impulse force applied when mask is dropped.")]
    public float maskDropForce = 5f;
    [Tooltip("Random angle range (degrees). Values should be around 'up' (90°).")]
    public Vector2 maskDropAngleRange = new Vector2(60f, 120f);
    [Tooltip("Random angular velocity range (degrees/sec).")]
    public Vector2 maskDropSpinRange = new Vector2(-360f, 360f);
    [Tooltip("If true, adds Rigidbody2D to mask when dropping (if missing).")]
    public bool addRigidbodyOnDrop = true;
    [Tooltip("If true, ensures mask Rigidbody2D is simulated on drop.")]
    public bool enableRigidbodyOnDrop = true;

    private State _state = State.Falling;
    private int _hitsLeft;
    private Rigidbody2D _rb;
    private Collider2D _col;
    private SpriteRenderer _sr;
    private Animator _animator;
    private int _dir = 1;
    private bool _maskAttached;
    private bool _isGrounded;
    private float _baseGravity;
    private Vector3 _baseScale;
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    [Header("Animation")]
    [Tooltip("Optional animator (if not set, will use Animator on same GameObject).")]
    public Animator animator;
    [Tooltip("Optional sprite renderer (if not set, will use SpriteRenderer on same GameObject).")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("Seconds to wait before destroying after death (to allow die animation).")]
    public float deathDelay = 0.6f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _baseScale = transform.localScale;
        if (spriteRenderer != null)
        {
            _sr = spriteRenderer;
        }
        else
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null)
            {
                var childSprite = transform.Find("Sprite");
                if (childSprite != null)
                    _sr = childSprite.GetComponent<SpriteRenderer>();
            }
        }

        if (animator != null)
        {
            _animator = animator;
        }
        else
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                var childSprite = transform.Find("Sprite");
                if (childSprite != null)
                    _animator = childSprite.GetComponent<Animator>();
            }
        }

        _rb.gravityScale = 3f;
        _baseGravity = _rb.gravityScale;
        _rb.freezeRotation = true;

        int variance = Random.Range(-hitVariance, hitVariance + 1);
        _hitsLeft = Mathf.Max(1, baseHitsToKill + variance);
    }

    private void Start()
    {
        SetState(State.Falling);
    }

    public void TakeHit(int amount = 1)
    {
        if (_state == State.Dead) return;
        _hitsLeft -= amount;
        if (_hitsLeft <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _state = State.Dead;
        onDeath?.Invoke();
        // Simula la máscara saliendo hacia el jugador; aquí solo disparamos evento.
        if (_maskAttached)
        {
            _maskAttached = false;
            onMaskLost?.Invoke();
        }
        DropMask();
        if (_animator != null)
        {
            _animator.ResetTrigger(AnimDie);
            _animator.SetTrigger(AnimDie);
        }
        if (_col != null) _col.enabled = false;
        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;
        Destroy(gameObject, Mathf.Max(0f, deathDelay));
    }

    private void SetState(State newState)
    {
        if (_state == newState) return;
        _state = newState;

        switch (_state)
        {
            case State.Falling:
                _col.isTrigger = false;
                _rb.gravityScale = _baseGravity;
                break;
            case State.Dormant:
                _col.isTrigger = true; // sin colisión ni daño
                _rb.gravityScale = 0f;
                _rb.linearVelocity = Vector2.zero;
                StartCoroutine(DormantRoutine());
                break;
            case State.Active:
                _col.isTrigger = false;
                _rb.gravityScale = _baseGravity;
                _dir = Random.value < 0.5f ? -1 : 1;
                break;
        }
    }

    private IEnumerator DormantRoutine()
    {
        yield return new WaitForSeconds(dormantSeconds);
        SetState(State.Active);
    }

    private void FixedUpdate()
    {
        UpdateGrounded();

        if (_state == State.Active && !_isGrounded)
        {
            SetState(State.Falling);
        }

        if (_state == State.Active)
        {
            _rb.linearVelocity = new Vector2(_dir * moveSpeed, _rb.linearVelocity.y);

            // Chequeo simple de pared lateral
            Vector2 origin = transform.position;
            Vector2 dirVec = new Vector2(_dir, 0f);
            float dist = 1f;
            var hit = Physics2D.Raycast(origin, dirVec, dist, wallMask);
            if (hit.collider != null)
            {
                _dir *= -1;
                var scale = _baseScale;
                scale.x = Mathf.Abs(scale.x) * _dir;
                transform.localScale = scale;
            }
        }

        UpdateAnimator();
    }

    private void UpdateGrounded()
    {
        if (groundCheck == null)
        {
            _isGrounded = false;
            return;
        }

        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;
        bool moving = _state == State.Active && Mathf.Abs(_rb.linearVelocity.x) > 0.01f;
        _animator.SetBool(AnimIsMoving, moving);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_state == State.Falling)
        {
            // Primer contacto con el piso -> dormido
            SetState(State.Dormant);
        }

        if (_state == State.Active && collision.collider.CompareTag("Player"))
        {
            // daño al jugador (lo maneja el sistema de player)
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_state == State.Dormant) return;
        if (_state == State.Falling || _state == State.Active)
        {
            if (other.CompareTag("Player"))
            {
                // daño al jugador
            }
        }
    }

    // Simular máscara adjunta al jugador (llamar desde lógica de muerte)
    public void AttachMaskToPlayer()
    {
        if (_maskAttached) return;
        _maskAttached = true;
        onMaskAttached?.Invoke();
    }

    public void DetachMask()
    {
        if (!_maskAttached) return;
        _maskAttached = false;
        onMaskLost?.Invoke();
    }

    private void DropMask()
    {
        Transform maskTransform = mask != null ? mask : transform.Find("Mask");
        if (maskTransform == null) return;

        maskTransform.SetParent(null, true);

        Rigidbody2D maskRb = maskTransform.GetComponent<Rigidbody2D>();
        if (maskRb == null && addRigidbodyOnDrop)
        {
            maskRb = maskTransform.gameObject.AddComponent<Rigidbody2D>();
        }

        var pickup = maskTransform.GetComponent<MaskPickup>();
        if (pickup != null)
        {
            pickup.OnDropped();
        }

        if (maskRb != null)
        {
            if (enableRigidbodyOnDrop) maskRb.simulated = true;
            float angle = Random.Range(maskDropAngleRange.x, maskDropAngleRange.y) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            maskRb.AddForce(dir * maskDropForce, ForceMode2D.Impulse);
            maskRb.angularVelocity = Random.Range(maskDropSpinRange.x, maskDropSpinRange.y);
        }
    }
}
