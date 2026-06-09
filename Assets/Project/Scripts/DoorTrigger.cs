using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public DoorAuto door;
    public Collider boxCollide;
    
    

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collided atleast" + other);
        if (other.CompareTag("Player"))
            door.Open();
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Collided atleast"+ other);
        if (other.CompareTag("Player"))
            Debug.Log("Colliede wit player");
            door.Close();
    }

    public void CanBeOpened()
    {
        boxCollide.enabled = true;
        door.canOpen = true;
        Debug.Log("Door Can be Opened");
    }
}