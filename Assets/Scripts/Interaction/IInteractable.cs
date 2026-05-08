namespace ReactorTechnician
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        InteractionInputAction InteractionAction { get; }
        int InteractionPriority { get; }

        void Interact(PlayerInteractor interactor);
    }
}
