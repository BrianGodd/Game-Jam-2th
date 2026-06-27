using System;
using System.Collections.Generic;
using UnityEngine;

namespace Surveillance
{
    public class SurveillanceManager : MonoBehaviour
    {
        private static SurveillanceManager m_Instance;
        public static SurveillanceManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = FindObjectOfType<SurveillanceManager>();
                }
                return m_Instance;
            }
            set
            {
                m_Instance = value;
            }
        }

        [SerializeField] private int renderTextureWidth = 1280;
        [SerializeField] private int renderTextureHeight = 720;
        [SerializeField] private int renderTextureDepth = 24;
        [SerializeField] private bool autoSelectFirstCamera = true;

        [SerializeField] private List<SurveillanceCamera> cameras = new();
        private RenderTexture feedTexture;
        private SurveillanceCamera currentCamera;

        public event Action<SurveillanceCamera> ActiveCameraChanged;

        public RenderTexture FeedTexture
        {
            get { return feedTexture; }
        }

        public SurveillanceCamera CurrentCamera
        {
            get { return currentCamera; }
        }

        public int CameraCount
        {
            get { return cameras.Count; }
        }

        public int CurrentCameraIndex
        {
            get { return GetCurrentIndex(); }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CreateFeedTexture();
        }

        private void Start()
        {
            InitializeCameraList();
            if (!autoSelectFirstCamera)
            {
                ApplyActiveCamera(null);
            }
            else if (cameras.Count > 0)
            {
                SelectDefaultCamera();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            ReleaseFeedTexture();
        }

        public void RegisterCamera(SurveillanceCamera surveillanceCamera)
        {
            if (surveillanceCamera == null || cameras.Contains(surveillanceCamera))
            {
                return;
            }

            cameras.Add(surveillanceCamera);

            if (currentCamera == null && autoSelectFirstCamera)
            {
                ApplyActiveCamera(surveillanceCamera);
            }
            else
            {
                surveillanceCamera.SetActiveState(false, null);
            }
        }

        public void UnregisterCamera(SurveillanceCamera surveillanceCamera)
        {
            if (surveillanceCamera == null)
            {
                return;
            }

            int index = cameras.IndexOf(surveillanceCamera);
            if (index < 0)
            {
                return;
            }

            cameras.RemoveAt(index);
            surveillanceCamera.SetActiveState(false, null);

            if (currentCamera == surveillanceCamera)
            {
                currentCamera = null;
                SelectDefaultCamera();
            }
        }

        public void SelectNextCamera()
        {
            if (cameras.Count == 0)
            {
                ApplyActiveCamera(null);
                return;
            }

            int index = GetCurrentIndex();
            if (index < 0)
            {
                ApplyActiveCamera(cameras[0]);
                return;
            }

            index = (index + 1) % cameras.Count;
            ApplyActiveCamera(cameras[index]);
        }

        public void SelectPreviousCamera()
        {
            if (cameras.Count == 0)
            {
                ApplyActiveCamera(null);
                return;
            }

            int index = GetCurrentIndex();
            if (index < 0)
            {
                ApplyActiveCamera(cameras[0]);
                return;
            }

            index = (index - 1 + cameras.Count) % cameras.Count;
            ApplyActiveCamera(cameras[index]);
        }

        public void SelectCamera(int index)
        {
            if (index < 0 || index >= cameras.Count)
            {
                return;
            }

            ApplyActiveCamera(cameras[index]);
        }

        private void InitializeCameraList()
        {
            List<SurveillanceCamera> orderedCameras = new(cameras.Count);
            for (int i = 0; i < cameras.Count; i++)
            {
                SurveillanceCamera surveillanceCamera = cameras[i];
                if (surveillanceCamera != null && !orderedCameras.Contains(surveillanceCamera))
                {
                    orderedCameras.Add(surveillanceCamera);
                }
            }

            cameras.Clear();
            for (int i = 0; i < orderedCameras.Count; i++)
            {
                RegisterCamera(orderedCameras[i]);
            }
        }

        private void SelectDefaultCamera()
        {
            if (cameras.Count == 0)
            {
                ApplyActiveCamera(null);
                return;
            }

            ApplyActiveCamera(cameras[0]);
        }

        private int GetCurrentIndex()
        {
            if (currentCamera == null)
            {
                return -1;
            }

            return cameras.IndexOf(currentCamera);
        }

        private void ApplyActiveCamera(SurveillanceCamera selectedCamera)
        {
            if (currentCamera == selectedCamera)
            {
                SyncCameraStates();
                return;
            }

            currentCamera = selectedCamera;
            SyncCameraStates();
            ActiveCameraChanged?.Invoke(currentCamera);
        }

        private void SyncCameraStates()
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                SurveillanceCamera surveillanceCamera = cameras[i];
                if (surveillanceCamera == null)
                {
                    continue;
                }

                bool isActiveCamera = surveillanceCamera == currentCamera;
                surveillanceCamera.SetActiveState(isActiveCamera, isActiveCamera ? feedTexture : null);
            }
        }

        private void CreateFeedTexture()
        {
            ReleaseFeedTexture();

            feedTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, renderTextureDepth, RenderTextureFormat.ARGB32)
            {
                name = "SurveillanceFeedTexture"
            };
            feedTexture.Create();
        }

        private void ReleaseFeedTexture()
        {
            if (feedTexture == null)
            {
                return;
            }

            feedTexture.Release();
            Destroy(feedTexture);
            feedTexture = null;
        }
    }
}
