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

    // Called externally (e.g., when room se vuelve activo) to play the intro SFX once.
    public void PlayAppear()
    {
        TryPlayAppear();
    }

    private void OnEnable()
    {
        TryPlayAppear();
    }

    private void Start()
    {
        // En caso de que OnEnable ocurra antes de asignar el clip en prefab instanciado.
        TryPlayAppear();
    }

    private void TryPlayAppear()
    {
        if (_playedAppear) return;
        if (audioSource == null || appearClip == null)
        {
            Debug.LogWarning($"BossRoomController: faltan audioSource o appearClip en {name}");
            return;
        }
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"BossRoomController: {name} no está activo en jerarquía, no reproduce SFX.");
            return;
        }
        Debug.Log($"BossRoomController: reproduce appearClip {appearClip.name} en {name}");
        audioSource.priority = appearPriority;
        audioSource.PlayOneShot(appearClip, appearVolume);
        if (fallbackAtCamera && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(appearClip, Camera.main.transform.position, appearVolume);
        }
        _playedAppear = true;
    }
}
