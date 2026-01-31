using System.Collections;
using UnityEngine;

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

    [Header("Vida")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    [Range(0f, 1f)] public float phase2Threshold = 0.75f; // pasa a fase 2 cuando vida <= 75%
    [Range(0f, 1f)] public float phase3Threshold = 0.5f;  // pasa a fase 3 cuando vida <= 50%
    [Range(0f, 1f)] public float phase4Threshold = 0.25f; // pasa a fase 4 cuando vida <= 25%

    [Header("Timings")]
    public float phaseDuration = 15f;
    public float slapInterval = 2f;
    public float moveWait = 1f;
    public float moveSpeed = 6f;
    public float mixedCooldown = 1.5f;
    public float meepBurstDuration = 3f;

    [Header("Damage")]
    public int touchDamage = 1;

    private Phase _phase = Phase.One;
    private bool _running;
    private Transform _player;
    private Coroutine _loop;

    private void Awake()
    {
        if (room == null) room = GetComponentInParent<Room>();
        if (bossRoomController == null) bossRoomController = GetComponent<BossRoomController>();
        currentHealth = maxHealth;
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
                    yield return SlapAtPlayer();
                    break;
                case Phase.Two:
                    yield return MoveBetweenPoints(moveWait * 2f);
                    break;
                case Phase.Three:
                    yield return SpawnMeepsFor(meepBurstDuration);
                    break;
                case Phase.Four:
                    int pick = Random.Range(1, 4);
                    switch ((Phase)pick)
                    {
                        case Phase.One: yield return SlapAtPlayer(); break;
                        case Phase.Two: yield return MoveBetweenPoints(moveWait * 1.5f); break;
                        case Phase.Three: yield return SpawnMeepsFor(meepBurstDuration * 0.5f); break;
                    }
                    yield return new WaitForSeconds(mixedCooldown);
                    break;
            }
        }
    }

    private IEnumerator SlapAtPlayer()
    {
        var target = GetPlayerPos();
        // Aquí solo un telegraph: podrías instanciar un golpe o animación
        Debug.DrawLine(handOrigin ? handOrigin.position : transform.position, target, Color.red, 0.5f);
        yield return new WaitForSeconds(slapInterval);
    }

    private IEnumerator MoveBetweenPoints(float duration)
    {
        if (movePoints == null || movePoints.Length == 0) yield break;
        float t = 0f;
        int idx = 0;
        while (t < duration)
        {
            var target = movePoints[idx].position;
            while (Vector2.Distance(transform.position, target) > 0.1f)
            {
                transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }
            yield return new WaitForSeconds(moveWait);
            idx = (idx + 1) % movePoints.Length;
            t += moveWait;
        }
    }

    private IEnumerator SpawnMeepsFor(float duration)
    {
        if (meepSpawner == null) yield break;
        float t = 0f;
        meepSpawner.interval = 0.75f;
        while (t < duration)
        {
            meepSpawner.SpawnOne();
            t += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
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
        bossRoomController?.EndFight();
    }

    public void TakeDamage(int amount = 1)
    {
        if (amount <= 0 || !_running) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdatePhaseByHealth();
        if (currentHealth <= 0)
        {
            OnBossDefeated();
            Destroy(gameObject);
        }
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
        if (collision.collider.CompareTag("Player"))
        {
            // daño al jugador (lo maneja el sistema de player)
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // daño al jugador
        }
    }
}
