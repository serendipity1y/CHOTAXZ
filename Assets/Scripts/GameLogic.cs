using System;
using Player;
using UnityEngine;
using UnityEngine.UI;

public class GameLogic : MonoBehaviour
{
    [SerializeField] private HealthSystem _healthSystem;
    [SerializeField] private PeakSystem _peaksystem;
    [SerializeField] private PlayerStateSystem _stateSystem;
    
    private GameObject Player;
    [Header("UI")]
    public Slider HealthBar;

    public Slider PeakBar;
    public GameObject StateBar;
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

    void Start()
    {
        SetValues();
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetValues()
    {
        HealthBar.maxValue = _healthSystem.MaxHealth;
        PeakBar.maxValue = _peaksystem.MaxPeakValue;
        
        HealthBar.value = _healthSystem.CurrentHealth;
        PeakBar.value = _peaksystem.CurrentPeakValue;
    }
    
}
