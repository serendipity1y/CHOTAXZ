using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Handles player interaction with environment objects.
    /// Delegates state-specific behavior to IStateInteractable objects.
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float interactionRadius = 2f;
        [SerializeField] private LayerMask interactableLayer;


        // References
        private PlayerStateSystem _stateSystem;
        private Transform _playerTransform;

        // Events
        public event Action<IStateInteractable> OnInteraction;

        /// <summary>
        /// Initializes the system with required dependencies.
        /// </summary>
        public void Initialize(PlayerStateSystem stateSystem, Transform playerTransform)
        {
            _stateSystem = stateSystem;
            _playerTransform = playerTransform;
        }

        public void OnInteract(InputValue val)
        {
            if (val.isPressed)
            {
                TryInteract();
            }
        }

        /// <summary>
        /// Attempts to interact with nearby objects.
        /// </summary>
        public void TryInteract()
        {
            if (_playerTransform == null || _stateSystem == null) return;

            Collider[] hitColliders = Physics.OverlapSphere(
                _playerTransform.position,
                interactionRadius,
                interactableLayer
            );

            PlayerState currentState = _stateSystem.CurrentState;

            foreach (Collider hit in hitColliders)
            {
                IStateInteractable interactable = hit.GetComponent<IStateInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(currentState);
                    OnInteraction?.Invoke(interactable);
                }
            }
        }

        /// <summary>
        /// Gets all interactable objects within range.
        /// </summary>
        public IStateInteractable[] GetNearbyInteractables()
        {
            if (_playerTransform == null) return Array.Empty<IStateInteractable>();

            Collider[] hitColliders = Physics.OverlapSphere(
                _playerTransform.position,
                interactionRadius,
                interactableLayer
            );

            var interactables = new System.Collections.Generic.List<IStateInteractable>();
            foreach (Collider hit in hitColliders)
            {
                IStateInteractable interactable = hit.GetComponent<IStateInteractable>();
                if (interactable != null)
                {
                    interactables.Add(interactable);
                }
            }

            return interactables.ToArray();
        }
    }
}
