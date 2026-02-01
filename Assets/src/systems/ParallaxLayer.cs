using UnityEngine;

// Moves a background layer based on camera movement for parallax.
public class ParallaxLayer : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax")]
    [Tooltip("0 = locked to camera, 1 = same speed as camera, >1 = faster than camera.")]
    [SerializeField] private Vector2 parallaxMultiplier = new Vector2(0.5f, 0.5f);
    [SerializeField] private bool lockX;
    [SerializeField] private bool lockY;

    private Vector3 _lastCamPos;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            _lastCamPos = cameraTransform.position;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        Vector3 delta = cameraTransform.position - _lastCamPos;
        Vector3 move = new Vector3(delta.x * parallaxMultiplier.x, delta.y * parallaxMultiplier.y, 0f);

        if (lockX) move.x = 0f;
        if (lockY) move.y = 0f;

        transform.position += move;
        _lastCamPos = cameraTransform.position;
    }
}
