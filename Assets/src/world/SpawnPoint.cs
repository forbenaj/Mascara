using UnityEngine;

// Visual marker to place the player inside a Room.
[DisallowMultipleComponent]
public class SpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        float s = 0.4f;
        Vector3 p = transform.position;
        Gizmos.DrawLine(p + Vector3.left * s, p + Vector3.right * s);
        Gizmos.DrawLine(p + Vector3.up * s, p + Vector3.down * s);
    }
    
}
