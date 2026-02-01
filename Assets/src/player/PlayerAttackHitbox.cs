using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PlayerAttackHitbox : MonoBehaviour
{
    private Collider2D hitbox;
    private readonly HashSet<Collider2D> hitThisSwing = new HashSet<Collider2D>();
    private Vector2 sourcePosition;
    private Vector2 knockback;
    private Coroutine activeRoutine;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        if (hitbox != null)
            hitbox.enabled = false;
    }

    public void Activate(float duration, Vector2 attackKnockback, Vector2 source)
    {
        if (hitbox == null) return;
        if (duration <= 0f) return;

        knockback = attackKnockback;
        sourcePosition = source;
        hitThisSwing.Clear();

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        hitbox.enabled = true;
        activeRoutine = StartCoroutine(DisableAfter(duration));
    }

    private IEnumerator DisableAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (hitbox != null)
            hitbox.enabled = false;
        activeRoutine = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (hitbox == null || !hitbox.enabled || other == null) return;
        if (hitThisSwing.Contains(other)) return;

        hitThisSwing.Add(other);

        var hurtbox = other.GetComponent<MeepHurtbox>();
        if (hurtbox != null)
        {
            hurtbox.ApplyHit(1, sourcePosition, knockback);
            return;
        }

        var meep = other.GetComponent<Meep>();
        if (meep != null)
        {
            if (!meep.HasHurtbox)
                meep.TakeHit(1, sourcePosition, knockback);
            return;
        }

        var boss = other.GetComponent<BossController>();
        if (boss != null)
            boss.TakeDamage(1);
    }
}
