using Player;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour
{
    [SerializeField] private HealthSystem _healthSystem;
    [SerializeField] private PeakSystem _peaksystem;
    [SerializeField] private PlayerStateSystem _stateSystem;

    [Header("UI")]
    [SerializeField] private Slider HealthBar;
    [SerializeField] private Slider PeakBar;
    [SerializeField] private TextMeshProUGUI StateBar;
    [SerializeField] private GameObject gameOverPanel;

    [Header("State Colors")]
    [SerializeField] private Color yinColor = Color.white;
    [SerializeField] private Color yangColor = Color.black;
    [SerializeField] private float colorTransitionSpeed = 5f;

    private Image _peakBarFillImage;
    private Color _targetColor;
    private GameObject _player;

    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        
        if (_healthSystem == null && _player != null)
            _healthSystem = _player.GetComponent<HealthSystem>();
        
        if (_peaksystem == null && _player != null)
            _peaksystem = _player.GetComponent<PeakSystem>();
        
        if (_stateSystem == null && _player != null)
            _stateSystem = _player.GetComponent<PlayerStateSystem>();
        
        if (PeakBar != null)
            _peakBarFillImage = PeakBar.fillRect.GetComponent<Image>();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void Start()
    {
        // Re-subscribe in case Awake found references after OnEnable
        SubscribeToEvents();
        UpdateUI();
        InitializeStateColor();
    }

    private void SubscribeToEvents()
    {
        if (_stateSystem != null)
        {
            _stateSystem.OnStateChanged -= HandleStateChangeUI;
            _stateSystem.OnStateChanged += HandleStateChangeUI;
        }

        if (_peaksystem != null)
        {
            _peaksystem.OnPeakMeterChanged -= HandlePeakBarChangeUI;
            _peaksystem.OnPeakMeterChanged += HandlePeakBarChangeUI;
        }

        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged -= HandleHPChange;
            _healthSystem.OnHealthChanged += HandleHPChange;
            _healthSystem.OnDeath -= HandleDeath;
            _healthSystem.OnDeath += HandleDeath;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (_stateSystem != null)
            _stateSystem.OnStateChanged -= HandleStateChangeUI;

        if (_peaksystem != null)
            _peaksystem.OnPeakMeterChanged -= HandlePeakBarChangeUI;

        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged -= HandleHPChange;
            _healthSystem.OnDeath -= HandleDeath;
        }
            
        
    }

    private void Update()
    {
        UpdateColorTransition();
    }

    private void HandlePeakBarChangeUI(float peakval)
    {
        if (PeakBar != null)
            PeakBar.value = peakval;
    }

    private void HandleStateChangeUI(PlayerState newState)
    {
        if (StateBar != null)
            StateBar.text = newState.ToString();

        _targetColor = GetColorForState(newState);
    }

    private void HandleHPChange(int current, int max)
    {
        if (HealthBar != null)
        {
            HealthBar.maxValue = max;
            HealthBar.value = current;
        }
    }

    private void HandleDeath()
    {
        Cursor.lockState = CursorLockMode.None;
        GameOver();
        
    }

    private void InitializeStateColor()
    {
        if (_stateSystem != null && _peakBarFillImage != null)
        {
            _targetColor = GetColorForState(_stateSystem.CurrentState);
            _peakBarFillImage.color = _targetColor;
        }
    }

    private Color GetColorForState(PlayerState state)
    {
        return state == PlayerState.Yin ? yinColor : yangColor;
    }

    private void UpdateColorTransition()
    {
        if (_peakBarFillImage == null) return;

        _peakBarFillImage.color = Color.Lerp(
            _peakBarFillImage.color,
            _targetColor,
            colorTransitionSpeed * Time.deltaTime
        );
    }

    public void UpdateUI()
    {
        if (_healthSystem != null && HealthBar != null)
        {
            HealthBar.maxValue = _healthSystem.MaxHealth;
            HealthBar.value = _healthSystem.CurrentHealth;
        }

        if (_peaksystem != null && PeakBar != null)
        {
            PeakBar.maxValue = _peaksystem.MaxPeakValue;
            PeakBar.value = _peaksystem.CurrentPeakValue;
        }

        if (_stateSystem != null && StateBar != null)
        {
            StateBar.text = _stateSystem.CurrentState.ToString();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // unpause
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
    }

    public void GameOver()
    {
        Time.timeScale = 0f; // pause
        gameOverPanel.SetActive(true);
    }
    
}
