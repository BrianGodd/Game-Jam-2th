using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DoorSystem
{
    public class DoorManager : MonoBehaviour
    {
        #region Singleton

        private static DoorManager instance;
        public static DoorManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DoorManager>();
                }
                return instance;
            }
        }

        #endregion

        #region Serialized Fields

        [SerializeField] private List<DoorControl> doors = new();

        #endregion

        #region Properties

        public List<DoorControl> Doors => doors;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void Start()
        {
            InitializeDoorList();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                instance = null;
            }
        }

        #endregion

        #region Public Methods

        public void RegisterDoor(DoorControl door)
        {
            if (door != null && !doors.Contains(door))
            {
                doors.Add(door);
            }
        }

        public void UnregisterDoor(DoorControl door)
        {
            if (door != null)
            {
                doors.Remove(door);
            }
        }

        // Check if any door has the specified state
        // This method uses bitwise AND to check if the door's state matches the specified state
        // YOu can use a bitmask to check for multiple states at once
        public bool HasDoorWithState(DoorControl.DoorState state)
        {
            return doors.Any(door => door != null && (door.State & state) != 0);
        }

        #endregion

        #region Private Methods

        private void InitializeDoorList()
        {
            List<DoorControl> orderedDoors = new(doors.Count);
            for (int i = 0; i < doors.Count; i++)
            {
                DoorControl door = doors[i];
                if (door != null && !orderedDoors.Contains(door))
                {
                    orderedDoors.Add(door);
                }
            }

            doors.Clear();
            for (int i = 0; i < orderedDoors.Count; i++)
            {
                RegisterDoor(orderedDoors[i]);
            }
        }

        #endregion
    }
}
