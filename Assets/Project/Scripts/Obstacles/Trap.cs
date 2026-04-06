using UnityEngine;
using Player;
using System.Collections;

namespace Obstacles
{
    /// <summary>
    /// Trap that can damage the player. Can be disabled by phase wave.
    /// Only affects player in Yang state.
    /// </summary>
    public class Trap : MonoBehaviour, IDisableable, IStateInteractable
    {
        [Header("Configuration")]
        [SerializeField] private int damage = 1;
        [SerializeField] private float cooldown = 1f;
        private MaterialPropertyBlock _materialPropertyBlock;
        private static readonly int ColorID = Shader.PropertyToID("_BaseColor");
        private GameObject _GB;
        private Color _originalColor;
        private Renderer _render;
        private bool _isDisabled;
        private float _disableTimer;
        private float _cooldownTimer;
        
        private void Awake() 
        {
            _GB = this.gameObject;
            _render = GetComponent<Renderer>();
            _materialPropertyBlock = new MaterialPropertyBlock();

            _render.GetPropertyBlock(_materialPropertyBlock);
            _originalColor = _materialPropertyBlock.GetColor(ColorID);
        }

        private void Update()
        {
            // Update disable timer
            if (_isDisabled)
            {
                _disableTimer -= Time.deltaTime;
                if (_disableTimer <= 0f)
                {
                    _isDisabled = false;
                    SetColor(_originalColor);
                }
            }

            // Update cooldown timer
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        public void DisableTemporarily(float duration)
        {
            _isDisabled = true;
            _disableTimer = duration;

            SetColor(Color.gray);
        }

        public void Interact(PlayerState state)
        {
            // Traps only affect Yang state and when not disabled
            if (_isDisabled || state != PlayerState.Yang) return;
            if (_cooldownTimer > 0f) return;

            // Find player's health system and apply damage
            // Note: In a real scenario, you'd cache this reference
            var player = GameManager.Instance.Player;
            if (player != null)
            {
                player.HealthSystem.TakeDamage(damage);
                _cooldownTimer = cooldown;
            }
        }

        private void SetColor(Color color)
        {
            _render.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor(ColorID, color);
            _render.SetPropertyBlock(_materialPropertyBlock);
        }

        private void OnTriggerStay(Collider other)
        {
            if (_isDisabled) return;
            if (_cooldownTimer > 0f) return;

            var controller = other.GetComponent<PlayerController>();
            if (controller != null && controller.StateSystem.CurrentState == PlayerState.Yang)
            {
                controller.HealthSystem.TakeDamage(damage);
                _cooldownTimer = cooldown;
            }
        }

        public bool IsDisabled => _isDisabled;
    }
}
