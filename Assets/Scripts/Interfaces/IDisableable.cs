/// <summary>
/// Interface for objects that can be temporarily disabled by peak effects (phase wave).
/// </summary>
public interface IDisableable
{
    void DisableTemporarily(float duration);
}
