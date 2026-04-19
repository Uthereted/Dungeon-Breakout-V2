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
    public Transform cameraPivot;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float groundCheckDistance = 0.25f;

    [Header("Stairs / Slopes")]
    public float groundStickForce = 10f;
    public float maxSlopeAngle = 50f;
    public float groundCheckRadius = 0.3f;

    [Header("Animation")]
    public Animator animator;
    public float groundedGraceTime = 0.15f;

    Rigidbody rb;
    Vector2 move;
    Vector2 look;
    float pitch;
    bool sprintInput;
    bool isSprinting;
    bool jumpRequest;
    float lastGroundedTime;
    float jumpCooldownUntil;
    bool grounded;
    RaycastHit groundHit;

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
        UpdateCameraPitch();
    }

    void FixedUpdate()
    {
        bool rawGrounded = CheckGrounded(out groundHit);
        // Ignore ground detection briefly after jumping so stick force doesn't cancel the jump
        grounded = rawGrounded && Time.time >= jumpCooldownUntil;
        if (grounded) lastGroundedTime = Time.time;

        if (!movementLocked)
        {
            Rotate();
            Jump();
            Move();
        }
        else
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        }

        jumpRequest = false;
    }

    void UpdateAnimation()
    {
        if (movementLocked)
        {
            if (animator == null) return;

            animator.SetFloat("InputX", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("InputY", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("Speed", 0f);

            // no haga "Turn" mientras est� atacando/recogiendo
            animator.SetFloat("Turn", 0f, 0.1f, Time.deltaTime);

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
        animator.SetBool("IsGrounded", IsGroundedForAnimation());

        // --- 6. Velocidad global ---
        animator.speed = 1f;
    }

    void Rotate()
    {
        float yaw = look.x * mouseSensitivity;
        Quaternion targetRot = Quaternion.Euler(0f, rb.rotation.eulerAngles.y + yaw, 0f);
        rb.MoveRotation(targetRot);
    }

    void UpdateCameraPitch()
    {
        if (cameraPivot)
        {
            pitch -= look.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void Move()
    {
        Vector3 dir = transform.forward * move.y + transform.right * move.x;

        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        if (grounded && Vector3.Angle(groundHit.normal, Vector3.up) < maxSlopeAngle)
        {
            // Proyectar movimiento sobre la superficie del suelo (escaleras/rampas)
            Vector3 slopeDir = Vector3.ProjectOnPlane(dir, groundHit.normal).normalized;
            Vector3 target = slopeDir * speed;

            rb.velocity = target + Vector3.down * groundStickForce * Time.fixedDeltaTime;
        }
        else
        {
            Vector3 target = dir * speed;
            rb.velocity = new Vector3(target.x, rb.velocity.y, target.z);
        }
    }

    void Jump()
    {
        if (!jumpRequest) return;
        if (!grounded) return;

        if (animator != null) animator.SetTrigger("Jump");

        Vector3 vel = rb.velocity;
        vel.y = 0f;
        rb.velocity = vel;

        lastGroundedTime = -1f;
        grounded = false;
        jumpCooldownUntil = Time.time + 0.25f; // ignore ground for 0.25s so jump isn't cancelled
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    bool CheckGrounded(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * (groundCheckRadius + 0.05f);
        return Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out hit,
            0.05f + groundCheckDistance, ~0, QueryTriggerInteraction.Ignore);
    }

    bool IsGroundedForAnimation()
    {
        // Gracia para no activar fall animation en escalones pequeños
        return grounded || (Time.time - lastGroundedTime < groundedGraceTime);
    }
}