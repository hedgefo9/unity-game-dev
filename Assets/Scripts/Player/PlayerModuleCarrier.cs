using System.Collections.Generic;
using UnityEngine;

namespace ReactorTechnician
{
    public sealed class PlayerModuleCarrier : MonoBehaviour
    {
        [SerializeField] private int capacity = 2;
        [SerializeField] private Transform carryAnchor;
        [SerializeField] private StationFeedbackUI feedbackUI;

        private readonly List<CoolingModuleInteractable> carriedModules = new List<CoolingModuleInteractable>();

        public int Capacity => capacity;
        public int Count => carriedModules.Count;
        public bool HasModule => carriedModules.Count > 0;

        private void Awake()
        {
            if (carryAnchor == null)
            {
                GameObject anchor = new GameObject("Cooling Module Carry Anchor");
                anchor.transform.SetParent(transform);
                anchor.transform.localPosition = new Vector3(0f, 1.35f, 0.55f);
                anchor.transform.localRotation = Quaternion.identity;
                carryAnchor = anchor.transform;
            }
        }

        private void Start()
        {
            UpdateInventoryUI();
        }

        public bool TryPickUp(CoolingModuleInteractable module)
        {
            if (module == null || module.State != CoolingModuleInteractable.ModuleState.Stored)
            {
                return false;
            }

            if (carriedModules.Count >= capacity)
            {
                ShowMessage($"Module limit reached ({carriedModules.Count}/{capacity}).");
                return false;
            }

            carriedModules.Add(module);
            module.MarkCarried(this, carryAnchor, GetCarryOffset(carriedModules.Count - 1));
            ShowMessage($"Cooling module picked up ({carriedModules.Count}/{capacity}).");
            UpdateInventoryUI();
            return true;
        }

        public bool TryInstallAt(CoolingModuleSocket socket)
        {
            if (socket == null || socket.HasModule)
            {
                return false;
            }

            if (carriedModules.Count == 0)
            {
                ShowMessage("No cooling module carried.");
                return false;
            }

            CoolingModuleInteractable module = carriedModules[carriedModules.Count - 1];
            carriedModules.RemoveAt(carriedModules.Count - 1);
            socket.Install(module);

            ShowMessage($"Cooling module installed ({carriedModules.Count}/{capacity}).");
            UpdateInventoryUI();
            return true;
        }

        public void SetFeedbackUI(StationFeedbackUI feedback)
        {
            feedbackUI = feedback;
            UpdateInventoryUI();
        }

        private Vector3 GetCarryOffset(int index)
        {
            float side = index % 2 == 0 ? -0.32f : 0.32f;
            float row = index / 2;
            return new Vector3(side, row * 0.35f, 0f);
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

        private void UpdateInventoryUI()
        {
            if (feedbackUI != null)
            {
                feedbackUI.SetModuleCount(carriedModules.Count, capacity);
            }
        }
    }
}
