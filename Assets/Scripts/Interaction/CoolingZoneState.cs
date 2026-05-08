using UnityEngine;

namespace ReactorTechnician
{
    public sealed class CoolingZoneState : MonoBehaviour
    {
        [SerializeField] private string zoneName = "Cooling Zone";
        [SerializeField] private float currentTemperature = 80f;
        [SerializeField] private float stableTemperature = 45f;
        [SerializeField] private float warningTemperature = 70f;
        [SerializeField] private float criticalTemperature = 100f;
        [SerializeField] private float heatingPerSecond = 1.5f;
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Material overheatedMaterial;
        [SerializeField] private Material coolingMaterial;
        [SerializeField] private Material stableMaterial;
        [SerializeField] private StationFeedbackUI feedbackUI;

        private bool stabilized;
        private float coolingVisualTimer;

        public string ZoneName => zoneName;
        public float CurrentTemperature => currentTemperature;
        public float StableTemperature => stableTemperature;
        public float WarningTemperature => warningTemperature;
        public float CriticalTemperature => criticalTemperature;
        public bool IsStabilized => stabilized;
        public bool IsDangerous => currentTemperature >= warningTemperature && !stabilized;
        public float Danger01 => Mathf.InverseLerp(warningTemperature, criticalTemperature, currentTemperature);

        private void Awake()
        {
            if (statusRenderer == null)
            {
                statusRenderer = GetComponentInChildren<Renderer>();
            }

            ApplyMaterial();
        }

        private void Update()
        {
            if (!stabilized)
            {
                currentTemperature = Mathf.Min(criticalTemperature, currentTemperature + heatingPerSecond * Time.deltaTime);
            }

            coolingVisualTimer = Mathf.Max(0f, coolingVisualTimer - Time.deltaTime);
            ApplyMaterial();
        }

        public void ApplyCooling(float amount)
        {
            if (stabilized)
            {
                return;
            }

            currentTemperature = Mathf.Max(0f, currentTemperature - amount);
            coolingVisualTimer = 0.25f;

            if (currentTemperature <= stableTemperature)
            {
                stabilized = true;
                currentTemperature = stableTemperature;
                ShowMessage($"{zoneName} stabilized by coolant flow.");
            }

            ApplyMaterial();
        }

        public void SetTemperature(float temperature)
        {
            currentTemperature = Mathf.Clamp(temperature, 0f, criticalTemperature);
            if (currentTemperature > stableTemperature)
            {
                stabilized = false;
            }

            ApplyMaterial();
        }

        public void SetHeatingPerSecond(float value)
        {
            heatingPerSecond = Mathf.Max(0f, value);
        }

        public void ConfigureTemperatures(float current, float stable, float warning, float critical, float heating)
        {
            stableTemperature = Mathf.Max(0f, stable);
            warningTemperature = Mathf.Max(stableTemperature, warning);
            criticalTemperature = Mathf.Max(warningTemperature + 1f, critical);
            heatingPerSecond = Mathf.Max(0f, heating);
            SetTemperature(current);
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

            Material targetMaterial = stabilized ? stableMaterial : overheatedMaterial;
            if (!stabilized && coolingVisualTimer > 0f)
            {
                targetMaterial = coolingMaterial;
            }

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
