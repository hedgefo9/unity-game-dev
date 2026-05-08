using UnityEngine;

namespace ReactorTechnician
{
    [RequireComponent(typeof(Collider))]
    public sealed class HeatZoneTrigger : MonoBehaviour
    {
        [SerializeField] private CoolingZoneState zoneState;

        private void Awake()
        {
            Collider zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;

            if (zoneState == null)
            {
                zoneState = GetComponentInParent<CoolingZoneState>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerHeatExposure exposure = other.GetComponentInParent<PlayerHeatExposure>();
            if (exposure != null)
            {
                exposure.EnterZone(zoneState);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerHeatExposure exposure = other.GetComponentInParent<PlayerHeatExposure>();
            if (exposure != null)
            {
                exposure.ExitZone(zoneState);
            }
        }
    }
}
