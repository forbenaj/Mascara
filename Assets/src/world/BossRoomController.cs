using UnityEngine;

[DisallowMultipleComponent]
public class BossRoomController : MonoBehaviour
{
    [Tooltip("Room asociado (se autocompleta si está en el mismo GO).")]
    public Room room;

    [Tooltip("Controla capas de ambiente; agrega una por fase.")]
    public AmbientAudioLayers ambientLayers;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip appearClip;
    [Range(0f, 3f)] public float appearVolume = 1.2f;
    [Tooltip("Prioridad baja = más importante. 0 = más alto, 256 = más bajo.")]
    public int appearPriority = 16;
    private bool _playedAppear;
    [Tooltip("Si true, además de PlayOneShot se dispara un PlayClipAtPoint en la posición de la cámara.")]
    public bool fallbackAtCamera = true;

    private void Awake()
    {
        if (room == null)
            room = GetComponent<Room>();

        if (room != null)
            room.lockDoorsOnEnter = true;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f; // 2D para que siempre se escuche
            audioSource.priority = appearPriority;
        }
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

    // Audio de aparición deshabilitado; interfaz vacía por compatibilidad.
    public void PlayAppear() { }
}
