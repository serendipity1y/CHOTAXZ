using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerWhip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _handPoint;
    [SerializeField] private GameObject _whipPrefab; // prefab просто с LineRenderer + WhipController

    [Header("Settings")]
    [SerializeField] private LayerMask _attachableLayers;
    [SerializeField] private float _cooldown = 0.5f;

    private CharacterController _cc;
    private WhipController _currentWhip;
    private float _lastAttackTime;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Start()
    {
        EquipWhip(_whipPrefab);
    }

    // Можно вызвать из Start или из системы инвентаря
    public void EquipWhip(GameObject whipPrefab)
    {
        if (_currentWhip != null)
            Destroy(_currentWhip.gameObject);

        GameObject whipGO = Instantiate(whipPrefab, _handPoint);
        whipGO.transform.localPosition = Vector3.zero;
        whipGO.transform.localRotation = Quaternion.identity;

        // LineRenderer — минимальная настройка
        LineRenderer lr = whipGO.AddComponent<LineRenderer>();
        lr.startWidth = 0.1f;
        lr.endWidth = 0.08f;
        // Определяй шейдер правильно
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color"); // fallback для Built-in
        lr.material = new Material(shader);
        lr.startColor = new Color(0.5f, 0.28f, 0.08f);
        lr.endColor = new Color(0.25f, 0.12f, 0.04f);

        lr.useWorldSpace = true;
        lr.numCapVertices = 4;

        _currentWhip = whipGO.AddComponent<WhipController>();
        _currentWhip.whipOrigin = _handPoint;
        _currentWhip.characterController = _cc;
        _currentWhip.playerTransform = transform;
        _currentWhip.PlayerMovement = GameManager.Instance.Movement;
        _currentWhip.SetLayers(_attachableLayers);
    }

    // Input System — назначь в Player Input компоненте
    public void OnAttack(InputValue value)
    {
        if (_currentWhip == null) return;
        if (Time.time < _lastAttackTime + _cooldown) return;

        if (value.isPressed)
        {
            if(_currentWhip.IsAttached)
                _currentWhip.Retract();
            else
                _currentWhip.Throw();
        }
    }
}