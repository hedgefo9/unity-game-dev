using UnityEngine;
using UnityEngine.Events;

namespace ReactorTechnician
{
    public sealed class ValveInteractable : InteractableObject
    {
        [SerializeField] private string valveName = "Valve";
        [SerializeField] private Transform valveWheel;
        [SerializeField] private float turnAngle = 90f;
        [SerializeField] private bool startsOpen;
        [SerializeField] private UnityEvent<bool> onValveChanged;

        private bool open;

        public string ValveName => valveName;
        public bool IsOpen => open;
        public event System.Action<ValveInteractable, bool> ValveChanged;
        public override InteractionInputAction InteractionAction => InteractionInputAction.Valve;
        public override int InteractionPriority => 20;
        public override string InteractionPrompt => open ? $"F: Close {valveName}" : $"F: Open {valveName}";

        private void Awake()
        {
            if (valveWheel == null)
            {
                valveWheel = transform;
            }

            open = startsOpen;
            ApplyRotation();
        }

        public void SetOpen(bool value)
        {
            if (open == value)
            {
                return;
            }

            open = value;
            ApplyRotation();
            onValveChanged?.Invoke(open);
            ValveChanged?.Invoke(this, open);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            SetOpen(!open);
            Debug.Log($"{valveName}: cooling valve {(open ? "opened" : "closed")}.");
        }

        private void ApplyRotation()
        {
            if (valveWheel != null)
            {
                valveWheel.localRotation = Quaternion.Euler(0f, open ? turnAngle : 0f, 0f);
            }
        }
    }
}
