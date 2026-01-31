using System.IO;
using UnityEditor;
using UnityEngine;

/// Generates a small kit of obstacle prefabs if they are missing.
public static class ObstaclePrefabBootstrap
{
    private const string Folder = "Assets/Prefabs/Obstacles";

    [InitializeOnLoadMethod]
    private static void EnsurePrefabs()
    {
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);

        CreatePlatformPrefab("StaticPlatform", new Vector2(3f, 1f), oneWay: false);
        CreatePlatformPrefab("OneWayPlatform", new Vector2(3f, 0.5f), oneWay: true);
        CreateSpikePrefab();
        CreateMovingPlatformPrefab();
    }

    private static Sprite GetDefaultSprite()
    {
        // Built-in UI sprite is available even in URP/2D templates.
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static void CreatePlatformPrefab(string name, Vector2 scale, bool oneWay)
    {
        string path = $"{Folder}/{name}.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSprite();
        sr.color = oneWay ? new Color(0.55f, 0.8f, 1f) : new Color(0.8f, 0.8f, 0.8f);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        var box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
        box.usedByEffector = oneWay;

        if (oneWay)
        {
            var eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 170f;
        }

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateSpikePrefab()
    {
        string path = $"{Folder}/SpikeHazard.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject("SpikeHazard");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSprite();
        sr.color = new Color(1f, 0.3f, 0.3f);
        go.transform.localScale = new Vector3(1f, 0.5f, 1f);

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        var hz = go.AddComponent<Hazard>();
        hz.damage = 1;
        hz.triggerOnly = true;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateMovingPlatformPrefab()
    {
        string path = $"{Folder}/MovingPlatform.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject("MovingPlatform");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSprite();
        sr.color = new Color(0.7f, 0.9f, 0.6f);
        go.transform.localScale = new Vector3(2.5f, 0.6f, 1f);

        go.AddComponent<BoxCollider2D>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        go.AddComponent<MovingPlatform>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
