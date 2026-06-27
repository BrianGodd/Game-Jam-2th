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
        public float soundDetectionThreshold = 75f;

        [Header("Chase Settings")]
        public float initialChaseSpeed = 8f;
        public float finalChaseSpeed = 1.5f;
        public float speedDecayDuration = 5f;
        public float attackDistance = 1.5f;
        public float loseDistance = 20f;
        public float chaseGracePeriod = 3f;
        public float searchDuration = 4f;
        public float searchExtraDistance = 2f;

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
            Searching,
            BackingAway,
            Jumpscare
        }

        private MonsterState state;
        private float nextSightingTime;
        private float hideTime;
        private float chaseStartTime;
        private float escapeEndTime;
        private float jumpscareEndTime;
        private float searchEndTime;
        private float searchCenterYaw;
        private float stateEnterTime;
        private Vector3 lastSeenPosition;
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
            stateEnterTime = Time.time;
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

            // Only face the player when not searching or in jumpscare
            if (state != MonsterState.Searching && state != MonsterState.Jumpscare)
            {
                FacePlayer();
            }

            switch (state)
            {
                case MonsterState.MovingToStalk:
                case MonsterState.Stalking:
                    // Check dynamic chase trigger
                    if (threat >= threatThreshold && Random.value < chaseChance * Time.deltaTime)
                    {
                        Debug.Log($"[Monster] Dynamic trigger: Threat high ({threat:F1}). Transitioning to CHASE!");
                        StartChasing();
                        return;
                    }
                    // Check dynamic retreat trigger
                    if (ShouldVanishNow())
                    {
                        Debug.Log($"[Monster] Spotted by player! Retreating to HidePoint[{currentPairIndex}].");
                        Retreat();
                        return;
                    }

                    if (state == MonsterState.MovingToStalk)
                    {
                        if (HasReachedPosition(targetPoint.position))
                        {
                            state = MonsterState.Stalking;
                            stateEnterTime = Time.time;
                            hideTime = Time.time + stayDuration;
                            agent.ResetPath();
                            Debug.Log($"[Monster] Reached StalkPoint. Standing still for {stayDuration}s...");
                        }
                    }
                    else // Stalking
                    {
                        if (Time.time >= hideTime)
                        {
                            Debug.Log("[Monster] Stalk stay duration expired. Retreating.");
                            Retreat();
                        }
                    }
                    break;

                case MonsterState.MovingToHide:
                    if (HasReachedPosition(targetPoint.position))
                    {
                        monsterObject.SetActive(false);
                        state = MonsterState.Hidden;
                        stateEnterTime = Time.time;
                        ScheduleNextSighting();
                        Debug.Log("[Monster] Reached HidePoint and vanished. Waiting for next appearance.");
                    }
                    break;

                case MonsterState.Chasing:
                    // Speed decays over time during chase
                    float elapsed = Time.time - chaseStartTime;
                    float t = Mathf.Clamp01(elapsed / speedDecayDuration);
                    agent.speed = Mathf.Lerp(initialChaseSpeed, finalChaseSpeed, t);

                    // Check if player is detected (line of sight or noise)
                    bool playerDetected = CanSeePlayer() || (threat >= soundDetectionThreshold);

                    if (playerDetected)
                    {
                        Vector3 playerPos = playerCamera.transform.position;
                        Vector3 chaseDir = (playerPos - monsterObject.transform.position).normalized;
                        chaseDir.y = 0f;
                        chaseDir = chaseDir.normalized;

                        Vector3 targetPos = playerPos + chaseDir * searchExtraDistance;
                        
                        NavMeshHit navHit;
                        if (NavMesh.SamplePosition(targetPos, out navHit, 4.0f, NavMesh.AllAreas))
                        {
                            lastSeenPosition = navHit.position;
                        }
                        else
                        {
                            lastSeenPosition = playerPos;
                        }

                        agent.destination = playerPos;
                    }

                    float distance = Vector3.Distance(monsterObject.transform.position, playerCamera.transform.position);
                    if (distance <= attackDistance)
                    {
                        StartJumpscare();
                        return;
                    }

                    if (!playerDetected)
                    {
                        StartSearching();
                        return;
                    }

                    if (elapsed >= chaseGracePeriod && distance >= loseDistance)
                    {
                        threat = 0f;
                        StartBackingAway();
                    }
                    break;

                case MonsterState.Searching:
                    // Resume chase if player is detected again
                    if (CanSeePlayer() || threat >= soundDetectionThreshold)
                    {
                        string reason = CanSeePlayer() ? "Player spotted" : $"Player too loud (Threat: {threat:F1} >= {soundDetectionThreshold:F1})";
                        Debug.LogWarning($"[Monster] Player re-detected ({reason}) during search! Resuming CHASE!");
                        StartChasing();
                        return;
                    }

                    if (searchEndTime <= 0f)
                    {
                        // Rotate in movement direction
                        if (!agent.pathPending && agent.velocity.sqrMagnitude > 0.01f)
                        {
                            Vector3 moveDir = agent.velocity.normalized;
                            moveDir.y = 0f;
                            if (moveDir.sqrMagnitude > 0.001f)
                            {
                                monsterObject.transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
                            }
                        }

                        if (HasReachedPosition(lastSeenPosition))
                        {
                            searchEndTime = Time.time + searchDuration;
                            searchCenterYaw = monsterObject.transform.eulerAngles.y;
                            agent.ResetPath();
                            Debug.Log($"[Monster] Reached last seen position. Look-around started for {searchDuration}s...");
                        }
                    }
                    else
                    {
                        // Look left and right at destination
                        float angle = Mathf.Sin(Time.time * 3f) * 45f;
                        monsterObject.transform.rotation = Quaternion.Euler(0f, searchCenterYaw + angle, 0f);

                        if (Time.time >= searchEndTime)
                        {
                            threat = 0f;
                            Debug.Log("[Monster] Look-around finished without finding player. Retreating.");
                            Retreat();
                        }
                    }
                    break;

                case MonsterState.BackingAway:
                    // If player gets close and is detected again during retreat, resume chase!
                    float distToPlayer = Vector3.Distance(monsterObject.transform.position, playerCamera.transform.position);
                    if (distToPlayer < loseDistance && (CanSeePlayer() || threat >= soundDetectionThreshold))
                    {
                        string reason = CanSeePlayer() ? "Player spotted" : $"Player too loud (Threat: {threat:F1} >= {soundDetectionThreshold:F1})";
                        Debug.LogWarning($"[Monster] Player re-entered loseDistance ({distToPlayer:F1} < {loseDistance:F1}) and detected ({reason}) during retreat! Resuming CHASE!");
                        StartChasing();
                        return;
                    }

                    if (HasReachedPosition(agent.destination) || Time.time >= escapeEndTime)
                    {
                        monsterObject.SetActive(false);
                        state = MonsterState.Hidden;
                        stateEnterTime = Time.time;
                        ScheduleNextSighting();
                        Debug.Log("[Monster] Finished backing away and vanished.");
                    }
                    break;

                case MonsterState.Jumpscare:
                    playerCamera.transform.LookAt(monsterObject.transform.position + Vector3.up * 1.5f);

                    if (Time.time >= jumpscareEndTime)
                    {
                        Debug.Log("[Monster] Jumpscare finished. Reloading scene...");
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
            stateEnterTime = Time.time;

            if (threat >= threatThreshold && Random.value < chaseChance)
            {
                Debug.Log($"[Monster] Threat high ({threat:F1} >= {threatThreshold:F1}). Transitioning to CHASE immediately!");
                StartChasing();
            }
            else
            {
                targetPoint = stalkPoints[currentPairIndex];
                agent.speed = moveSpeed;
                agent.destination = targetPoint.position;
                state = MonsterState.MovingToStalk;
                Debug.Log($"[Monster] Spawned. Moving from HidePoint[{currentPairIndex}] to StalkPoint[{currentPairIndex}]. Threat: {threat:F1}");
            }
        }

        private void StartChasing()
        {
            state = MonsterState.Chasing;
            stateEnterTime = Time.time;
            chaseStartTime = Time.time;
            monsterObject.SetActive(true);
            agent.speed = initialChaseSpeed;
            agent.destination = playerCamera.transform.position;
            Debug.LogWarning($"[Monster] CHASE started! Speed: {initialChaseSpeed:F1}. Threat: {threat:F1}");
        }

        private void StartSearching()
        {
            state = MonsterState.Searching;
            stateEnterTime = Time.time;
            searchEndTime = 0f;
            agent.speed = moveSpeed;
            agent.destination = lastSeenPosition;
            Debug.Log($"[Monster] Lost player (LOS blocked & Threat {threat:F1} < {soundDetectionThreshold:F1}). Searching last seen position: {lastSeenPosition}");
        }

        private void StartBackingAway()
        {
            state = MonsterState.BackingAway;
            stateEnterTime = Time.time;
            escapeEndTime = Time.time + escapeDuration;

            Vector3 awayDirection = (monsterObject.transform.position - playerCamera.transform.position).normalized;
            awayDirection.y = 0f;
            awayDirection = awayDirection.normalized;

            Vector3 destination = monsterObject.transform.position + awayDirection * escapeDistance;

            agent.speed = moveSpeed;
            agent.destination = destination;
            Debug.Log($"[Monster] Player outran the monster. Backing away for {escapeDuration}s...");
        }

        private void StartJumpscare()
        {
            state = MonsterState.Jumpscare;
            stateEnterTime = Time.time;
            threat = 0f;
            jumpscareEndTime = Time.time + jumpscareDuration;
            Debug.LogWarning($"[Monster] JUMPSCARE triggered! Caught player!");

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

        private bool CanSeePlayer()
        {
            Vector3 start = monsterObject.transform.position + Vector3.up * 1.5f;
            Vector3 end = playerCamera.transform.position;

            RaycastHit hit;
            if (Physics.Linecast(start, end, out hit))
            {
                // Check if the hit object belongs to the player structure (root or child)
                var hitController = hit.transform.GetComponentInParent<StarterAssets.FirstPersonController>();
                if (hitController != playerController && hit.transform != playerCamera.transform)
                {
                    return false;
                }
            }
            return true;
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
            stateEnterTime = Time.time;
        }

        private bool HasReachedPosition(Vector3 targetPos)
        {
            // Ignore target arrival check for the first 0.2 seconds of state transition to handle latency
            if (Time.time - stateEnterTime < 0.2f)
            {
                return false;
            }

            Vector3 flatMonster = new Vector3(monsterObject.transform.position.x, 0f, monsterObject.transform.position.z);
            Vector3 flatTarget = new Vector3(targetPos.x, 0f, targetPos.z);
            
            if (Vector3.Distance(flatMonster, flatTarget) <= (agent.stoppingDistance + 0.5f))
            {
                return true;
            }

            if (agent.isActiveAndEnabled && !agent.pathPending && agent.hasPath)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
