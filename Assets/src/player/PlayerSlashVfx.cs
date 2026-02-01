using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSlashVfx : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;

    [Header("Timing")]
    [SerializeField] private float frameSeconds = 0.05f;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        Hide();
    }

    public void Play()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null || frameSeconds <= 0f)
            return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        spriteRenderer.enabled = true;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                spriteRenderer.sprite = frames[i];
            yield return new WaitForSeconds(frameSeconds);
        }

        Hide();
        playRoutine = null;
    }

    private void Hide()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }
#endif
}
