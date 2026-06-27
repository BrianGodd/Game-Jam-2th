using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Continuously casts a ray from a camera (or specified origin) to detect Interactable components.
/// The component raises enter/exit events on targeted Interactable objects and triggers their interact event when the interact input is pressed.
/// Requires the new Input System; this component will log an error and disable itself when the project is not using it.
/// When an InputActionReference is not assigned, this component will attempt to find an action named "Interact" on a PlayerInput component on the same GameObject.
///
/// Style & safety notes:
/// - Prefer TryGetComponent&lt;T&gt; when a component may be missing; use RequireComponent only when a dependency is mandatory.
/// - Avoid leading underscores in field names to follow common Unity conventions and inspector expectations.
/// - This script uses TryGetComponent to detect PlayerInput and interactable targets.
/// </summary>
public class RaycastInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Maximum distance to search for interactable objects.")]
    [SerializeField] private float maxDistance = 3f;

    [Tooltip("Layers that contain interactable objects.")]
    [SerializeField] private LayerMask interactLayers = ~0;

    [Tooltip("Optional camera used as ray origin. If null Camera.main will be used.")]
    [SerializeField] private Camera originCamera;

    [Header("Input")]
#if ENABLE_INPUT_SYSTEM
    [Tooltip("Optional Input Action Reference to an 'Interact' action. If not assigned, the script will try to find a 'Interact' action on a PlayerInput component on the same GameObject.")]
    [SerializeField] private InputActionReference interactAction;
#endif

    // Currently targeted interactable by the raycast.
    private Interactable current;

#if ENABLE_INPUT_SYSTEM
    // The runtime InputAction we subscribe to (from the reference or found on PlayerInput).
    private InputAction runtimeAction;
#endif

    private void OnEnable()
    {
#if !ENABLE_INPUT_SYSTEM
        Debug.LogError("RaycastInteractor requires the Unity Input System (package) and ENABLE_INPUT_SYSTEM scripting define. Enable the new Input System in Project Settings and add the scripting define.");
        enabled = false;
        return;
#else
        // Prefer the serialized InputActionReference when provided.
        if (interactAction != null && interactAction.action != null)
        {
            runtimeAction = interactAction.action;
        }
        else
        {
            // Try to locate a PlayerInput on this GameObject and find an action named "Interact".
            if (TryGetComponent<PlayerInput>(out var playerInput) 
                && playerInput.actions != null)
            {
                runtimeAction = playerInput.actions.FindAction("Interact", false);
            }
        }

        if (runtimeAction != null)
        {
            runtimeAction.performed += OnInteractPerformed;
            if (!runtimeAction.enabled) runtimeAction.Enable();
        }
        else
        {
            Debug.LogWarning("RaycastInteractor: No Interact action found. Assign an InputActionReference or add an 'Interact' action to the PlayerInput.");
        }
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (runtimeAction != null)
        {
            runtimeAction.performed -= OnInteractPerformed;
            runtimeAction = null;
        }
#endif
    }

    private void Update()
    {
        // Constantly perform the raycast to detect enter/exit on Interactable components.
        PerformRaycast();
    }

#if ENABLE_INPUT_SYSTEM
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (current != null) current.Interact();
    }
#endif

    private void PerformRaycast()
    {
        Camera cam = originCamera != null ? originCamera : Camera.main;
        if (cam == null) return;

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, interactLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.TryGetComponent<Interactable>(out Interactable found))
            {
                // If we started targeting a new interactable, notify enter and exit appropriately.
                if (found != current)
                {
                    if (current != null) current.RaycastExit();
                    current = found;
                    current.RaycastEnter();
                }
                return;
            }
        }

        // No valid interactable hit this frame: if we previously had one, notify exit.
        if (current != null)
        {
            current.RaycastExit();
            current = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Camera cam = originCamera != null ? originCamera : Camera.main;
        if (cam == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * maxDistance);
    }
}
