using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private WhipController _whip;
    [SerializeField] private Image _crosshair;      // 8x8 точка или текстура
    [SerializeField] private Image _whipIndicator;  // иконка или полоска

    // Цвета прицела — показывает можно ли зацепиться
    private static readonly Color _colorReady    = Color.white;
    private static readonly Color _colorAttached = new Color(0.4f, 1f, 0.4f); // зелёный
    private static readonly Color _colorNoTarget = new Color(1f, 0.4f, 0.4f); // красный

    
    private void Update()
    {
        if (_whip == null)
        {
            _whip = FindAnyObjectByType<WhipController>();
            return;
        }
        UpdateCrosshairColor();
    }

    private void UpdateCrosshairColor()
    {
        if (_whip == null) return;

        if (_whip.IsAttached)
        {
            _crosshair.color = _colorAttached;
            return;
        }

        // Проверяем есть ли цель по центру экрана
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        bool hasTarget = Physics.Raycast(ray, 8f, _whip.AttachableLayers);
        _crosshair.color = hasTarget ? _colorReady : _colorNoTarget;
    }
}