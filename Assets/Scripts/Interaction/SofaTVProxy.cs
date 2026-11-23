using UnityEngine;

namespace Canal420.Interaction
{
    public class SofaTVProxy : MonoBehaviour, IInteractable
    {
        [SerializeField] private TVController targetTV;
        public bool CanInteract => targetTV != null && targetTV.IsWaitingToSitDay1;

        private void Awake()
        {
            if (targetTV == null)
            {
                targetTV = FindAnyObjectByType<TVController>();
            }
        }

        public string GetInteractionPrompt()
        {
            return targetTV != null ? targetTV.GetInteractionPrompt() : "Sentar no sofá e ligar a TV";
        }

        public void Interact(GameObject interactor)
        {
            if (targetTV != null && targetTV.IsWaitingToSitDay1)
            {
                targetTV.Interact(interactor);
            }
        }

        public void LinkTV(TVController tv)
        {
            targetTV = tv;
        }
    }
}
