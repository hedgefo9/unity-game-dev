using UnityEngine;

namespace ReactorTechnician
{
    public sealed class PlayerHeatExposure : MonoBehaviour
    {
        [SerializeField] private float maxSuitIntegrity = 100f;
        [SerializeField] private float heatDamagePerSecond = 10f;
        [SerializeField] private float recoveryPerSecond = 8f;
        [SerializeField] private StationFeedbackUI feedbackUI;

        private CoolingZoneState currentZone;
        private float suitIntegrity;
        private bool incapacitated;

        public float SuitIntegrity => suitIntegrity;
        public bool IsIncapacitated => incapacitated;

        private void Awake()
        {
            suitIntegrity = maxSuitIntegrity;
        }

        private void Update()
        {
            if (incapacitated)
            {
                return;
            }

            if (currentZone != null && currentZone.IsDangerous)
            {
                float damage = heatDamagePerSecond * Mathf.Max(0.35f, currentZone.Danger01) * Time.deltaTime;
                suitIntegrity = Mathf.Max(0f, suitIntegrity - damage);
                SetExposure($"Heat exposure: {Mathf.RoundToInt(suitIntegrity)}% suit integrity");

                if (suitIntegrity <= 0f)
                {
                    incapacitated = true;
                    ShowMessage("Technician incapacitated by heat exposure.");
                    feedbackUI?.ShowFailure("The technician stayed too long in an overheated compartment.\nPress R to restart the level.");
                }
            }
            else if (suitIntegrity < maxSuitIntegrity)
            {
                suitIntegrity = Mathf.Min(maxSuitIntegrity, suitIntegrity + recoveryPerSecond * Time.deltaTime);
                SetExposure(suitIntegrity >= maxSuitIntegrity ? string.Empty : $"Recovering: {Mathf.RoundToInt(suitIntegrity)}%");
            }
            else
            {
                SetExposure(string.Empty);
            }
        }

        public void EnterZone(CoolingZoneState zone)
        {
            currentZone = zone;
            if (zone != null && zone.IsDangerous)
            {
                ShowMessage($"Warning: {zone.ZoneName} is overheated.");
            }
        }

        public void ExitZone(CoolingZoneState zone)
        {
            if (currentZone == zone)
            {
                currentZone = null;
            }
        }

        public void SetFeedbackUI(StationFeedbackUI feedback)
        {
            feedbackUI = feedback;
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

        private void SetExposure(string status)
        {
            if (feedbackUI != null)
            {
                feedbackUI.SetExposureStatus(status);
            }
        }
    }
}
