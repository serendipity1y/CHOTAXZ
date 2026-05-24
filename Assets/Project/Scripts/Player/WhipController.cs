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
    private Vector3 _attachPoint;
    private Vector3 _tipTarget;
    private float _currentLength;

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

    // Вызывается из PlayerWhip.OnAttack
    public void Throw()
    {
        if (_state != WhipState.Idle) return;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        _rope.Initialize(whipOrigin.position, whipOrigin.position + ray.direction * 0.1f);
        Vector3 aimPoint = ray.GetPoint(_ropeLength);
        Vector3 direction = (aimPoint - whipOrigin.position).normalized;
        
        _tipTarget = whipOrigin.position + direction * _ropeLength;
        _currentLength = 0f;
        _state = WhipState.Throwing;
    }

    // Вызывается из PlayerWhip при отпускании кнопки
    public void Retract()
    {
        if (_state == WhipState.Idle) return;
        _swingVelocity = Vector3.zero;
        _state = WhipState.Retracting;
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

    public void SetLayers(LayerMask attachable)
    {
        _attachableLayers = attachable;
    }

    private void UpdateThrowing()
    {
        if (whipOrigin == null) return;
        _currentLength = Mathf.MoveTowards(_currentLength, _ropeLength, _throwSpeed * Time.deltaTime);

        Vector3 direction = (_tipTarget - whipOrigin.position).normalized;
        Vector3 currentTip = whipOrigin.position + direction * _currentLength;

        _rope.Simulate(whipOrigin.position, currentTip, _gravity * 0.2f, _damping);

        if (_currentLength >= _ropeLength * 0.95f)
            CheckAttach(currentTip, direction);
    }

    private void CheckAttach(Vector3 tipPosition, Vector3 direction)
    {
        Debug.DrawRay(tipPosition, direction, Color.red, 1f);
        Debug.Log($"CheckAttach | layers: {_attachableLayers.value}");
        
        if (Physics.SphereCast(
            tipPosition - direction * 0.5f,
            _attachRadius,
            direction,
            out RaycastHit hit,
            1f,
            _attachableLayers))
        {
            _attachPoint = hit.point;
            _state = WhipState.Attached;

            // Сохраняем текущую скорость CC как начальную для свинга
            // (если есть доступ — можно получить снаружи)
            _swingVelocity = characterController != null
                ? characterController.velocity
                : Vector3.zero;

            IWhipInteractable interactable = hit.collider.GetComponent<IWhipInteractable>();
            interactable?.OnWhipHit();
        }
        else
        {
            _state = WhipState.Retracting;
        }
    }

    private void UpdateSwing()
    {
        if (whipOrigin == null) return;
        if (characterController == null) return;

        Vector3 playerPos = playerTransform.position;
        Vector3 toAnchor = _attachPoint - playerPos;
        float ropeDistance = Vector3.Distance(playerPos, _attachPoint);

        // Гравитация
        _swingVelocity += Physics.gravity * Time.deltaTime;
        
        // Убираем компоненту velocity которая растягивает верёвку
        // (constraint: держим дистанцию постоянной)
        if (ropeDistance > 0.01f)
        {
            Vector3 ropeDir = toAnchor.normalized;
            float radialVelocity = Vector3.Dot(_swingVelocity, ropeDir);

            // Если игрок отдаляется от точки — убираем эту компоненту
            if (radialVelocity < 0)
                _swingVelocity -= ropeDir * radialVelocity;
        }

        // Дополнительное управление во время свинга (A/D)
        Vector3 inputDir = GetSwingInput();
        _swingVelocity += inputDir * _swingForce * Time.deltaTime;

        characterController.Move(_swingVelocity * Time.deltaTime);

        // Корректируем позицию чтобы не выходить за длину верёвки
        Vector3 newToAnchor = _attachPoint - playerTransform.position;
        if (newToAnchor.magnitude > _currentLength)
        {
            Vector3 corrected = _attachPoint - newToAnchor.normalized * _currentLength;
            Vector3 correction = corrected - playerTransform.position;
            characterController.Move(correction);

            // Убираем радиальную компоненту после коррекции
            Vector3 ropeDir = newToAnchor.normalized;
            float radial = Vector3.Dot(_swingVelocity, -ropeDir);
            if (radial > 0)
                _swingVelocity += ropeDir * radial;
        }

        _rope.Simulate(whipOrigin.position, _attachPoint, _gravity, _damping);
    }

    private Vector3 GetSwingInput()
    {
        if (playerTransform == null || PlayerMovement == null) return Vector3.zero;

        float h = PlayerMovement.MoveInput.x;
        float v = PlayerMovement.MoveInput.y;

        // Направление относительно камеры, но только горизонталь
        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Camera.main.transform.right;

        return (camRight * h + camForward * v);
    }

    private void UpdateRetracting()
    {
        _currentLength = Mathf.MoveTowards(_currentLength, 0f, _retractSpeed * Time.deltaTime);
        Vector3 currentTip = Vector3.Lerp(whipOrigin.position, _attachPoint, _currentLength / _ropeLength);

        _rope.Simulate(whipOrigin.position, currentTip, _gravity, _damping);

        if (_currentLength <= 0.05f)
            _state = WhipState.Idle;
    }

    private void UpdateRopeVisual()
    {
        if (whipOrigin == null) return;
        if (_state == WhipState.Idle)
        {
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