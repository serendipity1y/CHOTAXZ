using System;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Manages the player's current state (Yin/Yang) and handles state switching.
    /// Respects instability lock and provides both manual and forced switching.
    /// </summary>
    public class PlayerStateSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlayerState initialState = PlayerState.Yin;
        [SerializeField] private float instabilityDuration = 0.7f;

        // Current state
        private PlayerState _currentState;
        private bool _isSwitchLocked;
        private bool _isSwitching;
        private float _instabilityTimer;

        // Events
        public event Action<PlayerState> OnStateChanged;
        public event Action<PlayerState, PlayerState> OnStateChangedWithPrevious;
        public event Action OnInstabilityStarted;
        public event Action OnInstabilityEnded;

        // Public properties
        public PlayerState CurrentState => _currentState;
        public bool IsSwitchLocked => _isSwitchLocked;
        public bool IsInInstability => _instabilityTimer > 0f;
        public float InstabilityTimeRemaining => _instabilityTimer;

        private void Awake()
        {
            _currentState = initialState;
        }

        private void Update()
        {
            UpdateInstability();
        }

        /// <summary>
        /// Attempts a manual state switch. Respects instability lock.
        /// </summary>
        /// <returns>True if switch was successful, false otherwise.</returns>
        public bool TrySwitch()
        {
            if (_isSwitchLocked || _isSwitching || IsInInstability)
            {
                return false;
            }

            PlayerState newState = GetOppositeState(_currentState);
            ExecuteSwitch(newState, isForced: false);
            return true;
        }

        /// <summary>
        /// Forces a state switch (called by PeakSystem). Ignores lock but not if already switching.
        /// </summary>
        /// <returns>True if switch was executed, false if already switching.</returns>
        public bool ForceSwitch()
        {
            if (_isSwitching)
            {
                return false;
            }

            PlayerState newState = GetOppositeState(_currentState);
            ExecuteSwitch(newState, isForced: true);
            return true;
        }

        /// <summary>
        /// Starts the instability phase, locking state switching.
        /// </summary>
        public void StartInstability()
        {
            if (IsInInstability) return;

            _instabilityTimer = instabilityDuration;
            _isSwitchLocked = true;
            OnInstabilityStarted?.Invoke();
        }

        /// <summary>
        /// Sets the switch lock state. Used by external systems if needed.
        /// </summary>
        public void SetSwitchLock(bool locked)
        {
            _isSwitchLocked = locked;
        }

        private void ExecuteSwitch(PlayerState newState, bool isForced)
        {
            if (newState == _currentState) return;

            _isSwitching = true;
            PlayerState previousState = _currentState;
            _currentState = newState;
            _isSwitching = false;

            OnStateChangedWithPrevious?.Invoke(previousState, _currentState);
            OnStateChanged?.Invoke(_currentState);
        }

        private void UpdateInstability()
        {
            if (!IsInInstability) return;

            _instabilityTimer -= Time.deltaTime;

            if (_instabilityTimer <= 0f)
            {
                _instabilityTimer = 0f;
                _isSwitchLocked = false;
                OnInstabilityEnded?.Invoke();
            }
        }

        private PlayerState GetOppositeState(PlayerState state)
        {
            return state == PlayerState.Yin ? PlayerState.Yang : PlayerState.Yin;
        }

        /// <summary>
        /// Resets the system to its initial state.
        /// </summary>
        public void Reset()
        {
            _currentState = initialState;
            _isSwitchLocked = false;
            _isSwitching = false;
            _instabilityTimer = 0f;
        }
    }
}
