using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Cinemachine;
using System.Collections.Generic;
using DoorSystem;

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
        public Animator monsterAnimator;
        [Tooltip("修正模型朝向的旋轉偏移量（若模型背對玩家，請設為 180）")]
        public float rotationOffset = 180f;

        [Header("Stalking Settings")]
        public float firstDelay = 8f;
        public float minInterval = 12f;
        public float maxInterval = 25f;
        public float stayDuration = 5f;
        public float minStalkDistance = 4f;
        public float maxStalkDistance = 18f;
        public float moveSpeed = 2f;
        public bool vanishWhenLookedAt = true;
        public float sightRange = 20f;

        [Header("Threat Settings")]
        public float threat = 0f;
        public float threatThreshold = 50f;
        public float walkThreatRate = 8f;
        public float runThreatRate = 20f;
        public float threatDecayRate = 4f;
        public float jumpThreat = 10f;
        public float landThreat = 15f;
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
        public float chaseStuckSearchDelay = 1.5f;
        public float chaseProgressDistance = 0.25f;
        public float blockedChaseRepathDistance = 1.25f;
        public float playerNavMeshSampleDistance = 4f;

        [Header("Door Settings")]
        public float doorOpenRadius = 2f;
        public float chaseDoorOpenInterval = 0.25f;
        [Range(0f, 1f)]
        public float stalkArrivalDoorOpenChance = 0.25f;
        public LayerMask doorDetectionLayers = ~0;

        [Header("Escape Settings")]
        public float escapeDistance = 8f;
        public float escapeDuration = 3f;

        [Header("Stealth Settings")]
        [Tooltip("Minimum distance from monster to player for hiding to succeed.")]
        public float hideMinDistance = 5f;
        [Tooltip("Max distance above player camera/head to check for obstruction/ceiling.")]
        public float headObstructionCheckDistance = 1.5f;
        [Tooltip("Layer mask for objects that can hide the player (e.g. Default, Environment).")]
        public LayerMask hidingObstructionLayers = ~0;

        [Header("Jumpscare Settings")]
        public AudioSource jumpscareAudioSource;
        public AudioClip jumpscareClip;
        public AudioClip chaseClip;
        public float jumpscareDuration = 2.5f;
        public Vector3 jumpscareOffset = new Vector3(0f, -0.5f, 1.0f);
        public float jumpscareShakeIntensity = 3.0f; // maximum rotation shake in degrees
        public float jumpscarePositionalShake = 0.05f; // maximum position shake offset in meters

        [Header("Body Shake")]
        public float bodyShakeIntensity = 0.03f; // position offset in meters
        public float bodyShakeSpeed = 25f; // Perlin noise speed

        private Vector3 jumpscareCameraBaseLocalPos;
        private Quaternion jumpscareCameraBaseRot;
        private Vector3 jumpscareMonsterBasePos;
        private float bodyShakeSeed;

        public enum MonsterState
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

        public MonsterState CurrentState => state;
        private float nextSightingTime;
        private float hideTime;
        private float chaseStartTime;
        private float escapeEndTime;
        private float jumpscareEndTime;
        private float searchEndTime;
        private float closestChaseDistance;
        private float chaseStuckStartTime;
        private bool hasBlockedChasePlayerPosition;
        private Vector3 blockedChasePlayerPosition;
        private float searchCenterYaw;
        private float stateEnterTime;
        private float nextDoorOpenTime;
        private Vector3 lastSeenPosition;
        private Transform targetPoint;
        private int currentPairIndex = -1;
        private NavMeshAgent agent;
        private bool wasGrounded = true;

        private void Awake()
        {
            AutoAssignPointArrays();

            if (stalkPoints == null || stalkPoints.Length == 0 || hidePoints == null || hidePoints.Length == 0)
            {
                throw new System.InvalidOperationException("Stalk points or Hide points are not configured.");
            }

            agent = monsterObject.GetComponent<NavMeshAgent>();
            agent.updateRotation = false; // FacePlayer handles rotation manually
            agent.updatePosition = false; // Prevent manual position offsets (body shake) from polluting agent pathfinding

            if (monsterAnimator == null && monsterObject != null)
            {
                monsterAnimator = monsterObject.GetComponent<Animator>();
            }

            if (monsterAnimator != null)
            {
                monsterAnimator.enabled = true;
            }

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
            monsterObject.transform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation * Quaternion.Euler(0, rotationOffset, 0));
            monsterObject.SetActive(false);
            state = MonsterState.Hidden;
            stateEnterTime = Time.time;
            nextSightingTime = Time.time + firstDelay;
            bodyShakeSeed = Random.Range(0f, 1000f);
            wasGrounded = playerController != null ? playerController.Grounded : true;
        }

        private void Update()
        {
            if (GameManager.CurrentDay <= 0)
            {
                threat = 0f;
                state = MonsterState.Hidden;
                monsterObject.SetActive(false);
                return;
            }

            bool isMoving = false;
            bool isRunning = false;
            bool playerJustJumped = false;
            bool playerJustLanded = false;

            var inputs = playerController.GetComponent<StarterAssets.StarterAssetsInputs>();
            var cc = playerController.GetComponent<CharacterController>();
            if (inputs != null && cc != null)
            {
                if (playerController.Grounded && inputs.move.sqrMagnitude > 0.01f && cc.velocity.sqrMagnitude > 0.01f)
                {
                    isMoving = true;
                    isRunning = inputs.sprint;
                }

                if (wasGrounded && !playerController.Grounded && inputs.jump)
                {
                    playerJustJumped = true;
                }
                else if (!wasGrounded && playerController.Grounded)
                {
                    playerJustLanded = true;
                }
                wasGrounded = playerController.Grounded;
            }

            // Update Threat based on movement (bypass during Chase/Jumpscare/Search to allow proper state behaviors)
            if (state != MonsterState.Chasing && state != MonsterState.Searching && state != MonsterState.Jumpscare)
            {
                if (isMoving)
                {
                    float increaseRate = isRunning ? runThreatRate : walkThreatRate;
                    threat += Time.deltaTime * increaseRate;
                }
                else
                {
                    threat -= Time.deltaTime * threatDecayRate;
                }

                if (playerJustJumped)
                {
                    threat += jumpThreat;
                    Debug.Log($"[Monster] Player jumped! Threat +{jumpThreat}. Current threat: {threat:F1}");
                }

                if (playerJustLanded)
                {
                    threat += landThreat;
                    Debug.Log($"[Monster] Player landed! Threat +{landThreat}. Current threat: {threat:F1}");
                }

                threat = Mathf.Max(threat, 0f);
            }

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

            // --- Continuous body shake (unsettling vibration) ---
            ApplyBodyShake();

            switch (state)
            {
                case MonsterState.MovingToStalk:
                case MonsterState.Stalking:
                    // Check dynamic chase trigger
                    if (CanChaseToday() && threat >= threatThreshold && Random.value < chaseChance * Time.deltaTime)
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
                        // Ensure it plays the Idle animation even while moving
                        if (monsterAnimator != null && !monsterAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                        {
                            PlayAnimation("Idle");
                        }

                        if (HasReachedPosition(targetPoint.position))
                        {
                            state = MonsterState.Stalking;
                            stateEnterTime = Time.time;
                            hideTime = Time.time + stayDuration;
                            agent.ResetPath();
                            PlayAnimation("Idle");
                            TryOpenNearbyDoor(stalkArrivalDoorOpenChance);
                            Debug.Log($"[Monster] Reached StalkPoint. Standing still for {stayDuration}s...");
                        }
                    }
                    else // Stalking
                    {
                        // Ensure it plays the Idle animation (and does not default to walk or freeze)
                        if (monsterAnimator != null && !monsterAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                        {
                            PlayAnimation("Idle");
                        }

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
                    TryOpenNearbyDoorEvery(chaseDoorOpenInterval);

                    // Speed decays over time during chase
                    float elapsed = Time.time - chaseStartTime;
                    float t = Mathf.Clamp01(elapsed / speedDecayDuration);
                    agent.speed = Mathf.Lerp(initialChaseSpeed, finalChaseSpeed, t);

                    // Check if player is detected (line of sight or noise)
                    float distToPlayer = Vector3.Distance(monsterObject.transform.position, playerCamera.transform.position);
                    bool isHiding = IsPlayerHiding() && distToPlayer >= hideMinDistance;
                    bool playerDetected = !isHiding && (CanSeePlayer() || (threat >= soundDetectionThreshold));

                    if (playerDetected)
                    {
                        Vector3 playerPos = playerCamera.transform.position;
                        Vector3 playerNavPos = playerController.transform.position;
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

                        if (!TryGetReachableNavMeshPosition(playerNavPos, out Vector3 reachablePlayerPos))
                        {
                            MarkBlockedChasePosition();
                            StartSearching();
                            return;
                        }

                        agent.destination = reachablePlayerPos;
                    }

                    Vector3 flatMonster = new Vector3(monsterObject.transform.position.x, 0f, monsterObject.transform.position.z);
                    Vector3 flatPlayer = new Vector3(playerController.transform.position.x, 0f, playerController.transform.position.z);
                    float distance = Vector3.Distance(flatMonster, flatPlayer);

                    if (playerDetected && IsChaseStuck(distance))
                    {
                        MarkBlockedChasePosition();
                        StartSearching();
                        return;
                    }

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
                    float searchDistToPlayer = Vector3.Distance(monsterObject.transform.position, playerCamera.transform.position);
                    bool searchIsHiding = IsPlayerHiding() && searchDistToPlayer >= hideMinDistance;
                    bool searchCanSeePlayer = CanSeePlayer();
                    bool searchCanHearPlayer = threat >= soundDetectionThreshold;
                    if (!searchIsHiding
                        && (searchCanSeePlayer || searchCanHearPlayer)
                        && !IsNearBlockedChasePosition()
                        && TryGetReachableNavMeshPosition(playerController.transform.position, out _))
                    {
                        string reason = searchCanSeePlayer ? "Player spotted" : $"Player too loud (Threat: {threat:F1} >= {soundDetectionThreshold:F1})";
                        Debug.LogWarning($"[Monster] Player re-detected ({reason}) during search! Resuming CHASE!");
                        StartChasing(false);
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
                                monsterObject.transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up) * Quaternion.Euler(0, rotationOffset, 0);
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
                    float backingAwayDistToPlayer = Vector3.Distance(monsterObject.transform.position, playerCamera.transform.position);
                    bool backingAwayIsHiding = IsPlayerHiding() && backingAwayDistToPlayer >= hideMinDistance;
                    if (backingAwayDistToPlayer < loseDistance && !backingAwayIsHiding && (CanSeePlayer() || threat >= soundDetectionThreshold))
                    {
                        string reason = CanSeePlayer() ? "Player spotted" : $"Player too loud (Threat: {threat:F1} >= {soundDetectionThreshold:F1})";
                        Debug.LogWarning($"[Monster] Player re-entered loseDistance ({backingAwayDistToPlayer:F1} < {loseDistance:F1}) and detected ({reason}) during retreat! Resuming CHASE!");
                        StartChasing(false);
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
                    // Apply random positional shake relative to cached base local position
                    Vector3 randomOffset = new Vector3(
                        Random.Range(-jumpscarePositionalShake, jumpscarePositionalShake),
                        Random.Range(-jumpscarePositionalShake, jumpscarePositionalShake),
                        Random.Range(-jumpscarePositionalShake, jumpscarePositionalShake)
                    );
                    playerCamera.transform.localPosition = jumpscareCameraBaseLocalPos + randomOffset;

                    // Apply rotational shake on top of looked-at base rotation
                    Quaternion shakeRot = Quaternion.Euler(
                        Random.Range(-jumpscareShakeIntensity, jumpscareShakeIntensity),
                        Random.Range(-jumpscareShakeIntensity, jumpscareShakeIntensity),
                        Random.Range(-jumpscareShakeIntensity, jumpscareShakeIntensity)
                    );
                    playerCamera.transform.rotation = jumpscareCameraBaseRot * shakeRot;

                    if (Time.time >= jumpscareEndTime)
                    {
                        Debug.Log("[Monster] Jumpscare finished. Reloading scene...");
                        GameManager.Instance.LoseGame();
                    }
                    break;
            }
        }

        private void StartStalking()
        {
            currentPairIndex = PickValidPairIndex();

            if (currentPairIndex == -1)
            {
                state = MonsterState.Hidden;
                ScheduleNextSighting();
                return;
            }

            Transform hidePoint = hidePoints[currentPairIndex];
            monsterObject.SetActive(true);
            agent.Warp(hidePoint.position);
            monsterObject.transform.position = hidePoint.position; // Manually sync transform when updatePosition is false
            stateEnterTime = Time.time;

            if (CanChaseToday() && threat >= threatThreshold && Random.value < chaseChance)
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
                PlayAnimation("Idle");
                Debug.Log($"[Monster] Spawned. Moving from HidePoint[{currentPairIndex}] to StalkPoint[{currentPairIndex}]. Threat: {threat:F1}");
            }
        }

        private void StartChasing(bool playChaseClip = true)
        {
            if (!CanChaseToday())
            {
                Debug.Log("[Monster] Chase blocked before Day 3.");
                return;
            }

            state = MonsterState.Chasing;
            stateEnterTime = Time.time;
            chaseStartTime = Time.time;
            closestChaseDistance = float.PositiveInfinity;
            chaseStuckStartTime = Time.time;
            hasBlockedChasePlayerPosition = false;
            nextDoorOpenTime = 0f;
            monsterObject.SetActive(true);
            agent.speed = initialChaseSpeed;
            if (TryGetReachableNavMeshPosition(playerController.transform.position, out Vector3 reachablePlayerPos))
            {
                agent.destination = reachablePlayerPos;
            }
            PlayAnimation("FastWalking");
            if (playChaseClip)
            {
                jumpscareAudioSource.PlayOneShot(chaseClip);
            }
            Debug.LogWarning($"[Monster] CHASE started! Speed: {initialChaseSpeed:F1}. Threat: {threat:F1}");
        }

        private void StartSearching()
        {
            state = MonsterState.Searching;
            stateEnterTime = Time.time;
            searchEndTime = 0f;
            agent.speed = moveSpeed;
            agent.destination = lastSeenPosition;
            PlayAnimation("Walking");
            Debug.Log($"[Monster] Lost player (LOS blocked & Threat {threat:F1} < {soundDetectionThreshold:F1}). Searching last seen position: {lastSeenPosition}");
        }

        private bool CanChaseToday()
        {
            return GameManager.CurrentDay >= 1;
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
            PlayAnimation("PeaceWalking");
            Debug.Log($"[Monster] Player outran the monster. Backing away for {escapeDuration}s...");
        }

        private void StartJumpscare()
        {
            state = MonsterState.Jumpscare;
            stateEnterTime = Time.time;
            threat = 0f;
            jumpscareEndTime = Time.time + jumpscareDuration;
            
            if (monsterAnimator != null)
            {
                monsterAnimator.enabled = false; // 凍結所有骨骼動畫，保持最後捕捉玩家時的動作
            }

            Debug.LogWarning($"[Monster] JUMPSCARE triggered! Caught player!");

            // Cache camera base local position and rotation
            jumpscareCameraBaseLocalPos = playerCamera.transform.localPosition;
            jumpscareCameraBaseRot = playerCamera.transform.rotation;

            // Disable CinemachineBrain to allow manual camera control/shake
            var brain = playerCamera.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

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

            // Position from camera yaw only so looking up/down or jumping does not push the monster off angle.
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = playerCamera.transform.right;
            right.y = 0f;
            right.Normalize();
            Vector3 spawnPos = playerCamera.transform.position
                + right * jumpscareOffset.x
                + Vector3.up * jumpscareOffset.y
                + forward * jumpscareOffset.z;
            jumpscareMonsterBasePos = spawnPos;
            monsterObject.transform.position = spawnPos;

            Vector3 dirToPlayer = playerCamera.transform.position - spawnPos;
            dirToPlayer.y = 0f; // Keep upright
            if (dirToPlayer.sqrMagnitude > 0.001f)
            {
                monsterObject.transform.rotation = Quaternion.LookRotation(dirToPlayer, Vector3.up) * Quaternion.Euler(0, rotationOffset, 0);
            }

            jumpscareAudioSource.PlayOneShot(jumpscareClip);
        }

        private void AutoAssignPointArrays()
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (stalkPoints == null || stalkPoints.Length == 0)
            {
                stalkPoints = FindNamedPoints(transforms, "StalkPoint");
            }

            if (hidePoints == null || hidePoints.Length == 0)
            {
                hidePoints = FindNamedPoints(transforms, "HidePoint");
            }
        }

        private Transform[] FindNamedPoints(Transform[] transforms, string prefix)
        {
            List<Transform> points = new();
            for (int i = 0; i < transforms.Length; i++)
            {
                if (IsPointName(transforms[i].name, prefix))
                {
                    points.Add(transforms[i]);
                }
            }

            Transform[] result = points.ToArray();
            System.Array.Sort(result, (a, b) => string.CompareOrdinal(a.name, b.name));
            return result;
        }

        private bool IsPointName(string objectName, string prefix)
        {
            if (!objectName.StartsWith(prefix)) return false;
            if (objectName.Length == prefix.Length) return true;

            char next = objectName[prefix.Length];
            return !char.IsLetter(next);
        }

        private bool IsPlayerHiding()
        {
            if (playerController == null) return false;

            var inputs = playerController.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (inputs == null || !inputs.crouch) return false;

            var cc = playerController.GetComponent<CharacterController>();
            if (cc != null)
            {
                if (inputs.move.sqrMagnitude > 0.01f || cc.velocity.sqrMagnitude > 0.01f)
                {
                    return false;
                }
            }

            // Raycast straight up from the player's camera position
            Vector3 rayStart = playerCamera.transform.position;
            Vector3 rayDir = Vector3.up;
            int playerLayer = playerController.gameObject.layer;
            int obstructionMask = hidingObstructionLayers & ~(1 << playerLayer);

            if (Physics.Raycast(rayStart, rayDir, out RaycastHit hit, headObstructionCheckDistance, obstructionMask))
            {
                return true;
            }

            return false;
        }

        private bool CanSeePlayer()
        {
            float dist = Vector3.Distance(monsterObject.transform.position, playerCamera.transform.position);
            if (dist > sightRange)
            {
                return false;
            }

            if (IsPlayerHiding() && dist >= hideMinDistance)
            {
                return false;
            }

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

        private bool TryGetReachableNavMeshPosition(Vector3 position, out Vector3 navMeshPosition)
        {
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, playerNavMeshSampleDistance, NavMesh.AllAreas))
            {
                navMeshPosition = position;
                return false;
            }

            navMeshPosition = hit.position;
            NavMeshPath path = new NavMeshPath();
            return agent.CalculatePath(navMeshPosition, path)
                && path.status == NavMeshPathStatus.PathComplete;
        }

        private bool IsChaseStuck(float distance)
        {
            if (closestChaseDistance - distance > chaseProgressDistance)
            {
                closestChaseDistance = distance;
                chaseStuckStartTime = Time.time;
            }

            return Time.time - chaseStuckStartTime >= chaseStuckSearchDelay;
        }

        private void MarkBlockedChasePosition()
        {
            hasBlockedChasePlayerPosition = true;
            blockedChasePlayerPosition = playerController.transform.position;
        }

        private bool IsNearBlockedChasePosition()
        {
            if (!hasBlockedChasePlayerPosition) return false;

            Vector3 current = playerController.transform.position;
            current.y = 0f;
            Vector3 blocked = blockedChasePlayerPosition;
            blocked.y = 0f;
            return Vector3.Distance(current, blocked) < blockedChaseRepathDistance;
        }

        private void TryOpenNearbyDoorEvery(float interval)
        {
            if (Time.time < nextDoorOpenTime) return;

            nextDoorOpenTime = Time.time + interval;
            TryOpenNearbyDoor(1f);
        }

        private void TryOpenNearbyDoor(float chance)
        {
            if (Random.value > chance) return;

            Collider[] hits = Physics.OverlapSphere(monsterObject.transform.position, doorOpenRadius, doorDetectionLayers, QueryTriggerInteraction.Collide);
            List<DoorControl> doors = new();
            for (int i = 0; i < hits.Length; i++)
            {
                DoorControl door = hits[i].GetComponentInParent<DoorControl>();
                if (door != null && door.State == DoorControl.DoorState.Closed && !doors.Contains(door))
                {
                    doors.Add(door);
                }
            }

            if (doors.Count == 0) return;

            doors[Random.Range(0, doors.Count)].Open();
        }

        private bool CanHidePointSeePlayer(Vector3 hidePointPosition)
        {
            Vector3 start = hidePointPosition + Vector3.up * 1.5f; // eye level from hidepoint
            Vector3 end = playerCamera.transform.position;

            RaycastHit hit;
            if (Physics.Linecast(start, end, out hit))
            {
                // If it hits an obstacle (wall/collider) other than the player, the sight is blocked
                var hitController = hit.transform.GetComponentInParent<StarterAssets.FirstPersonController>();
                if (hitController != playerController && hit.transform != playerCamera.transform)
                {
                    return false; // sight blocked, so HidePoint cannot see player
                }
            }
            return true; // clear line of sight, HidePoint can see player
        }

        private int PickValidPairIndex()
        {
            int numPoints = Mathf.Min(stalkPoints.Length, hidePoints.Length);
            int startIndex = Random.Range(0, numPoints);
            for (int i = 0; i < numPoints; i++)
            {
                int index = (startIndex + i) % numPoints;
                Transform hidePoint = hidePoints[index];
                Transform stalkPoint = stalkPoints[index];
                
                float distance = Vector3.Distance(playerCamera.transform.position, stalkPoint.position);
                
                // HidePoint must NOT be able to see the player to ensure it is hidden behind physical geometry
                if (distance >= minStalkDistance && distance <= maxStalkDistance && !CanHidePointSeePlayer(hidePoint.position))
                {
                    return index;
                }
            }

            Debug.LogWarning("[Monster] No valid stalk point is within range, or all corresponding hide points have direct line of sight to the player.");
            return -1;
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

        private void ApplyBodyShake()
        {
            if (bodyShakeIntensity <= 0f) return;

            float intensity = bodyShakeIntensity;
            if (state == MonsterState.Chasing)
                intensity *= 3f;
            else if (state == MonsterState.Jumpscare)
                intensity *= 3f; // Keep it shaking violently during jumpscare

            float t = Time.time * bodyShakeSpeed;
            float offsetX = (Mathf.PerlinNoise(t, bodyShakeSeed) - 0.5f) * 2f * intensity;
            float offsetY = (Mathf.PerlinNoise(bodyShakeSeed, t) - 0.5f) * 2f * intensity;
            float offsetZ = (Mathf.PerlinNoise(t + 50f, bodyShakeSeed + 50f) - 0.5f) * 2f * intensity;

            // Apply as world-space offset on top of current base position (Agent or Jumpscare pivot)
            Vector3 basePos = (state == MonsterState.Jumpscare) ? jumpscareMonsterBasePos : agent.nextPosition;
            monsterObject.transform.position = basePos + new Vector3(offsetX, offsetY, offsetZ);
        }

        private void FacePlayer()
        {
            Vector3 targetPosition = playerCamera.transform.position;
            targetPosition.y = monsterObject.transform.position.y;
            monsterObject.transform.LookAt(targetPosition);
            monsterObject.transform.Rotate(0, rotationOffset, 0);
        }

        private void Retreat()
        {
            targetPoint = hidePoints[currentPairIndex];
            agent.speed = moveSpeed;
            agent.destination = targetPoint.position;
            state = MonsterState.MovingToHide;
            stateEnterTime = Time.time;
            PlayAnimation("PeaceWalking");
        }

        private void PlayAnimation(string stateName)
        {
            if (monsterAnimator != null)
            {
                monsterAnimator.Play(stateName);
            }
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
