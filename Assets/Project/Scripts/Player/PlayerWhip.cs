using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWhip : MonoBehaviour
{
    public Transform handPoint;
    private WhipController currentWhip;

    public float cooldown = 0.5f;
    private float lastAttack;

    public void EquipWhip(GameObject whipPrefab)
    {
        if (currentWhip != null) return;

        GameObject whip = Instantiate(whipPrefab, handPoint);
        currentWhip = whip.GetComponent<WhipController>();
        currentWhip.owner = this;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (Time.time < lastAttack + cooldown) return;

        lastAttack = Time.time;
        
        if (context.performed && currentWhip != null)
        {
            currentWhip.OnAttack();
        }
    }


}