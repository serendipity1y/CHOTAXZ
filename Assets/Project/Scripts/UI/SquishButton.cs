using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace Project.UI
{
    public class SquishButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Settings")]
        [SerializeField] private RectTransform _squishTarget;
        [SerializeField] private Vector3 _pressedScale = new Vector3(0.9f, 0.9f, 1f);
        [SerializeField] private float _animationDuration = 0.1f;
        [SerializeField] private AnimationCurve _squishCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Vector3 _originalScale;
        private Coroutine _animationCoroutine;

        private void Awake()
        {
            if (_squishTarget == null)
            {
                _squishTarget = GetComponent<RectTransform>();
            }
            _originalScale = _squishTarget.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            StartAnimation(_pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StartAnimation(_originalScale);
        }

        private void StartAnimation(Vector3 targetScale)
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _animationCoroutine = StartCoroutine(AnimateScale(targetScale));
        }

        private IEnumerator AnimateScale(Vector3 targetScale)
        {
            Vector3 initialScale = _squishTarget.localScale;
            float elapsed = 0f;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = _squishCurve.Evaluate(elapsed / _animationDuration);
                _squishTarget.localScale = Vector3.Lerp(initialScale, targetScale, t);
                yield return null;
            }

            _squishTarget.localScale = targetScale;
            _animationCoroutine = null;
        }
    }
}
