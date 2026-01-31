using UnityEditor;
using UnityEngine;

// Forces gizmos to render even when the object is not selected.
public static class RoomGizmosDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active | GizmoType.Pickable | GizmoType.NotInSelectionHierarchy)]
    private static void DrawRoomGizmo(Room room, GizmoType gizmoType)
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = room.transform.localToWorldMatrix;
        var size3 = new Vector3(room.size.x, room.size.y, 0.1f);
        Gizmos.DrawWireCube(Vector3.zero, size3);

        var boundsRoot = room.transform.Find("_Bounds");
        if (boundsRoot != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            foreach (Transform child in boundsRoot)
            {
                var box = child.GetComponent<BoxCollider2D>();
                if (box == null) continue;
                Gizmos.matrix = child.localToWorldMatrix;
                Gizmos.DrawCube(box.offset, box.size);
            }
        }
        Gizmos.matrix = Matrix4x4.identity;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active | GizmoType.Pickable | GizmoType.NotInSelectionHierarchy)]
    private static void DrawSpawnPointGizmo(SpawnPoint sp, GizmoType gizmoType)
    {
        Gizmos.color = Color.green;
        float s = 0.4f;
        Vector3 p = sp.transform.position;
        Gizmos.DrawLine(p + Vector3.left * s, p + Vector3.right * s);
        Gizmos.DrawLine(p + Vector3.up * s, p + Vector3.down * s);
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active | GizmoType.Pickable | GizmoType.NotInSelectionHierarchy)]
    private static void DrawHazardGizmo(Hazard hz, GizmoType gizmoType)
    {
        var col = hz.GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.matrix = hz.transform.localToWorldMatrix;

        switch (col)
        {
            case BoxCollider2D box:
                Gizmos.DrawCube(box.offset, box.size);
                break;
            case CircleCollider2D circle:
                Gizmos.DrawSphere(circle.offset, circle.radius);
                break;
            case CapsuleCollider2D capsule:
                Gizmos.DrawCube(capsule.offset, capsule.size);
                break;
        }
        Gizmos.matrix = Matrix4x4.identity;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active | GizmoType.Pickable | GizmoType.NotInSelectionHierarchy)]
    private static void DrawMovingPlatformGizmo(MovingPlatform mp, GizmoType gizmoType)
    {
        Vector3 start = Application.isPlaying ? mp.transform.position : mp.transform.position;
        Vector3 end = Application.isPlaying ? mp.transform.position + (Vector3)mp.endOffset : mp.transform.position + (Vector3)mp.endOffset;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.1f);

        var box = mp.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = mp.transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.35f);
            Gizmos.DrawCube(box.offset, box.size);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}
