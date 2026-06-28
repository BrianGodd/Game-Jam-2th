using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Surveillance
{
    public class SurveillanceFeedPanel : MonoBehaviour
    {
        [SerializeField] private RawImage feedImage;
        [SerializeField] private TMP_Text cameraLabel;
        private SurveillanceCamera displayedCamera;
        private string lastTimestampText;

#if ENABLE_INPUT_SYSTEM
        [SerializeField] private InputActionReference shiftLeftAction;
        [SerializeField] private InputActionReference shiftRightAction;
        private InputAction leftRuntimeAction;
        private InputAction rightRuntimeAction;
#endif

        private void OnEnable()
        {
            SurveillanceManager manager = SurveillanceManager.Instance;
            manager.ActiveCameraChanged += OnActiveCameraChanged;
            feedImage.texture = manager.FeedTexture;
            RefreshDisplay(manager.CurrentCamera);

#if ENABLE_INPUT_SYSTEM
            BindShiftAction(ref leftRuntimeAction, shiftLeftAction, "SurveillanceLeft", OnShiftLeftPerformed);
            BindShiftAction(ref rightRuntimeAction, shiftRightAction, "SurveillanceRight", OnShiftRightPerformed);
#endif
        }

        private void OnDisable()
        {
            if (SurveillanceManager.Instance != null)
            {
                SurveillanceManager.Instance.ActiveCameraChanged -= OnActiveCameraChanged;
            }

#if ENABLE_INPUT_SYSTEM
            UnbindShiftAction(ref leftRuntimeAction, OnShiftLeftPerformed);
            UnbindShiftAction(ref rightRuntimeAction, OnShiftRightPerformed);
#endif
        }

        private void Update()
        {
            RefreshCameraLabel();

            if(Input.GetKeyDown(KeyCode.Q))
            {
                transform.root.gameObject.SetActive(false);
            }
        }

        [ContextMenu("Shift Left")]
        public void ShiftLeft()
        {
            SurveillanceManager.Instance.SelectPreviousCamera();
        }

        [ContextMenu("Shift Right")]
        public void ShiftRight()
        {
            SurveillanceManager.Instance.SelectNextCamera();
        }

        private void OnActiveCameraChanged(SurveillanceCamera camera)
        {
            RefreshDisplay(camera);
        }

        private void RefreshDisplay(SurveillanceCamera camera)
        {
            displayedCamera = camera;
            lastTimestampText = null;
            RefreshCameraLabel();
        }

        private void RefreshCameraLabel()
        {
            if (cameraLabel == null)
            {
                return;
            }

            if (displayedCamera == null)
            {
                cameraLabel.text = "No Signal";
                return;
            }

            string timestampText = DateTime.Now.ToString("yyyy/MM/dd  HH:mm:ss");
            if (timestampText == lastTimestampText)
            {
                return;
            }

            int cameraNumber = SurveillanceManager.Instance.CurrentCameraIndex + 1;
            cameraLabel.text = $"{timestampText}\nCAM{cameraNumber} {displayedCamera.CameraLabel}";
            lastTimestampText = timestampText;
        }

#if ENABLE_INPUT_SYSTEM
        private void BindShiftAction(
            ref InputAction runtimeAction,
            InputActionReference actionReference,
            string fallbackActionName,
            System.Action<InputAction.CallbackContext> performedCallback)
        {
            if (actionReference != null && actionReference.action != null)
            {
                runtimeAction = actionReference.action;
            }
            else if (TryGetComponent<PlayerInput>(out var playerInput) && playerInput.actions != null)
            {
                runtimeAction = playerInput.actions.FindAction(fallbackActionName, false);
            }

            if (runtimeAction == null)
            {
                return;
            }

            runtimeAction.performed += performedCallback;
            if (!runtimeAction.enabled)
            {
                runtimeAction.Enable();
            }
        }

        private void UnbindShiftAction(
            ref InputAction runtimeAction,
            System.Action<InputAction.CallbackContext> performedCallback)
        {
            if (runtimeAction == null)
            {
                return;
            }

            runtimeAction.performed -= performedCallback;
            runtimeAction = null;
        }

        private void OnShiftLeftPerformed(InputAction.CallbackContext ctx)
        {
            ShiftLeft();
        }

        private void OnShiftRightPerformed(InputAction.CallbackContext ctx)
        {
            ShiftRight();
        }
#endif
    }
}
