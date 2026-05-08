using UnityEngine;

namespace ReactorTechnician
{
    public sealed class ReactorSectionNode : MonoBehaviour
    {
        [SerializeField] private string sectionName = "Cooling Section";
        [SerializeField] private int requiredModules = 1;
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Material unstableMaterial;
        [SerializeField] private Material stabilizedMaterial;
        [SerializeField] private StationFeedbackUI feedbackUI;

        private int installedModules;
        private bool stabilized;

        public bool IsStabilized => stabilized;

        private void Awake()
        {
            ApplyMaterial();
        }

        public void RegisterInstalledModule(CoolingModuleSocket socket)
        {
            if (stabilized)
            {
                return;
            }

            installedModules++;
            if (installedModules >= requiredModules)
            {
                stabilized = true;
                ShowMessage($"{sectionName} stabilized.");
            }
            else
            {
                ShowMessage($"{sectionName}: {installedModules}/{requiredModules} modules installed.");
            }

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
                statusRenderer = GetComponentInChildren<Renderer>();
            }

            if (statusRenderer == null)
            {
                return;
            }

            Material targetMaterial = stabilized ? stabilizedMaterial : unstableMaterial;
            if (targetMaterial != null)
            {
                statusRenderer.sharedMaterial = targetMaterial;
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
