using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour

{
    [Tooltip("Panel que contiene el texto de diálogo.")]
    public GameObject dialoguePanel;
    [Tooltip("Texto donde se mostrará la línea actual (UI Text).")]
    public Text dialogueText;
#if TMP_PRESENT
    [Tooltip("Alternativa con TMP_Text; si está asignado, se usa en lugar de dialogueText.")]
    public TMPro.TMP_Text dialogueTMP;
#endif
    [Tooltip("Opcional: límite de caracteres por línea para evitar que se salga del panel.")]
    public int maxCharsPerLine = 60;
    [Tooltip("Referencia al player para bloquear controles.")]
    public PlayerController player;

    [Tooltip("Tecla para avanzar diálogo.")]
    public KeyCode advanceKey = KeyCode.Return;

    private readonly Queue<string> _lines = new Queue<string>();
    private bool _active;

    private void Awake()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (player == null)
            player = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        if (!_active) return;
        if (AdvancePressed())
            ShowNext();
    }

    public void StartDialogue(IEnumerable<string> lines)
    {
        _lines.Clear();
        foreach (var l in lines)
            _lines.Enqueue(l);

        _active = true;
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (player != null) player.controlsLocked = true;

        ShowNext();
    }

    private void ShowNext()
    {
        if (_lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        var line = _lines.Dequeue();
        line = WrapLine(line);
        if (dialogueText != null)
            dialogueText.text = line;
#if TMP_PRESENT
        if (dialogueTMP != null)
            dialogueTMP.text = line;
#endif
    }

    private string WrapLine(string line)
    {
        if (maxCharsPerLine <= 0) return line;
        var words = line.Split(' ');
        var result = "";
        var current = "";
        foreach (var w in words)
        {
            if (current.Length + w.Length + 1 > maxCharsPerLine)
            {
                result += current.TrimEnd() + "\n";
                current = "";
            }
            current += w + " ";
        }
        result += current.TrimEnd();
        return result;
    }

    private bool AdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && keyboard.enterKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(advanceKey);
#endif
    }

    private void EndDialogue()
    {
        _active = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (player != null) player.controlsLocked = false;
    }
}
