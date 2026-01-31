using UnityEngine;

// Marker for a door/transition anchor inside a Room.
[DisallowMultipleComponent]
public class RoomDoor : MonoBehaviour
{
    [Tooltip("Opcional: identificador local de la puerta (para enlazar ida/vuelta).")]
    public string doorId;

    [Tooltip("Room id this door should send the player to.")]
    public string targetRoomId;

    [Tooltip("Optional spawn offset inside the destination room.")]
    public Vector2 targetSpawnOffset;

    [Tooltip("Visualize facing direction of the door.")]
    public Vector2 facing = Vector2.up;

    [Header("State")]
    [SerializeField, Tooltip("Si está bloqueada, el trigger no hará nada.")]
    private bool locked;

    public bool IsLocked => locked;

    public void Lock() => locked = true;
    public void Unlock() => locked = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = locked ? Color.red : Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.15f);

        if (facing.sqrMagnitude > 0.001f)
        {
            Vector3 dir = ((Vector3)facing).normalized * 0.6f;
            Gizmos.DrawLine(transform.position, transform.position + dir);
        }
    }
}
