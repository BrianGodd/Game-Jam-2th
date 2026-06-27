using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Horror
{
    public class MonsterPresenceDirector : MonoBehaviour
    {
        [Header("References")]
        public GameObject monsterObject;
        public Camera playerCamera;
        public StarterAssets.FirstPersonController playerController;
        public Transform[] stalkPoints;
        public Transform[] hidePoints;

        [Header("Stalking Settings")]
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

        [Header("Jumpscare Settings")]
        public AudioSource jumpscareAudioSource;
        public AudioClip jumpscareClip;
        public float jumpscareDuration = 2.5f;

        private enum MonsterState
        {
            Hidden,
            MovingToStalk,
            Stalking,
            MovingToHide,
            Chasing,
            BackingAway,
            Jumpscare
        }

        private MonsterState state;
        private float nextSightingTime;
        private float hideTime;
        private float chaseStartTime;
        private float escapeEndTime;
        private float jumpscareEndTime;
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
            agent.updateRotation = false; // FacePlayer handles rotation manually

            if (playerController == null)
            {
                playerController = playerCamera.GetComponentInParent<StarterAssets.FirstPersonController>();
                if (playerController == null)
                {
                    Debug.LogError("MonsterPresenceDirector: playerController is not assigned! Disabling script.", this);
                    enabled = false;
                    return;
                }
            }

            currentPairIndex = Random.Range(0, Mathf.Min(stalkPoints.Length, hidePoints.Length));
            targetPoint = hidePoints[currentPairIndex];
            monsterObject.transform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation);
            monsterObject.SetActive(false);
            state = MonsterState.Hidden;
            nextSightingTime = Time.time + firstDelay;
        }

        private void Update()
        {
            threat += Time.deltaTime * threatIncreaseRate;

            if (state == MonsterState.Hidden)
            {
                if (Time.time >= nextSightingTime)
                {
                    StartStalking();
                }
                return;
            }

            FacePlayer();

            switch (state)
            {
                case MonsterState.MovingToStalk:
                case MonsterState.Stalking:
                    // Check dynamic chase trigger
                    if (threat >= threatThreshold && Random.value < chaseChance * Time.deltaTime)
                    {
                        StartChasing();
                        return;
                    }
                    // Check dynamic retreat trigger
                    if (ShouldVanishNow())
                    {
                        Retreat();
                        return;
                    }

                    if (state == MonsterState.MovingToStalk)
                    {
                        if (HasReachedTarget())
                        {
                            state = MonsterState.Stalking;
                            hideTime = Time.time + stayDuration;
                            agent.ResetPath();
                        }
                    }
                    else // Stalking
                    {
                        if (Time.time >= hideTime)
                        {
                            Retreat();
                        }
                    }
                    break;

                case MonsterState.MovingToHide:
                    if (HasReachedTarget())
                    {
                        monsterObject.SetActive(false);
                        state = MonsterState.Hidden;
                        ScheduleNextSighting();
                    }
                    break;

                case MonsterState.Chasing:
                    // Speed decays over time during chase
                    float elapsed = Time.time - chaseStartTime;
                    float t = Mathf.Clamp01(elapsed / speedDecayDuration);
                    agent.speed = Mathf.Lerp(initialChaseSpeed, finalChaseSpeed, t);

                    agent.destination = playerCamera.transform.position;

                    float distance = Vector3.Distance(monsterObject.transform.position, playerCamera.transform.position);
                    if (distance <= attackDistance)
                    {
                        StartJumpscare();
                    }
                    else if (elapsed >= chaseGracePeriod && distance >= loseDistance)
                    {
                        threat = 0f;
                        StartBackingAway();
                    }
                    break;

                case MonsterState.BackingAway:
                    if (HasReachedTarget() || Time.time >= escapeEndTime)
                    {
                        monsterObject.SetActive(false);
                        state = MonsterState.Hidden;
                        ScheduleNextSighting();
                    }
                    break;

                case MonsterState.Jumpscare:
                    playerCamera.transform.LookAt(monsterObject.transform.position + Vector3.up * 1.5f);

                    if (Time.time >= jumpscareEndTime)
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(
                            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                        );
                    }
                    break;
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

            Vector3 awayDirection = (monsterObject.transform.position - playerCamera.transform.position).normalized;
            awayDirection.y = 0f;
            awayDirection = awayDirection.normalized;

            Vector3 destination = monsterObject.transform.position + awayDirection * escapeDistance;

            agent.speed = moveSpeed;
            agent.destination = destination;
        }

        private void StartJumpscare()
        {
            state = MonsterState.Jumpscare;
            threat = 0f;
            jumpscareEndTime = Time.time + jumpscareDuration;

            // Freeze player inputs and movement
            playerController.enabled = false;

            var playerInput = playerController.GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;

            var inputs = playerController.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (inputs != null)
            {
                inputs.move = Vector2.zero;
                inputs.look = Vector2.zero;
                inputs.enabled = false;
            }

            // Freeze monster NavMeshAgent
            if (agent != null)
            {
                agent.ResetPath();
                agent.enabled = false;
            }

            // Position monster in front of camera
            Vector3 facePos = playerCamera.transform.position + playerCamera.transform.forward * 1.0f;
            facePos.y = playerCamera.transform.position.y - 0.5f;
            monsterObject.transform.position = facePos;
            monsterObject.transform.LookAt(playerCamera.transform.position);

            playerCamera.transform.LookAt(monsterObject.transform.position + Vector3.up * 1.5f);

            if (jumpscareAudioSource != null && jumpscareClip != null)
            {
                jumpscareAudioSource.PlayOneShot(jumpscareClip);
            }
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
