using UnityEngine;

public class SimpleHover : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverAmplitude = 0.1f;
    [SerializeField] private float hoverFrequency = 1.0f;
    
    [Header("Rotate Settings")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 50, 0);

    private Vector3 _startPosition;

    private void Start()
    {
        _startPosition = transform.position;
    }

    private void Update()
    {
        // Hover
        float newY = _startPosition.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);

        // Rotate
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
