using UnityEngine;
using UnityEngine.AI;
using DungeonBreakoutV2;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Animator animator;

    [Header("Configuración")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4.5f;
    public float patrolRadius = 10f;
    public float detectionRange = 8f;
    public float loseRange = 12f;
    public float attackRange = 1.8f;
    public float attackCooldown = 1.5f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("Damage")]
    public float damage = 15f;

    [Header("SFX")]
    public AudioClip attackSfx;
    [Range(0f, 1f)] public float attackSfxVolume = 1f;

    [Header("Animación")]
    public float animDamp = 0.12f;
    public float attackLockTime = 0.7f;
    public float hitDelay = 0.35f;

    private NavMeshAgent agent;
    private float waitTimer;
    private float attackTimer;
    private bool isChasing;
    private float attackLockTimer;
    private EnemyHealth health;

    int SpeedHash = Animator.StringToHash("Speed");
    int IsChasingHash = Animator.StringToHash("IsChasing");
    int AttackHash = Animator.StringToHash("Attack");

    public BoxCollider patrolArea;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = patrolSpeed;
        health = GetComponent<EnemyHealth>();

        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        if (agent.isOnNavMesh)
        {
            GoToRandomPoint();
        }
    }

    void Update()
    {
        // ── Muerto: no hacer nada ──
        if (health != null && health.IsDead)
        {
            agent.isStopped = true;
            agent.ResetPath();
            CancelInvoke();
            return;
        }

        // ── Player muerto: parar ──
        if (HealthSystem.Instance != null && HealthSystem.Instance.IsDead)
        {
            agent.isStopped = true;
            agent.ResetPath();
            isChasing = false;
            UpdateAnimations();
            return;
        }

        if (player == null) return;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            UpdateAnimations();
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        attackTimer -= Time.deltaTime;
        if (attackLockTimer > 0f) attackLockTimer -= Time.deltaTime;

        // ── Detección / pérdida ──
        if (!isChasing && dist < detectionRange)
        {
            isChasing = true;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = attackRange * 0.9f;
        }
        else if (isChasing && dist > loseRange)
        {
            isChasing = false;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = 0f;
            GoToRandomPoint();
        }

        // ── Attack lock (esperando a que termine la animación) ──
        if (attackLockTimer > 0f)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
            LookAtPlayer();
        }
        else
        {
            if (isChasing)
            {
                if (dist <= attackRange)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    agent.ResetPath();
                    LookAtPlayer();

                    // ── No ataca si está stuneado ──
                    if (health != null && health.IsStunned)
                    {
                        UpdateAnimations();
                        return;
                    }

                    if (attackTimer <= 0f)
                    {
                        animator.SetTrigger(AttackHash);
                        attackTimer = attackCooldown;
                        attackLockTimer = attackLockTime;
                        Invoke(nameof(DealDamageToPlayer), hitDelay);
                    }
                }
                else
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
            }
            else
            {
                agent.isStopped = false;

                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0f) GoToRandomPoint();
                }
            }
        }

        UpdateAnimations();
    }

    void GoToRandomPoint()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        waitTimer = Random.Range(minWaitTime, maxWaitTime);

        Vector3 targetPoint;

        if (patrolArea != null)
        {
            Bounds b = patrolArea.bounds;
            targetPoint = new Vector3(
                Random.Range(b.min.x, b.max.x),
                transform.position.y,
                Random.Range(b.min.z, b.max.z)
            );
        }
        else
        {
            targetPoint = Random.insideUnitSphere * patrolRadius + transform.position;
            targetPoint.y = transform.position.y;
        }

        if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void LookAtPlayer()
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                10f * Time.deltaTime
            );
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            animator.SetBool(IsChasingHash, false);
            animator.SetFloat(SpeedHash, 0f, animDamp, Time.deltaTime);
            return;
        }

        animator.SetBool(IsChasingHash, isChasing);

        float speed01 = (chaseSpeed <= 0.01f)
            ? 0f
            : Mathf.Clamp01(agent.velocity.magnitude / chaseSpeed);

        if (agent.isStopped || agent.velocity.magnitude < 0.05f || attackLockTimer > 0f)
            speed01 = 0f;

        animator.SetFloat(SpeedHash, speed01, animDamp, Time.deltaTime);
    }

    public void DealDamageToPlayer()
    {
        // No pegar si estamos muertos o stuneados
        if (health != null && (health.IsDead || health.IsStunned)) return;
        if (HealthSystem.Instance == null || HealthSystem.Instance.IsDead) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange + 0.2f)
        {
            HealthSystem.Instance.TakeDamage(damage);

            if (attackSfx != null)
                AudioSource.PlayClipAtPoint(attackSfx, transform.position, attackSfxVolume);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, loseRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
