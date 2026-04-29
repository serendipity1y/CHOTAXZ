using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public DoorAuto door;

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
}