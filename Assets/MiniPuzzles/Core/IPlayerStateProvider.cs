using Player;

namespace MiniPuzzles
{
    /// <summary>
    /// Read-only access to the player's current Yin/Yang state.
    /// The puzzle system depends on this interface, never on a concrete player class.
    /// </summary>
    public interface IPlayerStateProvider
    {
        /// <summary>The player's current state.</summary>
        PlayerState CurrentState { get; }
    }
}
