using UnityEngine;

/// Simple damage marker for hazardous obstacles (spikes, traps).
[DisallowMultipleComponent]
public class Hazard : MonoBehaviour
{
    [Tooltip("Damage dealt on touch (handled by your health system).")]
    public int damage = 1;

    [Tooltip("If true, this collider should be a trigger.")]
    public bool triggerOnly = true;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = triggerOnly;
    }

    private void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = triggerOnly;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        var col = GetComponent<Collider2D>() as BoxCollider2D;
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.offset, col.size);
        }
    }
}
