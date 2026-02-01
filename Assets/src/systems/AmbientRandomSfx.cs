using UnityEngine;
using System.Collections;

public class AmbientRandomSfx : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] clips;
    public Vector2 intervalRange = new Vector2(15f, 20f);

    private Coroutine _routine;

    private void OnEnable()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (clips != null && clips.Length > 0)
            _routine = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (_routine != null) StopCoroutine(_routine);
    }

    private IEnumerator Loop()
    {
        while (true)
        {
            float wait = Random.Range(intervalRange.x, intervalRange.y);
            yield return new WaitForSeconds(wait);
            PlayRandom();
        }
    }

    private void PlayRandom()
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;
        var clip = clips[Random.Range(0, clips.Length)];
        if (clip != null) audioSource.PlayOneShot(clip);
    }
}
