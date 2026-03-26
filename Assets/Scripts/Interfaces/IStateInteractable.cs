using Player;

/// <summary>
/// Interface for objects that can interact with player states.
/// </summary>
public interface IStateInteractable
{
    void Interact(PlayerState state);
}
