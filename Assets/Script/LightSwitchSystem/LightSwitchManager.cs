using System.Collections.Generic;
using UnityEngine;
using Horror;

namespace LightSwitchSystem
{
    public class LightSwitchManager : MonoBehaviour
    {
        private static LightSwitchManager instance;
        public static LightSwitchManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<LightSwitchManager>();
                }
                return instance;
            }
        }

        [SerializeField] private List<LightSwitch> lightSwitches = new();
        public List<LightSwitch> LightSwitches => lightSwitches;

        [Header("Light Anomaly Settings")]
        [SerializeField] private bool enableLightAnomaly = true;
        [SerializeField] private float warningDistance = 15f;
        [SerializeField] [Range(0f, 1f)] private float maxNormalFlickerReduction = 0.75f;

        private class LightData
        {
            public LightSwitch lightSwitch;
            public Light lightComponent;
            public float originalIntensity;
            public Color originalColor;
        }

        private List<LightData> cachedAnomalyLights = new();
        private bool lightsCached = false;
        private MonsterPresenceDirector monsterDirector;

        // Global synchronized flicker timing states
        private float nextGlobalFlickerTime = 0f;
        private float globalFlickerEndTime = 0f;
        private float globalBurstDuration = 0.5f;

        // Smooth transition state
        private float smoothedBrightness = 1f;
        private float smoothedDip = 0f; // 0 = no dip, 1 = full blackout

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
            InitializeLightSwitchList();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                instance = null;
            }
        }

        public void RegisterLightSwitch(LightSwitch lightSwitch)
        {
            if (lightSwitch != null && !lightSwitches.Contains(lightSwitch))
            {
                lightSwitches.Add(lightSwitch);
            }
        }

        public void UnregisterLightSwitch(LightSwitch lightSwitch)
        {
            if (lightSwitch != null)
            {
                lightSwitches.Remove(lightSwitch);
            }
        }

        public bool HasSwitchOn()
        {
            return GetSwitchOnCount() > 0;
        }

        public int GetSwitchOnCount()
        {
            int count = 0;
            for (int i = 0; i < lightSwitches.Count; i++)
            {
                LightSwitch lightSwitch = lightSwitches[i];
                if (lightSwitch != null && lightSwitch.IsOn)
                {
                    count++;
                }
            }

            return count;
        }

        private void InitializeLightSwitchList()
        {
            HashSet<LightSwitch> uniqueSwitches = new();
            List<LightSwitch> cleaned = new();
            foreach (var sw in lightSwitches)
            {
                if (sw != null && uniqueSwitches.Add(sw))
                {
                    cleaned.Add(sw);
                }
            }
            lightSwitches = cleaned;
        }

        private int lastKnownSwitchCount = -1;

        private void Update()
        {
            if (monsterDirector == null)
            {
                monsterDirector = FindObjectOfType<MonsterPresenceDirector>();
            }

            if (enableLightAnomaly && monsterDirector != null)
            {
                // Rebuild cache when switches are added/removed (handles Start() registration timing)
                if (lightSwitches.Count != lastKnownSwitchCount)
                {
                    RebuildAnomalyLightCache();
                    lastKnownSwitchCount = lightSwitches.Count;
                }
                if (cachedAnomalyLights.Count > 0)
                {
                    UpdateLightAnomaly();
                }
            }
            else if (lightsCached)
            {
                RestoreAnomalyNormalLights();
                lightsCached = false;
                lastKnownSwitchCount = -1;
            }
        }

        private void RebuildAnomalyLightCache()
        {
            cachedAnomalyLights.Clear();

            HashSet<Light> seen = new();
            foreach (var sw in lightSwitches)
            {
                if (sw != null && sw.Lights != null)
                {
                    foreach (var lt in sw.Lights)
                    {
                        if (lt != null && lt.type != LightType.Directional && seen.Add(lt))
                        {
                            cachedAnomalyLights.Add(new LightData
                            {
                                lightSwitch = sw,
                                lightComponent = lt,
                                originalIntensity = lt.intensity,
                                originalColor = lt.color,
                            });
                        }
                    }
                }
            }
            lightsCached = cachedAnomalyLights.Count > 0;
        }

        private void UpdateLightAnomaly()
        {
            MonsterPresenceDirector.MonsterState state = monsterDirector.CurrentState;
            float threat = Mathf.Clamp(monsterDirector.threat, 0f, 100f);
            float dt = Time.deltaTime;

            // Jumpscare: instant total blackout
            if (state == MonsterPresenceDirector.MonsterState.Jumpscare)
            {
                foreach (var data in cachedAnomalyLights)
                {
                    if (data.lightComponent != null)
                        data.lightComponent.enabled = false;
                }
                return;
            }

            // --- Ambient dread: target brightness dims as threat rises ---
            // threat 0 → 100%, threat 100 → 50%
            float targetBrightness = Mathf.Lerp(1f, 0.5f, threat / 100f);
            // Smooth transition (~2s to settle)
            smoothedBrightness = Mathf.MoveTowards(smoothedBrightness, targetBrightness, dt * 0.5f);

            // --- Occasional blackout dip (synchronized, all lights together) ---
            bool allowFlicker = threat > 5f
                && state != MonsterPresenceDirector.MonsterState.Chasing;

            bool wantDip = false;

            if (allowFlicker)
            {
                if (nextGlobalFlickerTime <= 0f)
                    nextGlobalFlickerTime = Time.time + Random.Range(5f, 12f);

                if (Time.time >= nextGlobalFlickerTime && Time.time >= globalFlickerEndTime)
                {
                    // Burst gets longer at high threat: 0.4s → 0.8s
                    globalBurstDuration = Mathf.Lerp(0.4f, 0.8f, threat / 100f);
                    globalFlickerEndTime = Time.time + globalBurstDuration;

                    // Cooldown: threat 0 → ~15s, threat 100 → ~3s
                    float interval = Mathf.Lerp(15f, 3f, threat / 100f);
                    nextGlobalFlickerTime = Time.time + interval * Random.Range(0.8f, 1.2f);
                }

                if (Time.time < globalFlickerEndTime)
                {
                    float elapsed = Time.time - (globalFlickerEndTime - globalBurstDuration);
                    float p = elapsed / globalBurstDuration;

                    bool dip1 = (p >= 0.08f && p <= 0.20f);
                    bool dip2 = (p >= 0.45f && p <= 0.57f);
                    bool dip3 = threat >= 40f && (p >= 0.75f && p <= 0.87f);

                    wantDip = dip1 || dip2 || dip3;
                }
            }

            // Smooth the dip transition: fast drop (~20ms), slower recovery (~80ms)
            float dipTarget = wantDip ? 1f : 0f;
            float dipSpeed = wantDip ? 50f : 12f;
            smoothedDip = Mathf.MoveTowards(smoothedDip, dipTarget, dt * dipSpeed);

            // --- Apply to all lights ---
            float finalMultiplier = smoothedBrightness * (1f - smoothedDip);
            foreach (var data in cachedAnomalyLights)
            {
                if (data.lightComponent == null) continue;

                if (!data.lightSwitch.IsOn)
                {
                    data.lightComponent.enabled = false;
                    continue;
                }

                data.lightComponent.color = data.originalColor;
                data.lightComponent.enabled = true;
                data.lightComponent.intensity = data.originalIntensity * finalMultiplier;
            }
        }

        private void RestoreAnomalyNormalLights()
        {
            foreach (var data in cachedAnomalyLights)
            {
                if (data.lightComponent == null) continue;

                if (!data.lightSwitch.IsOn)
                {
                    data.lightComponent.enabled = false;
                    continue;
                }

                data.lightComponent.color = data.originalColor;
                data.lightComponent.intensity = data.originalIntensity;
                data.lightComponent.enabled = true;
            }
        }
    }
}
