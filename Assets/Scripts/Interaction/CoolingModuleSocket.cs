using UnityEngine;

namespace ReactorTechnician
{
    public sealed class CoolingModuleSocket : InteractableObject
    {
        [SerializeField] private Transform moduleAnchor;
        [SerializeField] private ReactorSectionNode linkedNode;
        [SerializeField] private CoolingZoneState linkedCoolingZone;
        [SerializeField] private float moduleCoolingAmount = 20f;
        [SerializeField] private Material emptyMaterial;
        [SerializeField] private Material filledMaterial;

        private Renderer objectRenderer;
        private CoolingModuleInteractable installedModule;

        public Transform ModuleAnchor => moduleAnchor == null ? transform : moduleAnchor;
        public bool HasModule => installedModule != null;
        public override InteractionInputAction InteractionAction => InteractionInputAction.Module;
        public override int InteractionPriority => HasModule ? -10 : 100;

        public override string InteractionPrompt => HasModule ? "Cooling module installed" : "Q: Install cooling module";

        private void Awake()
        {
            objectRenderer = GetComponentInChildren<Renderer>();
            if (moduleAnchor == null)
            {
                moduleAnchor = transform;
            }

            ApplyMaterial();
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (HasModule)
            {
                return;
            }

            PlayerModuleCarrier carrier = interactor.GetComponent<PlayerModuleCarrier>();
            if (carrier == null)
            {
                Debug.LogWarning($"{name}: player has no PlayerModuleCarrier component.");
                return;
            }

            carrier.TryInstallAt(this);
        }

        public void Install(CoolingModuleInteractable module)
        {
            if (module == null || HasModule)
            {
                return;
            }

            installedModule = module;
            installedModule.MarkInstalled(this);
            SetInteractionEnabled(false);
            ApplyMaterial();

            if (linkedNode != null)
            {
                linkedNode.RegisterInstalledModule(this);
            }

            if (linkedCoolingZone != null)
            {
                linkedCoolingZone.ApplyCooling(moduleCoolingAmount);
            }
        }

        private void ApplyMaterial()
        {
            if (objectRenderer == null)
            {
                return;
            }

            Material targetMaterial = HasModule ? filledMaterial : emptyMaterial;
            if (targetMaterial != null)
            {
                objectRenderer.sharedMaterial = targetMaterial;
            }
        }
    }
}
