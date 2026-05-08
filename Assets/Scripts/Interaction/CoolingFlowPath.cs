using System;
using UnityEngine;

namespace ReactorTechnician
{
    public sealed class CoolingFlowPath : MonoBehaviour
    {
        [Serializable]
        private struct ValveCondition
        {
            public ValveInteractable valve;
            public bool requiredOpen;
        }

        [SerializeField] private string pathName = "Cooling Flow";
        [SerializeField] private ValveCondition[] valveConditions;
        [SerializeField] private CoolingZoneState targetZone;
        [SerializeField] private Renderer[] pipeRenderers;
        [SerializeField] private Material inactiveMaterial;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private float coolingPerSecond = 10f;
        [SerializeField] private StationFeedbackUI feedbackUI;

        private bool active;

        public bool IsActive => active;

        private void OnEnable()
        {
            SubscribeToValves(true);
        }

        private void OnDisable()
        {
            SubscribeToValves(false);
        }

        private void Start()
        {
            UpdateState(true);
        }

        private void Update()
        {
            UpdateState(false);

            if (active && targetZone != null)
            {
                targetZone.ApplyCooling(coolingPerSecond * Time.deltaTime);
            }
        }

        public void RefreshState()
        {
            UpdateState(false);
        }

        public void ConfigurePipeRenderers(Renderer[] renderers)
        {
            pipeRenderers = renderers;
            ApplyMaterial();
        }

        public string GetDiagnosticStatus()
        {
            if (valveConditions == null || valveConditions.Length == 0)
            {
                return $"{pathName}: no valve route configured";
            }

            string status = $"{pathName}: ";
            for (int i = 0; i < valveConditions.Length; i++)
            {
                ValveInteractable valve = valveConditions[i].valve;
                string valveName = valve == null ? "Missing valve" : valve.ValveName;
                bool currentOpen = valve != null && valve.IsOpen;
                bool requiredOpen = valveConditions[i].requiredOpen;
                string result = currentOpen == requiredOpen ? "OK" : "WAIT";
                string desired = requiredOpen ? "OPEN" : "CLOSED";
                status += $"{valveName} {desired} {result}";
                if (i < valveConditions.Length - 1)
                {
                    status += " / ";
                }
            }

            return status;
        }

        private void UpdateState(bool force)
        {
            bool nextActive = ConditionsMet();
            if (!force && nextActive == active)
            {
                return;
            }

            active = nextActive;
            ApplyMaterial();

            string state = active ? "ACTIVE" : "BLOCKED";
            string message = active ? $"{pathName}: {state}" : GetDiagnosticStatus();
            if (feedbackUI != null)
            {
                feedbackUI.SetFlowStatus(message);
                if (!force)
                {
                    feedbackUI.ShowMessage(message);
                }
            }
            else if (!force)
            {
                Debug.Log(message);
            }
        }

        private void HandleValveChanged(ValveInteractable valve, bool isOpen)
        {
            UpdateState(false);
        }

        private void SubscribeToValves(bool subscribe)
        {
            if (valveConditions == null)
            {
                return;
            }

            for (int i = 0; i < valveConditions.Length; i++)
            {
                ValveInteractable valve = valveConditions[i].valve;
                if (valve == null)
                {
                    continue;
                }

                if (subscribe)
                {
                    valve.ValveChanged += HandleValveChanged;
                }
                else
                {
                    valve.ValveChanged -= HandleValveChanged;
                }
            }
        }

        private bool ConditionsMet()
        {
            if (valveConditions == null || valveConditions.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < valveConditions.Length; i++)
            {
                ValveInteractable valve = valveConditions[i].valve;
                if (valve == null || valve.IsOpen != valveConditions[i].requiredOpen)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyMaterial()
        {
            Material targetMaterial = active ? activeMaterial : inactiveMaterial;
            if (targetMaterial == null || pipeRenderers == null)
            {
                return;
            }

            for (int i = 0; i < pipeRenderers.Length; i++)
            {
                if (pipeRenderers[i] != null)
                {
                    pipeRenderers[i].sharedMaterial = targetMaterial;
                }
            }
        }
    }
}
