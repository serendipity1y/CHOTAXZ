using UnityEngine;
using Obstacles;

public class Tip : MonoBehaviour
{
    public float hitForce = 10f;
    
    void OnCollisionEnter(Collision col)
    {
        // Try to find destructible
        IDestructible destructible = col.gameObject.GetComponentInParent<IDestructible>();
        if (destructible != null)
        {
            destructible.Destroy();
            return;
        }

        // Standard knockback for physics objects
        Rigidbody rb = col.gameObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (col.transform.position - transform.position).normalized;
            rb.AddForce(dir * hitForce, ForceMode.Impulse);
        }

        if (col.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Hit Enemy!");
            // Here you could add damage logic if there was an Enemy class
        }
    }
}

