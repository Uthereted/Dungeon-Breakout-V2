using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerDemoController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float turnSpeed = 720f;

    [Header("Step Climb")]
    public float stepHeight = 0.35f;        
    public float stepCheckDistance = 0.45f; 
    public float stepSmooth = 10f;       

    private Rigidbody rb;
    private CapsuleCollider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(-h, 0f, -v).normalized;

        // Movimiento con Rigidbody (manteniendo la Y)
        Vector3 vel = rb.linearVelocity;
        Vector3 targetVel = inputDir * moveSpeed;
        rb.linearVelocity = new Vector3(targetVel.x, vel.y, targetVel.z);

        // Rotaci�n hacia donde camina
        if (inputDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDir, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
        }

        // Subir escalones (simple)
        StepClimb(inputDir);
    }

    void StepClimb(Vector3 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.001f) return;

        // Punto de raycast en la parte baja del collider
        Vector3 bottom = new Vector3(transform.position.x, col.bounds.min.y + 0.05f, transform.position.z);

        // Ray bajo (detecta obst�culo)
        if (Physics.Raycast(bottom, moveDir, out RaycastHit hitLower, stepCheckDistance))
        {
            // Ray alto (si arriba est� libre -> sube)
            Vector3 upper = bottom + Vector3.up * stepHeight;

            if (!Physics.Raycast(upper, moveDir, stepCheckDistance))
            {
                rb.MovePosition(rb.position + Vector3.up * stepSmooth * Time.fixedDeltaTime);
            }
        }
    }
}


