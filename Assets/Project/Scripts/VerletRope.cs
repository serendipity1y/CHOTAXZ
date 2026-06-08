using UnityEngine;

public class VerletRope
{
    public struct Node
    {
        public Vector3 position;
        public Vector3 prevPosition;
        public bool locked;
    }

    private Node[] _nodes;
    private int _segmentCount;
    private float _segmentLength;

    public VerletRope(int segments, float totalLength)
    {
        _segmentCount = segments;
        _segmentLength = totalLength / segments;
        _nodes = new Node[segments + 1];
    }

    public void Initialize(Vector3 start, Vector3 end)
    {
        for (int i = 0; i <= _segmentCount; i++)
        {
            float t = (float)i / _segmentCount;
            _nodes[i].position = Vector3.Lerp(start, end, t);
            _nodes[i].prevPosition = _nodes[i].position;
            _nodes[i].locked = false;
        }
    }

    public void Simulate(Vector3 startPoint, Vector3? endPoint, float gravity, float damping)
    {
        // Verlet integration
        for (int i = 0; i <= _segmentCount; i++)
        {
            if (_nodes[i].locked) continue;

            Vector3 velocity = (_nodes[i].position - _nodes[i].prevPosition) * damping;
            _nodes[i].prevPosition = _nodes[i].position;
            _nodes[i].position += velocity + Vector3.down * gravity * Time.deltaTime * Time.deltaTime;
        }

        // Anchors
        _nodes[0].position = startPoint;
        _nodes[0].locked = true;

        if (endPoint.HasValue)
        {
            _nodes[_segmentCount].position = endPoint.Value;
            _nodes[_segmentCount].locked = true;
        }
        else
        {
            _nodes[_segmentCount].locked = false;
        }

        // Segment length constraints
        for (int iteration = 0; iteration < 20; iteration++)
        {
            for (int i = 0; i < _segmentCount; i++)
            {
                Vector3 delta = _nodes[i + 1].position - _nodes[i].position;
                float currentLength = delta.magnitude;
                if (currentLength < 0.0001f) continue;

                float error = (currentLength - _segmentLength) / currentLength;
                Vector3 correction = delta * 0.5f * error;

                if (!_nodes[i].locked)
                    _nodes[i].position += correction;
                if (!_nodes[i + 1].locked)
                    _nodes[i + 1].position -= correction;
            }
        }
    }

    public Vector3 GetNodePosition(int index) => _nodes[index].position;
    public int NodeCount => _segmentCount + 1;
}