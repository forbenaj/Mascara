using System.Collections.Generic;
using UnityEngine;

// Basic rectangular room; generates wall colliders from size.
[DisallowMultipleComponent]
[ExecuteAlways]
public class Room : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Width (X) and Height (Y) of the room in world units.")]
    public Vector2 size = new Vector2(30f, 18f);

    [Tooltip("Thickness of generated boundary colliders.")]
    public float wallThickness = 0.5f;

    [Tooltip("Optional unique id used by RoomManager / doors.")]
    public string roomId = "Room";

    [Header("References")]
    [Tooltip("Parent for obstacles placed in editor (purely organizational).")]
    public Transform obstaclesRoot;

    [Header("Flow")]
    [Tooltip("Si está activo, al entrar se bloquean las puertas hasta que el room se marque como cleared.")]
    public bool lockDoorsOnEnter;

    private List<RoomDoor> _doors = new List<RoomDoor>();

    private const string BoundsRootName = "_Bounds";
    private readonly string[] _wallNames = { "North", "South", "East", "West" };

    private void Awake()
    {
        CacheDoors();
        BuildBounds();
    }

    private void OnValidate()
    {
        size.x = Mathf.Max(1f, size.x);
        size.y = Mathf.Max(1f, size.y);
        wallThickness = Mathf.Clamp(wallThickness, 0.05f, 5f);
        CacheDoors();
        BuildBounds();
    }

    public void OnEnter()
    {
        if (lockDoorsOnEnter)
            SetDoorsLocked(true);
    }

    public void MarkCleared()
    {
        SetDoorsLocked(false);
    }

    private void CacheDoors()
    {
        _doors.Clear();
        GetComponentsInChildren(true, _doors);
    }

    private void SetDoorsLocked(bool locked)
    {
        foreach (var d in _doors)
        {
            if (locked) d.Lock();
            else d.Unlock();
        }
    }

    private void BuildBounds()
    {
        var boundsRoot = GetOrCreateBoundsRoot();

        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;
        float t = wallThickness;

        // North / South (horizontal)
        ConfigureWall(boundsRoot, _wallNames[0],
            new Vector2(0f, halfH + t * 0.5f),
            new Vector2(size.x, t));

        ConfigureWall(boundsRoot, _wallNames[1],
            new Vector2(0f, -halfH - t * 0.5f),
            new Vector2(size.x, t));

        // East / West (vertical)
        ConfigureWall(boundsRoot, _wallNames[2],
            new Vector2(halfW + t * 0.5f, 0f),
            new Vector2(t, size.y + (t * 2f)));

        ConfigureWall(boundsRoot, _wallNames[3],
            new Vector2(-halfW - t * 0.5f, 0f),
            new Vector2(t, size.y + (t * 2f)));
    }

    private Transform GetOrCreateBoundsRoot()
    {
        var child = transform.Find(BoundsRootName);
        if (child != null) return child;

        var go = new GameObject(BoundsRootName);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    private void ConfigureWall(Transform parent, string name, Vector2 localPos, Vector2 size2D)
    {
        var wall = parent.Find(name);
        if (wall == null)
        {
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            wall = go.transform;
            wall.SetParent(parent);
            wall.localRotation = Quaternion.identity;
            wall.localScale = Vector3.one;
            go.AddComponent<BoxCollider2D>();
        }

        wall.localPosition = (Vector3)localPos;
        var box = wall.GetComponent<BoxCollider2D>();
        box.size = size2D;
        box.offset = Vector2.zero;
        box.isTrigger = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        var center = Vector3.zero;
        var size3 = new Vector3(size.x, size.y, 0.1f);
        Gizmos.DrawWireCube(center, size3);
    }
}
