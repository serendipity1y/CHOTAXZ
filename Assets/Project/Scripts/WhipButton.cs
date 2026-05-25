using UnityEngine;

public class WhipButton : MonoBehaviour, IWhipInteractable
{
    [SerializeField] private UnityEngine.Events.UnityEvent _onActivated;

    public void OnWhipHit()
    {
        _onActivated.Invoke();
        Debug.Log("Button works");
    }
}
