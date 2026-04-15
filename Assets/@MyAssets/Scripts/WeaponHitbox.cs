using System.Collections.Generic;
using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    public LayerMask enemyMask;
    BoxCollider box;
    bool active;
    int damage;
    float knockback;
    Transform attacker;
    int currentSwingId = -1;
    HashSet<EnemyHealth> hitThisSwing = new HashSet<EnemyHealth>();

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        if (box) box.isTrigger = true;
    }

    public void SetActive(bool isAttacking, int dmg, float kb = 0f, Transform source = null, int swingId = 0)
    {
        if (isAttacking)
        {
            // new swing detected — clear hits even if we were already active
            if (swingId != currentSwingId)
            {
                currentSwingId = swingId;
                hitThisSwing.Clear();
            }

            if (!active)
            {
                active = true;
                if (box) box.enabled = true;
            }

            damage = dmg;
            knockback = kb;
            attacker = source;
        }
        else if (active)
        {
            active = false;
            currentSwingId = -1;
            hitThisSwing.Clear();
            if (box) box.enabled = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!active) return;
        if (((1 << other.gameObject.layer) & enemyMask.value) == 0) return;

        var eh = other.GetComponentInParent<EnemyHealth>();
        if (!eh || eh.IsDead) return;
        if (hitThisSwing.Contains(eh)) return;

        hitThisSwing.Add(eh);
        eh.TakeDamage(damage);

        if (knockback > 0f && attacker != null)
        {
            var enemyRb = other.GetComponentInParent<Rigidbody>();
            if (enemyRb && !enemyRb.isKinematic)
            {
                Vector3 dir = (other.transform.position - attacker.position).normalized;
                dir.y = 0f;
                enemyRb.AddForce(dir * knockback, ForceMode.Impulse);
            }
        }
    }
}
