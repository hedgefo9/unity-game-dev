using UnityEngine;

namespace ReactorTechnician
{
    public sealed class ReactorOverheatManager : MonoBehaviour
    {
        [SerializeField] private CoolingZoneState[] monitoredZones;
        [SerializeField] private float riskIncreasePerSecond = 0.08f;
        [SerializeField] private float riskRecoveryPerSecond = 0.04f;
        [SerializeField] private StationFeedbackUI feedbackUI;

        private float globalRisk;
        private bool criticalFailure;

        public float GlobalRisk => globalRisk;
        public bool CriticalFailure => criticalFailure;

        private void Awake()
        {
            if (monitoredZones == null || monitoredZones.Length == 0)
            {
                monitoredZones = FindObjectsByType<CoolingZoneState>();
            }
        }

        private void Update()
        {
            float danger = CalculateDanger();
            if (danger > 0f)
            {
                globalRisk += danger * riskIncreasePerSecond * Time.deltaTime;
            }
            else
            {
                globalRisk -= riskRecoveryPerSecond * Time.deltaTime;
            }

            globalRisk = Mathf.Clamp01(globalRisk);
            feedbackUI?.SetReactorRisk(globalRisk);
            feedbackUI?.SetTemperatureStatus(GetTemperatureStatus());

            if (!criticalFailure && globalRisk >= 1f)
            {
                criticalFailure = true;
                if (feedbackUI != null)
                {
                    feedbackUI.ShowMessage("Critical reactor overheating reached.");
                }
                else
                {
                    Debug.LogWarning("Critical reactor overheating reached.");
                }
            }
        }

        public void SetFeedbackUI(StationFeedbackUI feedback)
        {
            feedbackUI = feedback;
        }

        private float CalculateDanger()
        {
            if (monitoredZones == null || monitoredZones.Length == 0)
            {
                return 0f;
            }

            float highestDanger = 0f;
            for (int i = 0; i < monitoredZones.Length; i++)
            {
                CoolingZoneState zone = monitoredZones[i];
                if (zone != null && !zone.IsStabilized)
                {
                    highestDanger = Mathf.Max(highestDanger, zone.Danger01);
                }
            }

            return highestDanger;
        }

        private string GetTemperatureStatus()
        {
            if (monitoredZones == null || monitoredZones.Length == 0)
            {
                return string.Empty;
            }

            CoolingZoneState hottestZone = null;
            for (int i = 0; i < monitoredZones.Length; i++)
            {
                CoolingZoneState zone = monitoredZones[i];
                if (zone == null)
                {
                    continue;
                }

                if (hottestZone == null || zone.CurrentTemperature > hottestZone.CurrentTemperature)
                {
                    hottestZone = zone;
                }
            }

            if (hottestZone == null)
            {
                return string.Empty;
            }

            return $"{hottestZone.ZoneName}: {Mathf.RoundToInt(hottestZone.CurrentTemperature)}C";
        }
    }
}
