using UnityEngine;

public class WhipController : MonoBehaviour
{
    public Rigidbody firstSegment;
    public float force = 500f;

    [HideInInspector] public PlayerWhip owner;

    public void OnAttack()
    {
        firstSegment.AddForce(owner.transform.forward * force, ForceMode.Impulse);
    }
}