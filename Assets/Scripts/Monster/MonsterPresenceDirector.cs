using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    public class MonsterPresenceDirector : MonoBehaviour
    {
        public GameObject monsterObject;
        public Camera playerCamera;
        public Transform[] stalkPoints;
        public Transform[] hidePoints;
        public float firstDelay = 8f;
        public float minInterval = 12f;
        public float maxInterval = 25f;
        public float stayDuration = 5f;
        public float minStalkDistance = 4f;
        public float maxStalkDistance = 18f;
        public float moveSpeed = 2f;
        public bool vanishWhenLookedAt = true;

        [Header("Threat Settings")]
        public float threat = 0f;
        public float threatThreshold = 50f;
        public float threatIncreaseRate = 2f;
        [Range(0f, 1f)]
        public float chaseChance = 0.5f;

        [Header("Chase Settings")]
        public float initialChaseSpeed = 8f;
        public float finalChaseSpeed = 1.5f;
        public float speedDecayDuration = 5f;
        public float attackDistance = 1.5f;
        public float loseDistance = 20f;
        public float chaseGracePeriod = 3f;

        [Header("Escape Settings")]
        public float escapeDistance = 8f;
        public float escapeDuration = 3f;

        private enum MonsterState
        {
            Hidden,
            MovingToStalk,
            Stalking,
            MovingToHide,
            Chasing,
            BackingAway
        }

        private MonsterState state;
        private float nextSightingTime;
        private float hideTime;
        private float chaseStartTime;
        private float escapeEndTime;
        private Transform targetPoint;
        private int currentPairIndex = -1;
        private NavMeshAgent agent;

        private void Awake()
        {
            if (stalkPoints == null || stalkPoints.Length == 0 || hidePoints == null || hidePoints.Length == 0)
            {
                throw new System.InvalidOperationException("Stalk points or Hide points are not configured.");
            }

            agent = monsterObject.GetComponent<NavMeshAgent>();
            agent.updateRotation = false; // Disable auto-rotation so FacePlayer can control rotation

            currentPairIndex = Random.Range(0, Mathf.Min(stalkPoints.Length, hidePoints.Length));
            targetPoint = hidePoints[currentPairIndex];
            monsterObject.transform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation);
            monsterObject.SetActive(false);
            state = MonsterState.Hidden;
            nextSightingTime = Time.time + firstDelay;
        }

        private void Update()
        {
            // Threat continuously increases
            threat += Time.deltaTime * threatIncreaseRate;

            if (state == MonsterState.Hidden)
            {
                if (Time.time >= nextSightingTime)
                {
                    StartStalking();
                }
                return;
            }

            // Continuously face the player when not hidden
            FacePlayer();

            // If threat is above threshold, and we are stalking or moving to stalk,
            // we have a chance to trigger a chase dynamically
            if (threat >= threatThreshold && (state == MonsterState.MovingToStalk || state == MonsterState.Stalking))
            {
                if (Random.value < chaseChance * Time.deltaTime)
                {
                    StartChasing();
                }
            }

            // If player looks at the monster, immediately retreat (move back to HidePoint)
            if (ShouldVanishNow())
            {
                if (state == MonsterState.MovingToStalk || state == MonsterState.Stalking)
                {
                    Retreat();
                }
            }

            if (state == MonsterState.MovingToStalk)
            {
                if (HasReachedTarget())
                {
                    state = MonsterState.Stalking;
                    hideTime = Time.time + stayDuration;
                    agent.ResetPath();
                }
                return;
            }

            if (state == MonsterState.Stalking)
            {
                if (Time.time >= hideTime)
                {
                    Retreat();
                }
                return;
            }

            if (state == MonsterState.MovingToHide)
            {
                if (HasReachedTarget())
                {
                    monsterObject.SetActive(false);
                    state = MonsterState.Hidden;
                    ScheduleNextSighting();
                }
                return;
            }

            if (state == MonsterState.Chasing)
            {
                // Calculate speed decay over time
                float elapsed = Time.time - chaseStartTime;
                float t = Mathf.Clamp01(elapsed / speedDecayDuration);
                agent.speed = Mathf.Lerp(initialChaseSpeed, finalChaseSpeed, t);

                agent.destination = playerCamera.transform.position;

                float distance = Vector3.Distance(monsterObject.transform.position, playerCamera.transform.position);
                if (distance <= attackDistance)
                {
                    Debug.LogWarning("Monster caught the player!");
                    threat = 0f;
                    Retreat();
                }
                else if (elapsed >= chaseGracePeriod && distance >= loseDistance)
                {
                    Debug.Log("Player outran the monster. Backing away.");
                    threat = 0f;
                    StartBackingAway();
                }
                return;
            }

            if (state == MonsterState.BackingAway)
            {
                if (HasReachedTarget() || Time.time >= escapeEndTime)
                {
                    monsterObject.SetActive(false);
                    state = MonsterState.Hidden;
                    ScheduleNextSighting();
                }
                return;
            }
        }

        private void StartStalking()
        {
            currentPairIndex = PickValidPairIndex();

            Transform hidePoint = hidePoints[currentPairIndex];
            monsterObject.SetActive(true);
            agent.Warp(hidePoint.position);

            if (threat >= threatThreshold && Random.value < chaseChance)
            {
                StartChasing();
            }
            else
            {
                targetPoint = stalkPoints[currentPairIndex];
                agent.speed = moveSpeed;
                agent.destination = targetPoint.position;
                state = MonsterState.MovingToStalk;
            }
        }

        private void StartChasing()
        {
            state = MonsterState.Chasing;
            chaseStartTime = Time.time;
            monsterObject.SetActive(true);
            agent.speed = initialChaseSpeed;
            agent.destination = playerCamera.transform.position;
        }

        private void StartBackingAway()
        {
            state = MonsterState.BackingAway;
            escapeEndTime = Time.time + escapeDuration;

            // Calculate direction away from the player
            Vector3 awayDirection = (monsterObject.transform.position - playerCamera.transform.position).normalized;
            awayDirection.y = 0f;
            awayDirection = awayDirection.normalized;

            Vector3 destination = monsterObject.transform.position + awayDirection * escapeDistance;

            agent.speed = moveSpeed;
            agent.destination = destination;
        }

        private int PickValidPairIndex()
        {
            int numPoints = Mathf.Min(stalkPoints.Length, hidePoints.Length);
            int startIndex = Random.Range(0, numPoints);
            for (int i = 0; i < numPoints; i++)
            {
                int index = (startIndex + i) % numPoints;
                Transform point = stalkPoints[index];
                float distance = Vector3.Distance(playerCamera.transform.position, point.position);
                if (distance >= minStalkDistance && distance <= maxStalkDistance && !IsInView(point.position))
                {
                    return index;
                }
            }

            throw new System.InvalidOperationException("No stalk point is outside the camera view and within the configured distance range.");
        }

        private void ScheduleNextSighting()
        {
            nextSightingTime = Time.time + Random.Range(minInterval, maxInterval);
        }

        private bool ShouldVanishNow()
        {
            return vanishWhenLookedAt && IsInView(monsterObject.transform.position);
        }

        private bool IsInView(Vector3 position)
        {
            Vector3 viewportPoint = playerCamera.WorldToViewportPoint(position);
            return viewportPoint.z > 0f
                && viewportPoint.x >= 0f
                && viewportPoint.x <= 1f
                && viewportPoint.y >= 0f
                && viewportPoint.y <= 1f;
        }

        private void FacePlayer()
        {
            Vector3 targetPosition = playerCamera.transform.position;
            targetPosition.y = monsterObject.transform.position.y;
            monsterObject.transform.LookAt(targetPosition);
        }

        private void Retreat()
        {
            targetPoint = hidePoints[currentPairIndex];
            agent.speed = moveSpeed;
            agent.destination = targetPoint.position;
            state = MonsterState.MovingToHide;
        }

        private bool HasReachedTarget()
        {
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
        }
    }
}
