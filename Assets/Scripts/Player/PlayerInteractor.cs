using UnityEngine;

namespace ReactorTechnician
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private InteractionPromptUI promptUI;
        [SerializeField] private float interactDistance = 3.2f;
        [SerializeField] private float fallbackRadius = 1.6f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private IInteractable currentTarget;
        private readonly Collider[] nearbyHits = new Collider[32];

        private void Awake()
        {
            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }
        }

        private void Update()
        {
            currentTarget = FindTarget();
            promptUI?.SetPrompt(currentTarget == null ? string.Empty : currentTarget.InteractionPrompt);

            TryInteract(InteractionInputAction.Module, ReactorInput.ModulePressed);
            TryInteract(InteractionInputAction.Valve, ReactorInput.ValvePressed);
            TryInteract(InteractionInputAction.Use, ReactorInput.InteractPressed);
        }

        private void TryInteract(InteractionInputAction action, bool pressed)
        {
            if (!pressed)
            {
                return;
            }

            IInteractable target = FindTarget(action);
            if (target != null)
            {
                target.Interact(this);
            }
        }

        private IInteractable FindTarget()
        {
            IInteractable moduleTarget = FindTarget(InteractionInputAction.Module);
            if (moduleTarget != null && moduleTarget.InteractionPriority >= 100)
            {
                return moduleTarget;
            }

            IInteractable rayTarget = FindRayTarget();
            if (rayTarget != null)
            {
                return rayTarget;
            }

            return FindBestOverlapTarget(null);
        }

        private IInteractable FindTarget(InteractionInputAction action)
        {
            IInteractable rayTarget = FindRayTarget();
            if (rayTarget != null && rayTarget.InteractionAction == action)
            {
                return rayTarget;
            }

            return FindBestOverlapTarget(action);
        }

        private IInteractable FindBestOverlapTarget(InteractionInputAction? action)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, fallbackRadius, nearbyHits, interactionMask, QueryTriggerInteraction.Collide);
            float bestDistance = float.MaxValue;
            int bestPriority = int.MinValue;
            IInteractable bestTarget = null;

            for (int i = 0; i < hitCount; i++)
            {
                IInteractable interactable = nearbyHits[i].GetComponentInParent<IInteractable>();
                if (interactable == null)
                {
                    continue;
                }

                if (action.HasValue && interactable.InteractionAction != action.Value)
                {
                    continue;
                }

                int priority = interactable.InteractionPriority;
                float distance = Vector3.SqrMagnitude(nearbyHits[i].transform.position - transform.position);
                if (priority > bestPriority || (priority == bestPriority && distance < bestDistance))
                {
                    bestPriority = priority;
                    bestDistance = distance;
                    bestTarget = interactable;
                }
            }

            return bestTarget != null ? bestTarget : FindBestTransformTarget(action);
        }

        private IInteractable FindBestTransformTarget(InteractionInputAction? action)
        {
            float bestDistance = fallbackRadius * fallbackRadius;
            int bestPriority = int.MinValue;
            IInteractable bestTarget = null;
            InteractableObject[] interactables = FindObjectsByType<InteractableObject>(FindObjectsInactive.Exclude);

            for (int i = 0; i < interactables.Length; i++)
            {
                InteractableObject interactable = interactables[i];
                if (interactable == null || !interactable.isActiveAndEnabled)
                {
                    continue;
                }

                if (action.HasValue && interactable.InteractionAction != action.Value)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(interactable.transform.position - transform.position);
                int priority = interactable.InteractionPriority;
                if (distance <= bestDistance && (priority > bestPriority || (priority == bestPriority && distance < bestDistance)))
                {
                    bestDistance = distance;
                    bestPriority = priority;
                    bestTarget = interactable;
                }
            }

            return bestTarget;
        }

        private IInteractable FindRayTarget()
        {
            if (interactionCamera == null)
            {
                return null;
            }

            Ray ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                return null;
            }

            return hit.collider.GetComponentInParent<IInteractable>();
        }
    }
}
