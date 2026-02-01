using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BossController : MonoBehaviour
{
    public enum Phase { One = 1, Two = 2, Three = 3, Four = 4 }

    [Header("Refs")]
    public Room room;
    public Transform[] movePoints;
    public Transform handOrigin;
    public MeepSpawner meepSpawner;
    public BossRoomController bossRoomController;
    public Rigidbody2D rb;
    public Collider2D hitCollider;
    public Collider2D bodyCollider;
    public SpriteRenderer[] visuals;
    public Animator animator;
    [Header("Hands")]
    public Transform rightHand;
    public Transform leftHand;
    public Collider2D rightHandHitbox;
    public Collider2D leftHandHitbox;
    [Header("Visual Masks (se caen por fase)")]
    public SpriteRenderer[] masks; // ordenadas de izquierda a derecha o arriba a abajo

    [Header("Vida")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    [Range(0f, 1f)] public float phase2Threshold = 0.75f; // pasa a fase 2 cuando vida <= 75%
    [Range(0f, 1f)] public float phase3Threshold = 0.5f;  // pasa a fase 3 cuando vida <= 50%
    [Range(0f, 1f)] public float phase4Threshold = 0.25f; // pasa a fase 4 cuando vida <= 25%

    [Header("Timings")]
    public float phaseDuration = 15f;
    public float clawTrackTime = 1.2f;
    public float clawChargeTime = 0.35f;
    public float clawCooldown = 1.4f;
    public float moveWaitMin = 3f;
    public float moveWaitMax = 5f;
    public float moveSpeed = 6f;
    public float mixedCooldown = 1.2f;
    public float meepBurstDuration = 3f;
    public float minMoveDistance = 3f;
    [Header("Meeps")]
    public bool spawnMeepsAllPhases = true;

    [Header("Damage")]
    public int touchDamage = 1;
    public int clawDamage = 2;
    public LayerMask playerMask;
    public LayerMask obstacleIgnoreMask;
    public float clawHitWidth = 2f;
    public float clawHitHeight = 4f;

    private Phase _phase = Phase.One;
    private bool _running;
    private Transform _player;
    private Coroutine _loop;
    private bool _flashing;
    [HideInInspector] public UnityEvent<int, int> onHealthChanged = new UnityEvent<int, int>(); // current, max

    private void Awake()
    {
        if (room == null) room = GetComponentInParent<Room>();
        if (bossRoomController == null) bossRoomController = GetComponent<BossRoomController>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (hitCollider == null) hitCollider = GetComponent<Collider2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;
        onHealthChanged.Invoke(currentHealth, maxHealth);

        // El boss no debe ser empujado por el player ni por la gravedad.
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            // Permite colisionar sin ser empujado: inmóvil en física pero con collider sólido.
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePosition;
        }

        if (bodyCollider == null) bodyCollider = hitCollider;
        if (bodyCollider != null) bodyCollider.isTrigger = false; // bloquea al player

        // Auto-fill masks if not set
        if (playerMask == 0)
        {
            int mask = LayerMask.GetMask("Player");
            playerMask = mask != 0 ? mask : -1;
        }
        if (obstacleIgnoreMask == 0)
        {
            int mask = LayerMask.GetMask("Obstacle", "PlatformSurface", "Default");
            obstacleIgnoreMask = mask != 0 ? mask : -1;
        }

        // Ignore collisions with obstacle layers proactively
        var allCols = GetComponentsInChildren<Collider2D>();
        int ignoreMask = obstacleIgnoreMask.value;
        if (ignoreMask != 0)
        {
            var colliders = FindObjectsOfType<Collider2D>();
            foreach (var c in colliders)
            {
                if (((1 << c.gameObject.layer) & ignoreMask) != 0)
                {
                    foreach (var myCol in allCols)
                        Physics2D.IgnoreCollision(myCol, c, true);
                }
            }
        }
    }

    private void OnEnable()
    {
        _running = true;
        _loop = StartCoroutine(PhaseLoop());
    }

    private IEnumerator PhaseLoop()
    {
        while (_running)
        {
            UpdatePhaseByHealth();

            switch (_phase)
            {
                case Phase.One:
                    yield return ClawAttack();
                    if (spawnMeepsAllPhases && meepSpawner != null)
                        yield return SpawnMeepsFor(meepBurstDuration);
                    break;
                case Phase.Two:
                    yield return MoveBetweenPoints();
                    if (spawnMeepsAllPhases && meepSpawner != null)
                        yield return SpawnMeepsFor(meepBurstDuration);
                    break;
                case Phase.Three:
                    // combina movimientos y garras
                    yield return RandomPhaseCombo();
                    break;
                case Phase.Four:
                    yield return RandomPhaseCombo(true);
                    break;
            }
        }
    }

    private IEnumerator RandomPhaseCombo(bool fast = false)
    {
        int pick = Random.Range(0, 2);
        if (pick == 0)
            yield return ClawAttack(fast);
        else
            yield return MoveBetweenPoints(fast);
        if (meepSpawner != null && Random.value < 0.6f)
            yield return SpawnMeepsFor(fast ? meepBurstDuration * 0.5f : meepBurstDuration);
        yield return new WaitForSeconds(fast ? mixedCooldown * 0.6f : mixedCooldown);
    }

    private IEnumerator ClawAttack(bool fast = false)
    {
        float trackT = fast ? clawTrackTime * 0.6f : clawTrackTime;
        float chargeT = fast ? clawChargeTime * 0.6f : clawChargeTime;
        float cooldown = fast ? clawCooldown * 0.6f : clawCooldown;

        TrySetTrigger("Claw");

        var hand = PickHandForClaw();
        if (hand.hit != null) hand.hit.enabled = false; // mientras trackea/carga no golpea
        Vector3 handStart = hand.t != null ? hand.t.position : Vector3.zero;
        // Asegura que la mano sea visible durante el tracking
        var handRenderer = hand.t != null ? hand.t.GetComponentInChildren<SpriteRenderer>() : null;
        if (handRenderer != null) handRenderer.enabled = true;

        // Track player
        Vector3 target = GetPlayerPos();
        float t = 0f;
        while (t < trackT)
        {
            target = GetPlayerPos();
            DrawClawGizmo(target);
            if (hand.t != null)
                hand.t.position = target + Vector3.up * 6f; // mano persigue por arriba
            t += Time.deltaTime;
            yield return null;
        }

        // Charge visual pause
        float elapsed = 0f;
        while (elapsed < chargeT)
        {
            DrawClawGizmo(target);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Slam down
        DoClawHit(target, hand);
        yield return new WaitForSeconds(cooldown);

        // Regresa la mano a su posición inicial
        if (hand.t != null)
            hand.t.position = handStart;
    }

    private void DrawClawGizmo(Vector3 target)
    {
        Debug.DrawLine(target + Vector3.up * 6f, target, Color.red, 0f);
    }

    private void DoClawHit(Vector3 target, (Transform t, Collider2D hit) hand)
    {
        if (hand.t != null)
        {
            // coloca la mano arriba, luego baja y activa hitbox en el impacto
            hand.t.position = target + Vector3.up * 6f;
            if (hand.hit != null)
            {
                hand.hit.gameObject.SetActive(true);
                hand.hit.enabled = true;
            }
            hand.t.position = target;
        }
        Vector2 center = new Vector2(target.x, target.y + clawHitHeight * 0.5f);
        var size = new Vector2(clawHitWidth, clawHitHeight);
        var hits = Physics2D.OverlapBoxAll(center, size, 0f, playerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;
            // daño al jugador: llama a ApplyDamage si existe
            h.SendMessage("ApplyDamage", clawDamage, SendMessageOptions.DontRequireReceiver);
        }
    } 

    private IEnumerator SpawnMeepsFor(float duration)
    {
        if (meepSpawner == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            meepSpawner.SpawnOne();
            float step = 0.6f;
            t += step;
            yield return new WaitForSeconds(step);
        }
    }

    private IEnumerator MoveBetweenPoints(bool fast = false)
    {
        if (movePoints == null || movePoints.Length == 0) yield break;
        float wait = fast ? moveWaitMin * 0.5f : Random.Range(moveWaitMin, moveWaitMax);
        Transform next = PickNextPoint();
        animator?.SetBool("Moving", true);
        while (next != null && Vector2.Distance(transform.position, next.position) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(transform.position, next.position, moveSpeed * Time.deltaTime);
            FaceDirection(next.position.x - transform.position.x);
            yield return null;
        }
        animator?.SetBool("Moving", false);
        yield return new WaitForSeconds(wait);
    }

    private Transform PickNextPoint()
    {
        if (movePoints.Length == 1) return movePoints[0];
        int attempts = 8;
        Transform best = null;
        float bestDist = 0f;
        for (int i = 0; i < attempts; i++)
        {
            var candidate = movePoints[Random.Range(0, movePoints.Length)];
            float d = Vector2.Distance(transform.position, candidate.position);
            if (d >= minMoveDistance && d > bestDist)
            {
                best = candidate;
                bestDist = d;
            }
        }
        return best ?? movePoints[Random.Range(0, movePoints.Length)];
    }

    private Vector3 GetPlayerPos()
    {
        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
        }
        return _player ? _player.position : transform.position;
    }

    public void OnBossDefeated()
    {
        _running = false;
        if (_loop != null) StopCoroutine(_loop);
        onHealthChanged.Invoke(0, maxHealth);
        bossRoomController?.EndFight();
    }

    public void TakeDamage(int amount = 1)
    {
        if (amount <= 0 || !_running) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        FlashHit();
        onHealthChanged.Invoke(currentHealth, maxHealth);
        UpdatePhaseByHealth();
        if (currentHealth <= 0)
        {
            OnBossDefeated();
            Destroy(gameObject);
        }
    }

    private void FlashHit()
    {
        if (visuals == null || visuals.Length == 0) return;
        if (_flashing) return;
        StartCoroutine(HitFlashRoutine());
    }

    private System.Collections.IEnumerator HitFlashRoutine()
    {
        _flashing = true;
        Color flash = Color.white;
        float dur = 0.12f;
        foreach (var sr in visuals)
        {
            if (sr == null) continue;
            sr.color = flash;
        }
        yield return new WaitForSeconds(dur);
        foreach (var sr in visuals)
        {
            if (sr == null) continue;
            sr.color = Color.white;
        }
        _flashing = false;
    }

    private void UpdatePhaseByHealth()
    {
        float frac = (float)currentHealth / Mathf.Max(1, maxHealth);
        Phase newPhase = Phase.One;
        if (frac <= phase4Threshold) newPhase = Phase.Four;
        else if (frac <= phase3Threshold) newPhase = Phase.Three;
        else if (frac <= phase2Threshold) newPhase = Phase.Two;

        if (newPhase != _phase)
        {
            _phase = newPhase;
            UpdateMaskVisuals(_phase);
            TrySetInt("Phase", (int)_phase);
            bossRoomController?.OnPhaseChanged((int)_phase);
        }
    }

    private void TrySetTrigger(string name)
    {
        if (animator == null) return;
        if (AnimatorHasParam(name, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(name);
            animator.SetTrigger(name);
        }
    }

    private void TrySetInt(string name, int value)
    {
        if (animator == null) return;
        if (AnimatorHasParam(name, AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(name, value);
        }
    }

    private bool AnimatorHasParam(string name, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == name && p.type == type) return true;
        }
        return false;
    }

    private void UpdateMaskVisuals(Phase phase)
    {
        if (masks == null || masks.Length == 0) return;
        // Fase 1: todas las máscaras visibles. Fase 2: cae 1. Fase 3: cae 2. Fase 4: caen todas.
        int fallen = Mathf.Clamp((int)phase - 1, 0, masks.Length);
        int active = Mathf.Max(0, masks.Length - fallen);
        for (int i = 0; i < masks.Length; i++)
        {
            bool enable = i < active;
            if (masks[i] != null) masks[i].enabled = enable;
        }
        Debug.Log($"Boss masks active {active}/{masks.Length} en fase {phase}");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.collider.gameObject.layer) & obstacleIgnoreMask) != 0)
        {
            Physics2D.IgnoreCollision(collision.collider, hitCollider);
            return;
        }
        if (collision.collider.CompareTag("Player"))
        {
            // bloquea al jugador; el daño lo maneja Player
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & obstacleIgnoreMask) != 0)
        {
            Physics2D.IgnoreCollision(other, hitCollider);
            return;
        }
        if (other.CompareTag("Player"))
        {
            // daño al jugador (si se usa trigger en otra parte)
        }
    }

    private void FaceDirection(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        var scale = transform.localScale;
        scale.x = Mathf.Sign(dirX) * Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    private (Transform t, Collider2D hit) PickHandForClaw()
    {
        bool facingRight = transform.localScale.x > 0f;
        if (facingRight && rightHand != null) return (rightHand, rightHandHitbox);
        if (!facingRight && leftHand != null) return (leftHand, leftHandHitbox);
        if (rightHand != null) return (rightHand, rightHandHitbox);
        if (leftHand != null) return (leftHand, leftHandHitbox);
        return (null, null);
    }
}
