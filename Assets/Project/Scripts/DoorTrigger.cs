using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public DoorAuto door;
    public Collider boxCollide;
    
    

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collided atleast");
        if (other.CompareTag("Player"))
            door.Open();
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Collided atleast");
        if (other.CompareTag("Player"))
            door.Close();
    }

    public void CanBeOpened()
    {
        boxCollide.enabled = true;
    }
}