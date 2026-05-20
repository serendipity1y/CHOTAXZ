using UnityEngine;
using System.Collections.Generic;

public class WhipController : MonoBehaviour
{
    [Header("Physics Settings")]
    public Rigidbody firstSegment;
    public float force = 40f;
    
    [Header("Visuals")]
    public LineRenderer lineRenderer;
    public bool useLineRenderer = true;
    public Color startColor = Color.white;
    public Color endColor = Color.red;

    [HideInInspector] public PlayerWhip owner;
    
    private List<Transform> _segments = new List<Transform>();
    private Rigidbody[] _rigidbodies;

    private void Awake()
    {
        InitializeSegments();
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = endColor;
        }
    }

    private void InitializeSegments()
    {
        _segments.Clear();
        _segments.Add(transform);
        
        // Find all segments in children
        Transform current = transform;
        while (current.childCount > 0)
        {
            // Specifically look for the first child that might be a segment
            current = current.GetChild(0);
            _segments.Add(current);
        }

        _rigidbodies = GetComponentsInChildren<Rigidbody>();
        if (_rigidbodies.Length > 0 && firstSegment == null)
        {
            firstSegment = _rigidbodies[0];
        }
    }

    private void Update()
    {
        if (useLineRenderer && lineRenderer != null)
        {
            UpdateLineRenderer();
        }
    }

    private void UpdateLineRenderer()
    {
        lineRenderer.positionCount = _segments.Count;
        for (int i = 0; i < _segments.Count; i++)
        {
            lineRenderer.SetPosition(i, _segments[i].position);
        }
    }

    public void OnAttack()
    {
        if (owner == null) return;

        Vector3 attackDir = owner.transform.forward;
        
        // Apply force to segments
        for (int i = 0; i < _rigidbodies.Length; i++)
        {
            // Propagate force - stronger at the base, then stronger at the tip again for the "crack"
            float multiplier = 1f;
            if (i == _rigidbodies.Length - 1) multiplier = 2.5f; // Tip crack
            else multiplier = 1.0f - ((float)i / _rigidbodies.Length * 0.3f);
            
            _rigidbodies[i].AddForce(attackDir * force * multiplier, ForceMode.Impulse);
            
            // Add a bit of upward force for the "swing" feel
            _rigidbodies[i].AddForce(Vector3.up * force * 0.3f, ForceMode.Impulse);
        }
    }
}

