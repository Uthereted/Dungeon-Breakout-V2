using UnityEngine;
using DungeonBreakoutV2;

[RequireComponent(typeof(Rigidbody))]
public class StoneProjectile : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 5f;

    private bool hasHit;

    public void SetOwner(GameObject owner)
    {
        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        Collider[] myColliders = GetComponentsInChildren<Collider>();

        foreach (Collider mine in myColliders)
            foreach (Collider theirs in ownerColliders)
                Physics.IgnoreCollision(mine, theirs);
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        if (HealthSystem.Instance != null && !HealthSystem.Instance.IsDead)
        {
            if (collision.transform.root.CompareTag("Player"))
            {
                HealthSystem.Instance.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }

        // Hit something else
        Destroy(gameObject, 0.5f);
    }
}
