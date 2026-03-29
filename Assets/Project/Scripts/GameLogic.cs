using Player;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameLogic : MonoBehaviour
{
    [SerializeField] private HealthSystem _healthSystem;
    [SerializeField] private PeakSystem _peaksystem;
    [SerializeField] private PlayerStateSystem _stateSystem;
    
    private GameObject Player;
    [Header("UI")]
    public Slider HealthBar;

    public Slider PeakBar;
    public TextMeshProUGUI StateBar;
    private void Awake()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        if (_healthSystem == null)
        {
            _healthSystem = Player.GetComponent<HealthSystem>();
        }

        if (_peaksystem == null)
        {
            _peaksystem = Player.GetComponent<PeakSystem>();
        }

        if (_stateSystem == null)
        {
            _stateSystem = Player.GetComponent<PlayerStateSystem>();
        }
        
        
    }

    private void OnEnable()
    {
        _stateSystem.OnStateChanged += HandleStateChangeUI;
        _peaksystem.OnPeakMeterChanged += HandlePeakBarChangeUI;
    }

    private void OnDisable()
    {
        _stateSystem.OnStateChanged -= HandleStateChangeUI;
        _peaksystem.OnPeakMeterChanged -= HandlePeakBarChangeUI;
    }

    void Start()
    {
        SetValues();
        
        
    }

    
    void Update()
    {
        
    }

    void HandlePeakBarChangeUI(float peakval)
    {
        PeakBar.value = peakval;
    }

    void HandleStateChangeUI(PlayerState newState)
    {
        StateBar.text = newState.ToString();
    }

    public void SetValues()
    {
        HealthBar.maxValue = _healthSystem.MaxHealth;
        PeakBar.maxValue = _peaksystem.MaxPeakValue;
        
        HealthBar.value = _healthSystem.CurrentHealth;
        PeakBar.value = _peaksystem.CurrentPeakValue;
    }
    
}
