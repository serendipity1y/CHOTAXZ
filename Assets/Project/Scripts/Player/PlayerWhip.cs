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
        if (currentWhip != null)
        {
            Destroy(currentWhip.gameObject);
        }

        GameObject whip = Instantiate(whipPrefab, handPoint);
        whip.transform.localPosition = Vector3.zero;
        whip.transform.localRotation = Quaternion.identity;
        
        currentWhip = whip.GetComponent<WhipController>();
        if (currentWhip == null)
        {
            currentWhip = whip.AddComponent<WhipController>();
        }
        
        currentWhip.owner = this;
        
        // Remove hover if it exists on the equipped version
        SimpleHover hover = whip.GetComponent<SimpleHover>();
        if (hover != null) Destroy(hover);

        // Hide mesh renderers of segments and improve physics
        MeshRenderer[] renderers = whip.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in renderers) mr.enabled = false;

        Rigidbody[] rbs = whip.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            rb.linearDamping = 2f;
            rb.angularDamping = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (Time.time < lastAttack + cooldown) return;
            if (currentWhip == null) return;

            lastAttack = Time.time;
            currentWhip.OnAttack();
        }
    }
}

