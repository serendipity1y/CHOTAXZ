using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Main player controller that coordinates all player systems.
    /// This is the ONLY class that wires systems together via events.
    /// Contains NO gameplay logic - only system coordination and input handling.
    /// </summary>
    [RequireComponent(typeof(PlayerStateSystem))]
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(PeakSystem))]
    [RequireComponent(typeof(InteractionSystem))]
    [RequireComponent(typeof(PeakEffectsHandler))]
    public class PlayerController : MonoBehaviour
    {
        // System references
        private PlayerStateSystem _stateSystem;
        private HealthSystem _healthSystem;
        private PeakSystem _peakSystem;
        private InteractionSystem _interactionSystem;
        private PeakEffectsHandler _peakEffectsHandler;
        private PlayerMovement _movement;

        // Configuration
        [Header("State Movement Speeds")]
        [SerializeField] private float yinMoveSpeed = 3f;
        [SerializeField] private float yangMoveSpeed = 7f;

        [Header("Peak Settings")]
        [SerializeField] private int peakDamage = 1;

        // Track state for peak handling
        private PlayerState _stateBeforePeak;

        // Events for external systems
        public event Action<PlayerState> OnPlayerStateChanged;
        public event Action OnPlayerDied;
        public event Action OnPeakOccurred;

        private void Awake()
        {
            // Get system references
            GameManager.Instance.RegisterPlayer(this);
            _stateSystem = GetComponent<PlayerStateSystem>();
            _healthSystem = GetComponent<HealthSystem>();
            _peakSystem = GetComponent<PeakSystem>();
            _interactionSystem = GetComponent<InteractionSystem>();
            _peakEffectsHandler = GetComponent<PeakEffectsHandler>();
            _movement = GetComponent<PlayerMovement>();

            // Initialize systems with dependencies
            _interactionSystem.Initialize(_stateSystem, transform);
            _peakEffectsHandler.Initialize(transform);
        }

        private void OnEnable()
        {
            // Subscribe to system events for coordination
            _stateSystem.OnStateChanged += HandleStateChanged;
            _healthSystem.OnDeath += HandlePlayerDeath;
            _peakSystem.OnPeakReached += HandlePeakReached;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            _stateSystem.OnStateChanged -= HandleStateChanged;
            _healthSystem.OnDeath -= HandlePlayerDeath;
            _peakSystem.OnPeakReached -= HandlePeakReached;
        }

        /// <summary>
        /// Input handler for state switch (called by Input System).
        /// </summary>
        public void OnSwitch(InputValue value)
        {
            if (value.isPressed)
            {
                _stateSystem.TrySwitch();
                Debug.Log($"State changed to: {_stateSystem.CurrentState}");
            }
            
        }

        /// <summary>
        /// Handles state change events - updates movement speed and resets peak meter on manual switch.
        /// </summary>
        private void HandleStateChanged(PlayerState newState)
        {
            // Update movement speed
            if (_movement != null)
            {
                _movement.speed = newState == PlayerState.Yin ? yinMoveSpeed : yangMoveSpeed;
            }

            // Reset peak meter on manual switch (not during peak)
            if (!_peakSystem.IsPeakInProgress)
            {
                _peakSystem.ResetMeter();
            }

            // Forward event
            OnPlayerStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Handles peak reached event - orchestrates all peak-related actions.
        /// This is where all systems are coordinated.
        /// </summary>
        private void HandlePeakReached()
        {
            // Check if player is already dead
            if (_healthSystem.IsDead)
            {
                _peakSystem.AbortPeak();
                return;
            }

            // Store the state before switching (for peak effect determination)
            _stateBeforePeak = _stateSystem.CurrentState;

            // 1. Force state switch
            bool switchSucceeded = _stateSystem.ForceSwitch();
            if (!switchSucceeded)
            {
                _peakSystem.AbortPeak();
                return;
            }

            // 2. Apply peak damage
            _healthSystem.TakeDamage(peakDamage);

            // Check if player died from damage
            if (_healthSystem.IsDead)
            {
                _peakSystem.AbortPeak();
                return;
            }

            // 3. Start instability (switch lock)
            _stateSystem.StartInstability();

            // 4. Execute peak effect based on previous state
            _peakEffectsHandler.ExecutePeakEffect(_stateBeforePeak);

            // 5. Validate position (player might be inside solid geometry after switch)
            if (!_peakEffectsHandler.ValidatePosition())
            {
                // Invalid position - kill the player
                _healthSystem.Die();
                _peakSystem.AbortPeak();
                return;
            }

            // 6. Complete peak processing
            _peakSystem.CompletePeak();

            // Fire peak occurred event
            OnPeakOccurred?.Invoke();
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

            // Stop peak system
            _peakSystem.SetActive(false);

            // Forward event
            OnPlayerDied?.Invoke();

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
                _movement.speed = _stateSystem.CurrentState == PlayerState.Yin ? yinMoveSpeed : yangMoveSpeed;
            }
        }

        // Public accessors for other systems that need references
        public PlayerStateSystem StateSystem => _stateSystem;
        public HealthSystem HealthSystem => _healthSystem;
        public PeakSystem PeakSystem => _peakSystem;
        public InteractionSystem InteractionSystem => _interactionSystem;
        public PeakEffectsHandler PeakEffectsHandler => _peakEffectsHandler;
    }
}
