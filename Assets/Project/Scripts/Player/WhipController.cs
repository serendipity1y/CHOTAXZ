using UnityEngine;

public class WhipController : MonoBehaviour
{
    [Header("Rope Settings")]
    [SerializeField] private int _segments = 12;
    [SerializeField] private float _ropeLength = 6f;
    [SerializeField] private float _gravity = 8f;
    [SerializeField] private float _damping = 0.98f;

    [Header("Whip Settings")]
    [SerializeField] private float _throwSpeed = 22f;
    [SerializeField] private float _retractSpeed = 10f;
    [SerializeField] private float _attachRadius = 0.35f;
    [SerializeField] private LayerMask _attachableLayers;
    [SerializeField] private LayerMask _interactableLayers;

    [Header("Swing Settings")]
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
    private Vector3 _throwDirection;
    private float _currentLength;
    private bool _hasAttachTarget;
    private Vector3 _swingVelocity;

    // Намотка
    private AttachablePost _attachedPost;
    private float _effectiveLength;

    public bool IsAttached => _state == WhipState.Attached;
    public LayerMask AttachableLayers => _attachableLayers;

    private enum WhipState { Idle, Throwing, Attached, Retracting }

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

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        float camToHandOffset = Vector3.Distance(ray.origin, whipOrigin.position);
        float castDistance = _ropeLength + camToHandOffset;

        LayerMask combinedMask = _attachableLayers | _interactableLayers;

        if (Physics.SphereCast(
                ray.origin,         // ← от камеры, не от руки
                _attachRadius,
                ray.direction,
                out RaycastHit hit,
                castDistance,
                combinedMask))
        {
            if (Vector3.Distance(whipOrigin.position, hit.point) <= _ropeLength)
            {
                _attachPoint = hit.point;

                int hitLayer = 1 << hit.collider.gameObject.layer;
                bool isAttachable   = (hitLayer & _attachableLayers.value)   != 0;
                bool isInteractable = (hitLayer & _interactableLayers.value) != 0;

                if (isInteractable)
                    hit.collider.GetComponent<IWhipInteractable>()?.OnWhipHit();

                _hasAttachTarget = isAttachable;

                // Считаем намотку уже здесь — знаем дистанцию и радиус столба
                if (isAttachable)
                {
                    _attachedPost = hit.collider.GetComponent<AttachablePost>();
                    if (_attachedPost != null)
                    {
                        float wrapFactor = 1f - Mathf.Clamp01(hit.distance / _ropeLength);
                        float wrappedLength = _attachedPost.Radius * Mathf.PI * 2f * wrapFactor;
                        _effectiveLength = Mathf.Max(0.5f, _ropeLength - wrappedLength);
                    }
                    else
                    {
                        _effectiveLength = _ropeLength;
                    }
                } 
            }
            else
            {
                _attachPoint = ray.origin + ray.direction * castDistance;
                _hasAttachTarget = false;
                _effectiveLength = _ropeLength;
            }
        }
        else
        {
            _attachPoint = ray.origin + ray.direction * castDistance;
            _hasAttachTarget = false;
            _effectiveLength = _ropeLength;
        }
        _throwDirection = (_attachPoint - whipOrigin.position).normalized;
        
        _rope.Initialize(whipOrigin.position, whipOrigin.position + _throwDirection * 0.1f);
        _currentLength = 0f;
        _state = WhipState.Throwing;

        if (_animator != null)
            _animator.enabled = false;
    }

    public void Retract()
    {
        if (_state == WhipState.Idle) return;

        if (_state == WhipState.Throwing)
            _retractFromTip = whipOrigin.position + _throwDirection * _currentLength;
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
            case WhipState.Throwing:   UpdateThrowing();   break;
            case WhipState.Attached:   UpdateSwing();      break;
            case WhipState.Retracting: UpdateRetracting(); break;
        }
    }

    private void UpdateThrowing()
    {
        float targetLength = Vector3.Distance(whipOrigin.position, _attachPoint);
        _currentLength = Mathf.MoveTowards(_currentLength, targetLength, _throwSpeed * Time.deltaTime);

        Vector3 currentTip = whipOrigin.position + _throwDirection * _currentLength;
        _rope.Simulate(whipOrigin.position, currentTip, _gravity * 0.2f, _damping);

        if (_currentLength >= targetLength - 0.05f)
        {
            if (_hasAttachTarget)
            {
                _retractFromTip = _attachPoint;
                _state = WhipState.Attached;
                _swingVelocity = characterController != null
                    ? characterController.velocity
                    : Vector3.zero;
            }
            else
            {
                _retractFromTip = currentTip;
                _state = WhipState.Retracting;
            }
        }
    }

    public void SetBones(Transform[] bones)
    {
        _whipBones = bones;
    }

    private void UpdateSwing()
    {
        if (characterController == null) return;

        Vector3 toAnchor = _attachPoint - playerTransform.position;
        float ropeDistance = toAnchor.magnitude;

        _swingVelocity += Physics.gravity * Time.deltaTime;

        // Убираем компоненту velocity, удлиняющую верёвку
        if (ropeDistance > 0.01f)
        {
            Vector3 ropeDir = toAnchor.normalized;
            float radialVelocity = Vector3.Dot(_swingVelocity, ropeDir);
            if (radialVelocity < 0)
                _swingVelocity -= ropeDir * radialVelocity;
        }

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

            int nodeIndex = Mathf.RoundToInt((float)i / (boneCount - 1) * (nodeCount - 1));
            _whipBones[i].position = _rope.GetNodePosition(nodeIndex);

            if (i < boneCount - 1)
            {
                int nextNodeIndex = Mathf.RoundToInt((float)(i + 1) / (boneCount - 1) * (nodeCount - 1));
                Vector3 dir = _rope.GetNodePosition(nextNodeIndex) - _whipBones[i].position;
                if (dir.sqrMagnitude > 0.0001f)
                    _whipBones[i].rotation = Quaternion.LookRotation(dir);
            }
        }
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