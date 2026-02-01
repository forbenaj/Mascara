using UnityEngine;

/// Attach to a child object (e.g., GroundCheck) with a BoxCollider2D trigger to detect ground reliably.
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class GroundCheck : MonoBehaviour
{
    [Tooltip("Layers considered ground.")]
    public LayerMask groundMask = -1; // set to Ground/PlatformSurface; auto-fill if empty

    [Tooltip("Seconds to buffer grounded after leaving ground (coyote time).")]
    public float coyoteTime = 0.05f;

    [Tooltip("Small downward offset to catch tiny separations.")]
    public float skinDepth = 0.02f;

    public bool IsGrounded => _timer > 0f;

    private float _timer;
    private BoxCollider2D _col;

    private void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        _col.isTrigger = true;
        // Auto-fill mask if none set
        if (groundMask == 0)
        {
            int mask = LayerMask.GetMask("Ground", "PlatformSurface");
            groundMask = mask != 0 ? mask : -1;
        }
    }

    private void FixedUpdate()
    {
        CheckGround();
        if (_timer > 0f) _timer -= Time.fixedDeltaTime;
    }

    private void CheckGround()
    {
        Vector2 size = Vector2.Scale(_col.size, transform.lossyScale);
        Vector2 center = (Vector2)transform.TransformPoint(_col.offset) + Vector2.down * skinDepth;
        float angle = transform.eulerAngles.z;

        var hits = Physics2D.OverlapBoxAll(center, size, angle, groundMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;
            if (h.attachedRigidbody == _col.attachedRigidbody) continue; // ignore self
            _timer = coyoteTime;
            return;
        }
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(col.offset + Vector2.down * skinDepth, col.size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
