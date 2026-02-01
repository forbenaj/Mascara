using UnityEngine;
#if CINEMACHINE
using Cinemachine;
#endif

/// Ensures the camera collision/confiner only interacts with the Wall layer.
/// Attach to the GameObject that has the CinemachineVirtualCamera (and Collider/Confiner extension).
[DisallowMultipleComponent]
public class CameraCollisionMask : MonoBehaviour
{
    [Tooltip("Layer names the camera should collide/confine against (default: Wall).")]
    public string collideLayerName = "Wall";

    [Tooltip("Optional: tag to ignore (e.g., Player).")]
    public string ignoreTag = "Player";

#if CINEMACHINE
    private void Awake()
    {
        int mask = LayerMask.GetMask(collideLayerName);
        var vcam = GetComponent<CinemachineVirtualCamera>();
        if (vcam != null)
        {
            var collider = vcam.GetComponent<CinemachineCollider>();
            if (collider != null)
            {
                collider.m_CollideAgainst = mask;
                collider.m_IgnoreTag = ignoreTag;
            }

            var confiner = vcam.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                confiner.m_BoundingShape2D = FindWallBounds();
            }
        }
    }

    private Collider2D FindWallBounds()
    {
        // Try to find a composite collider on Wall layer
        int mask = LayerMask.GetMask(collideLayerName);
        var all = FindObjectsOfType<CompositeCollider2D>();
        foreach (var c in all)
        {
            if (((1 << c.gameObject.layer) & mask) != 0)
                return c;
        }
        return null;
    }
#endif
}
