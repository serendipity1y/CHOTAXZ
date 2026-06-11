using System.Collections.Generic;
using UnityEngine;

public class WhipController : MonoBehaviour
{
    [Header("Rope Settings")]
    [SerializeField] private int _segments = 20;
    [SerializeField] private float _ropeLength = 6f;
    [SerializeField] private float _gravity = 8f;
    [SerializeField] private float _damping = 0.98f;

    [Header("Whip Settings")]
    [SerializeField] private float _retractSpeed = 10f;
    [SerializeField] private LayerMask _attachableLayers;
    [SerializeField] private LayerMask _interactableLayers;

    [Header("Swing Attack")]
    [Tooltip("Seconds for the whip to travel the arc.")]
    [SerializeField] private float _swingDuration = 0.4f;
    [Tooltip("Arc angle at swing start (windup). Negative = up/back.")]
    [SerializeField] private float _arcStartAngle = -70f;
    [Tooltip("Arc angle at swing end (crack). Positive = down/front.")]
    [SerializeField] private float _arcEndAngle = 10f;
    [Tooltip("Thickness of the hit sweep along the arc.")]
    [SerializeField] private float _sweepRadius = 0.4f;
    [Tooltip("Eases the tip along the arc. Slow windup then fast crack.")]
    [SerializeField] private AnimationCurve _swingEase = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0.3f),
        new Keyframe(0.6f, 0.1f, 0.8f, 4f),
        new Keyframe(1f, 1f, 6f, 0f));

    [Header("Grapple Swing Settings")]
    [SerializeField] private float _swingForce = 8f;

    [Header("Visuals")]
    [SerializeField] private Transform[] _whipBones;

    // Заполняется из PlayerWhip
    [HideInInspector] public Transform whipOrigin;
    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public Transform playerTransform;
    [HideInInspector] public PlayerMovement PlayerMovement;

    private Animator _animator;
    private VerletRope _rope;

    private WhipState _state = WhipState.Idle;

    private Vector3 _attachPoint;
    private Vector3 _retractFromTip;
    private Vector3 _currentTip;
    private float _currentLength;
    private bool _hasAttachTarget;
    private Vector3 _swingVelocity;

    // Arc swing state
    private float _swingTimer;
    private Vector3 _planeForward;
    private Vector3 _planeRight;
    private readonly HashSet<Collider> _hitThisSwing = new HashSet<Collider>();

    // Намотка
    private AttachablePost _attachedPost;
    private float _effectiveLength;

    public bool IsAttached => _state == WhipState.Attached;
    public LayerMask AttachableLayers => _attachableLayers;

    private enum WhipState { Idle, Swinging, Attached, Retracting }

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>(true);
        _rope = new VerletRope(_segments, _ropeLength);
        _effectiveLength = _ropeLength;
    }

    private void Update()
    {
        if (whipOrigin == null) return;
        UpdateStateMachine();
    }

    private void LateUpdate()
    {
        UpdateBones();
    }

    // ─── Public API ───────────────────────────────────────────────

    public void Throw()
    {
        if (_state != WhipState.Idle) return;

        // Aim from hand toward camera-center target so whip hits what crosshair points at.
        Vector3 camForward = Camera.main != null
            ? Camera.main.transform.forward
            : playerTransform.forward;

        Vector3 aimPoint;
        if (Camera.main != null)
        {
            Ray camRay = new Ray(Camera.main.transform.position, camForward);
            LayerMask combinedMask = _attachableLayers | _interactableLayers;
            aimPoint = Physics.Raycast(camRay, out RaycastHit aimHit, _ropeLength * 1.5f, combinedMask,
                                       QueryTriggerInteraction.Ignore)
                ? aimHit.point
                : camRay.origin + camRay.direction * _ropeLength;
        }
        else
        {
            aimPoint = whipOrigin.position + playerTransform.forward * _ropeLength;
        }

        // Direction from hand to aim point, flattened for the horizontal swing plane.
        Vector3 toAim = aimPoint - whipOrigin.position;
        _planeForward = Vector3.ProjectOnPlane(toAim, Vector3.up).normalized;
        if (_planeForward.sqrMagnitude < 0.0001f)
            _planeForward = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;
        _planeRight = Vector3.Cross(Vector3.up, _planeForward).normalized;

        _hasAttachTarget = false;
        _attachedPost = null;
        _effectiveLength = _ropeLength;
        _hitThisSwing.Clear();
        _swingTimer = 0f;

        _currentTip = whipOrigin.position + ArcDirection(_arcStartAngle) * _ropeLength;
        _rope.Initialize(whipOrigin.position, _currentTip);
        _state = WhipState.Swinging;

        if (_animator != null)
            _animator.enabled = false;
    }

    /// <summary>Direction in the swing plane at the given arc angle (deg). Negative = up.</summary>
    private Vector3 ArcDirection(float angle) =>
        Quaternion.AngleAxis(angle, _planeRight) * _planeForward;

    public void Retract()
    {
        if (_state == WhipState.Idle) return;

        if (_state == WhipState.Swinging)
        {
            _retractFromTip = _currentTip;
            _currentLength = (_currentTip - whipOrigin.position).magnitude;
        }
        else if (_state == WhipState.Attached)
            _retractFromTip = _attachPoint;

        _swingVelocity = Vector3.zero;
        _attachedPost = null;
        _effectiveLength = _ropeLength;
        _state = WhipState.Retracting;
    }

    public void SetLayers(LayerMask attachable, LayerMask interactable)
    {
        _attachableLayers = attachable;
        _interactableLayers = interactable;
    }

    // ─── State machine ────────────────────────────────────────────

    private void UpdateStateMachine()
    {
        switch (_state)
        {
            case WhipState.Swinging:   UpdateSwinging();   break;
            case WhipState.Attached:   UpdateSwing();      break;
            case WhipState.Retracting: UpdateRetracting(); break;
        }
    }

    private void UpdateSwinging()
    {
        _swingTimer += Time.deltaTime;
        float p = _swingDuration > 0f ? Mathf.Clamp01(_swingTimer / _swingDuration) : 1f;
        float eased = _swingEase != null ? _swingEase.Evaluate(p) : p;
        float angle = Mathf.Lerp(_arcStartAngle, _arcEndAngle, eased);

        Vector3 prevTip = _currentTip;
        _currentTip = whipOrigin.position + ArcDirection(angle) * _ropeLength;

        // Hit everything the whip sweeps across this frame.
        SweepHits(prevTip, _currentTip);
        if (_state != WhipState.Swinging) return; // grappled mid-swing

        _rope.Simulate(whipOrigin.position, _currentTip, _gravity * 0.2f, _damping);

        if (p >= 1f)
        {
            _retractFromTip = _currentTip;
            _currentLength = (_currentTip - whipOrigin.position).magnitude;
            _state = WhipState.Retracting;
        }
    }

    /// <summary>Capsule-sweep between two tip positions; fire interactables, grapple first attachable.</summary>
    private void SweepHits(Vector3 from, Vector3 to)
    {
        LayerMask combinedMask = _attachableLayers | _interactableLayers;
        Collider[] hits = Physics.OverlapCapsule(from, to, _sweepRadius, combinedMask,
                                                 QueryTriggerInteraction.Ignore);

        foreach (Collider col in hits)
        {
            if (_hitThisSwing.Contains(col)) continue;

            int layerBit = 1 << col.gameObject.layer;
            bool isInteractable = (layerBit & _interactableLayers.value) != 0;
            bool isAttachable   = (layerBit & _attachableLayers.value)   != 0;

            if (isInteractable)
            {
                col.GetComponent<IWhipInteractable>()?.OnWhipHit();
                _hitThisSwing.Add(col);
            }

            if (isAttachable)
            {
                AttachTo(col, to);
                return; // attaching ends the swing
            }
        }

        // Camera-aim raycast — compensates for hand-vs-camera position offset
        if (Camera.main != null && _interactableLayers.value != 0)
        {
            Ray camRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(camRay, out RaycastHit camHit, _ropeLength * 1.2f,
                                _interactableLayers, QueryTriggerInteraction.Ignore))
            {
                if (!_hitThisSwing.Contains(camHit.collider))
                {
                    camHit.collider.GetComponent<IWhipInteractable>()?.OnWhipHit();
                    _hitThisSwing.Add(camHit.collider);
                }
            }
        }
    }

    private void AttachTo(Collider col, Vector3 tip)
    {
        _attachedPost = col.GetComponent<AttachablePost>();
        if (_attachedPost != null)
        {
            _attachPoint = _attachedPost.GetClosestPoint(tip);
            float dist = Vector3.Distance(whipOrigin.position, _attachPoint);
            float wrapFactor = 1f - Mathf.Clamp01(dist / _ropeLength);
            float wrappedLength = _attachedPost.Radius * Mathf.PI * 2f * wrapFactor;
            _effectiveLength = Mathf.Max(0.5f, _ropeLength - wrappedLength);
        }
        else
        {
            _attachPoint = col.ClosestPoint(tip);
            _effectiveLength = _ropeLength;
        }

        _hasAttachTarget = true;
        _retractFromTip = _attachPoint;
        _swingVelocity = characterController != null ? characterController.velocity : Vector3.zero;
        _state = WhipState.Attached;
    }

    public void SetBones(Transform[] bones)
    {
        _whipBones = bones;
    }

    private void UpdateSwing()
    {
        if (characterController == null) return;

        _swingVelocity += Physics.gravity * Time.deltaTime;

        _swingVelocity += GetSwingInput() * _swingForce * Time.deltaTime;

        characterController.Move(_swingVelocity * Time.deltaTime);

        // Позиционный constraint по effectiveLength (не ropeLength)
        Vector3 newToAnchor = _attachPoint - playerTransform.position;
        float newDist = newToAnchor.magnitude;
        if (newDist > _effectiveLength)
        {
            Vector3 correction = (_attachPoint - newToAnchor.normalized * _effectiveLength)
                                 - playerTransform.position;
            characterController.Move(correction);

            Vector3 ropeDir = newToAnchor.normalized;
            float radial = Vector3.Dot(_swingVelocity, -ropeDir);
            if (radial > 0)
                _swingVelocity += ropeDir * radial;
        }

        _rope.Simulate(whipOrigin.position, _attachPoint, _gravity, _damping);
    }

    private void UpdateRetracting()
    {
        _currentLength = Mathf.MoveTowards(_currentLength, 0f, _retractSpeed * Time.deltaTime);

        float retractDist = (_retractFromTip - whipOrigin.position).magnitude;
        float t = retractDist > 0.001f ? _currentLength / retractDist : 0f;
        Vector3 currentTip = Vector3.Lerp(whipOrigin.position, _retractFromTip, t);

        _rope.Simulate(whipOrigin.position, currentTip, _gravity, _damping);

        if (_currentLength <= 0.05f)
        {
            _state = WhipState.Idle;
            StartCoroutine(EnableAnimatorNextFrame());
        }
    }

    private System.Collections.IEnumerator EnableAnimatorNextFrame()
    {
        yield return null; // ждём один кадр
        if (_animator != null)
        {
            _animator.enabled = true;
            _animator.Play("WhipIdle", 0, 0f);
        }
    }

    // ─── Visuals ──────────────────────────────────────────────────

    private void UpdateBones()
    {
        if (_whipBones == null || _whipBones.Length == 0) return;
        if (_state == WhipState.Idle) return;
        if (whipOrigin == null) return;

        int boneCount = _whipBones.Length;
        int nodeCount = _rope.NodeCount;

        for (int i = 0; i < boneCount; i++)
        {
            if (_whipBones[i] == null) continue;

            float t = boneCount > 1 ? (float)i / (boneCount - 1) : 0f;
            Vector3 pos = SampleRope(t, nodeCount);
            _whipBones[i].position = pos;

            if (i < boneCount - 1)
            {
                float tNext = (float)(i + 1) / (boneCount - 1);
                Vector3 nextPos = SampleRope(tNext, nodeCount);
                Vector3 dir = nextPos - pos;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dir);
                    _whipBones[i].rotation = i == 0
                        ? Quaternion.Slerp(whipOrigin.rotation, lookRot, 0.35f)
                        : lookRot;
                }
            }
        }
    }

    private Vector3 SampleRope(float t, int nodeCount)
    {
        float nodeFloat = t * (nodeCount - 1);
        int nodeA = Mathf.FloorToInt(nodeFloat);
        int nodeB = Mathf.Min(nodeA + 1, nodeCount - 1);
        float lerpT = nodeFloat - nodeA;
        return Vector3.Lerp(_rope.GetNodePosition(nodeA), _rope.GetNodePosition(nodeB), lerpT);
    }

    private Vector3 GetSwingInput()
    {
        if (PlayerMovement == null) return Vector3.zero;

        float h = PlayerMovement.MoveInput.x;
        float v = PlayerMovement.MoveInput.y;

        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 camRight   = Camera.main.transform.right;

        return camRight * h + camForward * v;
    }
}