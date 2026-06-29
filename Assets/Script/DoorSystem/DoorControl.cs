using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Horror;

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
        private NavMeshObstacle[] navMeshObstacles;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            navMeshObstacles = GetComponentsInChildren<NavMeshObstacle>(true);
            if (navMeshObstacles.Length == 0)
            {
                throw new MissingComponentException($"{name} requires a NavMeshObstacle in its hierarchy.");
            }

            for (int i = 0; i < navMeshObstacles.Length; i++)
            {
                navMeshObstacles[i].carving = true;
                navMeshObstacles[i].carveOnlyStationary = false;
            }

            ApplyNavMeshObstacleState();
        }

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
            ApplyNavMeshObstacleState();
            animator.SetTrigger(CloseHash);
            MonsterPresenceDirector.Instance.AddDoorThreat();
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
            ApplyNavMeshObstacleState();
            MonsterPresenceDirector.Instance.ClearBlockedChasePosition();
            animator.SetTrigger(OpenHash);
            OnOpenStart?.Invoke();

            waitStateRoutine = StartCoroutine(WaitForMotionEnd(DoorOpenStateHash, DoorState.Opened, OnOpenEnd));
        }

        [ContextMenu("Lock DoorControl")]
        public void Lock()
        {
            if (!IsLockable()) return;
            doorState = DoorState.Locked;
            ApplyNavMeshObstacleState();
            Debug.Log($"[{name}|{transform.position}] is now locked");
        }

        [ContextMenu("Unlock DoorControl")]
        public void Unlock()
        {
            if (!IsUnlockable()) return;
            doorState = DoorState.Closed;
            ApplyNavMeshObstacleState();
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
            ApplyNavMeshObstacleState();

            endEvent?.Invoke();
            waitStateRoutine = null;
        }

        private void ApplyNavMeshObstacleState()
        {
            bool blocksNavMesh = doorState == DoorState.Closed
                || doorState == DoorState.Closing
                || doorState == DoorState.Locked;

            for (int i = 0; i < navMeshObstacles.Length; i++)
            {
                navMeshObstacles[i].enabled = blocksNavMesh;
            }
        }

        #endregion
    }
}
