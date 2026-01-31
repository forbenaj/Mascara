using UnityEngine;

[DisallowMultipleComponent]
public class BossRoomController : MonoBehaviour
{
    [Tooltip("Room asociado (se autocompleta si está en el mismo GO).")]
    public Room room;

    private void Awake()
    {
        if (room == null)
            room = GetComponent<Room>();

        if (room != null)
            room.lockDoorsOnEnter = true;
    }

    // Llamar cuando termine la pelea (win/lose) para abrir puertas.
    public void EndFight()
    {
        if (room != null)
            room.MarkCleared();
    }
}
