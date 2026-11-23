using UnityEngine;

namespace Canal420.Interaction
{
    public interface IInteractable
    {
        bool CanInteract { get; }

        string GetInteractionPrompt();

        void Interact(GameObject interactor);
    }
}
