using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLabel : MonoBehaviour
{
    public string label = "Label";
    public Color color = Color.yellow;
    public Vector3 offset = Vector3.up;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = color;
        Handles.Label(transform.position + offset, label);
    }
#endif
}