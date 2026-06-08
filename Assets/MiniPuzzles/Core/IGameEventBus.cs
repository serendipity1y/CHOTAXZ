using Player;

namespace MiniPuzzles
{
    /// <summary>
    /// Fire-and-forget event bus abstraction. The puzzle system publishes plain structs;
    /// the concrete bus implementation is hidden behind this interface.
    /// </summary>
    public interface IGameEventBus
    {
        /// <summary>Publish an event to any listeners.</summary>
        void Publish<T>(T gameEvent) where T : struct;
    }

    /// <summary>Fired when a puzzle UI is opened.</summary>
    public struct PuzzleOpenedEvent
    {
        public PuzzleType Type;
        public PlayerState State;
        public int Seed;
    }

    /// <summary>Fired when a puzzle ends, whether solved or given up.</summary>
    public struct PuzzleCompletedEvent
    {
        public PuzzleType Type;
        public bool Success;
        public float TimeSeconds;
    }
}
