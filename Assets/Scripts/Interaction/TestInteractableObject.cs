using UnityEngine;

namespace ReactorTechnician
{
    public sealed class TestInteractableObject : InteractableObject
    {
        [SerializeField] private string prompt = "Press E: Test station interaction";
        [SerializeField] private Material idleMaterial;
        [SerializeField] private Material activatedMaterial;

        private Renderer objectRenderer;
        private bool activated;

        public override string InteractionPrompt => activated ? "Press E: Reset test station" : prompt;

        private void Awake()
        {
            objectRenderer = GetComponentInChildren<Renderer>();
            ApplyMaterial();
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            activated = !activated;
            ApplyMaterial();
            Debug.Log(activated ? "Test interaction activated." : "Test interaction reset.");
        }

        private void ApplyMaterial()
        {
            if (objectRenderer == null)
            {
                return;
            }

            Material targetMaterial = activated ? activatedMaterial : idleMaterial;
            if (targetMaterial != null)
            {
                objectRenderer.sharedMaterial = targetMaterial;
            }
        }
    }
}
