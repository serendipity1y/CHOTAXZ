using UnityEngine;

namespace Player
{
public class TestINteract : MonoBehaviour, IStateInteractable 
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Renderer Meshrenderer;
    private Color _originalColor;
    
    private void Awake() 
    {
        Meshrenderer = GetComponent<Renderer>();
        if(Meshrenderer != null) _originalColor = Meshrenderer.material.color;
    }

    public void Interact(PlayerState currentState)
    {
        if (currentState == PlayerState.Yin)
        {
            Meshrenderer.material.color = Color.blue;
            Debug.Log("Объек синий?");
        }
        else
        {
            Meshrenderer.material.color = _originalColor;
            Debug.Log("Объект вернул изначальный цвет");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
}

    


