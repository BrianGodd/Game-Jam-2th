using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Collections.Generic;

namespace Horror.Editor
{
    public class SetupCellarMonster : EditorWindow
    {
        private static GameObject FindGameObjectIncludingInactive(string name)
        {
            var allGo = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGo)
            {
                if (go.name == name && go.scene.name == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
                {
                    return go;
                }
            }
            return null;
        }

        [MenuItem("Tools/Setup Cellar Monster")]
        public static void Setup()
        {
            // Find player camera
            Camera playerCam = Camera.main;
            if (playerCam == null)
            {
                var camObj = GameObject.FindWithTag("MainCamera");
                if (camObj != null)
                {
                    playerCam = camObj.GetComponent<Camera>();
                }
            }

            if (playerCam == null)
            {
                Debug.LogError("SetupCellarMonster: Could not find Main Camera in the scene. Please ensure your camera is tagged as 'MainCamera'.");
                return;
            }

            // Find player controller
            var playerController = GameObject.FindObjectOfType<StarterAssets.FirstPersonController>();
            if (playerController == null)
            {
                Debug.LogError("SetupCellarMonster: Could not find StarterAssets.FirstPersonController in the scene. Please ensure your player prefab is present.");
                return;
            }

            // Get NavMesh triangulation vertices
            NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
            Debug.Log($"[Setup Diagnostics] Raw NavMesh triangulation vertex count: {tri.vertices.Length}");

            if (tri.vertices == null || tri.vertices.Length == 0)
            {
                Debug.LogError("SetupCellarMonster: NavMesh triangulation is empty! Please verify that you have opened the correct scene ('Assets/BrianGodd/Main_Cellar.unity') and baked the NavMesh (via Window -> AI -> Navigation).");
                return;
            }

            // Determine floor height dynamically using the player's Y position
            float playerY = playerController.transform.position.y;
            float floorMinY = playerY - 0.8f; 
            float floorMaxY = playerY + 0.8f;
            Debug.Log($"[Setup Diagnostics] Player Y is {playerY:F4}. Dynamic floor Y filter range set to: [{floorMinY:F4}, {floorMaxY:F4}].");

            // Find existing MonsterManager in the scene
            GameObject monsterManagerObj = FindGameObjectIncludingInactive("MonsterManager");
            if (monsterManagerObj == null)
            {
                Debug.LogError("SetupCellarMonster: Could not find 'MonsterManager' GameObject in the active scene. Please ensure it is present.");
                return;
            }

            // Clean up the redundant MonsterPresenceSetup GameObject from the scene to prevent duplicate executions
            GameObject oldSetup = FindGameObjectIncludingInactive("MonsterPresenceSetup");
            if (oldSetup != null)
            {
                Debug.Log("SetupCellarMonster: Destroying redundant 'MonsterPresenceSetup' GameObject to clean up the scene.");
                DestroyImmediate(oldSetup);
            }

            // Find or create Stalk and Hide parent groups inside the MonsterManager
            GameObject stalkParent = GameObject.Find("StalkPoints");
            if (stalkParent != null)
            {
                DestroyImmediate(stalkParent);
            }
            stalkParent = new GameObject("StalkPoints");
            stalkParent.transform.SetParent(monsterManagerObj.transform);

            GameObject hideParent = GameObject.Find("HidePoints");
            if (hideParent != null)
            {
                DestroyImmediate(hideParent);
            }
            hideParent = new GameObject("HidePoints");
            hideParent.transform.SetParent(monsterManagerObj.transform);

            // Generate points based on NavMesh vertices
            List<Transform> stalkList = new List<Transform>();
            List<Transform> hideList = new List<Transform>();

            // Filter triangulation vertices: only keep points on the floor
            List<Vector3> floorVertices = new List<Vector3>();
            foreach (var v in tri.vertices)
            {
                if (v.y >= floorMinY && v.y <= floorMaxY)
                {
                    floorVertices.Add(v);
                }
            }

            Debug.Log($"[Setup Diagnostics] Vertices matching floor Y range [{floorMinY:F2}, {floorMaxY:F2}]: {floorVertices.Count}");

            if (floorVertices.Count == 0)
            {
                Debug.LogError($"SetupCellarMonster: No NavMesh vertices found on the floor Y level range [{floorMinY:F2}, {floorMaxY:F2}].");
                return;
            }

            // Shuffle floor vertices to ensure random spatial distribution
            for (int i = 0; i < floorVertices.Count; i++)
            {
                int tempIdx = Random.Range(i, floorVertices.Count);
                Vector3 temp = floorVertices[i];
                floorVertices[i] = floorVertices[tempIdx];
                floorVertices[tempIdx] = temp;
            }

            int pairsCount = 50; // We want exactly 50 pairs of points
            int successCount = 0;

            for (int i = 0; i < floorVertices.Count; i++)
            {
                if (successCount >= pairsCount)
                {
                    break;
                }

                Vector3 stalkPos = floorVertices[i];

                // Ensure even distribution of stalk points (minimum 1.5m spacing)
                bool tooCloseToOthers = false;
                foreach (var st in stalkList)
                {
                    if (Vector3.Distance(stalkPos, st.position) < 1.5f)
                    {
                        tooCloseToOthers = true;
                        break;
                    }
                }
                if (tooCloseToOthers)
                {
                    continue;
                }

                // Find a nearby floor vertex to serve as the HidePoint
                Vector3 hidePos = Vector3.zero;
                bool foundHidePoint = false;

                // Search floor vertices to find a valid HidePoint
                int startSearchIdx = Random.Range(0, floorVertices.Count);
                for (int j = 0; j < floorVertices.Count; j++)
                {
                    int searchIdx = (startSearchIdx + j) % floorVertices.Count;
                    Vector3 candidateHidePos = floorVertices[searchIdx];

                    float dist = Vector3.Distance(stalkPos, candidateHidePos);
                    if (dist >= 1.5f && dist <= 6.0f)
                    {
                        // Check if there is an obstacle (wall/collider) between them at waist height (0.7m)
                        RaycastHit rayHit;
                        Vector3 rayStart = stalkPos + Vector3.up * 0.7f;
                        Vector3 rayEnd = candidateHidePos + Vector3.up * 0.7f;

                        if (Physics.Linecast(rayStart, rayEnd, out rayHit))
                        {
                            if (rayHit.collider.isTrigger)
                            {
                                continue;
                            }

                            // Ensure the hit object is a vertical wall/obstacle, not floor/ceiling
                            if (Mathf.Abs(rayHit.normal.y) < 0.85f)
                            {
                                hidePos = candidateHidePos;
                                foundHidePoint = true;
                                break;
                            }
                        }
                    }
                }

                if (foundHidePoint)
                {
                    // Create StalkPoint GameObject
                    GameObject stObj = new GameObject($"StalkPoint_{successCount}");
                    stObj.transform.position = stalkPos;
                    stObj.transform.SetParent(stalkParent.transform);
                    stalkList.Add(stObj.transform);

                    // Create HidePoint GameObject
                    GameObject hdObj = new GameObject($"HidePoint_{successCount}");
                    hdObj.transform.position = hidePos;
                    hdObj.transform.SetParent(hideParent.transform);
                    hideList.Add(hdObj.transform);

                    successCount++;
                }
            }

            if (successCount == 0)
            {
                Debug.LogError($"SetupCellarMonster: Could not find any valid pairs with a wall blocking the view between StalkPoint and HidePoint.");
                return;
            }

            if (successCount < pairsCount)
            {
                Debug.LogWarning($"SetupCellarMonster: Requested {pairsCount} pairs, but only successfully generated {successCount} pairs due to space/occlusion limits. Proceeding with {successCount} pairs.");
            }

            // Find the actual moving "Monster" GameObject (which is a child of MonsterManager or separate in scene)
            GameObject monsterObj = FindGameObjectIncludingInactive("Monster");
            if (monsterObj == null)
            {
                Debug.LogError("SetupCellarMonster: Could not find 'Monster' GameObject in the active scene. Please ensure your prefab contains the child 'Monster'.");
                return;
            }

            // Ensure the actual moving Monster object has a NavMeshAgent attached
            var agent = monsterObj.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = monsterObj.AddComponent<NavMeshAgent>();
                agent.speed = 2f;
                agent.angularSpeed = 120f;
                agent.acceleration = 8f;
                Debug.Log("SetupCellarMonster: Automatically attached missing 'NavMeshAgent' component to the moving 'Monster' GameObject.");
            }

            // Find the existing MonsterPresenceDirector component on the MonsterManager (prefab structure)
            var director = monsterManagerObj.GetComponent<Horror.MonsterPresenceDirector>();
            if (director == null)
            {
                director = monsterManagerObj.AddComponent<Horror.MonsterPresenceDirector>();
            }

            // Clean up any extra MonsterPresenceDirector components attached to other GameObjects in the scene
            var allDirectors = Resources.FindObjectsOfTypeAll<Horror.MonsterPresenceDirector>();
            foreach (var dir in allDirectors)
            {
                if (dir != director && dir.gameObject.scene.name == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
                {
                    Debug.Log($"SetupCellarMonster: Destroying duplicate director component on '{dir.gameObject.name}'.");
                    DestroyImmediate(dir);
                }
            }

            // Assign references
            director.monsterObject = monsterObj; // Points to the actual moving "Monster" child
            director.playerCamera = playerCam;
            director.playerController = playerController;
            director.stalkPoints = stalkList.ToArray();
            director.hidePoints = hideList.ToArray();

            // Configure default settings
            director.firstDelay = 8f;
            director.minInterval = 12f;
            director.maxInterval = 25f;
            director.stayDuration = 5f;
            director.minStalkDistance = 4f;
            director.maxStalkDistance = 20f;
            director.moveSpeed = 2.5f;
            director.vanishWhenLookedAt = true;
            director.sightRange = 20f;

            director.threat = 0f;
            director.threatThreshold = 50f;
            director.walkThreatRate = 8f;
            director.runThreatRate = 20f;
            director.threatDecayRate = 4f;
            director.chaseChance = 0.5f;
            director.soundDetectionThreshold = 75f;

            director.initialChaseSpeed = 8f;
            director.finalChaseSpeed = 2f;
            director.speedDecayDuration = 6f;
            director.attackDistance = 1.6f;
            director.loseDistance = 22f;
            director.chaseGracePeriod = 3f;
            director.searchDuration = 4f;
            director.searchExtraDistance = 2f;

            director.escapeDistance = 8f;
            director.escapeDuration = 3f;
            director.jumpscareDuration = 2.5f;
            director.jumpscareHeightOffset = -0.5f;
            director.jumpscareShakeIntensity = 3f;
            director.jumpscarePositionalShake = 0.05f;

            var audio = monsterManagerObj.GetComponent<AudioSource>();
            if (audio == null)
            {
                audio = monsterManagerObj.AddComponent<AudioSource>();
            }
            director.jumpscareAudioSource = audio;

            // Mark director component as dirty so its references are serialized and saved
            EditorUtility.SetDirty(director);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log($"SetupCellarMonster: Successfully created {successCount} paired Stalk & Hide points inside 'MonsterManager'! Monster child and Director references have been configured.");
        }
    }
}
