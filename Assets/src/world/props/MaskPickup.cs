using UnityEngine;

/// Handles delayed pickup and attraction to the player after a mask is dropped.
[DisallowMultipleComponent]
public class MaskPickup : MonoBehaviour
{
    [Header("Pickup")]
    public string playerTag = "Player";
    public string socketName = "Face";
    public float pickupDelay = 0.6f;
    public float stickDistance = 0.2f;

    [Header("Attraction")]
    public float minSpeed = 1.5f;
    public float maxSpeed = 8f;
    public float acceleration = 20f;

    [Header("Refs (optional)")]
    public Collider2D triggerCollider;

    private Rigidbody2D _rb;
    private Transform _moveTransform;
    private Transform _player;
    private Transform _socket;
    private bool _canPickup;
    private bool _isAttracting;
    private bool _isDropped;
    private float _delayTimer;
    private float _attractRange = 3f;
    private Vector2 _velocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            _rb = GetComponentInParent<Rigidbody2D>();
        }
        _moveTransform = _rb != null ? _rb.transform : transform;
        if (triggerCollider == null)
        {
            var cols = GetComponents<Collider2D>();
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i].isTrigger)
                {
                    triggerCollider = cols[i];
                    break;
                }
            }
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
            _attractRange = Mathf.Max(0.1f, triggerCollider.bounds.extents.magnitude);
        }
    }

    private void Update()
    {
        if (!_isDropped || _canPickup)
        {
            return;
        }

        if (!_canPickup)
        {
            _delayTimer -= Time.deltaTime;
            if (_delayTimer <= 0f)
            {
                EnablePickup();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_isAttracting || _player == null) return;

        var target = _socket != null ? _socket.position : _player.position;
        var toTarget = target - _moveTransform.position;
        float dist = toTarget.magnitude;
        if (dist <= stickDistance)
        {
            StickToPlayer();
            return;
        }

        var dir = dist > 0.0001f ? toTarget / dist : Vector3.zero;
        float t = Mathf.Clamp01(dist / Mathf.Max(0.01f, _attractRange));
        float speed = Mathf.Lerp(maxSpeed, minSpeed, t);
        Vector2 desiredVel = (Vector2)dir * speed;
        _velocity = Vector2.MoveTowards(_velocity, desiredVel, acceleration * Time.fixedDeltaTime);

        if (_rb != null && _rb.simulated)
        {
            if (_rb.bodyType == RigidbodyType2D.Kinematic)
            {
                _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
            }
            else
            {
                _rb.linearVelocity = _velocity;
            }
        }
        else
        {
            _moveTransform.position = Vector3.MoveTowards(_moveTransform.position, target, speed * Time.fixedDeltaTime);
        }
    }

    public void OnDropped()
    {
        _isDropped = true;
        _delayTimer = Mathf.Max(0f, pickupDelay);
        _canPickup = false;
        _isAttracting = false;
        if (triggerCollider != null) triggerCollider.enabled = false;
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
        {
            _rb.simulated = true;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _moveTransform = _rb.transform;
        }
        else
        {
            _moveTransform = transform;
        }
    }

    private void EnablePickup()
    {
        _canPickup = true;
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
            _attractRange = Mathf.Max(0.1f, triggerCollider.bounds.extents.magnitude);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStartAttract(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryStartAttract(other);
    }

    private void TryStartAttract(Collider2D other)
    {
        if (_isAttracting || !_canPickup) return;
        var rb = other.attachedRigidbody;
        var candidate = rb != null ? rb.transform : other.transform.root;
        if (candidate == null || !candidate.CompareTag(playerTag)) return;
        CachePlayer(candidate);
        _isAttracting = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_isAttracting || !_canPickup) return;
        var rb = other.attachedRigidbody;
        var candidate = rb != null ? rb.transform : other.transform.root;
        if (candidate == null || _player == null) return;
        if (candidate != _player) return;
        _isAttracting = false;
        _velocity = Vector2.zero;
    }

    private void CachePlayer(Transform playerTransform)
    {
        _player = playerTransform;
        _socket = FindDeepChild(_player, socketName);
    }

    private void StickToPlayer()
    {
        if (_socket == null && _player != null)
        {
            _socket = FindDeepChild(_player, socketName);
        }

        var parent = _socket != null ? _socket : _player;
        if (parent == null) return;

        transform.SetParent(parent, worldPositionStays: true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.simulated = false;
        }

        var cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].enabled = false;
        }

        enabled = false;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name)) return null;
        var children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == name)
                return children[i];
        }
        return null;
    }
}
