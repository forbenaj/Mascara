using System.Collections.Generic;
using UnityEngine;

/// Manages additive ambient layers (boss phases, masks).
[RequireComponent(typeof(AudioSource))]
public class AmbientAudioLayers : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.6f;
    }

    public List<Layer> layers = new List<Layer>();

    private readonly Dictionary<string, AudioSource> _active = new Dictionary<string, AudioSource>();

    public void EnableLayer(string key)
    {
        var layer = layers.Find(l => l.key == key);
        if (layer == null || layer.clip == null) return;
        if (_active.ContainsKey(key)) return;

        var src = gameObject.AddComponent<AudioSource>();
        src.clip = layer.clip;
        src.loop = true;
        src.volume = layer.volume;
        src.Play();
        _active[key] = src;
    }

    public void DisableLayer(string key)
    {
        if (_active.TryGetValue(key, out var src))
        {
            src.Stop();
            Destroy(src);
            _active.Remove(key);
        }
    }
}
