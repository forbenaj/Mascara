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
    [Header("Knockback")]
    [Tooltip("Impulso aplicado al jugador al tocar (x=horizontal, y=arriba).")]
    public Vector2 touchKnockback = new Vector2(5f, 2.5f);
    [Tooltip("Impulso aplicado al meep al recibir un golpe (x=horizontal, y=arriba).")]
    public Vector2 hitKnockback = new Vector2(7f, 3f);
    [Tooltip("Segundos en los que se pausa el movimiento tras el golpe.")]
    public float hitStunSeconds = 0.15f;
    [Header("Player Touch Check")]
    [Tooltip("Capas que representan al jugador para el chequeo de contacto.")]
    public LayerMask playerMask;
    [Tooltip("Radio del chequeo de contacto con el jugador.")]
    public float touchRadius = 0.6f;
    [Tooltip("Origen opcional del chequeo de contacto; si no existe usa la posición del meep.")]
    public Transform touchOrigin;
    [Tooltip("Cooldown mínimo entre empujes al jugador.")]
    public float touchCooldownSeconds = 0.2f;
    [Header("Hitbox/Hurtbox (optional)")]
    [Tooltip("Optional hitbox trigger. If set, overlap-circle touch checks are skipped.")]
    [SerializeField] private MeepHitbox hitbox;
    [Tooltip("Optional hurtbox trigger to receive player hits.")]
    [SerializeField] private MeepHurtbox hurtbox;
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
    [Tooltip("Random rotation applied on drop (degrees).")]
    public Vector2 maskDropRotationRange = new Vector2(-10f, 10f);
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
    private float _hitStunTimer;
    private float _nextTouchTime;
    private float _baseGravity;
    private Vector3 _baseScale;
    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimDie = Animator.StringToHash("Die");
    private bool _flashing;

    public bool HasHurtbox => hurtbox != null && hurtbox.isActiveAndEnabled;

    [Header("Animation")]
    [Tooltip("Optional animator (if not set, will use Animator on same GameObject).")]
    public Animator animator;
    [Tooltip("Optional sprite renderer (if not set, will use SpriteRenderer on same GameObject).")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("Seconds to wait before destroying after death (to allow die animation).")]
    public float deathDelay = 0.6f;
    [Tooltip("Optional idle animation variants to randomize per spawn.")]
    public AnimationClip[] idleVariants;
    [Header("Hit Flash")]
    public Color hitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float hitFlashSeconds = 0.12f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _baseScale = transform.localScale;
        if (playerMask == 0)
        {
            int mask = LayerMask.GetMask("Player");
            if (mask != 0) playerMask = mask;
        }
        if (hitbox == null)
            hitbox = GetComponentInChildren<MeepHitbox>();
        if (hurtbox == null)
            hurtbox = GetComponentInChildren<MeepHurtbox>();

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

        ApplyIdleVariant();

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

    public void ApplyIdleVariant()
    {
        if (_animator == null || idleVariants == null || idleVariants.Length == 0) return;
        var clip = idleVariants[Random.Range(0, idleVariants.Length)];
        if (clip == null) return;
        var controller = _animator.runtimeAnimatorController;
        if (controller == null) return;
        var overrideController = new AnimatorOverrideController(controller);
        var baseClips = controller.animationClips;
        if (baseClips == null || baseClips.Length == 0) return;
        var baseClip = baseClips[0];
        for (int i = 0; i < baseClips.Length; i++)
        {
            if (baseClips[i] != null && baseClips[i].name.StartsWith("Idle"))
            {
                baseClip = baseClips[i];
                break;
            }
        }
        overrideController[baseClip] = clip;
        _animator.runtimeAnimatorController = overrideController;
    }

    public void TakeHit(int amount = 1)
    {
        if (_state == State.Dead) return;
        if (ApplyDamage(amount))
        {
            FlashHit();
        }
    }

    public void TakeHit(int amount, Vector2 sourcePosition, Vector2? knockbackOverride = null)
    {
        if (_state == State.Dead) return;
        if (ApplyDamage(amount))
        {
            FlashHit();
            return;
        }
        ApplyKnockback(sourcePosition, knockbackOverride ?? hitKnockback);
    }

    private bool ApplyDamage(int amount)
    {
        if (amount <= 0) return false;
        _hitsLeft -= amount;
        if (_hitsLeft <= 0)
        {
            Die();
            return true;
        }
        return false;
    }

    private void FlashHit()
    {
        if (_sr == null && mask == null && transform.Find("Mask") == null) return;
        if (_flashing) return;
        if (hitFlashSeconds <= 0f) return;
        StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        _flashing = true;
        var renderers = new System.Collections.Generic.List<SpriteRenderer>();
        if (_sr != null) renderers.Add(_sr);

        Transform maskTransform = mask != null ? mask : transform.Find("Mask");
        if (maskTransform != null)
        {
            var maskRenderers = maskTransform.GetComponentsInChildren<SpriteRenderer>(true);
            if (maskRenderers != null && maskRenderers.Length > 0)
                renderers.AddRange(maskRenderers);
        }

        var originals = new Color[renderers.Count];
        for (int i = 0; i < renderers.Count; i++)
        {
            originals[i] = renderers[i] != null ? renderers[i].color : Color.white;
            if (renderers[i] != null)
            {
                var flash = hitFlashColor;
                flash.a = originals[i].a;
                renderers[i].color = flash;
            }
        }
        yield return new WaitForSeconds(hitFlashSeconds);
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = originals[i];
        }
        _flashing = false;
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
        if (hitbox == null || !hitbox.isActiveAndEnabled)
            CheckPlayerTouch();

        if (_state == State.Active && !_isGrounded)
        {
            SetState(State.Falling);
        }

        if (_hitStunTimer > 0f)
        {
            _hitStunTimer -= Time.fixedDeltaTime;
            UpdateAnimator();
            return;
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

    private void CheckPlayerTouch()
    {
        if (touchDamage <= 0) return;
        if (_state == State.Dormant) return;
        if (_state != State.Falling && _state != State.Active) return;
        if (Time.time < _nextTouchTime) return;

        Vector2 origin = touchOrigin != null ? (Vector2)touchOrigin.position : (Vector2)transform.position;
        int mask = playerMask == 0 ? -1 : playerMask;
        var hits = Physics2D.OverlapCircleAll(origin, touchRadius, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null) continue;
            if (!hit.CompareTag("Player")) continue;
            TryTouchDamage(hit);
            break;
        }
    }

    public bool TryTouchDamage(Collider2D other)
    {
        if (touchDamage <= 0) return false;
        if (_state == State.Dormant) return false;
        if (_state != State.Falling && _state != State.Active) return false;
        if (Time.time < _nextTouchTime) return false;
        if (other == null || !other.CompareTag("Player")) return false;

        if (TryDamagePlayer(other))
        {
            _nextTouchTime = Time.time + touchCooldownSeconds;
            return true;
        }

        return false;
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

    private bool TryDamagePlayer(Collider2D other)
    {
        if (touchDamage <= 0) return false;
        if (_state == State.Dormant) return false;
        if (_state != State.Falling && _state != State.Active) return false;
        if (other == null) return false;

        var hurtbox = other.GetComponent<PlayerHurtbox>();
        if (hurtbox != null)
        {
            hurtbox.ApplyDamageFrom(transform.position, touchDamage, touchKnockback);
            return true;
        }

        if (!other.CompareTag("Player")) return false;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.ApplyDamageFrom(transform.position, touchDamage, touchKnockback);
            return true;
        }

        other.SendMessage("ApplyDamage", touchDamage, SendMessageOptions.DontRequireReceiver);
        return true;
    }

    private void ApplyKnockback(Vector2 sourcePosition, Vector2 knockback)
    {
        if (_rb == null) return;

        float dir = Mathf.Sign(transform.position.x - sourcePosition.x);
        if (Mathf.Approximately(dir, 0f))
        {
            dir = _dir == 0 ? 1f : _dir;
        }

        Vector2 impulse = new Vector2(dir * knockback.x, knockback.y);
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        _rb.AddForce(impulse, ForceMode2D.Impulse);

        if (hitStunSeconds > 0f)
        {
            _hitStunTimer = Mathf.Max(_hitStunTimer, hitStunSeconds);
        }

        if (Mathf.Abs(dir) > 0.01f)
        {
            _dir = dir > 0f ? 1 : -1;
            var scale = _baseScale;
            scale.x = Mathf.Abs(scale.x) * _dir;
            transform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
        Vector3 origin = touchOrigin != null ? touchOrigin.position : transform.position;
        Gizmos.DrawWireSphere(origin, touchRadius);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryDamagePlayer(collision.collider))
        {
            _nextTouchTime = Time.time + touchCooldownSeconds;
        }

        if (_state == State.Falling)
        {
            // Primer contacto con el piso -> dormido
            SetState(State.Dormant);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryDamagePlayer(other))
        {
            _nextTouchTime = Time.time + touchCooldownSeconds;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (TryDamagePlayer(collision.collider))
        {
            _nextTouchTime = Time.time + touchCooldownSeconds;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (TryDamagePlayer(other))
        {
            _nextTouchTime = Time.time + touchCooldownSeconds;
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
            float rot = Random.Range(maskDropRotationRange.x, maskDropRotationRange.y);
            if (!Mathf.Approximately(rot, 0f))
            {
                maskTransform.rotation = maskTransform.rotation * Quaternion.Euler(0f, 0f, rot);
            }
            float angle = Random.Range(maskDropAngleRange.x, maskDropAngleRange.y) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            maskRb.AddForce(dir * maskDropForce, ForceMode2D.Impulse);
            maskRb.angularVelocity = Random.Range(maskDropSpinRange.x, maskDropSpinRange.y);
        }
    }
}
