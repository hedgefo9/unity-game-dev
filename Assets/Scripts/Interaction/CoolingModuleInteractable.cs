using UnityEngine;

namespace ReactorTechnician
{
    public sealed class CoolingModuleInteractable : InteractableObject
    {
        public enum ModuleState
        {
            Stored,
            Carried,
            Installed
        }

        [SerializeField] private Material storedMaterial;
        [SerializeField] private Material carriedMaterial;
        [SerializeField] private Material installedMaterial;

        private Renderer objectRenderer;
        private Collider[] colliders;
        private PlayerModuleCarrier carrier;
        private ModuleState state;

        public ModuleState State => state;
        public bool IsInstalled => state == ModuleState.Installed;
        public bool IsCarried => state == ModuleState.Carried;
        public override InteractionInputAction InteractionAction => InteractionInputAction.Module;
        public override int InteractionPriority => 60;

        public override string InteractionPrompt
        {
            get
            {
                switch (state)
                {
                    case ModuleState.Stored:
                        return "Q: Pick up cooling module";
                    case ModuleState.Carried:
                        return "Cooling module carried";
                    default:
                        return "Cooling module installed";
                }
            }
        }

        private void Awake()
        {
            objectRenderer = GetComponentInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            ApplyMaterial();
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (state != ModuleState.Stored)
            {
                return;
            }

            PlayerModuleCarrier moduleCarrier = interactor.GetComponent<PlayerModuleCarrier>();
            if (moduleCarrier == null)
            {
                Debug.LogWarning($"{name}: player has no PlayerModuleCarrier component.");
                return;
            }

            moduleCarrier.TryPickUp(this);
        }

        public void MarkCarried(PlayerModuleCarrier newCarrier, Transform carryAnchor, Vector3 localOffset)
        {
            carrier = newCarrier;
            state = ModuleState.Carried;
            SetInteractionEnabled(false);
            SetCollidersEnabled(false);

            transform.SetParent(carryAnchor);
            transform.localPosition = localOffset;
            transform.localRotation = Quaternion.identity;

            ApplyMaterial();
        }

        public void MarkInstalled(CoolingModuleSocket socket)
        {
            carrier = null;
            state = ModuleState.Installed;
            SetInteractionEnabled(false);
            SetCollidersEnabled(false);

            transform.SetParent(socket.ModuleAnchor);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            ApplyMaterial();
        }

        public void ReturnToWorld(Vector3 position, Quaternion rotation)
        {
            carrier = null;
            state = ModuleState.Stored;
            SetInteractionEnabled(true);
            SetCollidersEnabled(true);

            transform.SetParent(null);
            transform.position = position;
            transform.rotation = rotation;

            ApplyMaterial();
        }

        private void SetCollidersEnabled(bool enabled)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }

        private void ApplyMaterial()
        {
            if (objectRenderer == null)
            {
                return;
            }

            Material targetMaterial = storedMaterial;
            if (state == ModuleState.Carried)
            {
                targetMaterial = carriedMaterial;
            }
            else if (state == ModuleState.Installed)
            {
                targetMaterial = installedMaterial;
            }

            if (targetMaterial != null)
            {
                objectRenderer.sharedMaterial = targetMaterial;
            }
        }
    }
}
