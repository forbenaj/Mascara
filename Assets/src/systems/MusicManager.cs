using UnityEngine;

// Simple music manager: base loop always on, boss loop and mask layers additive.
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Base Music")]
    public AudioClip baseLoop;
    [Header("Boss Music")]
    public AudioClip bossLoop;
    public AudioClip mask1Loop;
    public AudioClip mask2Loop;
    public AudioClip mask3Loop;
    [Range(0f, 3f)] public float baseVolume = 1f;
    [Range(0f, 3f)] public float bossVolume = 1.5f;
    [Range(0f, 3f)] public float masksVolume = 1.5f;

    private bool _bossStarted;

    private AudioSource _baseSource;
    private AudioSource _bossSource;
    private AudioSource _mask1Source;
    private AudioSource _mask2Source;
    private AudioSource _mask3Source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _baseSource = CreateSource("BaseSource");
        _bossSource = CreateSource("BossSource");
        _mask1Source = CreateSource("Mask1Source");
        _mask2Source = CreateSource("Mask2Source");
        _mask3Source = CreateSource("Mask3Source");

        PlayBaseLoop();
    }

    private AudioSource CreateSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        return src;
    }

    public void PlayBaseLoop()
    {
        if (baseLoop == null) return;
        if (_baseSource.clip == baseLoop && _baseSource.isPlaying) return;
        _baseSource.clip = baseLoop;
        _baseSource.volume = baseVolume;
        _baseSource.Play();
    }

    public void StartBossLoop()
    {
        if (bossLoop == null) return;
        if (_bossSource.isPlaying) return;
        // Stop base when entering boss.
        if (_baseSource.isPlaying) _baseSource.Stop();
        _bossSource.clip = bossLoop;
        _bossSource.volume = bossVolume;
        _bossSource.Play();
        if (!_bossStarted)
        {
            Debug.Log($"[Music] Boss loop started: {bossLoop.name}, base stopped");
            _bossStarted = true;
        }
    }

    public void EnableMaskLayer(int phase)
    {
        // Phase 1: solo bossLoop
        // Phase 2: bossLoop + mask1
        // Phase 3: bossLoop + mask1 + mask2
        // Phase 4: bossLoop + mask1 + mask2 + mask3

        Debug.Log($"[Music] EnableMaskLayer called for phase {phase}");
        if (phase >= 2) PlayLayer(_mask1Source, mask1Loop, 1);
        if (phase >= 3) PlayLayer(_mask2Source, mask2Loop, 2);
        if (phase >= 4) PlayLayer(_mask3Source, mask3Loop, 3);
    }

    private void PlayLayer(AudioSource src, AudioClip clip, int idx)
    {
        if (src == null || clip == null) return;
        src.clip = clip;
        src.volume = masksVolume;
        if (src.isPlaying) src.Stop();
        src.Play();
        Debug.Log($"[Music] Mask layer {idx} on: {clip.name} vol {masksVolume}");
    }
}
