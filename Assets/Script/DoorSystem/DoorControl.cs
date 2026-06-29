using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;

namespace DoorSystem
{
    public class DoorControl : MonoBehaviour
    {
        private static readonly int CloseHash = Animator.StringToHash("Close");
        private static readonly int OpenHash = Animator.StringToHash("Open");
        private static readonly int DoorOpenStateHash = Animator.StringToHash("Door_Open");
        private static readonly int DoorCloseStateHash = Animator.StringToHash("Door_Close");

        #region Serialized Fields

        [SerializeField] private Animator animator;
        [SerializeField] private DoorState doorState = DoorState.Closed;

        #endregion

        #region Events

        [Header("Open Events")]
        [Tooltip("Invoked when the door begins opening.")]
        public UnityEvent OnOpenStart;

        [Tooltip("Invoked when the door finishes opening.")]
        public UnityEvent OnOpenEnd;

        [Header("Close Events")]
        [Tooltip("Invoked when the door begins closing.")]
        public UnityEvent OnCloseStart;

        [Tooltip("Invoked when the door finishes closing.")]
        public UnityEvent OnCloseEnd;

        #endregion

        #region Types

        public enum DoorState
        {
            Closed = 0b00001,
            Opened = 0b00010,
            Closing = 0b00100,
            Opening = 0b01000,
            Locked = 0b10000
        }

        #endregion

        #region Properties

        public DoorState State => doorState;

        #endregion

        #region Runtime Fields

        private Coroutine waitStateRoutine;

        #endregion

        #region Unity Methods

        private void Start()
        {
            DoorManager.Instance.RegisterDoor(this);
        }

        private void OnDestroy()
        {
            DoorManager.Instance?.UnregisterDoor(this);
        }

        #endregion

        #region Public Methods

        [ContextMenu("Close DoorControl")]
        public void Close()
        {
            if (!IsClosable()) return;

            if (waitStateRoutine != null)
            {
                StopCoroutine(waitStateRoutine);
                waitStateRoutine = null;
            }

            doorState = DoorState.Closing;
            animator.SetTrigger(CloseHash);
            OnCloseStart?.Invoke();

            waitStateRoutine = StartCoroutine(WaitForMotionEnd(DoorCloseStateHash, DoorState.Closed, OnCloseEnd));
        }

        [ContextMenu("Open DoorControl")]
        public void Open()
        {
            if (!IsOpenable()) return;

            if (waitStateRoutine != null)
            {
                StopCoroutine(waitStateRoutine);
                waitStateRoutine = null;
            }

            doorState = DoorState.Opening;
            animator.SetTrigger(OpenHash);
            OnOpenStart?.Invoke();

            waitStateRoutine = StartCoroutine(WaitForMotionEnd(DoorOpenStateHash, DoorState.Opened, OnOpenEnd));
        }

        [ContextMenu("Lock DoorControl")]
        public void Lock()
        {
            if (!IsLockable()) return;
            doorState = DoorState.Locked;
            Debug.Log($"[{name}|{transform.position}] is now locked");
        }

        [ContextMenu("Unlock DoorControl")]
        public void Unlock()
        {
            if (!IsUnlockable()) return;
            doorState = DoorState.Closed;
            Debug.Log($"[{name}|{transform.position}] is now unlocked");
        }

        public void ToggleOpen()
        {
            if (doorState == DoorState.Closed)
            {
                Open();
            }
            else if (doorState == DoorState.Opened)
            {
                Close();
            }
        }

        public void ToggleLock()
        {
            if (doorState == DoorState.Locked)
            {
                Unlock();
            }
            else if (doorState == DoorState.Closed)
            {
                Lock();
            }
        }

        public bool CanToggleOpen()
        {
            return doorState == DoorState.Closed || doorState == DoorState.Opened;
        }

        public bool CanToggleLock()
        {
            return doorState == DoorState.Closed || doorState == DoorState.Locked;
        }

        #endregion

        #region State Checkers

        private bool IsClosable()
        {
            // TODO: check if anything is in the way of the door before closing it

            if (doorState == DoorState.Opened) return true;
            return false;
        }

        private bool IsOpenable()
        {
            if (doorState == DoorState.Closed)
            {
                return true;
            }
            return false;
        }

        private bool IsLockable()
        {
            if (doorState == DoorState.Closed)
            {
                return true;
            }

            return false;
        }

        private bool IsUnlockable()
        {
            if (doorState == DoorState.Locked)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region Private Methods

        IEnumerator WaitForMotionEnd(int motionStateHash, DoorState finalState, UnityEvent endEvent)
        {
            yield return null;

            yield return new WaitUntil(() =>
                !animator.IsInTransition(0)
                && animator.GetCurrentAnimatorStateInfo(0).shortNameHash == motionStateHash);

            yield return new WaitUntil(() =>
            {
                if (animator.IsInTransition(0))
                {
                    return true;
                }

                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.shortNameHash == motionStateHash)
                {
                    return info.normalizedTime >= 1f;
                }

                return true;
            });

            while (animator.IsInTransition(0))
            {
                yield return null;
            }

            doorState = finalState;
            RefreshActiveNavMeshSurfaces();

            endEvent?.Invoke();
            waitStateRoutine = null;
        }

        private static void RefreshActiveNavMeshSurfaces()
        {
            IgnoreUnreadableNavMeshSources();

            for (int i = 0; i < NavMeshSurface.activeSurfaces.Count; i++)
            {
                NavMeshSurface surface = NavMeshSurface.activeSurfaces[i];
                if (surface.navMeshData == null) continue;

                surface.UpdateNavMesh(surface.navMeshData);
            }
        }

        private static void IgnoreUnreadableNavMeshSources()
        {
            MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                IgnoreUnreadableNavMeshSource(meshFilters[i].gameObject, meshFilters[i].sharedMesh);
            }

            MeshCollider[] meshColliders = FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < meshColliders.Length; i++)
            {
                IgnoreUnreadableNavMeshSource(meshColliders[i].gameObject, meshColliders[i].sharedMesh);
            }
        }

        private static void IgnoreUnreadableNavMeshSource(GameObject source, Mesh mesh)
        {
            if (mesh == null || mesh.isReadable) return;

            NavMeshModifier modifier = source.GetComponent<NavMeshModifier>();
            if (modifier == null)
            {
                modifier = source.AddComponent<NavMeshModifier>();
            }

            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = false;
        }

        #endregion
    }
}
