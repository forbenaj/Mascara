using UnityEngine;

// Colócalo en la RoomSpawn para disparar diálogos iniciales.
public class DialogueTrigger : MonoBehaviour
{
    [TextArea(2, 4)] public string[] lines;
    public bool playOnStart = true;

    public DialogueController controller;

    private bool _played;

    private void Awake()
    {
        if (controller == null)
            controller = FindObjectOfType<DialogueController>();
    }

    private void Start()
    {
        if (playOnStart && !_played)
            Trigger();
    }

    public void Trigger()
    {
        if (_played) return;
        if (controller == null || lines == null || lines.Length == 0) return;
        controller.StartDialogue(lines);
        _played = true;
    }
}
