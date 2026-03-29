using Player;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameLogic : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private HealthSystem _healthSystem;
    [SerializeField] private PeakSystem _peaksystem;
    [SerializeField] private PlayerStateSystem _stateSystem;

    [Header("UI")]
    [SerializeField] private Slider HealthBar;
    [SerializeField] private Slider PeakBar;
    [SerializeField] private TextMeshProUGUI StateBar;
    [SerializeField] private Image PeakFillImage;

    [Header("Colors")]
    [SerializeField] private Color YinColor = Color.white;
    [SerializeField] private Color YangColor = Color.black;
    [SerializeField] private bool SmoothColorTransition = false;
    [SerializeField] private float ColorLerpSpeed = 10f;

    private Color _targetFillColor;

    private void Awake()
    {
        if (PeakFillImage == null && PeakBar != null && PeakBar.fillRect != null)
        {
            PeakFillImage = PeakBar.fillRect.GetComponent<Image>();
        }

        _targetFillColor = YinColor;
    }

    private void OnEnable()
    {
        if (_stateSystem != null)
        {
            _stateSystem.OnStateChanged += HandleStateChangeUI;
        }

        if (_peaksystem != null)
        {
            _peaksystem.OnPeakMeterChanged += HandlePeakBarChangeUI;
        }

        InitializeUI();
    }

    private void OnDisable()
    {
        if (_stateSystem != null)
        {
            _stateSystem.OnStateChanged -= HandleStateChangeUI;
        }

        if (_peaksystem != null)
        {
            _peaksystem.OnPeakMeterChanged -= HandlePeakBarChangeUI;
        }
    }

    private void Update()
    {
        if (SmoothColorTransition && PeakFillImage != null)
        {
            PeakFillImage.color = Color.Lerp(PeakFillImage.color, _targetFillColor, ColorLerpSpeed * Time.deltaTime);
        }
    }

    private void HandlePeakBarChangeUI(float peakval)
    {
        if (PeakBar != null)
        {
            PeakBar.value = peakval;
        }
    }

    private void HandleStateChangeUI(PlayerState newState)
    {
        if (StateBar != null)
        {
            StateBar.text = newState.ToString();
        }

        UpdateFillColor(newState);
    }

    private void InitializeUI()
    {
        if (HealthBar != null && _healthSystem != null)
        {
            HealthBar.maxValue = _healthSystem.MaxHealth;
            HealthBar.value = _healthSystem.CurrentHealth;
        }

        if (PeakBar != null && _peaksystem != null)
        {
            PeakBar.minValue = 0f;
            PeakBar.maxValue = _peaksystem.MaxPeakValue;
            PeakBar.value = _peaksystem.NormalizedPeakValue;
        }

        if (_stateSystem != null)
        {
            HandleStateChangeUI(_stateSystem.CurrentState);
        }
    }

    private void UpdateFillColor(PlayerState state)
    {
        Color desiredColor = state == PlayerState.Yin ? YinColor : YangColor;

        _targetFillColor = desiredColor;

        if (!SmoothColorTransition && PeakFillImage != null)
        {
            PeakFillImage.color = desiredColor;
        }
    }
}
