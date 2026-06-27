using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Surveillance
{
    public class SurveillanceFeedPanel : MonoBehaviour
    {
        [SerializeField] private RawImage feedImage;
        [SerializeField] private TMP_Text cameraLabel;

        private void OnEnable()
        {
            var manager = SurveillanceManager.Instance;
            if (manager == null)
            {
                Debug.LogError("SurveillanceManager instance is not available. Please ensure that a SurveillanceManager is present in the scene.");
            }
            SurveillanceManager.Instance.ActiveCameraChanged += OnActiveCameraChanged;
            feedImage.texture = SurveillanceManager.Instance.FeedTexture;
            RefreshDisplay(SurveillanceManager.Instance.CurrentCamera);
        }

        private void OnDisable()
        {
            if(SurveillanceManager.Instance != null)
            {
                SurveillanceManager.Instance.ActiveCameraChanged -= OnActiveCameraChanged;
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
            if (cameraLabel != null)
            {
                cameraLabel.text = camera != null ? camera.CameraLabel : "No Signal";
            }
        }
    }
}
