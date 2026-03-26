using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController _CC;
    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;
    public float SprintSpeed = 10f;
    public float inAirSprintMultiplier = 0.5f;

    public Transform orientation;

    private Vector2 _move;
    private float verticalVelocity;
    private bool IsSprinting;
    private bool jumpRequested;

    public void OnMove(InputValue val)
    {
        _move = val.Get<Vector2>(); 
    }

    public void OnSprint(InputValue val)
    {
        IsSprinting = val.isPressed;
    }

    public void OnJump(InputValue val)
    {
        jumpRequested = val.isPressed;
    }

    private void Awake()
    {
        _CC = GetComponent<CharacterController>();
    }

    
    void Update()
    {
        // 1. Calculate direction based on input and orientation
        Vector3 dir = (orientation.forward * _move.y) + (orientation.right * _move.x);
        dir.y = 0f;

        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        // 2. Determine Speed
        bool isGrounded = _CC.isGrounded;
        float currentSpeed = IsSprinting ? SprintSpeed : speed;

        if (!isGrounded)
            currentSpeed *= inAirSprintMultiplier;

        // 3. Handle Gravity & Jumping
        if (isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f; // Slight downward force to keep grounded

            if (jumpRequested)
            {
                verticalVelocity = jumpForce;
                jumpRequested = false; // Reset jump so we don't double jump
            }
        }

        verticalVelocity += gravity * Time.deltaTime;

        // 4. Final Movement
        Vector3 finalVelocity = (dir * currentSpeed) + (Vector3.up * verticalVelocity);
        CollisionFlags flags = _CC.Move(finalVelocity * Time.deltaTime);

        // Reset velocity if we hit a ceiling
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
    }
}
