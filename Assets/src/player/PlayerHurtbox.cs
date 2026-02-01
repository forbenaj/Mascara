using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PlayerHurtbox : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<PlayerHealth>();

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    public void ApplyDamageFrom(Vector2 sourcePosition, int amount, Vector2? knockbackOverride = null)
    {
        if (health == null) return;
        health.ApplyDamageFrom(sourcePosition, amount, knockbackOverride);
    }
}
