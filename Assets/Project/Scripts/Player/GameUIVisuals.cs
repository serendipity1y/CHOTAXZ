using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Player
{
    public class GameUIVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStateSystem stateSystem;
        [SerializeField] private PeakSystem peakSystem;
        
        [Header("Sliders")]
        [SerializeField] private Slider peakSlider;
        [SerializeField] private Image peakFillImage;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        
        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI stateText;
        
        [Header("Colors")]
        [SerializeField] private Color yinColor = new Color(0.2f, 0.4f, 1.0f);
        [SerializeField] private Color yangColor = new Color(1.0f, 0.6f, 0.2f);
        [SerializeField] private Color healthColor = new Color(0.8f, 0.2f, 0.2f);
        
        private void Awake()
        {
            if (stateSystem == null) stateSystem = Object.FindAnyObjectByType<PlayerStateSystem>();
            if (peakSystem == null) peakSystem = Object.FindAnyObjectByType<PeakSystem>();
        }

        private void OnEnable()
        {
            if (stateSystem != null) stateSystem.OnStateChanged += HandleStateChanged;
            if (peakSystem != null) peakSystem.OnPeakMeterChanged += HandlePeakMeterChanged;
        }

        private void OnDisable()
        {
            if (stateSystem != null) stateSystem.OnStateChanged -= HandleStateChanged;
            if (peakSystem != null) peakSystem.OnPeakMeterChanged -= HandlePeakMeterChanged;
        }

        private void Start()
        {
            if (stateSystem != null) HandleStateChanged(stateSystem.CurrentState);
            if (healthFillImage != null) healthFillImage.color = healthColor;
        }

        private void HandleStateChanged(PlayerState newState)
        {
            Color targetColor = newState == PlayerState.Yin ? yinColor : yangColor;
            
            if (stateText != null)
            {
                stateText.text = newState.ToString().ToUpper();
                stateText.color = targetColor;
                StartCoroutine(PulseScale(stateText.transform, 1.2f, 0.2f));
            }
            
            if (peakFillImage != null)
            {
                peakFillImage.color = targetColor;
            }
        }

        private void HandlePeakMeterChanged(float normalizedValue)
        {
            if (peakSlider != null)
            {
                peakSlider.value = normalizedValue;
            }
        }

        private IEnumerator PulseScale(Transform t, float scale, float duration)
        {
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = Vector3.one * scale;
            
            float elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                t.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / (duration / 2f));
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                t.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / (duration / 2f));
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            t.localScale = originalScale;
        }
    }
}
