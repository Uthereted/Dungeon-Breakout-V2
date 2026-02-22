using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordController : MonoBehaviour
{
    [Header("Refs")]
    public Transform swordSocket;          // socket en la mano
    public PlayerInteractUI interactUI;    // UI "Grab [F]"
    public Animator animator;              // Animator del personaje
    public PlayerController player;        // tu script de movimiento (para bloquear)

    [Header("Grab Anim")]
    public string grabTrigger = "Grab";
    public float grabDuration = 1.0f;      // AJUSTA a la duración del clip (segundos)
    public string grabText = "Grab [F]";

    Transform equippedSword, nearbySword;
    bool grabbing;

    // =========================
    // ====== DEMO COMBAT ======
    // (Esto es SOLO DEMO para 3 hits y morir. Luego lo cambias por sistema real)
    [Header("DEMO Combat (solo demo)")]
    public string attackTrigger = "Attack";     // Trigger en Animator (si tienes anim de ataque)
    public float attackCooldown = 0.6f;
    public float attackRange = 2.0f;
    public LayerMask enemyMask;                 // pon enemigos en layer "Enemy"
    float attackTimer;
    // =========================

    void Awake()
    {
        if (!interactUI) interactUI = GetComponent<PlayerInteractUI>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!player) player = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
    }

    // Input System (Send Messages). Acción: "Interact" (F)
    public void OnInteract(InputValue v)
    {
        if (!v.isPressed || grabbing) return;
        if (nearbySword == null || equippedSword != null) return;

        StartCoroutine(GrabRoutine());
    }

    IEnumerator GrabRoutine()
    {
        grabbing = true;

        if (interactUI) interactUI.Hide();
        if (player) player.movementLocked = true;       // bloquea WASD/salto
        if (animator) animator.SetTrigger(grabTrigger); // anim pickup

        yield return new WaitForSeconds(grabDuration);

        EquipSword(nearbySword);

        grabbing = false;
        if (player) player.movementLocked = false;
    }

    void EquipSword(Transform sword)
    {
        if (!sword) return;

        var col = sword.GetComponent<Collider>();
        if (col) col.enabled = false;

        var rb = sword.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        sword.SetParent(swordSocket);
        sword.localPosition = Vector3.zero;
        sword.localRotation = Quaternion.identity;

        equippedSword = sword;
        nearbySword = null;
    }

    // =========================
    // ====== DEMO COMBAT ======
    // Input System: crea una acción "Attack" (ej: click izq) y que llame a OnAttack
    public void OnAttack(InputValue v)
    {
        if (!v.isPressed) return;

        // Solo puede atacar si tiene espada equipada
        if (equippedSword == null) return;

        // No atacar mientras recoge
        if (grabbing) return;

        // Cooldown simple
        if (attackTimer > 0f) return;
        attackTimer = attackCooldown;

        // Dispara anim si existe
        if (animator && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        // Hit demo instantáneo: da 1 golpe a un enemigo cercano delante
        Vector3 center = transform.position + transform.forward * 1.0f;
        Collider[] hits = Physics.OverlapSphere(center, attackRange, enemyMask);

        if (hits.Length == 0) return;

        // Busca Health en el enemigo
        HealthControllerDEMO h = hits[0].GetComponentInParent<HealthControllerDEMO>();
        if (h != null) h.TakeHit(1);
    }
    // =========================

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SwordPick")) return;
        if (equippedSword != null || grabbing) return;

        nearbySword = other.transform;
        if (interactUI) interactUI.Show(grabText);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("SwordPick")) return;
        if (nearbySword != other.transform) return;

        nearbySword = null;
        if (interactUI) interactUI.Hide();
    }

    // (Opcional) para ver el rango del ataque demo en escena
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position + transform.forward * 1.0f;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}