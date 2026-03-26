using UnityEngine;
using Player;

namespace Obstacles
{
    /// <summary>
    /// Example destructible object that can be destroyed by shockwave.
    /// </summary>
    public class DestructibleObject : MonoBehaviour, IDestructible, IStateInteractable
    {
        [Header("Configuration")]
        [SerializeField] private bool onlyDestroyableInYangState = true;
        [SerializeField] private GameObject destructionEffect;

        private bool _isDestroyed;

        public void Destroy()
        {
            if (_isDestroyed) return;

            _isDestroyed = true;

            if (destructionEffect != null)
            {
                Instantiate(destructionEffect, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        public void Interact(PlayerState state)
        {
            if (state == PlayerState.Yang || !onlyDestroyableInYangState)
            {
                Destroy();
            }
        }
    }
}
