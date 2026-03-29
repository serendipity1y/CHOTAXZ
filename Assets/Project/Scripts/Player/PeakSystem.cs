using System;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Handles the peak meter system that forces state switches when full.
    /// Only manages the meter - does NOT directly call other systems.
    /// Communicates via events only.
    /// </summary>
    public class PeakSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float peakFillRate = 0.1f;
        [SerializeField] private float maxPeakValue = 1f;

        // State
        private float _currentPeakValue;
        private bool _isPeakInProgress;
        private bool _isActive = true;
        private bool _isPaused;

        // Events - these are the ONLY way PeakSystem communicates
        public event Action<float> OnPeakMeterChanged; // 0-1 normalized value
        public event Action OnPeakReached; // Fired when peak threshold is hit

        // Public properties
        public float CurrentPeakValue => _currentPeakValue;
        public float MaxPeakValue => maxPeakValue; 
        public float NormalizedPeakValue => _currentPeakValue / maxPeakValue;
        public bool IsPeakInProgress => _isPeakInProgress;
        public bool IsActive => _isActive;

        private void Update()
        {
            if (!_isActive || _isPeakInProgress || _isPaused) return;

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

            _isPeakInProgress = true;

            // Fire event - let listeners handle what happens next
            OnPeakReached?.Invoke();
        }

        /// <summary>
        /// Signals that peak handling is complete. Called by PlayerController after processing.
        /// </summary>
        public void CompletePeak()
        {
            ResetMeter();
            _isPeakInProgress = false;
        }

        /// <summary>
        /// Aborts the current peak (e.g., if player dies during peak).
        /// </summary>
        public void AbortPeak()
        {
            _isPeakInProgress = false;
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
        /// Pauses the peak meter accumulation.
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// Resumes the peak meter accumulation.
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
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
            _isPaused = false;
            OnPeakMeterChanged?.Invoke(0f);
        }
    }
}
