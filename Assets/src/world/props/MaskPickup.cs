using UnityEngine;

/// Handles delayed pickup and attraction to the player after a mask is dropped.
[DisallowMultipleComponent]
public class MaskPickup : MonoBehaviour
{
    [Header("Pickup")]
    public string playerTag = "Player";
    public float pickupDelay = 0.6f;
    public float stickDistance = 0.2f;
    [Tooltip("Optional child name on player that defines the attach area (BoxCollider2D).")]
    public string attachAreaChildName = "MaskAttachArea";
    [Tooltip("Local-space min corner for random attach position on player.")]
    public Vector2 attachAreaMin = new Vector2(-0.3f, -0.2f);
    [Tooltip("Local-space max corner for random attach position on player.")]
    public Vector2 attachAreaMax = new Vector2(0.3f, 0.4f);

    [Header("Attraction")]
    public float minSpeed = 1.5f;
    public float maxSpeed = 8f;
    public float acceleration = 20f;

    [Header("Scale Near Player")]
    [Tooltip("Minimum scale factor when close to the player (e.g., 0.5 = half size).")]
    public float minScaleFactor = 0.5f;
    [Tooltip("Distance over which the scale ramps from min to full. 0 = use attraction range.")]
    public float scaleRange = 0f;
    [Header("Flash")]
    public Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float flashDuration = 0.15f;

    [Header("Refs (optional)")]
    public Collider2D triggerCollider;
    public Transform spriteTransform;

    private Rigidbody2D _rb;
    private Transform _moveTransform;
    private Transform _spriteTransform;
    private SpriteRenderer _spriteRenderer;
    private Color _baseColor = Color.white;
    private Transform _player;
    private Collider2D _playerCollider;
    private Collider2D[] _maskColliders;
    private BoxCollider2D _playerAttachArea;
    private Vector3 _rootBaseScale;
    private bool _canPickup;
    private bool _isAttracting;
    private bool _isDropped;
    private float _delayTimer;
    private float _attractRange = 3f;
    private Vector2 _velocity;
    private Vector3 _spriteBaseScale;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            _rb = GetComponentInParent<Rigidbody2D>();
        }
        _moveTransform = _rb != null ? _rb.transform : transform;
        _rootBaseScale = transform.localScale;
        _spriteTransform = ResolveSpriteTransform();
        _spriteBaseScale = _spriteTransform.localScale;
        _spriteRenderer = _spriteTransform.GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null) _baseColor = _spriteRenderer.color;
        _maskColliders = GetComponentsInChildren<Collider2D>(true);
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
        if (!_canPickup) return;

        EnsurePlayer();
        if (_player == null) return;

        float distToPlayer = GetDistanceToPlayer();
        if (distToPlayer <= stickDistance)
        {
            StickToPlayer();
            return;
        }

        var target = _player.position;
        var toTarget = target - _moveTransform.position;
        float dist = toTarget.magnitude;
        var dir = dist > 0.0001f ? toTarget / dist : Vector3.zero;
        float t = Mathf.Clamp01(dist / Mathf.Max(0.01f, _attractRange));
        float speed = Mathf.Lerp(maxSpeed, minSpeed, t);
        Vector2 desiredVel = (Vector2)dir * speed;
        _velocity = Vector2.MoveTowards(_velocity, desiredVel, acceleration * Time.fixedDeltaTime);
        UpdateScale(Mathf.Max(0f, distToPlayer));

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
        _player = null;
        _playerCollider = null;
        ResetScale();
        if (triggerCollider != null) triggerCollider.enabled = false;
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
        {
            _rb.simulated = true;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _moveTransform = _rb.transform;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStickToPlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryStickToPlayer(collision.collider);
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

    private void TryStickToPlayer(Collider2D other)
    {
        if (!_canPickup) return;
        var rb = other.attachedRigidbody;
        var candidate = rb != null ? rb.transform : other.transform.root;
        if (candidate == null || !candidate.CompareTag(playerTag)) return;
        CachePlayer(candidate);
        StickToPlayer();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_isAttracting || !_canPickup) return;
        var rb = other.attachedRigidbody;
        var candidate = rb != null ? rb.transform : other.transform.root;
        if (candidate == null || _player == null) return;
        if (candidate != _player) return;
        _isAttracting = false;
        _player = null;
        _playerCollider = null;
        _playerAttachArea = null;
        _velocity = Vector2.zero;
        ResetScale();
    }

    private void CachePlayer(Transform playerTransform)
    {
        _player = playerTransform;
        _playerCollider = ResolvePlayerCollider(playerTransform);
        _playerAttachArea = ResolvePlayerAttachArea(playerTransform);
    }

    private void EnsurePlayer()
    {
        if (_player != null) return;
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
        {
            _player = go.transform;
            _playerCollider = ResolvePlayerCollider(_player);
            _playerAttachArea = ResolvePlayerAttachArea(_player);
        }
    }

    private void StickToPlayer()
    {
        if (_player == null) return;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.simulated = false;
        }

        transform.SetParent(_player, worldPositionStays: false);
        transform.localScale = _rootBaseScale;
        transform.localPosition = ResolveAttachLocalPosition();
        SetMinScale();
        Flash();

        var cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].enabled = false;
        }

        enabled = false;
    }

    private void UpdateScale(float dist)
    {
        float range = scaleRange > 0f ? scaleRange : _attractRange;
        if (range <= 0.001f) return;

        float t = Mathf.Clamp01(dist / range);
        float factor = Mathf.Lerp(minScaleFactor, 1f, t);
        _spriteTransform.localScale = _spriteBaseScale * factor;
    }

    private void ResetScale()
    {
        _spriteTransform.localScale = _spriteBaseScale;
    }

    private void SetMinScale()
    {
        _spriteTransform.localScale = _spriteBaseScale * minScaleFactor;
    }

    private void Flash()
    {
        if (_spriteRenderer == null || flashDuration <= 0f) return;
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        _spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        _spriteRenderer.color = _baseColor;
    }

    private Transform ResolveSpriteTransform()
    {
        if (spriteTransform != null) return spriteTransform;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return sr.transform;
        var child = transform.Find("Sprite");
        return child != null ? child : transform;
    }

    private Vector3 ResolveAttachLocalPosition()
    {
        if (_playerAttachArea != null)
        {
            var areaTransform = _playerAttachArea.transform;
            var size = _playerAttachArea.size;
            var offset = _playerAttachArea.offset;
            Vector3 localPoint = new Vector3(
                Random.Range(-size.x * 0.5f, size.x * 0.5f) + offset.x,
                Random.Range(-size.y * 0.5f, size.y * 0.5f) + offset.y,
                0f);
            Vector3 worldPoint = areaTransform.TransformPoint(localPoint);
            return _player.InverseTransformPoint(worldPoint);
        }

        return new Vector3(
            Random.Range(attachAreaMin.x, attachAreaMax.x),
            Random.Range(attachAreaMin.y, attachAreaMax.y),
            0f);
    }

    private BoxCollider2D ResolvePlayerAttachArea(Transform playerTransform)
    {
        if (playerTransform == null) return null;
        var area = playerTransform.GetComponentInChildren<MaskAttachArea>();
        if (area != null) return area.AreaCollider;

        if (!string.IsNullOrEmpty(attachAreaChildName))
        {
            var child = playerTransform.Find(attachAreaChildName);
            if (child != null)
            {
                var col = child.GetComponent<BoxCollider2D>();
                if (col != null) return col;
            }
        }

        return null;
    }

    private Collider2D ResolvePlayerCollider(Transform playerTransform)
    {
        if (playerTransform == null) return null;
        var cols = playerTransform.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            var col = cols[i];
            if (col != null && col.enabled && !col.isTrigger)
            {
                return col;
            }
        }
        for (int i = 0; i < cols.Length; i++)
        {
            var col = cols[i];
            if (col != null && col.enabled)
            {
                return col;
            }
        }
        return null;
    }

    private float GetDistanceToPlayer()
    {
        if (_player == null) return float.PositiveInfinity;
        if (_playerCollider == null || !_playerCollider.enabled)
        {
            _playerCollider = ResolvePlayerCollider(_player);
        }

        if (_playerCollider == null)
        {
            return Vector2.Distance(_moveTransform.position, _player.position);
        }

        if (_maskColliders == null || _maskColliders.Length == 0)
        {
            _maskColliders = GetComponentsInChildren<Collider2D>(true);
        }

        float minDistance = float.PositiveInfinity;
        for (int i = 0; i < _maskColliders.Length; i++)
        {
            var col = _maskColliders[i];
            if (col == null || !col.enabled) continue;
            var distance = col.Distance(_playerCollider);
            if (distance.distance < minDistance)
            {
                minDistance = distance.distance;
                if (minDistance <= stickDistance)
                {
                    break;
                }
            }
        }

        if (float.IsInfinity(minDistance))
        {
            return Vector2.Distance(_moveTransform.position, _player.position);
        }

        return minDistance;
    }
}
