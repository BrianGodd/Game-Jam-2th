using System.Globalization;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Continuously casts a ray from a camera (or specified origin) to detect Interactable components.
/// The component raises enter/exit events on targeted Interactable objects and triggers their interact events from input.
/// Quick press triggers Interactable.OnInteract. When Hold Interact Duration is greater than zero,
/// holding for that duration triggers OnHoldInteract; releasing sooner triggers OnInteract instead.
/// Requires the new Input System; this component will log an error and disable itself when the project is not using it.
/// When an InputActionReference is not assigned, this component will attempt to find an action named "Interact" on a PlayerInput component on the same GameObject.
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

    [Tooltip("Seconds the interact input must be held before triggering OnHoldInteract. 0 uses quick press only.")]
    [SerializeField] private float holdInteractDuration = 2 ;
#endif

    private Interactable current;

#if ENABLE_INPUT_SYSTEM
    private InputAction runtimeAction;
    private bool isInteractHeld;
    private float holdElapsed;
    private Interactable holdTarget;
    private bool holdTriggered;
#endif

    private void OnEnable()
    {
#if !ENABLE_INPUT_SYSTEM
        Debug.LogError("RaycastInteractor requires the Unity Input System (package) and ENABLE_INPUT_SYSTEM scripting define. Enable the new Input System in Project Settings and add the scripting define.");
        enabled = false;
        return;
#else
        if (interactAction != null && interactAction.action != null)
        {
            runtimeAction = interactAction.action;
        }
        else if (TryGetComponent<PlayerInput>(out var playerInput) && playerInput.actions != null)
        {
            runtimeAction = playerInput.actions.FindAction("Interact", false);
        }

        if (runtimeAction != null)
        {
            runtimeAction.started += OnInteractStarted;
            runtimeAction.canceled += OnInteractCanceled;
            runtimeAction.performed += OnInteractPerformed;
            if (!runtimeAction.enabled)
            {
                runtimeAction.Enable();
            }
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
            runtimeAction.started -= OnInteractStarted;
            runtimeAction.canceled -= OnInteractCanceled;
            runtimeAction.performed -= OnInteractPerformed;
            runtimeAction = null;
        }

        CancelHoldInteract();
#endif
    }

    private void Update()
    {
        PerformRaycast();
#if ENABLE_INPUT_SYSTEM
        UpdateHoldInteract();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void OnInteractStarted(InputAction.CallbackContext ctx)
    {
        if (current == null || holdInteractDuration <= 0f)
        {
            return;
        }

        isInteractHeld = true;
        holdElapsed = 0f;
        holdTarget = current;
        holdTriggered = false;
    }

    private void OnInteractCanceled(InputAction.CallbackContext ctx)
    {
        if (isInteractHeld && !holdTriggered && holdTarget != null && holdInteractDuration > 0f)
        {
            holdTarget.Interact();
        }

        CancelHoldInteract();
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (current == null || holdInteractDuration > 0f)
        {
            return;
        }

        current.Interact();
    }

    private void UpdateHoldInteract()
    {
        if (!isInteractHeld || holdTarget == null || holdInteractDuration <= 0f)
        {
            return;
        }

        if (current != holdTarget)
        {
            CancelHoldInteract();
            return;
        }

        holdElapsed += Time.deltaTime;
        if (!holdTriggered && holdElapsed >= holdInteractDuration)
        {
            holdTriggered = true;
            holdTarget.HoldInteract();
        }
    }

    private void CancelHoldInteract()
    {
        isInteractHeld = false;
        holdElapsed = 0f;
        holdTarget = null;
        holdTriggered = false;
    }
#endif

    private void PerformRaycast()
    {
        Camera cam = originCamera != null ? originCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, interactLayers, QueryTriggerInteraction.Ignore)
            && hit.collider != null)
        {
            if(!hit.collider.TryGetComponent<Interactable>(out var found))
            {
                found = hit.collider.GetComponentInParent<Interactable>();
            }

            if(found != null)
            {
                if (found != current)
                {
                    if (current != null)
                    {
                        current.RaycastExit();
                    }
                    current = found;
                    current.RaycastEnter();
                }
                return;
            }
        }

        if (current != null)
        {
            current.RaycastExit();
            current = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Camera cam = originCamera != null ? originCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * maxDistance);
    }
}
