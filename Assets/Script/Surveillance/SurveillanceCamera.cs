using UnityEngine;

namespace Surveillance
{
    [RequireComponent(typeof(Camera))]
    public class SurveillanceCamera : MonoBehaviour
    {
        [SerializeField] private string cameraLabel;
        private new Camera camera;

        public string CameraLabel
        {
            get { return string.IsNullOrEmpty(cameraLabel) ? gameObject.name : cameraLabel; }
        }

        public Camera UnityCamera
        {
            get { return camera; }
        }

        private void Awake()
        {
            camera = GetComponent<Camera>();
            camera.enabled = false;
            camera.targetTexture = null;
        }

        private void Start()
        {
            SurveillanceManager.Instance.RegisterCamera(this);
        }

        private void OnDestroy()
        {
            SurveillanceManager.Instance?.UnregisterCamera(this);
        }

        public void SetActiveState(bool isActive, RenderTexture targetTexture)
        {
            if (camera == null)
            {
                camera = GetComponent<Camera>();
            }

            camera.targetTexture = targetTexture;
            camera.enabled = isActive;
        }
    }
}
