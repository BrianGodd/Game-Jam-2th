using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MissionSystem
{
    public class MissionManager : MonoBehaviour
    {
        static public MissionManager Instance { get; private set; }

        [Header("Runtime")]
        [SerializeField] List<Mission> missions = new();

        public bool HasIncompleteMission => missions.Count > 0;


        [Header("Debug")]
        [SerializeField] bool doBroadcastMissionCount = true;



        // ================================



        #region Unity Methods
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
        #endregion



        #region Public Methods
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
        #endregion



        #region Debug Methods
        IEnumerator RepeatBroadCastMissionCount()
        {
            var wait2Seconds = new WaitForSeconds(2f);
            while (doBroadcastMissionCount)
            {
                
                Debug.Log($"[{name}]Mission Count: {missions.Count}");
                yield return wait2Seconds;
            }
        }

        private void OnValidate()
        {
            if(doBroadcastMissionCount)
            {
                StartCoroutine(RepeatBroadCastMissionCount());
            }
            else
            {
                StopCoroutine(RepeatBroadCastMissionCount());
            }
        }
        #endregion
    }
}
