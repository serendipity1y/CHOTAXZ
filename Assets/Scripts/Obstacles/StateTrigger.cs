using UnityEngine;
using Player;

namespace Obstacles
{
    /// <summary>
    /// Trigger that can only be activated in Yang state.
    /// </summary>
    public class StateTrigger : MonoBehaviour, IStateInteractable
    {
        [Header("Configuration")]
        [SerializeField] private bool requireYangState = true;
        [SerializeField] private bool oneTimeActivation = true;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnActivated;

        private bool _hasBeenActivated;

        public void Interact(PlayerState state)
        {
            if (_hasBeenActivated && oneTimeActivation) return;

            bool canActivate = !requireYangState || state == PlayerState.Yang;

            if (canActivate)
            {
                Activate();
            }
        }

        private void Activate()
        {
            _hasBeenActivated = true;
            OnActivated?.Invoke();
        }

        public void ResetTrigger()
        {
            _hasBeenActivated = false;
        }

        public bool HasBeenActivated => _hasBeenActivated;
    }
}
