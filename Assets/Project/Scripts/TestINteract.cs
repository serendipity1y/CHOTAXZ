using UnityEngine;
using UnityEngine.Events;

namespace Player
{
public class TestINteract : MonoBehaviour, IStateInteractable 
{

        [Header("Events")]
        [SerializeField] private UnityEvent onYinInteract;

        [SerializeField] private UnityEvent onYangInteract;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private Renderer Meshrenderer;
        private Color _originalColor;
    
        private void Awake() 
        {
        }

        public void Interact(PlayerState currentState)
        {
            if (currentState == PlayerState.Yin)
            {
                
                Debug.Log("Объек синий?");
                OnYinInteract();
            }
            else
            {
                
                OnYangInteract();
                Debug.Log("Объект вернул изначальный цвет");
            }
        }

        public void OnYinInteract() {
            Debug.Log("Работает Yin");
            onYinInteract.Invoke();
        }

        public void OnYangInteract() { 

            Debug.Log("Работает Yang");
            onYangInteract.Invoke();
        }
        // Update is called once per frame
        void Update()
        {
        
        }

        public void Udali()
        {
            Destroy(gameObject);
        }
    }
}

    


