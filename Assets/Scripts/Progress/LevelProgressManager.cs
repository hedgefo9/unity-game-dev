using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReactorTechnician
{
    public sealed class LevelProgressManager : MonoBehaviour
    {
        public const string DifficultyTierPlayerPrefsKey = "ReactorTechnician.DifficultyTier";

        [Serializable]
        private sealed class ProgressStep
        {
            public string objective;
            public ReactorSectionNode[] requiredNodes;
            public CoolingZoneState[] requiredZones;
            public CoolingFlowPath[] requiredFlows;
            public AccessTerminalInteractable[] requiredTerminals;
            public StationSubsystem[] requiredSubsystems;
            public DoorInteractable[] doorsToUnlockOnComplete;
            public GameObject[] zonesToRevealOnComplete;
        }

        [SerializeField] private ProgressStep[] steps;
        [SerializeField] private ReactorOverheatManager overheatManager;
        [SerializeField] private PlayerHeatExposure playerHeatExposure;
        [SerializeField] private StationFeedbackUI feedbackUI;
        [SerializeField] private DoorInteractable finalDoor;
        [SerializeField] private StationSubsystem finalSubsystem;
        [SerializeField] private int maxDifficultyTier = 4;
        [SerializeField] private float victoryInputDelay = 0.75f;

        private int currentStepIndex;
        private bool levelCompleted;
        private bool levelFailed;
        private float victoryTimer;

        public int CurrentStepIndex => currentStepIndex;
        public bool LevelCompleted => levelCompleted;
        public bool LevelFailed => levelFailed;

        private void Start()
        {
            ApplyInitialLocks();
            UpdateObjective();
        }

        private void Update()
        {
            if (levelCompleted)
            {
                victoryTimer -= Time.unscaledDeltaTime;
                if (victoryTimer <= 0f && ReactorInput.AnyKeyPressed)
                {
                    LoadNextDifficultyLevel();
                }

                return;
            }

            RefreshProgress();
        }

        public void RefreshProgress()
        {
            if (levelFailed)
            {
                if (ReactorInput.RestartPressed)
                {
                    RestartLevel();
                }

                return;
            }

            if (levelCompleted || levelFailed)
            {
                return;
            }

            if (overheatManager != null && overheatManager.CriticalFailure)
            {
                FailLevel("The reactor reached critical overheating before stabilization.");
                return;
            }

            if (playerHeatExposure != null && playerHeatExposure.IsIncapacitated)
            {
                FailLevel("The technician was incapacitated by heat exposure.");
                return;
            }

            while (currentStepIndex < steps.Length && IsStepComplete(steps[currentStepIndex]))
            {
                CompleteCurrentStep();
            }

            if (currentStepIndex >= steps.Length)
            {
                CompleteLevel();
            }
            else
            {
                UpdateObjective();
            }
        }

        private void ApplyInitialLocks()
        {
            for (int i = 0; i < steps.Length; i++)
            {
                ProgressStep step = steps[i];
                if (step == null)
                {
                    continue;
                }

                SetDoorsLocked(step.doorsToUnlockOnComplete, true);
                SetObjectsActive(step.zonesToRevealOnComplete, false);
            }

            if (finalDoor != null)
            {
                finalDoor.SetLocked(true);
                finalDoor.SetOpen(false);
            }
        }

        private bool IsStepComplete(ProgressStep step)
        {
            if (step == null)
            {
                return true;
            }

            if (!AllNodesStabilized(step.requiredNodes))
            {
                return false;
            }

            if (!AllZonesStabilized(step.requiredZones))
            {
                return false;
            }

            if (!AllFlowsActive(step.requiredFlows))
            {
                return false;
            }

            if (!AllTerminalsActivated(step.requiredTerminals))
            {
                return false;
            }

            return AllSubsystemsActive(step.requiredSubsystems);
        }

        private void CompleteCurrentStep()
        {
            ProgressStep step = steps[currentStepIndex];
            SetDoorsLocked(step.doorsToUnlockOnComplete, false);
            SetObjectsActive(step.zonesToRevealOnComplete, true);

            feedbackUI?.ShowMessage($"Objective complete: {step.objective}");
            currentStepIndex++;
            UpdateObjective();
        }

        private void CompleteLevel()
        {
            levelCompleted = true;
            victoryTimer = victoryInputDelay;

            if (finalDoor != null)
            {
                finalDoor.SetLocked(false);
                finalDoor.SetOpen(true);
            }

            if (finalSubsystem != null)
            {
                finalSubsystem.Activate();
            }

            feedbackUI?.SetObjective("COMPLETE: reactor stabilized before critical overheating.");
            feedbackUI?.ShowMessage("Level complete: reactor stabilized.");
            feedbackUI?.ShowVictory("All key reactor systems are stable. Press any key to enter a harder generated layout.");
        }

        private void FailLevel(string reason)
        {
            levelFailed = true;
            feedbackUI?.SetObjective($"FAILED: {reason}");
            feedbackUI?.ShowMessage("Level failed.");
            feedbackUI?.ShowFailure($"{reason}\nPress R to restart the level.");
        }

        private void RestartLevel()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(activeScene.name);
            }
        }

        private void LoadNextDifficultyLevel()
        {
            int currentTier = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyTierPlayerPrefsKey, 1), 1, maxDifficultyTier);
            int nextTier = Mathf.Clamp(currentTier + 1, 1, maxDifficultyTier);
            PlayerPrefs.SetInt(DifficultyTierPlayerPrefsKey, nextTier);
            PlayerPrefs.Save();
            RestartLevel();
        }

        private void UpdateObjective()
        {
            if (feedbackUI == null || currentStepIndex >= steps.Length || steps[currentStepIndex] == null)
            {
                return;
            }

            feedbackUI.SetObjective($"Objective {currentStepIndex + 1}/{steps.Length}: {steps[currentStepIndex].objective}");
        }

        private static void SetDoorsLocked(DoorInteractable[] doors, bool locked)
        {
            if (doors == null)
            {
                return;
            }

            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null)
                {
                    doors[i].SetLocked(locked);
                    if (locked)
                    {
                        doors[i].SetOpen(false);
                    }
                }
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

        private static bool AllNodesStabilized(ReactorSectionNode[] nodes)
        {
            if (nodes == null)
            {
                return true;
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && !nodes[i].IsStabilized)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllZonesStabilized(CoolingZoneState[] zones)
        {
            if (zones == null)
            {
                return true;
            }

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null && !zones[i].IsStabilized)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllFlowsActive(CoolingFlowPath[] flows)
        {
            if (flows == null)
            {
                return true;
            }

            for (int i = 0; i < flows.Length; i++)
            {
                if (flows[i] != null && !flows[i].IsActive)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllTerminalsActivated(AccessTerminalInteractable[] terminals)
        {
            if (terminals == null)
            {
                return true;
            }

            for (int i = 0; i < terminals.Length; i++)
            {
                if (terminals[i] != null && !terminals[i].IsActivated)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllSubsystemsActive(StationSubsystem[] subsystems)
        {
            if (subsystems == null)
            {
                return true;
            }

            for (int i = 0; i < subsystems.Length; i++)
            {
                if (subsystems[i] != null && !subsystems[i].IsActive)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
