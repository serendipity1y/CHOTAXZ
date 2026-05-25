using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerWhip : MonoBehaviour
{
    public enum WhipSlot { Primary = 0, Secondary = 1 }

    [System.Serializable]
    private struct WhipSlotConfig
    {
        public Transform handPoint;
        public GameObject whipPrefab;
        public LayerMask attachableLayers;
        public LayerMask interactableLayers;
        public float cooldown;
    }

    [Header("Primary Whip (RMB)")]
    [SerializeField] private WhipSlotConfig _primary;

    [Header("Secondary Whip (LMB)")]
    [SerializeField] private WhipSlotConfig _secondary;

    private CharacterController _cc;

    private WhipController[] _whips = new WhipController[2];
    private float[] _lastAttackTime = new float[2];

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (_primary.whipPrefab != null)
            EquipWhip(WhipSlot.Primary, _primary.whipPrefab);
        // Secondary не экипируем — выдаётся по ходу игры
    }

    // Вызывается из системы прогрессии / инвентаря
    public void EquipWhip(WhipSlot slot, GameObject whipPrefab)
    {
        int i = (int)slot;
        WhipSlotConfig cfg = slot == WhipSlot.Primary ? _primary : _secondary;

        if (_whips[i] != null)
            Destroy(_whips[i].gameObject);

        if (cfg.handPoint == null)
        {
            Debug.LogWarning($"[PlayerWhip] handPoint not set for slot {slot}");
            return;
        }

        GameObject whipGO = Instantiate(whipPrefab, cfg.handPoint);
        whipGO.transform.localPosition = Vector3.zero;
        whipGO.transform.localRotation = Quaternion.identity;

        LineRenderer lr = whipGO.AddComponent<LineRenderer>();
        lr.startWidth = 0.1f;
        lr.endWidth = 0.08f;
        lr.useWorldSpace = true;
        lr.numCapVertices = 4;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Unlit/Color");
        
        lr.material = new Material(shader);
        lr.startColor = new Color(105, 44, 1);
        lr.endColor   = new Color(105, 44, 1);

        WhipController whip = whipGO.AddComponent<WhipController>();
        whip.whipOrigin        = cfg.handPoint;
        whip.characterController = _cc;
        whip.playerTransform   = transform;
        whip.PlayerMovement    = GameManager.Instance.Movement;
        whip.SetLayers(cfg.attachableLayers, cfg.interactableLayers);

        _whips[i] = whip;
    }

    // Input System callbacks — привязывай в Player Input:
    //   Attack        → OnAttack        (RMB / primary)
    //   AttackSecondary → OnAttackSecondary (LMB / secondary)

    public void OnAttack(InputValue value)
    {
        HandleInput(WhipSlot.Primary, value.isPressed);
    }

    public void OnAttackSecondary(InputValue value)
    {
        HandleInput(WhipSlot.Secondary, value.isPressed);
    }

    private void HandleInput(WhipSlot slot, bool pressed)
    {
        if (!pressed) return;

        int i = (int)slot;
        WhipController whip = _whips[i];

        if (whip == null) return;
        if (Time.time < _lastAttackTime[i] + GetCooldown(slot)) return;

        _lastAttackTime[i] = Time.time;

        if (whip.IsAttached)
            whip.Retract();
        else
            whip.Throw();
    }

    private float GetCooldown(WhipSlot slot) =>
        slot == WhipSlot.Primary ? _primary.cooldown : _secondary.cooldown;
}