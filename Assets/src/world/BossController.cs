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
    public SpriteRenderer[] visuals;

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
        currentHealth = maxHealth;
        onHealthChanged.Invoke(currentHealth, maxHealth);

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

        // Track player
        Vector3 target = GetPlayerPos();
        float t = 0f;
        while (t < trackT)
        {
            target = GetPlayerPos();
            DrawClawGizmo(target);
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
        DoClawHit(target);
        yield return new WaitForSeconds(cooldown);
    }

    private void DrawClawGizmo(Vector3 target)
    {
        Debug.DrawLine(target + Vector3.up * 6f, target, Color.red, 0f);
    }

    private void DoClawHit(Vector3 target)
    {
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
        while (next != null && Vector2.Distance(transform.position, next.position) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(transform.position, next.position, moveSpeed * Time.deltaTime);
            yield return null;
        }
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
            bossRoomController?.OnPhaseChanged((int)_phase);
        }
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
            // daño al jugador (lo maneja el sistema de player)
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
            // daño al jugador
        }
    }
}
