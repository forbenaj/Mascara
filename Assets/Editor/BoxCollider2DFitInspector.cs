using UnityEditor;
using UnityEngine;

// Adds a button to BoxCollider2D inspector to fit it to the attached SpriteRenderer.
[CustomEditor(typeof(BoxCollider2D))]
public class BoxCollider2DFitInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var box = (BoxCollider2D)target;
        var sr = box.GetComponent<SpriteRenderer>();

        if (sr != null && sr.sprite != null)
        {
            if (GUILayout.Button("Fit Collider To Sprite"))
            {
                Fit(box, sr);
            }
        }
    }

    private void Fit(BoxCollider2D box, SpriteRenderer sr)
    {
        var sprite = sr.sprite;
        var sizeWorld = sprite.bounds.size;
        var centerWorld = sprite.bounds.center;

        var lossy = box.transform.lossyScale;
        float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : Mathf.Abs(lossy.x);
        float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : Mathf.Abs(lossy.y);

        box.size = new Vector2(sizeWorld.x / sx, sizeWorld.y / sy);
        box.offset = new Vector2(centerWorld.x / sx, centerWorld.y / sy);

        EditorUtility.SetDirty(box);
    }
}
