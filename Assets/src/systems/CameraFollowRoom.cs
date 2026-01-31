using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollowRoom : MonoBehaviour
{
    [Tooltip("Tag del jugador a seguir.")]
    public string playerTag = "Player";

    [Tooltip("Suavizado de seguimiento.")]
    public float smoothTime = 0.08f;

    private Camera _cam;
    private Transform _player;
    private RoomManager _roomManager;
    private Vector3 _velocity;

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
        if (_roomManager == null || _roomManager.CurrentRoom == null) return;

        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) _player = go.transform;
            if (_player == null) return;
        }

        var room = _roomManager.CurrentRoom;
        var roomPos = room.transform.position;
        var halfCam = new Vector2(_cam.orthographicSize * _cam.aspect, _cam.orthographicSize);

        // Room extents
        var halfRoom = room.size * 0.5f;
        var min = roomPos - (Vector3)halfRoom;
        var max = roomPos + (Vector3)halfRoom;

        Vector3 targetPos;
        bool fitsX = room.size.x <= halfCam.x * 2f;
        bool fitsY = room.size.y <= halfCam.y * 2f;

        if (fitsX && fitsY)
        {
            targetPos = new Vector3(roomPos.x, roomPos.y, transform.position.z);
        }
        else
        {
            float x = Mathf.Clamp(_player.position.x, min.x + halfCam.x, max.x - halfCam.x);
            float y = Mathf.Clamp(_player.position.y, min.y + halfCam.y, max.y - halfCam.y);
            targetPos = new Vector3(x, y, transform.position.z);
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
    }
}
