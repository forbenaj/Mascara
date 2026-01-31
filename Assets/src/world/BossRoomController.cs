using UnityEngine;

[DisallowMultipleComponent]
public class BossRoomController : MonoBehaviour
{
    [Tooltip("Room asociado (se autocompleta si está en el mismo GO).")]
    public Room room;

    [Tooltip("Controla capas de ambiente; agrega una por fase.")]
    public AmbientAudioLayers ambientLayers;

    private void Awake()
    {
        if (room == null)
            room = GetComponent<Room>();

        if (room != null)
            room.lockDoorsOnEnter = true;
    }

    public void OnPhaseChanged(int phase)
    {
        if (ambientLayers == null) return;
        ambientLayers.EnableLayer($"phase{phase}");
    }

    // Llamar cuando termine la pelea (win/lose) para abrir puertas.
    public void EndFight()
    {
        if (room != null)
            room.MarkCleared();
    }
}
