using UnityEngine;

public class AttachablePost : MonoBehaviour
{
    [SerializeField] private float _radius = 0.3f;
    public float Radius => _radius;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
