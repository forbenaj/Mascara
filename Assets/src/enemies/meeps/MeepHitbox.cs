using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class MeepHitbox : MonoBehaviour
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null) return;
        owner.TryTouchDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (owner == null) return;
        owner.TryTouchDamage(other);
    }
}
