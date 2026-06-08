using UnityEngine;
using Player;

namespace MiniPuzzles
{
    /// <summary>
    /// Adapter that exposes the project's existing <see cref="PlayerStateSystem"/>
    /// through the <see cref="IPlayerStateProvider"/> interface used by the puzzle system.
    ///
    /// INTEGRATION: assign the scene's PlayerStateSystem in the Inspector, or leave it
    /// empty to auto-find one at runtime. Replace the body of <see cref="CurrentState"/>
    /// if your player state lives somewhere else.
    /// </summary>
    public class PlayerStateAdapter : MonoBehaviour, IPlayerStateProvider
    {
        [Tooltip("The existing player state source. Auto-found if left empty.")]
        [SerializeField] private PlayerStateSystem playerStateSystem;

        [Tooltip("Fallback state used only if no PlayerStateSystem can be found.")]
        [SerializeField] private PlayerState fallbackState = PlayerState.Yin;

        private void Awake()
        {
            if (playerStateSystem == null)
            {
                playerStateSystem = FindFirstObjectByType<PlayerStateSystem>();
            }
        }

        /// <inheritdoc />
        public PlayerState CurrentState
        {
            get
            {
                // TODO: if your player state lives elsewhere, return it here instead.
                return playerStateSystem != null ? playerStateSystem.CurrentState : fallbackState;
            }
        }
    }
}
