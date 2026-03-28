using System;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Handles peak effects (Shockwave, PhaseWave) and position validation.
    /// Fires events for VFX systems to hook into.
    /// </summary>
    public class PeakEffectsHandler : MonoBehaviour
    {
        [Header("Shockwave Settings (Yin -> Yang)")]
        [SerializeField] private float shockwaveRadius = 5f;
        [SerializeField] private LayerMask destructibleLayer;

        [Header("PhaseWave Settings (Yang -> Yin)")]
        [SerializeField] private float phaseWaveRadius = 8f;
        [SerializeField] private float phaseWaveDuration = 3f;
        [SerializeField] private LayerMask trapLayer;

        [Header("Position Validation")]
        [SerializeField] private float positionCheckRadius = 0.3f;
        [SerializeField] private LayerMask solidObjectsLayer;

        private Transform _playerTransform;

        // Events for VFX hooks
        public event Action<Vector3, float> OnShockwaveTriggered; // position, radius
        public event Action<Vector3, float, float> OnPhaseWaveTriggered; // position, radius, duration
        public event Action OnInvalidPositionDetected;

        /// <summary>
        /// Initialize with player transform reference.
        /// </summary>
        public void Initialize(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        /// <summary>
        /// Executes the appropriate peak effect based on the previous state.
        /// </summary>
        /// <param name="previousState">The state the player was in before the peak switch.</param>
        public void ExecutePeakEffect(PlayerState previousState)
        {
            if (_playerTransform == null) return;

            if (previousState == PlayerState.Yin)
            {
                // Yin -> Yang: Shockwave (destroys nearby destructibles)
                ExecuteShockwave();
            }
            else
            {
                // Yang -> Yin: PhaseWave (disables traps)
                ExecutePhaseWave();
            }
        }

        private void ExecuteShockwave()
        {
            Vector3 position = _playerTransform.position;

            // Fire event for VFX
            OnShockwaveTriggered?.Invoke(position, shockwaveRadius);

            // Apply effect to destructible objects
            Collider[] hitColliders = Physics.OverlapSphere(
                position,
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
            Vector3 position = _playerTransform.position;

            // Fire event for VFX
            OnPhaseWaveTriggered?.Invoke(position, phaseWaveRadius, phaseWaveDuration);

            // Apply effect to disableable objects (traps)
            Collider[] hitColliders = Physics.OverlapSphere(
                position,
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
        public bool ValidatePosition()
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
                OnInvalidPositionDetected?.Invoke();
                return false;
            }

            return true;
        }
    }
}
