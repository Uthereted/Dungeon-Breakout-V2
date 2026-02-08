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

    Rigidbody rb;
    Vector2 move;
    Vector2 look;
    bool sprint;
    bool jumpRequest;

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
    }

    // Input System (Send Messages)
    public void OnMove(InputValue v) => move = v.Get<Vector2>();
    public void OnLook(InputValue v) => look = v.Get<Vector2>();
    public void OnJump(InputValue v)
    {
        if (v.isPressed)
            jumpRequest = true;
    }

    void FixedUpdate()
    {
        sprint = sprintAction.IsPressed();

        Rotate();
        Move();
        Jump();
        jumpRequest = false;
    }

    void Rotate()
    {
        float yaw = look.x * mouseSensitivity;
        Quaternion targetRot =
            Quaternion.Euler(0f, rb.rotation.eulerAngles.y + yaw, 0f);

        rb.MoveRotation(targetRot);
    }

    void Move()
    {
        Vector3 dir =
            transform.forward * move.y +
            transform.right * move.x;

        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        float speed = sprint ? sprintSpeed : walkSpeed;

        Vector3 vel = rb.velocity;
        Vector3 target = dir * speed;

        rb.velocity = new Vector3(target.x, vel.y, target.z);
    }

    void Jump()
    {
        if (!jumpRequest) return;
        if (!IsGrounded()) return;

        // reset Y antes de saltar
        Vector3 vel = rb.velocity;
        vel.y = 0f;
        rb.velocity = vel;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            0.1f + groundCheckDistance
        );
    }
}
