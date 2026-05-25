using UnityEngine;

public class WhipPickup : MonoBehaviour
{
    [SerializeField] private GameObject whipPrefab;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerWhip playerWhip = other.GetComponent<PlayerWhip>();
            if (playerWhip != null && whipPrefab != null)
            {
                playerWhip.EquipWhip(PlayerWhip.WhipSlot.Secondary, whipPrefab);
                Destroy(gameObject);
            }
        }
    }
}