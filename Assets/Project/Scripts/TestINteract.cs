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
            Meshrenderer = GetComponent<Renderer>();
            if(Meshrenderer != null) _originalColor = Meshrenderer.material.color;
        }

        public void Interact(PlayerState currentState)
        {
            if (currentState == PlayerState.Yin)
            {
                Meshrenderer.material.color = Color.blue;
                Debug.Log("Объек синий?");
                OnYinInteract();
            }
            else
            {
                Meshrenderer.material.color = _originalColor;
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
    }
}

    


