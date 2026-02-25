using UnityEngine;

public class EnemyHealthDEMO : MonoBehaviour
{
    [Header("Health")]
    public int maxHp = 3;
    public int hp;

    [Header("Stun")]
    public float stunDuration = 2.0f;
    float stunUntil;

    [Header("Animator")]
    public Animator animator;
    public string hitTrigger = "Hit";
    public string dieTrigger = "Die";
    public string isDeadBool = "IsDead";

    [Header("Disable on death")]
    public MonoBehaviour[] scriptsToDisable;
    public Collider[] collidersToDisable;
    public Rigidbody rb;

    bool dead;

    public bool IsDead => dead;
    public bool IsStunned => Time.time < stunUntil;

    void Awake()
    {
        hp = maxHp;
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (collidersToDisable == null || collidersToDisable.Length == 0)
            collidersToDisable = GetComponentsInChildren<Collider>();
    }

    public void TakeDamage(int dmg)
    {
        if (dead) return;
        hp -= dmg;
        stunUntil = Time.time + stunDuration;

        if (hp > 0)
        {
            if (animator) animator.SetTrigger(hitTrigger);
            return;
        }
        Die();
    }

    void Die()
    {
        dead = true;
        hp = 0;

        gameObject.layer = 0;
        foreach (Transform child in GetComponentsInChildren<Transform>())
            child.gameObject.layer = 0;

        if (animator)
        {
            if (!string.IsNullOrEmpty(isDeadBool)) animator.SetBool(isDeadBool, true);
            animator.SetTrigger(dieTrigger);
        }

        foreach (var s in scriptsToDisable)
            if (s) s.enabled = false;
        foreach (var c in collidersToDisable)
            if (c) c.enabled = false;
        if (rb) rb.isKinematic = true;

        var enemy = GetComponent<EnemyController>();
        if (enemy) enemy.CancelInvoke();
    }
}