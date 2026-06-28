using System.Collections;
using UnityEngine;

namespace DoorSystem
{
    public class DoorControl : MonoBehaviour
    {
        private static readonly int CloseHash = Animator.StringToHash("Close");
        private static readonly int OpenHash = Animator.StringToHash("Open");

        #region Serialized Fields

        [SerializeField] private Collider doorCollider;
        [SerializeField] private Animator animator;
        [SerializeField] private DoorState doorState = DoorState.Closed;

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

        Coroutine waitStateRoutine;

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

            doorState = DoorState.Closing;
            animator.SetTrigger(CloseHash);

            waitStateRoutine = StartCoroutine(WaitForState(DoorState.Closed, "Closed"));
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

            doorCollider.enabled = false;

            waitStateRoutine = StartCoroutine(WaitForState(DoorState.Opened, "Opened"));
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

        IEnumerator WaitForState(DoorState state, string stateName)
        {
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName(stateName));
            doorState = state;

            if (doorState == DoorState.Closed)
            {
                doorCollider.enabled = true;
            }
        }

        #endregion
    }
}
