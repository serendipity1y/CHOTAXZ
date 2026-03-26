using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Main player controller that coordinates all player systems.
    /// Contains NO gameplay logic - only system coordination and input handling.
    /// </summary>
    [RequireComponent(typeof(PlayerStateSystem))]
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(PeakSystem))]
    [RequireComponent(typeof(InteractionSystem))]
    public class PlayerController : MonoBehaviour
    {
        // System references
        private PlayerStateSystem _stateSystem;
        private HealthSystem _healthSystem;
        private PeakSystem _peakSystem;
        private InteractionSystem _interactionSystem;
        private PlayerMovement _movement;

        // Configuration
        [Header("State Movement Speeds")]
        [SerializeField] private float yinMoveSpeed = 3f;
        [SerializeField] private float yangMoveSpeed = 7f;

        private void Awake()
        {
            // Get system references
            _stateSystem = GetComponent<PlayerStateSystem>();
            _healthSystem = GetComponent<HealthSystem>();
            _peakSystem = GetComponent<PeakSystem>();
            _interactionSystem = GetComponent<InteractionSystem>();
            _movement = GetComponent<PlayerMovement>();

            // Initialize systems with dependencies
            _peakSystem.Initialize(_stateSystem, _healthSystem, transform);
            _interactionSystem.Initialize(_stateSystem, transform);
        }

        private void OnEnable()
        {
            // Subscribe to system events for coordination
            _stateSystem.OnStateChanged += HandleStateChanged;
            _healthSystem.OnDeath += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            _stateSystem.OnStateChanged -= HandleStateChanged;
            _healthSystem.OnDeath -= HandlePlayerDeath;
        }

        /// <summary>
        /// Input handler for state switch (called by Input System).
        /// </summary>
        public void OnSwitch(InputValue value)
        {
            if (value.isPressed)
            {
                _stateSystem.TrySwitch();
            }
        }

        /// <summary>
        /// Handles state change events - updates movement speed.
        /// </summary>
        private void HandleStateChanged(PlayerState newState)
        {
            if (_movement != null)
            {
                _movement.speed = newState == PlayerState.Yin ? yinMoveSpeed : yangMoveSpeed;
            }
        }

        /// <summary>
        /// Handles player death - disable all systems.
        /// </summary>
        private void HandlePlayerDeath()
        {
            // Disable movement
            if (_movement != null)
            {
                _movement.enabled = false;
            }

            // Could trigger death animation, respawn logic, etc.
            Debug.Log("Player died!");
        }

        /// <summary>
        /// Resets all player systems to initial state.
        /// </summary>
        public void ResetPlayer()
        {
            _stateSystem.Reset();
            _healthSystem.Reset();
            _peakSystem.Reset();

            if (_movement != null)
            {
                _movement.enabled = true;
                _movement.speed = yinMoveSpeed; // Reset to initial state speed
            }
        }

        // Public accessors for other systems that need references
        public PlayerStateSystem StateSystem => _stateSystem;
        public HealthSystem HealthSystem => _healthSystem;
        public PeakSystem PeakSystem => _peakSystem;
        public InteractionSystem InteractionSystem => _interactionSystem;
    }
}
