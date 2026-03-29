using System;
using Player;
using UnityEngine;
using UnityEngine.UI;

public class ColorChanger : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Color yangColor = Color.white;
    [SerializeField] private Color yinColor = Color.black;
    [SerializeField] private PlayerStateSystem stateSystem;

    private Color targetColor;
    private void Awake()
    {
        if (stateSystem == null)
        {
            stateSystem = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStateSystem>();
        }
        if (image == null) 
            image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        stateSystem.OnStateChanged += HandleColorChange;
    }

    private void OnDisable()
    {
        stateSystem.OnStateChanged -= HandleColorChange;
    }

    private void HandleColorChange(PlayerState state)
    {
        targetColor = GetColorForState(state);
        image.color = targetColor;
    }

    private Color GetColorForState(PlayerState state)
    {
        return state == PlayerState.Yin ? yinColor : yangColor;
    }

    private void InitializeStateColor()
    {
        targetColor = GetColorForState(stateSystem.CurrentState);
        image.color = targetColor;
    }
    void Start()
    {
        InitializeStateColor();
    }

    
    void Update()
    {
        
    }
}
