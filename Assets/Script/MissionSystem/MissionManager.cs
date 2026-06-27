using System.Collections.Generic;
using UnityEngine;

namespace MissionSystem
{
    public class MissionManager : MonoBehaviour
    {
        static public MissionManager Instance { get; private set; }

        [SerializeField] List<Mission> missions = new();
        public bool HasIncompleteMission => missions.Count > 0;


        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Multiple instances of MissionManager detected. Destroying Self");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddMission(Mission mission)
        {
            if (mission == null) return;
            if (missions.Contains(mission)) return;

            missions.Add(mission);
            mission.OnComplete += RemoveMission;
        }

        private void RemoveMission(Mission mission)
        {
            if (mission == null) return;

            missions.Remove(mission);
            mission.OnComplete -= RemoveMission;
        }
    }
}
