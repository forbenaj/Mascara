using UnityEngine;

[RequireComponent(typeof(RoomDoor))]
[RequireComponent(typeof(Collider2D))]
public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private RoomDoor _door;

    private void Awake()
    {
        _door = GetComponent<RoomDoor>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_door.IsLocked) return;
        if (!other.CompareTag(playerTag)) return;

        var manager = FindManager();
        var destRoom = manager?.GetRoomById(_door.targetRoomId);
        if (manager == null || destRoom == null)
        {
            Debug.LogWarning($"DoorTrigger: destino '{_door.targetRoomId}' no encontrado.");
            return;
        }

        manager.SetCurrentRoom(destRoom, _door.targetSpawnOffset, other.transform);
    }

    private RoomManager FindManager()
    {
#if UNITY_2022_2_OR_NEWER
        return Object.FindFirstObjectByType<RoomManager>();
#else
        return Object.FindObjectOfType<RoomManager>();
#endif
    }
}
