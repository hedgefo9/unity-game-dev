using UnityEngine;

namespace ReactorTechnician
{
    public sealed class AccessTerminalInteractable : InteractableObject
    {
        [SerializeField] private string terminalName = "Access Terminal";
        [SerializeField] private ReactorSectionNode[] requiredNodes;
        [SerializeField] private CoolingZoneState[] requiredStableZones;
        [SerializeField] private CoolingFlowPath[] requiredActiveFlows;
        [SerializeField] private DoorInteractable[] linkedDoors;
        [SerializeField] private StationSubsystem[] linkedSubsystems;
        [SerializeField] private Material inactiveMaterial;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material blockedMaterial;
        [SerializeField] private StationFeedbackUI feedbackUI;

        private Renderer objectRenderer;
        private bool activated;

        public bool IsActivated => activated;
        public override InteractionInputAction InteractionAction => InteractionInputAction.Use;
        public override int InteractionPriority => 40;

        public override string InteractionPrompt
        {
            get
            {
                if (activated)
                {
                    return $"{terminalName}: active";
                }

                return RequirementsMet() ? $"E: Activate {terminalName}" : $"{terminalName}: progress required";
            }
        }

        private void Awake()
        {
            objectRenderer = GetComponentInChildren<Renderer>();
        }

        private void Start()
        {
            ApplyDoorState(false);
            ApplyMaterial();
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (activated)
            {
                ShowMessage($"{terminalName} already active.");
                return;
            }

            if (!RequirementsMet())
            {
                ShowMessage(GetBlockedReason());
                ApplyMaterial();
                return;
            }

            activated = true;
            ApplyDoorState(true);
            ActivateSubsystems();
            ApplyMaterial();
            ShowMessage($"{terminalName} activated. Access granted.");
        }

        private bool RequirementsMet()
        {
            if (requiredNodes != null)
            {
                for (int i = 0; i < requiredNodes.Length; i++)
                {
                    if (requiredNodes[i] != null && !requiredNodes[i].IsStabilized)
                    {
                        return false;
                    }
                }
            }

            if (requiredStableZones != null)
            {
                for (int i = 0; i < requiredStableZones.Length; i++)
                {
                    if (requiredStableZones[i] != null && !requiredStableZones[i].IsStabilized)
                    {
                        return false;
                    }
                }
            }

            if (requiredActiveFlows != null)
            {
                for (int i = 0; i < requiredActiveFlows.Length; i++)
                {
                    if (requiredActiveFlows[i] != null && !requiredActiveFlows[i].IsActive)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private string GetBlockedReason()
        {
            if (requiredNodes != null)
            {
                for (int i = 0; i < requiredNodes.Length; i++)
                {
                    if (requiredNodes[i] != null && !requiredNodes[i].IsStabilized)
                    {
                        return $"{terminalName}: stabilize reactor section first.";
                    }
                }
            }

            if (requiredStableZones != null)
            {
                for (int i = 0; i < requiredStableZones.Length; i++)
                {
                    if (requiredStableZones[i] != null && !requiredStableZones[i].IsStabilized)
                    {
                        return $"{terminalName}: cool the linked zone first.";
                    }
                }
            }

            if (requiredActiveFlows != null)
            {
                for (int i = 0; i < requiredActiveFlows.Length; i++)
                {
                    if (requiredActiveFlows[i] != null && !requiredActiveFlows[i].IsActive)
                    {
                        return $"{terminalName}: coolant flow is not routed.";
                    }
                }
            }

            return $"{terminalName}: access denied.";
        }

        private void ApplyDoorState(bool accessGranted)
        {
            if (linkedDoors == null)
            {
                return;
            }

            for (int i = 0; i < linkedDoors.Length; i++)
            {
                DoorInteractable door = linkedDoors[i];
                if (door == null)
                {
                    continue;
                }

                door.SetLocked(!accessGranted);
                door.SetOpen(accessGranted);
            }
        }

        private void ActivateSubsystems()
        {
            if (linkedSubsystems == null)
            {
                return;
            }

            for (int i = 0; i < linkedSubsystems.Length; i++)
            {
                if (linkedSubsystems[i] != null)
                {
                    linkedSubsystems[i].Activate();
                }
            }
        }

        private void ApplyMaterial()
        {
            if (objectRenderer == null)
            {
                return;
            }

            Material targetMaterial = activated ? activeMaterial : inactiveMaterial;
            if (!activated && !RequirementsMet())
            {
                targetMaterial = blockedMaterial;
            }

            if (targetMaterial != null)
            {
                objectRenderer.sharedMaterial = targetMaterial;
            }
        }

        private void ShowMessage(string message)
        {
            if (feedbackUI != null)
            {
                feedbackUI.ShowMessage(message);
            }
            else
            {
                Debug.Log(message);
            }
        }
    }
}
