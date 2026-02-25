using System.Collections.Generic;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public LayerMask enemyMask;
    BoxCollider box;
    bool active;
    int damage;
    HashSet<EnemyHealthDEMO> hitThisSwing = new HashSet<EnemyHealthDEMO>();

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        if (box) box.isTrigger = true;
    }

    /// Llamado cada frame por PlayerCombat
    public void SetActive(bool isAttacking, int dmg)
    {
        if (isAttacking && !active)
        {
            // Empieza un nuevo swing
            active = true;
            damage = dmg;
            hitThisSwing.Clear();
            if (box) box.enabled = true;
        }
        else if (!isAttacking && active)
        {
            // Terminó el ataque
            active = false;
            hitThisSwing.Clear();
            if (box) box.enabled = false;
        }

        // Actualizar daño por si cambia entre light/heavy
        if (active) damage = dmg;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!active) return;
        if (((1 << other.gameObject.layer) & enemyMask.value) == 0) return;

        var eh = other.GetComponentInParent<EnemyHealthDEMO>();
        if (!eh) return;
        if (hitThisSwing.Contains(eh)) return;

        hitThisSwing.Add(eh);
        eh.TakeDamage(damage);
    }
}