using UnityEngine;

namespace ReactorTechnician
{
    public sealed class DoorInteractable : InteractableObject
    {
        [SerializeField] private bool startsOpen;
        [SerializeField] private bool locked;
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 3.2f, 0f);
        [SerializeField] private float moveSpeed = 6f;

        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool isOpen;

        public bool IsOpen => isOpen;
        public bool IsLocked => locked;
        public override InteractionInputAction InteractionAction => InteractionInputAction.Use;
        public override int InteractionPriority => 10;

        private void Awake()
        {
            closedPosition = transform.position;
            openPosition = closedPosition + openOffset;
            SetOpen(startsOpen, true);
        }

        private void Update()
        {
            Vector3 targetPosition = isOpen ? openPosition : closedPosition;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }

        public override string InteractionPrompt
        {
            get
            {
                if (locked)
                {
                    return "E: Door locked";
                }

                return isOpen ? "E: Close door" : "E: Open door";
            }
        }

        public void SetOpen(bool open)
        {
            SetOpen(open, false);
        }

        public void SetOpen(bool open, bool instant)
        {
            isOpen = open;
            if (instant)
            {
                transform.position = isOpen ? openPosition : closedPosition;
            }
        }

        public void SetLocked(bool value)
        {
            locked = value;
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (locked)
            {
                Debug.Log($"{name}: door is locked.");
                return;
            }

            SetOpen(!isOpen);
            Debug.Log($"{name}: door {(isOpen ? "opened" : "closed")}.");
        }
    }
}
