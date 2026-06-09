using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Player
{
    /// <summary>
    /// Blends Yin/Yang global volume weights in response to player state changes.
    /// Visual-only; subscribes to PlayerStateSystem events without touching gameplay logic.
    /// </summary>
    public class PlayerStateVolumeVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStateSystem stateSystem;
        [SerializeField] private Volume yinVolume;
        [SerializeField] private Volume yangVolume;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 0.4f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine _transitionCoroutine;
        private float _currentYinWeight;
        private float _currentYangWeight;

        private void Awake()
        {
            if (stateSystem == null)
                stateSystem = Object.FindAnyObjectByType<PlayerStateSystem>();

            if (yinVolume == null)
            {
                var yinObject = GameObject.Find("YinVolume");
                if (yinObject != null)
                    yinVolume = yinObject.GetComponent<Volume>();
            }

            if (yangVolume == null)
            {
                var yangObject = GameObject.Find("YangVolume");
                if (yangObject != null)
                    yangVolume = yangObject.GetComponent<Volume>();
            }
        }

        private void OnEnable()
        {
            if (stateSystem != null)
                stateSystem.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (stateSystem != null)
                stateSystem.OnStateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            if (stateSystem == null)
                return;

            ApplyImmediate(stateSystem.CurrentState);
        }

        private void HandleStateChanged(PlayerState newState)
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(TransitionToState(newState));
        }

        private void ApplyImmediate(PlayerState state)
        {
            float yinTarget = state == PlayerState.Yin ? 1f : 0f;
            float yangTarget = state == PlayerState.Yang ? 1f : 0f;

            SetWeights(yinTarget, yangTarget);
            _currentYinWeight = yinTarget;
            _currentYangWeight = yangTarget;
        }

        private IEnumerator TransitionToState(PlayerState state)
        {
            float targetYin = state == PlayerState.Yin ? 1f : 0f;
            float targetYang = state == PlayerState.Yang ? 1f : 0f;
            float startYin = _currentYinWeight;
            float startYang = _currentYangWeight;

            if (transitionDuration <= 0f)
            {
                ApplyImmediate(state);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                float t = transitionCurve.Evaluate(elapsed / transitionDuration);
                float yin = Mathf.Lerp(startYin, targetYin, t);
                float yang = Mathf.Lerp(startYang, targetYang, t);

                SetWeights(yin, yang);
                _currentYinWeight = yin;
                _currentYangWeight = yang;

                elapsed += Time.deltaTime;
                yield return null;
            }

            SetWeights(targetYin, targetYang);
            _currentYinWeight = targetYin;
            _currentYangWeight = targetYang;
        }

        private void SetWeights(float yinWeight, float yangWeight)
        {
            if (yinVolume != null)
                yinVolume.weight = yinWeight;

            if (yangVolume != null)
                yangVolume.weight = yangWeight;
        }
    }
}
