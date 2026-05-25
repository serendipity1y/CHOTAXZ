using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
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

    // Заполняется из PlayerWhip
    [HideInInspector] public Transform whipOrigin;
    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public Transform playerTransform;
    [HideInInspector] public PlayerMovement PlayerMovement;

    private LineRenderer _lineRenderer;
    private VerletRope _rope;

    private WhipState _state = WhipState.Idle;

    // Точка attach — определяется в момент броска (precast), либо tip в момент miss
    private Vector3 _attachPoint;

    // Откуда ретрактим (фиксируется в момент перехода в Retracting)
    private Vector3 _retractFromTip;

    // Направление броска (нормаль от origin к tipTarget)
    private Vector3 _throwDirection;

    // Текущая длина развёрнутой верёвки (0 → _ropeLength)
    private float _currentLength;

    // Есть ли цель для attach (определяется precast'ом в Throw())
    private bool _hasAttachTarget;

    // Swing state
    private Vector3 _swingVelocity;

    public bool IsAttached => _state == WhipState.Attached;
    public LayerMask AttachableLayers => _attachableLayers;

    private enum WhipState { Idle, Throwing, Attached, Retracting }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _rope = new VerletRope(_segments, _ropeLength);
        _lineRenderer.positionCount = 0;
    }

    private void Update()
    {
        if (whipOrigin == null) return;

        UpdateStateMachine();
        UpdateRopeVisual();
    }

    public void Throw()
    {
        if (_state != WhipState.Idle) return;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        _throwDirection = ray.direction;

        LayerMask combinedMask = _attachableLayers | _interactableLayers;

        if (Physics.SphereCast(
                whipOrigin.position,
                _attachRadius,
                _throwDirection,
                out RaycastHit hit,
                _ropeLength,
                combinedMask))
        {
            _attachPoint = hit.point;

            int hitLayer = 1 << hit.collider.gameObject.layer;
            bool isAttachable   = (hitLayer & _attachableLayers.value)   != 0;
            bool isInteractable = (hitLayer & _interactableLayers.value) != 0;

            // IWhipInteractable вызывается независимо от attach
            if (isInteractable)
                hit.collider.GetComponent<IWhipInteractable>()?.OnWhipHit();

            _hasAttachTarget = isAttachable;
        }
        else
        {
            _attachPoint = whipOrigin.position + _throwDirection * _ropeLength;
            _hasAttachTarget = false;
        }

        _rope.Initialize(whipOrigin.position, whipOrigin.position + _throwDirection * 0.1f);
        _currentLength = 0f;
        _state = WhipState.Throwing;
    }

    public void Retract()
    {
        if (_state == WhipState.Idle) return;

        // Фиксируем текущий tip как начало ретракта
        if (_state == WhipState.Throwing)
            _retractFromTip = whipOrigin.position + _throwDirection * _currentLength;
        else if (_state == WhipState.Attached)
            _retractFromTip = _attachPoint;
        // Если уже Retracting — не трогаем _retractFromTip

        _swingVelocity = Vector3.zero;
        _state = WhipState.Retracting;
    }

    public void SetLayers(LayerMask attachable, LayerMask interactable)
    {
        _attachableLayers = attachable;
        _interactableLayers = interactable;
    }

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

    private void UpdateSwing()
    {
        if (characterController == null) return;

        Vector3 playerPos = playerTransform.position;
        Vector3 toAnchor = _attachPoint - playerPos;
        float ropeDistance = toAnchor.magnitude;

        _swingVelocity += Physics.gravity * Time.deltaTime;

        // Constraint: убираем компоненту velocity, удлиняющую верёвку
        if (ropeDistance > 0.01f)
        {
            Vector3 ropeDir = toAnchor.normalized;
            float radialVelocity = Vector3.Dot(_swingVelocity, ropeDir);
            if (radialVelocity < 0)
                _swingVelocity -= ropeDir * radialVelocity;
        }

        _swingVelocity += GetSwingInput() * _swingForce * Time.deltaTime;

        characterController.Move(_swingVelocity * Time.deltaTime);

        // Позиционный constraint: не выходить за длину верёвки
        Vector3 newToAnchor = _attachPoint - playerTransform.position;
        float newDist = newToAnchor.magnitude;
        if (newDist > _ropeLength)
        {
            Vector3 correction = (_attachPoint - newToAnchor.normalized * _ropeLength) - playerTransform.position;
            characterController.Move(correction);

            // Гасим радиальную компоненту после коррекции
            Vector3 ropeDir = newToAnchor.normalized;
            float radial = Vector3.Dot(_swingVelocity, -ropeDir);
            if (radial > 0)
                _swingVelocity += ropeDir * radial;
        }

        _rope.Simulate(whipOrigin.position, _attachPoint, _gravity, _damping);
    }

    private Vector3 GetSwingInput()
    {
        if (PlayerMovement == null) return Vector3.zero;

        float h = PlayerMovement.MoveInput.x;
        float v = PlayerMovement.MoveInput.y;

        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Camera.main.transform.right;

        return camRight * h + camForward * v;
    }

    private void UpdateRetracting()
    {
        _currentLength = Mathf.MoveTowards(_currentLength, 0f, _retractSpeed * Time.deltaTime);

        // Tip идёт от зафиксированной точки обратно к origin
        float t = (_retractFromTip - whipOrigin.position).magnitude > 0.001f
            ? _currentLength / (_retractFromTip - whipOrigin.position).magnitude
            : 0f;
        Vector3 currentTip = Vector3.Lerp(whipOrigin.position, _retractFromTip, t);

        _rope.Simulate(whipOrigin.position, currentTip, _gravity, _damping);

        if (_currentLength <= 0.05f)
            _state = WhipState.Idle;
    }

    private void UpdateRopeVisual()
    {
        if (_state == WhipState.Idle)
        {
            // Верёвка висит вниз от руки
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, whipOrigin.position);
            _lineRenderer.SetPosition(1, whipOrigin.position + Vector3.down * 0.4f);
            return;
        }

        _lineRenderer.positionCount = _rope.NodeCount;
        for (int i = 0; i < _rope.NodeCount; i++)
            _lineRenderer.SetPosition(i, _rope.GetNodePosition(i));
    }
}