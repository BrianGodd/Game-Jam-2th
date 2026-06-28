using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component that exposes UnityEvents for raycast-based interaction.
/// Other components (for example an interactor) can call the public methods when the object is looked at or interacted with.
/// </summary>
[AddComponentMenu("Interaction/Interactable")]
public class Interactable : MonoBehaviour
{
    [Header("Raycast Events")]
    [Tooltip("Invoked when an interactor starts looking at this object (raycast hit).")]
    public UnityEvent OnRaycastEnter;

    [Tooltip("Invoked when an interactor stops looking at this object (raycast no longer hits).")]
    public UnityEvent OnRaycastExit;

    [Header("Interact Events")]
    [Tooltip("Invoked on a quick interact press while this object is targeted.")]
    public UnityEvent OnInteract;

    [Tooltip("Invoked after the interact input has been held for the interactor's hold duration.")]
    public UnityEvent OnHoldInteract;

    public bool debug = false;

    public void RaycastEnter()
    {
        if (debug)
            Debug.Log($"[{gameObject.name}|{name}]: RaycastEnter called on " + gameObject.name);

        OnRaycastEnter?.Invoke();
    }

    public void RaycastExit()
    {
        if (debug)
            Debug.Log($"[{gameObject.name}|{name}]: RaycastExit called on " + gameObject.name);

        OnRaycastExit?.Invoke();
    }

    public void Interact()
    {
        if (debug)
            Debug.Log($"[{gameObject.name}|{name}]: Interact called on " + gameObject.name);

        OnInteract?.Invoke();
    }

    public void HoldInteract()
    {
        if (debug)
            Debug.Log($"[{gameObject.name}|{name}]: HoldInteract called on " + gameObject.name);

        OnHoldInteract?.Invoke();
    }
}
