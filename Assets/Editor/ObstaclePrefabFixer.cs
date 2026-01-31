using UnityEditor;
using UnityEngine;

// Regenera los prefabs de Assets/Prefabs/Obstacles con doble collider (cuerpo + superficie Ground).
public static class ObstaclePrefabFixer
{
    private const string Folder = "Assets/Prefabs/Obstacles";
    private const float SurfaceThickness = 0.08f;

    [InitializeOnLoadMethod]
    private static void AutoFix()
    {
        // Si los prefabs ya tienen Surface, no hacemos nada.
        var staticPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/StaticPlatform.prefab");
        if (staticPrefab != null && staticPrefab.transform.Find("Surface") != null) return;
        Rebuild();
    }

    [MenuItem("Tools/Obstacles/Rebuild Prefabs")]
    public static void Rebuild()
    {
        CreateStatic();
        CreateOneWay();
        CreateMoving();
        CreateSpike(); // se mantiene simple
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Obstacles prefabs rebuilt with body + surface colliders.");
    }

    private static void CreateStatic()
    {
        var go = MakePlatformBase("StaticPlatform", new Vector3(3f, 1f, 1f), false);
        Save(go, $"{Folder}/StaticPlatform.prefab");
    }

    private static void CreateOneWay()
    {
        var go = MakePlatformBase("OneWayPlatform", new Vector3(3f, 0.6f, 1f), true);
        Save(go, $"{Folder}/OneWayPlatform.prefab");
    }

    private static void CreateMoving()
    {
        var go = new GameObject("MovingPlatform");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSprite();
        sr.color = new Color(0.7f, 0.9f, 0.6f);
        go.transform.localScale = new Vector3(2.5f, 0.6f, 1f);

        var body = go.AddComponent<BoxCollider2D>();
        body.size = Vector2.one;
        body.sharedMaterial = LowFriction();

        CreateSurface(go.transform, body, SurfaceThickness);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        go.AddComponent<MovingPlatform>();
        Save(go, $"{Folder}/MovingPlatform.prefab");
    }

    private static void CreateSpike()
    {
        string path = $"{Folder}/SpikeHazard.prefab";
        var go = new GameObject("SpikeHazard");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSprite();
        sr.color = new Color(1f, 0.3f, 0.3f);
        go.transform.localScale = new Vector3(1f, 0.5f, 1f);

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        go.AddComponent<Hazard>();
        Save(go, path);
    }

    private static GameObject MakePlatformBase(string name, Vector3 scale, bool oneWay)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultSprite();
        sr.color = oneWay ? new Color(0.55f, 0.8f, 1f) : new Color(0.8f, 0.8f, 0.8f);
        go.transform.localScale = scale;

        // Cuerpo que bloquea al jugador
        var body = go.AddComponent<BoxCollider2D>();
        body.size = Vector2.one;
        body.sharedMaterial = LowFriction();

        // Superficie Ground delgada arriba, mismo ancho que el cuerpo
        var surface = CreateSurface(go.transform, body, SurfaceThickness);

        if (oneWay)
        {
            var eff = surface.gameObject.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 170f;
            var surfCol = surface.GetComponent<BoxCollider2D>();
            surfCol.usedByEffector = true;
        }

        return go;
    }

    private static Transform CreateSurface(Transform parent, BoxCollider2D body, float thickness)
    {
        var surface = new GameObject("Surface").transform;
        surface.SetParent(parent);
        float offsetY = (body.size.y * 0.5f) + (thickness * 0.5f);
        surface.localPosition = new Vector3(0f, offsetY, 0f);
        surface.localRotation = Quaternion.identity;
        surface.localScale = Vector3.one;
        var col = surface.gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(body.size.x, thickness);
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) surface.gameObject.layer = groundLayer;
        return surface;
    }

    private static PhysicsMaterial2D LowFriction()
    {
        var mat = new PhysicsMaterial2D("LowFriction") { friction = 0.01f, bounciness = 0f };
        return mat;
    }

    private static Sprite GetDefaultSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static void Save(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
