using UnityEngine;
public interface IWhipInteractable
{
    void OnWhipHit();
}

// Пример кнопки
public class WhipButton : MonoBehaviour, IWhipInteractable
{
    [SerializeField] private UnityEngine.Events.UnityEvent _onActivated;

    public void OnWhipHit()
    {
        _onActivated.Invoke();
        // анимация нажатия, звук и т.д.
    }
}