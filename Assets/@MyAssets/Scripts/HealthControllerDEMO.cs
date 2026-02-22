using UnityEngine;

public class HealthControllerDEMO : MonoBehaviour
{
    public int maxHits = 3;
    public Animator animator;
    public string dieTrigger = "Die";

    public float groundOffset = 0.02f;
    public LayerMask groundMask = ~0;

    int hits;
    public bool IsDead { get; private set; }

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    public void TakeHit(int amount = 1)
    {
        if (IsDead) return;

        hits += amount;
        if (hits < maxHits) return;

        IsDead = true;

        // bloquear control
        var pc = GetComponent<PlayerController>();
        if (pc) pc.movementLocked = true;

        // SNAP root del GameObject al suelo usando la cápsula
        var cap = GetComponent<CapsuleCollider>();
        if (cap)
        {
            Vector3 origin = cap.bounds.center + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f, groundMask))
            {
                float bottom = cap.bounds.min.y;
                float delta = (hit.point.y + groundOffset) - bottom;
                transform.position += new Vector3(0f, delta, 0f);
            }
        }

        // parar física del GO
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // ahora sí: desactiva cápsula para que no “sostenga” en el aire
        if (cap) cap.enabled = false;

        if (animator) animator.SetTrigger(dieTrigger);
    }
}