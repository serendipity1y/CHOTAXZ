using System;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Manages player health, damage, and death.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxHealth = 3;

        private int _currentHealth;
        private bool _isDead;

        // Events
        public event Action<int, int> OnHealthChanged; // (current, max)
        public event Action<int> OnDamageTaken;
        public event Action OnDeath;
        public event Action OnHealed;

        // Public properties
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => _isDead;
        
        // Game logic
        private GameLogic _gamelogic;
        private void Awake()
        {
            _currentHealth = maxHealth;
            _gamelogic = GameObject.FindGameObjectWithTag("Logic").GetComponent<GameLogic>();
        }

        /// <summary>
        /// Applies damage to the player.
        /// </summary>
        /// <param name="amount">Amount of damage to apply.</param>
        public void TakeDamage(int amount = 1)
        {
            if (_isDead || amount <= 0) return;

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            OnDamageTaken?.Invoke(amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Heals the player.
        /// </summary>
        /// <param name="amount">Amount of health to restore.</param>
        public void Heal(int amount = 1)
        {
            if (_isDead || amount <= 0) return;

            int previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            
            if (_currentHealth > previousHealth)
            {
                OnHealed?.Invoke();
                OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            }
        }

        /// <summary>
        /// Instantly kills the player.
        /// </summary>
        public void Die()
        {
            if (_isDead) return;

            _isDead = true;
            _currentHealth = 0;
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Resets health to max and revives the player.
        /// </summary>
        public void Reset()
        {
            _currentHealth = maxHealth;
            _isDead = false;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// Sets max health and optionally resets current health.
        /// </summary>
        public void SetMaxHealth(int newMax, bool resetCurrent = false)
        {
            maxHealth = Mathf.Max(1, newMax);
            if (resetCurrent)
            {
                _currentHealth = maxHealth;
            }
            else
            {
                _currentHealth = Mathf.Min(_currentHealth, maxHealth);
            }
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }
    }
}
