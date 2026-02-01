using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int startingHealth = 5;

    [Header("Damage")]
    [Tooltip("Seconds of invulnerability after taking damage.")]
    [SerializeField] private float invulnerabilitySeconds = 0.5f;
    [Tooltip("Default knockback when damage is applied (x=horizontal, y=up).")]
    [SerializeField] private Vector2 defaultKnockback = new Vector2(6f, 3f);
    [Tooltip("Seconds to lock player input while knockback is active.")]
    [SerializeField] private float knockbackLockSeconds = 0.12f;

    [Header("Events")]
    public UnityEvent<int> onHealthChanged;
    public UnityEvent onDeath;

    private Rigidbody2D rb;
    private PlayerController controller;
    private int currentHealth;
    private float nextDamageTime;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvulnerable => Time.time < nextDamageTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(startingHealth <= 0 ? maxHealth : startingHealth, 0, maxHealth);
    }

    public void ApplyDamage(int amount)
    {
        ApplyDamageFrom(transform.position, amount, null);
    }

    public bool ApplyDamageFrom(Vector2 sourcePosition, int amount, Vector2? knockbackOverride = null)
    {
        if (amount <= 0 || isDead) return false;
        if (IsInvulnerable)
        {
            ApplyKnockback(sourcePosition, knockbackOverride ?? defaultKnockback);
            return false;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        nextDamageTime = Time.time + invulnerabilitySeconds;
        onHealthChanged?.Invoke(currentHealth);

        if (currentHealth == 0)
        {
            isDead = true;
            onDeath?.Invoke();
        }

        ApplyKnockback(sourcePosition, knockbackOverride ?? defaultKnockback);
        return true;
    }

    private void ApplyKnockback(Vector2 sourcePosition, Vector2 knockback)
    {
        if (rb == null) return;

        float dir = Mathf.Sign(transform.position.x - sourcePosition.x);
        if (Mathf.Approximately(dir, 0f))
        {
            dir = transform.localScale.x >= 0f ? 1f : -1f;
        }

        Vector2 impulse = new Vector2(dir * knockback.x, knockback.y);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.AddForce(impulse, ForceMode2D.Impulse);

        if (controller != null && knockbackLockSeconds > 0f)
        {
            controller.StartKnockbackLock(knockbackLockSeconds);
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        if (startingHealth < 0) startingHealth = 0;
        if (startingHealth > maxHealth) startingHealth = maxHealth;
    }
}
