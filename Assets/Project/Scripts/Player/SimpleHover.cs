using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Event-driven transform actions. No Update loop.
/// Call public functions from UnityEvents (buttons, triggers, whip hits, etc).
/// Each action animates over time via coroutine and runs only while active.
/// </summary>
public class SimpleHover : MonoBehaviour
{
    [Header("Extend (scale)")]
    [Tooltip("Local scale to reach when Extend() called. Retract() returns to start scale.")]
    [SerializeField] private Vector3 extendedScale = new Vector3(1f, 2f, 1f);
    [SerializeField] private float extendDuration = 0.5f;

    [Header("Rotate")]
    [Tooltip("Degrees applied (local) when Rotate() called.")]
    [SerializeField] private Vector3 rotateAmount = new Vector3(0f, 90f, 0f);
    [SerializeField] private float rotateDuration = 0.5f;

    [Header("Move")]
    [Tooltip("World-space offset from start position when Move() called. MoveBack() returns to start.")]
    [SerializeField] private Vector3 moveOffset = new Vector3(0f, 0f, 3f);
    [SerializeField] private float moveDuration = 0.5f;

    [Header("Destroy")]
    [Tooltip("Optional object to destroy. If null, destroys this gameObject.")]
    [SerializeField] private GameObject destroyTarget;
    [SerializeField] private float destroyDelay = 0f;

    [Header("Easing")]
    [Tooltip("Applied to all timed actions. Default linear if unset.")]
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Events")]
    public UnityEvent onActionComplete;

    private Vector3 _startPosition;
    private Vector3 _startScale;

    private Coroutine _moveCo;
    private Coroutine _scaleCo;
    private Coroutine _rotateCo;

    private void Awake()
    {
        _startPosition = transform.position;
        _startScale = transform.localScale;
    }

    // ---------- EXTEND (scale) ----------

    public void Extend() => StartScale(extendedScale, extendDuration);
    public void Retract() => StartScale(_startScale, extendDuration);
    public void ScaleTo(Vector3 targetScale) => StartScale(targetScale, extendDuration);

    private void StartScale(Vector3 target, float duration)
    {
        if (_scaleCo != null) StopCoroutine(_scaleCo);
        _scaleCo = StartCoroutine(ScaleRoutine(target, duration));
    }

    private IEnumerator ScaleRoutine(Vector3 target, float duration)
    {
        Vector3 from = transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.LerpUnclamped(from, target, Eval(elapsed, duration));
            yield return null;
        }
        transform.localScale = target;
        _scaleCo = null;
        onActionComplete?.Invoke();
    }

    // ---------- ROTATE ----------

    public void Rotate() => StartRotate(rotateAmount, rotateDuration);
    public void RotateBy(Vector3 degrees) => StartRotate(degrees, rotateDuration);

    private void StartRotate(Vector3 degrees, float duration)
    {
        if (_rotateCo != null) StopCoroutine(_rotateCo);
        _rotateCo = StartCoroutine(RotateRoutine(degrees, duration));
    }

    private IEnumerator RotateRoutine(Vector3 degrees, float duration)
    {
        Quaternion from = transform.localRotation;
        Quaternion to = from * Quaternion.Euler(degrees);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.SlerpUnclamped(from, to, Eval(elapsed, duration));
            yield return null;
        }
        transform.localRotation = to;
        _rotateCo = null;
        onActionComplete?.Invoke();
    }

    // ---------- MOVE ----------

    public void Move() => StartMove(_startPosition + moveOffset, moveDuration);
    public void MoveBack() => StartMove(_startPosition, moveDuration);
    public void MoveTo(Vector3 worldTarget) => StartMove(worldTarget, moveDuration);

    private void StartMove(Vector3 target, float duration)
    {
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = StartCoroutine(MoveRoutine(target, duration));
    }

    private IEnumerator MoveRoutine(Vector3 target, float duration)
    {
        Vector3 from = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.LerpUnclamped(from, target, Eval(elapsed, duration));
            yield return null;
        }
        transform.position = target;
        _moveCo = null;
        onActionComplete?.Invoke();
    }

    // ---------- DESTROY ----------

    public void DestroyObject()
    {
        GameObject go = destroyTarget != null ? destroyTarget : gameObject;
        Destroy(go, destroyDelay);
    }

    public void DestroyObject(GameObject target)
    {
        if (target != null) Destroy(target, destroyDelay);
    }

    // ---------- shared easing helper ----------

    /// <summary>Eased 0..1 progress. Returns 1 if duration <= 0 (instant).</summary>
    private float Eval(float elapsed, float duration)
    {
        if (duration <= 0f) return 1f;
        float t = Mathf.Clamp01(elapsed / duration);
        return ease != null ? ease.Evaluate(t) : t;
    }
}
