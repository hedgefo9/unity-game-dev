using UnityEngine;

namespace ReactorTechnician
{
    public sealed class TerminalInteractable : InteractableObject
    {
        [SerializeField] private DoorInteractable[] linkedDoors;
        [SerializeField] private Material inactiveMaterial;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private bool startsActivated;

        private Renderer objectRenderer;
        private bool activated;

        public bool IsActivated => activated;
        public override InteractionInputAction InteractionAction => InteractionInputAction.Use;
        public override int InteractionPriority => 30;

        public override string InteractionPrompt => activated ? "E: Deactivate terminal" : "E: Activate terminal";

        private void Awake()
        {
            objectRenderer = GetComponentInChildren<Renderer>();
            activated = startsActivated;
            ApplyState();
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            activated = !activated;
            ApplyState();
            Debug.Log($"{name}: terminal {(activated ? "activated" : "deactivated")}.");
        }

        private void ApplyState()
        {
            if (objectRenderer != null)
            {
                Material targetMaterial = activated ? activeMaterial : inactiveMaterial;
                if (targetMaterial != null)
                {
                    objectRenderer.sharedMaterial = targetMaterial;
                }
            }

            if (linkedDoors == null)
            {
                return;
            }

            for (int i = 0; i < linkedDoors.Length; i++)
            {
                if (linkedDoors[i] != null)
                {
                    linkedDoors[i].SetOpen(activated);
                }
            }
        }
    }
}
