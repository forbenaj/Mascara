using UnityEngine;

/// Attach to a child object (e.g., GroundCheck) with a trigger collider to detect ground without using the main collider.
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class GroundCheck : MonoBehaviour
{
    [Tooltip("Layers considered ground.")]
    public LayerMask groundMask;

    [Tooltip("Seconds to buffer grounded after leaving ground (coyote time).")]
    public float coyoteTime = 0.05f;

    public bool IsGrounded => _timer > 0f;

    private float _timer;
    
    private Collider2D _col;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
    }

    private void Update()
    {
        if (_timer > 0f) _timer -= Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsGround(other.gameObject.layer))
        {
            _timer = coyoteTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsGround(other.gameObject.layer))
        {
            _timer = coyoteTime;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // timer will tick down; no immediate false to allow coyote time
    }

    private bool IsGround(int layer)
    {
        return (groundMask.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        var col = GetComponent<Collider2D>() as BoxCollider2D;
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.offset, col.size);
        }
    }
}
