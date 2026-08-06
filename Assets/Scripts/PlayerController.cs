using UnityEngine;
using UnityEngine.InputSystem; // <-- ADDED: Needed for the New Input System

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>(); 
    }

    void Update()
    {
        HandleMovement();
        HandleAttack();
    }

    void HandleMovement()
    {
        // 1. Get Input using the NEW Input System
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
        }

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // 2. If the player is trying to move
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            controller.Move(direction * moveSpeed * Time.deltaTime);

            if (animator != null) animator.SetBool("IsMoving", true);
        }
        else
        {
            if (animator != null) animator.SetBool("IsMoving", false);
        }

        // 3. Simple Gravity
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }

    void HandleAttack()
    {
        // Attack using the NEW Input System (Spacebar or Left Mouse Button)
        bool attackPressed = false;
        
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            attackPressed = true;
            
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            attackPressed = true;

        if (attackPressed && animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }
}
