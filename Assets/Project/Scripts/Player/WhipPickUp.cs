using UnityEngine;

public class WhipPickup : MonoBehaviour
{
    public GameObject whipPrefab;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerWhip playerWhip = other.GetComponent<PlayerWhip>();
            playerWhip.EquipWhip(whipPrefab);

            Destroy(gameObject);
        }
    }
}