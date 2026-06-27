using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("Invoked when the interact input is performed while this object is targeted.")]
    public UnityEvent OnInteract;

    /// <summary>
    /// Called by an interactor when a raycast starts hitting this object.
    /// </summary>
    public void RaycastEnter()
    {
        OnRaycastEnter?.Invoke();
    }

    /// <summary>
    /// Called by an interactor when a raycast stops hitting this object.
    /// </summary>
    public void RaycastExit()
    {
        OnRaycastExit?.Invoke();
    }

    /// <summary>
    /// Called by an interactor when the interact input is triggered while this object is targeted.
    /// </summary>
    public void Interact()
    {
        OnInteract?.Invoke();
    }
}
