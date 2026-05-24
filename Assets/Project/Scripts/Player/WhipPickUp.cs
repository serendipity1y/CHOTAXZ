using UnityEngine;

// WhipPickup — остаётся как есть
// WhipPickup
public class WhipPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerWhip playerWhip = other.GetComponent<PlayerWhip>();
            playerWhip.EquipWhip(null);
            Destroy(gameObject);
        }
    }
}