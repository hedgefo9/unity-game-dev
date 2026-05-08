using UnityEngine;

namespace ReactorTechnician
{
    public sealed class ProgressDifficultyDirector : MonoBehaviour
    {
        [SerializeField] private LevelProgressManager progressManager;
        [SerializeField] private WaveFunctionLevelGenerator levelGenerator;
        [SerializeField] private CoolingZoneState[] affectedZones;
        [SerializeField] private GameObject[] tierTwoObjects;
        [SerializeField] private GameObject[] tierThreeObjects;
        [SerializeField] private float baseHeating = 0.2f;
        [SerializeField] private float heatingStep = 0.035f;
        [SerializeField] private float maxHeating = 0.3f;
        [SerializeField] private StationFeedbackUI feedbackUI;

        private int appliedStep = -1;

        private void Awake()
        {
            if (progressManager == null)
            {
                progressManager = FindAnyObjectByType<LevelProgressManager>();
            }

            if (levelGenerator == null)
            {
                levelGenerator = FindAnyObjectByType<WaveFunctionLevelGenerator>();
            }

            if (affectedZones == null || affectedZones.Length == 0)
            {
                affectedZones = FindObjectsByType<CoolingZoneState>();
            }

            ApplyDifficulty(0, true);
        }

        private void Update()
        {
            if (progressManager == null)
            {
                return;
            }

            int step = progressManager.CurrentStepIndex;
            if (step != appliedStep)
            {
                ApplyDifficulty(step, false);
            }
        }

        private void ApplyDifficulty(int step, bool force)
        {
            if (!force && step == appliedStep)
            {
                return;
            }

            appliedStep = step;
            int savedTier = Mathf.Clamp(PlayerPrefs.GetInt(LevelProgressManager.DifficultyTierPlayerPrefsKey, 1), 1, 4);
            int tier = Mathf.Clamp(savedTier + step, 1, 4);
            levelGenerator?.SetDifficultyTier(tier);

            float heating = Mathf.Min(maxHeating, baseHeating + step * heatingStep);
            if (affectedZones != null)
            {
                for (int i = 0; i < affectedZones.Length; i++)
                {
                    if (affectedZones[i] != null)
                    {
                        affectedZones[i].SetHeatingPerSecond(heating);
                    }
                }
            }

            SetObjectsActive(tierTwoObjects, tier >= 2);
            SetObjectsActive(tierThreeObjects, tier >= 3);

            if (!force && feedbackUI != null)
            {
                feedbackUI.ShowMessage($"Station instability increased: tier {tier}.");
            }
        }

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(active);
                }
            }
        }
    }
}
