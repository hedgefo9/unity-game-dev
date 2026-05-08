using UnityEngine;

namespace ReactorTechnician
{
    public sealed class StationSubsystem : MonoBehaviour
    {
        [SerializeField] private string subsystemName = "Station Subsystem";
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Material inactiveMaterial;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private StationFeedbackUI feedbackUI;

        private bool active;

        public string SubsystemName => subsystemName;
        public bool IsActive => active;

        private void Awake()
        {
            if (statusRenderer == null)
            {
                statusRenderer = GetComponentInChildren<Renderer>();
            }

            ApplyMaterial();
        }

        public void Activate()
        {
            if (active)
            {
                return;
            }

            active = true;
            ApplyMaterial();

            if (feedbackUI != null)
            {
                feedbackUI.ShowMessage($"{subsystemName} activated.");
            }
            else
            {
                Debug.Log($"{subsystemName} activated.");
            }
        }

        public void Deactivate()
        {
            if (!active)
            {
                return;
            }

            active = false;
            ApplyMaterial();
        }

        public void SetFeedbackUI(StationFeedbackUI feedback)
        {
            feedbackUI = feedback;
        }

        private void ApplyMaterial()
        {
            if (statusRenderer == null)
            {
                return;
            }

            Material targetMaterial = active ? activeMaterial : inactiveMaterial;
            if (targetMaterial != null)
            {
                statusRenderer.sharedMaterial = targetMaterial;
            }
        }
    }
}
