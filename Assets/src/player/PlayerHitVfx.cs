using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHitVfx : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerHealth health;
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private bool autoFindRenderers = true;
    [SerializeField] private bool refreshRenderersOnFlash = true;

    [Header("Hit Flash")]
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashDuration = 0.12f;

    [Header("Death Fade")]
    [SerializeField] private float deathFadeSeconds = 0.6f;

    private Color[] cachedColors;
    private Coroutine flashRoutine;
    private Coroutine fadeRoutine;
    private int lastHealth;
    private bool listenersRegistered;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<PlayerHealth>();

        if ((renderers == null || renderers.Length == 0) && autoFindRenderers)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);

        EnsureColorCache();
        CaptureCurrentColors();

        if (health != null)
            lastHealth = health.CurrentHealth;
    }

    private void OnEnable()
    {
        RegisterListeners();
    }

    private void OnDisable()
    {
        UnregisterListeners();
        StopAllCoroutines();
        flashRoutine = null;
        fadeRoutine = null;
        RestoreColors();
    }

    private void RegisterListeners()
    {
        if (listenersRegistered || health == null)
            return;

        health.onHealthChanged?.AddListener(HandleHealthChanged);
        health.onDeath?.AddListener(HandleDeath);
        listenersRegistered = true;
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered || health == null)
            return;

        health.onHealthChanged?.RemoveListener(HandleHealthChanged);
        health.onDeath?.RemoveListener(HandleDeath);
        listenersRegistered = false;
    }

    private void HandleHealthChanged(int currentHealth)
    {
        if (currentHealth < lastHealth)
            TriggerFlash();

        lastHealth = currentHealth;
    }

    private void HandleDeath()
    {
        StartFadeOut();
    }

    public void TriggerFlash()
    {
        if (flashDuration <= 0f)
            return;
        if (refreshRenderersOnFlash)
            RefreshRenderers();
        if (renderers == null || renderers.Length == 0)
            return;
        if (fadeRoutine != null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        CaptureCurrentColors();
        ApplyFlashColor();
        yield return new WaitForSeconds(flashDuration);
        RestoreColors();
        flashRoutine = null;
    }

    private void StartFadeOut()
    {
        if (deathFadeSeconds <= 0f)
            return;
        if (refreshRenderersOnFlash)
            RefreshRenderers();
        if (renderers == null || renderers.Length == 0)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        CaptureCurrentColors();

        float t = 0f;
        while (t < deathFadeSeconds)
        {
            float alpha = 1f - (t / deathFadeSeconds);
            ApplyFade(alpha);
            t += Time.deltaTime;
            yield return null;
        }

        ApplyFade(0f);
        fadeRoutine = null;
    }

    private void EnsureColorCache()
    {
        if (renderers == null)
            return;
        if (cachedColors == null || cachedColors.Length != renderers.Length)
            cachedColors = new Color[renderers.Length];
    }

    private void CaptureCurrentColors()
    {
        EnsureColorCache();
        if (cachedColors == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var sr = renderers[i];
            cachedColors[i] = sr != null ? sr.color : Color.white;
        }
    }

    private void RefreshRenderers()
    {
        if (!autoFindRenderers)
            return;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        EnsureColorCache();
    }

    private void ApplyFlashColor()
    {
        if (cachedColors == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var sr = renderers[i];
            if (sr == null)
                continue;

            var c = flashColor;
            c.a = cachedColors[i].a;
            sr.color = c;
        }
    }

    private void RestoreColors()
    {
        if (cachedColors == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var sr = renderers[i];
            if (sr == null)
                continue;

            sr.color = cachedColors[i];
        }
    }

    private void ApplyFade(float alphaMultiplier)
    {
        if (cachedColors == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var sr = renderers[i];
            if (sr == null)
                continue;

            var c = cachedColors[i];
            c.a *= alphaMultiplier;
            sr.color = c;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if ((renderers == null || renderers.Length == 0) && autoFindRenderers)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        EnsureColorCache();
    }
#endif
}
