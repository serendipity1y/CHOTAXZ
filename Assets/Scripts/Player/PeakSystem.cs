using System;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Handles the peak meter system that forces state switches when full.
    /// Coordinates with other systems via events.
    /// </summary>
    public class PeakSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float peakFillRate = 0.1f; // Meter units per second
        [SerializeField] private float maxPeakValue = 1f;

        [Header("Peak Effects")]
        [SerializeField] private float shockwaveRadius = 5f;
        [SerializeField] private float phaseWaveDuration = 3f;
        [SerializeField] private float phaseWaveRadius = 8f;
        [SerializeField] private LayerMask destructibleLayer;
        [SerializeField] private LayerMask trapLayer;

        [Header("Position Validation")]
        [SerializeField] private LayerMask solidObjectsLayer;
        [SerializeField] private float positionCheckRadius = 0.3f;

        // References (injected via Initialize or Inspector)
        private PlayerStateSystem _stateSystem;
        private HealthSystem _healthSystem;
        private Transform _playerTransform;

        // State
        private float _currentPeakValue;
        private bool _isPeakInProgress;
        private bool _isActive = true;

        // Events
        public event Action<float> OnPeakMeterChanged; // 0-1 normalized value
        public event Action OnPeakTriggered;
        public event Action<PlayerState> OnPeakEffectTriggered; // The state we switched FROM

        // Public properties
        public float CurrentPeakValue => _currentPeakValue;
        public float NormalizedPeakValue => _currentPeakValue / maxPeakValue;
        public bool IsPeakInProgress => _isPeakInProgress;

        /// <summary>
        /// Initializes the system with required dependencies.
        /// </summary>
        public void Initialize(PlayerStateSystem stateSystem, HealthSystem healthSystem, Transform playerTransform)
        {
            _stateSystem = stateSystem;
            _healthSystem = healthSystem;
            _playerTransform = playerTransform;

            // Subscribe to state changes to reset meter on manual switch
            if (_stateSystem != null)
            {
                _stateSystem.OnStateChanged += OnPlayerStateChanged;
            }

            // Subscribe to death to stop the system
            if (_healthSystem != null)
            {
                _healthSystem.OnDeath += OnPlayerDeath;
            }
        }

        private void OnDestroy()
        {
            if (_stateSystem != null)
            {
                _stateSystem.OnStateChanged -= OnPlayerStateChanged;
            }

            if (_healthSystem != null)
            {
                _healthSystem.OnDeath -= OnPlayerDeath;
            }
        }

        private void Update()
        {
            if (!_isActive || _isPeakInProgress) return;
            if (_healthSystem != null && _healthSystem.IsDead) return;

            UpdatePeakMeter();
        }

        private void UpdatePeakMeter()
        {
            float previousValue = _currentPeakValue;
            _currentPeakValue += peakFillRate * Time.deltaTime;

            if (_currentPeakValue != previousValue)
            {
                OnPeakMeterChanged?.Invoke(NormalizedPeakValue);
            }

            if (_currentPeakValue >= maxPeakValue)
            {
                TriggerPeak();
            }
        }

        private void TriggerPeak()
        {
            if (_isPeakInProgress) return;
            if (_healthSystem != null && _healthSystem.IsDead) return;

            _isPeakInProgress = true;
            OnPeakTriggered?.Invoke();

            // Store the state we're switching FROM for the peak effect
            PlayerState previousState = _stateSystem != null ? _stateSystem.CurrentState : PlayerState.Yin;

            // 1. Force state switch
            bool switchSucceeded = _stateSystem != null && _stateSystem.ForceSwitch();

            if (!switchSucceeded)
            {
                // Already switching or system unavailable - abort peak
                _isPeakInProgress = false;
                return;
            }

            // 2. Apply damage
            _healthSystem?.TakeDamage(1);

            // Check if player died from damage
            if (_healthSystem != null && _healthSystem.IsDead)
            {
                _isPeakInProgress = false;
                return;
            }

            // 3. Start instability
            _stateSystem?.StartInstability();

            // 4. Trigger peak effect based on previous state
            ExecutePeakEffect(previousState);

            // 5. Validate position
            if (!ValidatePosition())
            {
                // Invalid position - kill the player
                _healthSystem?.Die();
                _isPeakInProgress = false;
                return;
            }

            // 6. Reset meter
            ResetMeter();

            _isPeakInProgress = false;
        }

        private void ExecutePeakEffect(PlayerState previousState)
        {
            OnPeakEffectTriggered?.Invoke(previousState);

            if (previousState == PlayerState.Yin)
            {
                // Yin → Yang: Shockwave (destroys nearby objects)
                ExecuteShockwave();
            }
            else
            {
                // Yang → Yin: Phase wave (disables traps)
                ExecutePhaseWave();
            }
        }

        private void ExecuteShockwave()
        {
            if (_playerTransform == null) return;

            Collider[] hitColliders = Physics.OverlapSphere(
                _playerTransform.position,
                shockwaveRadius,
                destructibleLayer
            );

            foreach (Collider hit in hitColliders)
            {
                IDestructible destructible = hit.GetComponent<IDestructible>();
                destructible?.Destroy();
            }
        }

        private void ExecutePhaseWave()
        {
            if (_playerTransform == null) return;

            Collider[] hitColliders = Physics.OverlapSphere(
                _playerTransform.position,
                phaseWaveRadius,
                trapLayer
            );

            foreach (Collider hit in hitColliders)
            {
                IDisableable disableable = hit.GetComponent<IDisableable>();
                disableable?.DisableTemporarily(phaseWaveDuration);
            }
        }

        /// <summary>
        /// Validates if the player is in a valid position after forced switch.
        /// Returns false if player is stuck inside solid geometry.
        /// </summary>
        private bool ValidatePosition()
        {
            if (_playerTransform == null) return true;

            // Check if player overlaps with solid objects
            Collider[] overlaps = Physics.OverlapSphere(
                _playerTransform.position,
                positionCheckRadius,
                solidObjectsLayer
            );

            // Filter out triggers and the player's own colliders
            foreach (Collider col in overlaps)
            {
                if (col.isTrigger) continue;
                if (col.transform.IsChildOf(_playerTransform) || col.transform == _playerTransform) continue;

                // Found a solid collider we're stuck in
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resets the peak meter to zero.
        /// </summary>
        public void ResetMeter()
        {
            _currentPeakValue = 0f;
            OnPeakMeterChanged?.Invoke(0f);
        }

        /// <summary>
        /// Called when player manually switches state - reset the meter.
        /// </summary>
        private void OnPlayerStateChanged(PlayerState newState)
        {
            // Only reset if not during a forced peak switch
            if (!_isPeakInProgress)
            {
                ResetMeter();
            }
        }

        /// <summary>
        /// Called when player dies - stop the system.
        /// </summary>
        private void OnPlayerDeath()
        {
            _isActive = false;
        }

        /// <summary>
        /// Sets the active state of the peak system.
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;
        }

        /// <summary>
        /// Resets the system to initial state.
        /// </summary>
        public void Reset()
        {
            _currentPeakValue = 0f;
            _isPeakInProgress = false;
            _isActive = true;
            OnPeakMeterChanged?.Invoke(0f);
        }
    }
}
