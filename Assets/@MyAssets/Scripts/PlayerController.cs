using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 9f;

    [Header("Look")]
    public float mouseSensitivity = 0.12f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float groundCheckDistance = 0.25f;

    [Header("Animation")]
    public Animator animator;

    Rigidbody rb;
    Vector2 move;
    Vector2 look;
    bool sprintInput; // Variable para saber si pulsa la tecla
    bool isSprinting; // Variable real (Tecla + Dirección correcta)
    bool jumpRequest;

    public bool movementLocked;

    PlayerInput playerInput;
    InputAction sprintAction;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        playerInput = GetComponent<PlayerInput>();
        sprintAction = playerInput.actions["Sprint"];

        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public void OnMove(InputValue v)
    {
        if (movementLocked) { move = Vector2.zero; return; }
        move = v.Get<Vector2>();
    }

    public void OnLook(InputValue v)
    {
        if (movementLocked) { look = Vector2.zero; return; } // si quieres que tampoco rote
        look = v.Get<Vector2>();
    }

    public void OnJump(InputValue v)
    {
        if (movementLocked) return;
        if (v.isPressed) jumpRequest = true;
    }

    void Update()
    {
        sprintInput = sprintAction.IsPressed();

        if (sprintInput && move.y > 0.1f)
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }

        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (!movementLocked)
        {
            Rotate();
            Move();
            Jump();
        }
        else
        {
            // Nos quedamos quietos
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        }

        jumpRequest = false;
    }

    void UpdateAnimation()
    {
        if (movementLocked)
        {
            animator.SetFloat("InputX", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("InputY", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("Speed", 0f);
            return;
        }

        if (animator == null) return;

        // --- 1. Calcular Intensidad ---
        float targetIntensity = 0f;
        if (move.sqrMagnitude > 0)
        {
            targetIntensity = isSprinting ? 1f : 0.5f;
        }

        // --- 2. Movimiento (InputX, InputY) ---
        float targetX = move.x * targetIntensity;
        float targetY = move.y * targetIntensity;

        animator.SetFloat("InputX", targetX, 0.1f, Time.deltaTime);
        animator.SetFloat("InputY", targetY, 0.1f, Time.deltaTime);

        // --- 3. GIRO (Turn) ---
        animator.SetFloat("Turn", look.x, 0.1f, Time.deltaTime);

        // --- 4. Saltos (Speed) ---
        animator.SetFloat("Speed", targetIntensity);

        // --- 5. Suelo ---
        animator.SetBool("IsGrounded", IsGrounded());

        // --- 6. Velocidad global ---
        animator.speed = 1f;
    }

    void Rotate()
    {
        float yaw = look.x * mouseSensitivity;
        Quaternion targetRot = Quaternion.Euler(0f, rb.rotation.eulerAngles.y + yaw, 0f);
        rb.MoveRotation(targetRot);
    }

    void Move()
    {
        Vector3 dir = transform.forward * move.y + transform.right * move.x;

        if (dir.sqrMagnitude > 1f) dir.Normalize();

        // <--- CAMBIO: Usamos la variable isSprinting calculada arriba
        float speed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 vel = rb.velocity;
        Vector3 target = dir * speed;

        rb.velocity = new Vector3(target.x, vel.y, target.z);
    }

    void Jump()
    {
        if (!jumpRequest) return;
        if (!IsGrounded()) return;

        if (animator != null) animator.SetTrigger("Jump");

        Vector3 vel = rb.velocity;
        vel.y = 0f;
        rb.velocity = vel;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.1f + groundCheckDistance);
    }
}