using UnityEngine;

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
        public float visibleSeconds = 2.5f;
        public float hideAfterSeenSeconds = 1f;
        public float minStalkDistance = 4f;
        public float maxStalkDistance = 18f;
        public float moveSpeed = 2f;
        public bool vanishWhenLookedAt = true;

        private enum MonsterState
        {
            Hidden,
            MovingToStalk,
            Stalking,
            MovingToHide
        }

        private MonsterState state;
        private float nextSightingTime;
        private float hideTime;
        private Transform targetPoint;
        private int currentPairIndex = -1;

        private void Awake()
        {
            if (stalkPoints == null || stalkPoints.Length == 0 || hidePoints == null || hidePoints.Length == 0)
            {
                throw new System.InvalidOperationException("Stalk points or Hide points are not configured.");
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
                MoveToTarget();
                if (HasReachedTarget())
                {
                    state = MonsterState.Stalking;
                    hideTime = Time.time + visibleSeconds;
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
                MoveToTarget();
                if (HasReachedTarget())
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
            monsterObject.transform.SetPositionAndRotation(hidePoint.position, hidePoint.rotation);

            targetPoint = stalkPoints[currentPairIndex];
            monsterObject.SetActive(true);
            state = MonsterState.MovingToStalk;
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
            state = MonsterState.MovingToHide;
        }

        private void MoveToTarget()
        {
            Vector3 targetPosition = targetPoint.position;
            monsterObject.transform.position = Vector3.MoveTowards(
                monsterObject.transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime);
        }

        private bool HasReachedTarget()
        {
            return monsterObject.transform.position == targetPoint.position;
        }
    }
}
