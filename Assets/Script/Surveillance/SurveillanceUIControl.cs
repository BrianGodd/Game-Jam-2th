using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SurveillanceUIControl : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionReference closeTabAction;
    private InputAction runtimeAction;
#endif

    private void OnEnable()
    {
#if !ENABLE_INPUT_SYSTEM
        Debug.LogError("SurveillanceUIControl requires the Unity Input System and ENABLE_INPUT_SYSTEM scripting define.");
        enabled = false;
        return;
#else
        if (closeTabAction != null && closeTabAction.action != null)
        {
            runtimeAction = closeTabAction.action;
        }
        else if (TryGetComponent<PlayerInput>(out var playerInput) && playerInput.actions != null)
        {
            runtimeAction = playerInput.actions.FindAction("CloseTab", false);
        }

        if (runtimeAction != null)
        {
            runtimeAction.performed += OnCloseTabPerformed;
            if (!runtimeAction.enabled)
            {
                runtimeAction.Enable();
            }
        }
        else
        {
            Debug.LogWarning("SurveillanceUIControl: No CloseTab action found. Assign an InputActionReference or add a CloseTab action to PlayerInput.");
        }
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (runtimeAction != null)
        {
            runtimeAction.performed -= OnCloseTabPerformed;
            runtimeAction = null;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void OnCloseTabPerformed(InputAction.CallbackContext ctx)
    {
        gameObject.SetActive(false);
    }
#endif
}
