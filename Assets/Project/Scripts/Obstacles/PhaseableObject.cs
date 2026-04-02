using UnityEngine;
using Player;

namespace Obstacles
{
    /// <summary>
    /// Object that can only be passed through in Yin state.
    /// Blocks player in Yang state.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PhaseableObject : MonoBehaviour, IStateInteractable
    {
        private Collider _collider;
        private PlayerController _player;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void Start()
        {
            // Find player and subscribe to state changes
            _player = GameManager.Instance.Player;
            if (_player != null)
            {
                _player.StateSystem.OnStateChanged += OnPlayerStateChanged;
                UpdateCollision(_player.StateSystem.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.StateSystem.OnStateChanged -= OnPlayerStateChanged;
            }
        }

        private void OnPlayerStateChanged(PlayerState newState)
        {
            UpdateCollision(newState);
        }

        private void UpdateCollision(PlayerState state)
        {
            if (_player == null) return;

            // Ignore collision when player is in Yin state
            var playerCollider = _player.GetComponent<Collider>();
            if (playerCollider != null)
            {
                Physics.IgnoreCollision(_collider, playerCollider, state == PlayerState.Yin);
            }

            var characterController = _player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                Physics.IgnoreCollision(_collider, characterController, state == PlayerState.Yin);
            }
        }

        public void Interact(PlayerState state)
        {
            // Phaseable objects don't have active interaction
            // Their behavior is passive (collision-based)
        }
    }
}
