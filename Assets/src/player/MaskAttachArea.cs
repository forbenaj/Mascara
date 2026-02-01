using UnityEngine;

/// Defines the area on the player where masks can attach.
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class MaskAttachArea : MonoBehaviour
{
    [Tooltip("Optional override for the attach area collider.")]
    public BoxCollider2D areaCollider;

    public BoxCollider2D AreaCollider => areaCollider != null ? areaCollider : GetComponent<BoxCollider2D>();

    private void Awake()
    {
        if (areaCollider == null)
        {
            areaCollider = GetComponent<BoxCollider2D>();
        }
        areaCollider.isTrigger = true;
    }

    private void OnDrawGizmos()
    {
        var col = AreaCollider;
        if (col == null) return;
        Gizmos.color = new Color(0.2f, 0.8f, 0.9f, 0.9f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(col.offset, col.size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
