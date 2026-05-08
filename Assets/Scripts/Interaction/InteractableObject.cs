using UnityEngine;
using UnityEngine.Events;

namespace ReactorTechnician
{
    public abstract class InteractableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Press E: Interact";
        [SerializeField] private string blockedPrompt = "Action unavailable";
        [SerializeField] private InteractionInputAction interactionAction = InteractionInputAction.Use;
        [SerializeField] private int interactionPriority;
        [SerializeField] private bool interactionEnabled = true;
        [SerializeField] private UnityEvent onInteracted;

        public virtual string InteractionPrompt => interactionEnabled ? interactionPrompt : blockedPrompt;
        public virtual InteractionInputAction InteractionAction => interactionAction;
        public virtual int InteractionPriority => interactionPriority;

        protected bool InteractionEnabled
        {
            get => interactionEnabled;
            set => interactionEnabled = value;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!interactionEnabled)
            {
                OnInteractionBlocked(interactor);
                return;
            }

            OnInteract(interactor);
            onInteracted?.Invoke();
        }

        public void SetPrompt(string prompt)
        {
            interactionPrompt = prompt;
        }

        public void SetBlockedPrompt(string prompt)
        {
            blockedPrompt = prompt;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
        }

        protected abstract void OnInteract(PlayerInteractor interactor);

        protected virtual void OnInteractionBlocked(PlayerInteractor interactor)
        {
            Debug.Log($"{name}: interaction blocked.");
        }
    }
}
