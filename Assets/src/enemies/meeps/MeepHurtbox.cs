using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class MeepHurtbox : MonoBehaviour
{
    [SerializeField] private Meep owner;

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<Meep>();

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    public void ApplyHit(int amount, Vector2 sourcePosition, Vector2? knockbackOverride = null)
    {
        if (owner == null) return;
        owner.TakeHit(amount, sourcePosition, knockbackOverride);
    }
}
