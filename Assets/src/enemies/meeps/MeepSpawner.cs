using UnityEngine;

public class MeepSpawner : MonoBehaviour
{
    [Tooltip("Prefab del meep.")]
    public Meep meepPrefab;
    [Tooltip("Lista de prefabs posibles; se elige uno al azar.")]
    public Meep[] meepPrefabs;

    [Tooltip("Segundos entre spawns.")]
    public float interval = 2.5f;

    [Tooltip("Límite simultáneo de meeps activos; 0 = sin límite.")]
    public int maxAlive = 6;

    [Tooltip("Ajuste inicial de golpe/variancia para este spawner.")]
    public int hitsBaseOverride = 0;

    [Tooltip("Opcional, parent donde instanciar.")]
    public Transform meepParent;

    private float _timer;

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        if (maxAlive > 0)
        {
            if (CurrentAlive() >= maxAlive) return;
        }

        SpawnOne();
        _timer = interval;
    }

    public void SpawnOne()
    {
        var prefab = PickPrefab();
        if (prefab == null) return;
        var parent = meepParent != null ? meepParent : transform;
        var meep = Instantiate(prefab, transform.position, Quaternion.identity, parent);
        if (hitsBaseOverride > 0)
            meep.baseHitsToKill = hitsBaseOverride;
    }

    public int CurrentAlive()
    {
        if (meepParent != null) return meepParent.childCount;
        int count = 0;
        foreach (Transform child in transform)
        {
            if (child != null) count++;
        }
        return count;
    }

    private Meep PickPrefab()
    {
        if (meepPrefabs != null && meepPrefabs.Length > 0)
        {
            return meepPrefabs[Random.Range(0, meepPrefabs.Length)];
        }

        return meepPrefab;
    }
}
