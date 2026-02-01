using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollowRoom : MonoBehaviour
{
    [Tooltip("Tag del jugador a seguir.")]
    public string playerTag = "Player";

    [Tooltip("Suavizado de seguimiento.")]
    public float smoothTime = 0.08f;

    [Tooltip("Velocidad máxima del desplazamiento de cámara (previene tirones en caídas rápidas).")]
    public float maxFollowSpeed = 35f;

    private Camera _cam;
    private Transform _player;
    private Rigidbody2D _playerRb;
    private RoomManager _roomManager;
    private Vector3 _velocity;
    private Room _lastRoom;
    private Room _queuedRoom;
    private bool _pendingSnap;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
#if UNITY_2022_2_OR_NEWER
        _roomManager = FindFirstObjectByType<RoomManager>();
#else
        _roomManager = FindObjectOfType<RoomManager>();
#endif
    }

    private void LateUpdate()
    {
        var currentRoom = _queuedRoom != null ? _queuedRoom : _roomManager?.CurrentRoom;
        if (currentRoom == null) return;

        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null)
            {
                _player = go.transform;
                _playerRb = go.GetComponent<Rigidbody2D>();
            }
            if (_player == null) return;
        }

        var room = currentRoom;
        bool roomChanged = room != _lastRoom || _pendingSnap;
        _lastRoom = room;

        var roomPos = room.transform.position;
        var roomScale = room.transform.lossyScale;
        var scaledSize = Vector2.Scale(room.size, new Vector2(Mathf.Abs(roomScale.x), Mathf.Abs(roomScale.y)));
        var halfCam = new Vector2(_cam.orthographicSize * _cam.aspect, _cam.orthographicSize);

        // Room extents (considering transform scale)
        var halfRoom = scaledSize * 0.5f;
        var min = roomPos - (Vector3)halfRoom;
        var max = roomPos + (Vector3)halfRoom;

        bool fitsX = scaledSize.x <= halfCam.x * 2f;
        bool fitsY = scaledSize.y <= halfCam.y * 2f;

        Vector3 playerPos = _playerRb ? (Vector3)_playerRb.position : _player.position;
        float x = fitsX ? roomPos.x : Mathf.Clamp(playerPos.x, min.x + halfCam.x, max.x - halfCam.x);
        float y = fitsY ? roomPos.y : Mathf.Clamp(playerPos.y, min.y + halfCam.y, max.y - halfCam.y);
        Vector3 targetPos = new Vector3(x, y, transform.position.z);

        if (roomChanged)
        {
            SnapInternal(targetPos, room);
            _pendingSnap = false;
            _queuedRoom = null;
        }
        else
        {
            var newPos = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime, maxFollowSpeed, Time.deltaTime);

            bool clampX = !fitsX && (Mathf.Approximately(targetPos.x, min.x + halfCam.x) || Mathf.Approximately(targetPos.x, max.x - halfCam.x));
            bool clampY = !fitsY && (Mathf.Approximately(targetPos.y, min.y + halfCam.y) || Mathf.Approximately(targetPos.y, max.y - halfCam.y));

            if (clampX)
            {
                float minX = min.x + halfCam.x;
                float maxX = max.x - halfCam.x;
                if (newPos.x < minX || newPos.x > maxX)
                {
                    newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
                    _velocity.x = 0f;
                }
            }
            if (!fitsY)
            {
                float minY = min.y + halfCam.y;
                float maxY = max.y - halfCam.y;
                if (newPos.y < minY || newPos.y > maxY)
                {
                    newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
                    _velocity.y = 0f;
                }
            }

            transform.position = newPos;
        }
    }

    // Called by RoomManager on room switch to snap instantly.
    public void SnapToRoom(Room room, Transform player)
    {
        _roomManager ??= room != null ? room.GetComponentInParent<RoomManager>() : _roomManager;
        _player = player != null ? player : _player;
        if (room == null) return;
        _queuedRoom = room;
        _pendingSnap = true;
    }

    private void SnapInternal(Vector3 targetPos, Room room)
    {
        _velocity = Vector3.zero;
        _lastRoom = room;
        transform.position = targetPos;
    }

    private Vector3 CalculateTarget(Room room, Transform player)
    {
        var roomPos = room.transform.position;
        var roomScale = room.transform.lossyScale;
        var scaledSize = Vector2.Scale(room.size, new Vector2(Mathf.Abs(roomScale.x), Mathf.Abs(roomScale.y)));
        var halfCam = new Vector2(_cam.orthographicSize * _cam.aspect, _cam.orthographicSize);
        var halfRoom = scaledSize * 0.5f;
        var min = roomPos - (Vector3)halfRoom;
        var max = roomPos + (Vector3)halfRoom;

        bool fitsX = scaledSize.x <= halfCam.x * 2f;
        bool fitsY = scaledSize.y <= halfCam.y * 2f;

        if (fitsX && fitsY)
        {
            return new Vector3(roomPos.x, roomPos.y, transform.position.z);
        }
        else
        {
            float x = Mathf.Clamp(player != null ? player.position.x : roomPos.x, min.x + halfCam.x, max.x - halfCam.x);
            float y = Mathf.Clamp(player != null ? player.position.y : roomPos.y, min.y + halfCam.y, max.y - halfCam.y);
            return new Vector3(x, y, transform.position.z);
        }
    }
}
