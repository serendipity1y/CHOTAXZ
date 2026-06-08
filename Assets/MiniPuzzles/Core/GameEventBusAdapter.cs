using UnityEngine;

namespace MiniPuzzles
{
    /// <summary>
    /// Adapter that forwards puzzle events into the project's existing event bus.
    ///
    /// INTEGRATION: implement <see cref="Publish{T}"/> to call your real bus. If no bus
    /// is wired, events are logged to the console so the system still runs.
    /// </summary>
    public class GameEventBusAdapter : MonoBehaviour, IGameEventBus
    {
        [Tooltip("Log published events to the console when no real bus is wired.")]
        [SerializeField] private bool logWhenUnwired = true;

        /// <inheritdoc />
        public void Publish<T>(T gameEvent) where T : struct
        {
            // TODO: forward to your existing event bus, e.g.
            //   YourEventBus.Instance.Raise(gameEvent);
            // Remove the log below once wired.
            if (logWhenUnwired)
            {
                Debug.Log($"[GameEventBusAdapter] (unwired) {typeof(T).Name}: {gameEvent}");
            }
        }
    }
}
