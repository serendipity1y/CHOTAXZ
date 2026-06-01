using UnityEngine;

public class AttachablePost : MonoBehaviour
{
    public enum CapsuleDirection { X, Y, Z }

    [SerializeField] private float _radius = 0.3f;
    [SerializeField] private float _height = 1f;
    [SerializeField] private CapsuleDirection _direction = CapsuleDirection.Y;

    public float Radius => _radius;
    public float Height => _height;

    public Vector3 GetClosestPoint(Vector3 worldPoint)
    {
        Vector3 axis = GetAxisVector();
        float halfOffset = Mathf.Max(0f, _height * 0.5f - _radius);
        Vector3 center = transform.position;

        float t = Mathf.Clamp(Vector3.Dot(worldPoint - center, axis), -halfOffset, halfOffset);
        Vector3 closestOnAxis = center + axis * t;

        Vector3 radial = worldPoint - closestOnAxis;
        if (radial.sqrMagnitude < 0.0001f)
            radial = Vector3.Cross(axis, Vector3.up).normalized;

        return closestOnAxis + radial.normalized * _radius;
    }

    private Vector3 GetAxisVector() => _direction switch
    {
        CapsuleDirection.X => transform.right,
        CapsuleDirection.Z => transform.forward,
        _ => transform.up,
    };

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 axis = GetAxisVector();
        float halfOffset = Mathf.Max(0f, _height * 0.5f - _radius);
        Vector3 p1 = transform.position + axis * halfOffset;
        Vector3 p2 = transform.position - axis * halfOffset;

        Gizmos.DrawWireSphere(p1, _radius);
        Gizmos.DrawWireSphere(p2, _radius);

        Vector3 perp1 = Vector3.Cross(axis, Vector3.up).normalized;
        if (perp1.sqrMagnitude < 0.01f) perp1 = Vector3.right;
        Vector3 perp2 = Vector3.Cross(axis, perp1).normalized;

        Gizmos.DrawLine(p1 + perp1 * _radius, p2 + perp1 * _radius);
        Gizmos.DrawLine(p1 - perp1 * _radius, p2 - perp1 * _radius);
        Gizmos.DrawLine(p1 + perp2 * _radius, p2 + perp2 * _radius);
        Gizmos.DrawLine(p1 - perp2 * _radius, p2 - perp2 * _radius);
    }
}
