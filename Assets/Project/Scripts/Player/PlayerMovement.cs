using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour
{
    private CharacterController _CC;
    private Animator animator;

    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;
    public float SprintSpeed = 10f;
    public float inAirSprintMultiplier = 1f;
    public float speedPercent = 0f;

    [SerializeField] private CinemachineCamera _cincam;

    private Vector2 _move;
    private float verticalVelocity;
    private bool IsSprinting;
    private bool jumpRequested;
    private bool didJump;

    public Vector2 MoveInput => _move;

    

    public void OnMove(InputValue val)
    {
        _move = val.Get<Vector2>(); 
        
    }

    public void OnSprint(InputValue val)
    {
        if (val.isPressed)
        {
            IsSprinting = !IsSprinting;
        }
        
    }

    public void OnJump(InputValue val)
    {
        jumpRequested = val.isPressed;
        
    }

    private void Awake()
    {
        _CC = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        GameManager.Instance.RegisterPlayerMovement(this);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }


    void Update()
    {
        // 1. Calculate direction based on input and orientation
        Vector3 dir = (GetForward() * _move.y) + ( GetRight() * _move.x);
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
                didJump = true;
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

        speedPercent = IsSprinting ? 1f : _move.magnitude;

        animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
        animator.SetFloat("MotionSpeed", speedPercent);

        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("FreeFall", !isGrounded && verticalVelocity < 0);
        animator.SetBool("Jump", didJump);
        didJump = false;       
        
        // В конце Update()
        Vector3 camForward = Vector3.ProjectOnPlane(_cincam.transform.forward, Vector3.up).normalized;
        if (camForward != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(camForward);
        
    }

    private Vector3 GetForward()
    {
        Vector3 forward = _cincam.transform.forward;
        forward.y = 0;

        return forward.normalized;
    }
    
    private Vector3 GetRight()
    {
        Vector3 right = _cincam.transform.right;
        right.y = 0;

        return right.normalized;
    }
    
    public void OnFootstep()
    {
        // Execute footstep logic here
    }

    public void OnLand()
    {
        // Execute landing logic here
    }
}
