using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public enum Mode { Loop, PingPong }

    [Tooltip("Local offset from start position to end position.")]
    public Vector2 endOffset = new Vector2(6f, 0f);

    [Tooltip("Units per second.")]
    public float speed = 3f;

    [Tooltip("Wait time at each end, in seconds.")]
    public float waitTime = 0.25f;

    public Mode mode = Mode.PingPong;

    private Vector3 _startPos;
    private Vector3 _endPos;
    private float _t;
    private int _dir = 1;
    private float _waitTimer;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.gravityScale = 0f;

        _startPos = transform.position;
        _endPos = _startPos + (Vector3)endOffset;
    }

    private void Update()
    {
        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.deltaTime;
            return;
        }

        _t += _dir * speed * Time.deltaTime / Mathf.Max(0.01f, Vector3.Distance(_startPos, _endPos));
        _t = Mathf.Clamp01(_t);

        var target = Vector3.Lerp(_startPos, _endPos, _t);
        _rb.MovePosition(target);

        if (_t <= 0f || _t >= 1f)
        {
            if (mode == Mode.PingPong)
            {
                _dir *= -1;
            }
            else
            {
                _t = 0f;
            }
            _waitTimer = waitTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        var start = Application.isPlaying ? _startPos : transform.position;
        var end = Application.isPlaying ? _endPos : transform.position + (Vector3)endOffset;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.1f);
    }
}
