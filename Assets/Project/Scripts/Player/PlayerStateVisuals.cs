using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace Player
{
    public class PlayerStateVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStateSystem stateSystem;
        [SerializeField] private PeakSystem peakSystem;
        [SerializeField] private Light directionalLight;
        [SerializeField] private Volume globalVolume;

        [Header("Colors")]
        [SerializeField] private Color yinLightColor = new Color(0.3f, 0.4f, 0.8f);
        [SerializeField] private Color yangLightColor = new Color(1.0f, 0.8f, 0.4f);
        [SerializeField] private Color yinFogColor = new Color(0.02f, 0.02f, 0.05f);
        [SerializeField] private Color yangFogColor = new Color(0.1f, 0.08f, 0.05f);

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Bloom _bloom;
        private ColorAdjustments _colorAdjustments;
        private ChromaticAberration _chromaticAberration;
        private Coroutine _transitionCoroutine;

        private void Awake()
        {
            if (stateSystem == null) stateSystem = GetComponent<PlayerStateSystem>();
            if (peakSystem == null) peakSystem = GetComponent<PeakSystem>();
            if (directionalLight == null) directionalLight = RenderSettings.sun;
            
            if (globalVolume != null && globalVolume.profile.TryGet(out _bloom)) { }
            if (globalVolume != null && globalVolume.profile.TryGet(out _colorAdjustments)) { }
            if (globalVolume != null && globalVolume.profile.TryGet(out _chromaticAberration)) { }
            else if (globalVolume != null)
            {
                _chromaticAberration = globalVolume.profile.Add<ChromaticAberration>();
                _chromaticAberration.active = true;
                _chromaticAberration.intensity.Override(0f);
            }
        }

        private void OnEnable()
        {
            if (stateSystem != null)
            {
                stateSystem.OnStateChanged += HandleStateChanged;
                stateSystem.OnInstabilityStarted += HandleInstabilityStarted;
                stateSystem.OnInstabilityEnded += HandleInstabilityEnded;
            }

            if (peakSystem != null)
            {
                peakSystem.OnPeakMeterChanged += HandlePeakMeterChanged;
                peakSystem.OnPeakReached += HandlePeakReached;
            }
        }

        private void OnDisable()
        {
            if (stateSystem != null)
            {
                stateSystem.OnStateChanged -= HandleStateChanged;
                stateSystem.OnInstabilityStarted -= HandleInstabilityStarted;
                stateSystem.OnInstabilityEnded -= HandleInstabilityEnded;
            }

            if (peakSystem != null)
            {
                peakSystem.OnPeakMeterChanged -= HandlePeakMeterChanged;
                peakSystem.OnPeakReached -= HandlePeakReached;
            }
        }

        private void HandleStateChanged(PlayerState newState)
        {
            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(TransitionToState(newState));
            
            // Trigger a quick pulse on switch
            StartCoroutine(FlashBloom(2.0f, 0.2f));
        }

        private void HandleInstabilityStarted()
        {
            if (_chromaticAberration != null)
                _chromaticAberration.intensity.Override(0.5f);
        }

        private void HandleInstabilityEnded()
        {
            if (_chromaticAberration != null)
                _chromaticAberration.intensity.Override(0f);
        }

        private void HandlePeakMeterChanged(float normalizedValue)
        {
            if (_bloom != null)
            {
                // Bloom intensity increases with peak meter
                float baseIntensity = 1.5f;
                _bloom.intensity.Override(baseIntensity + (normalizedValue * 2.0f));
            }
        }

        private void HandlePeakReached()
        {
            StartCoroutine(FlashBloom(5.0f, 0.5f));
            // Feedback for peak reached
        }

        private IEnumerator TransitionToState(PlayerState state)
        {
            float elapsed = 0f;
            Color startLightColor = directionalLight.color;
            Color targetLightColor = state == PlayerState.Yin ? yinLightColor : yangLightColor;
            
            Color startFogColor = RenderSettings.fogColor;
            Color targetFogColor = state == PlayerState.Yin ? yinFogColor : yangFogColor;

            while (elapsed < transitionDuration)
            {
                float t = transitionCurve.Evaluate(elapsed / transitionDuration);
                
                if (directionalLight != null)
                    directionalLight.color = Color.Lerp(startLightColor, targetLightColor, t);
                
                RenderSettings.fogColor = Color.Lerp(startFogColor, targetFogColor, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (directionalLight != null) directionalLight.color = targetLightColor;
            RenderSettings.fogColor = targetFogColor;
        }

        private IEnumerator FlashBloom(float intensity, float duration)
        {
            if (_bloom == null) yield break;
            
            float elapsed = 0f;
            float originalIntensity = _bloom.intensity.value;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float currentIntensity = Mathf.Lerp(intensity, originalIntensity, t);
                _bloom.intensity.Override(currentIntensity);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            _bloom.intensity.Override(originalIntensity);
        }
    }
}
