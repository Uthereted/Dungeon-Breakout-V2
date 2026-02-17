using UnityEngine;
using UnityEngine.AI;

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

    [Header("Animación (simple)")]
    public float animDamp = 0.12f;      // suaviza cambios de Speed
    public float attackLockTime = 0.7f; // tiempo bloqueado durante ataque (ajústalo al clip)

    private NavMeshAgent agent;
    private float waitTimer;
    private float attackTimer;
    private bool isChasing;

    private float attackLockTimer;

    // hashes para evitar typos
    int SpeedHash = Animator.StringToHash("Speed");
    int IsChasingHash = Animator.StringToHash("IsChasing");
    int AttackHash = Animator.StringToHash("Attack");

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = patrolSpeed;
        GoToRandomPoint();
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        attackTimer -= Time.deltaTime;
        if (attackLockTimer > 0f) attackLockTimer -= Time.deltaTime;

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

        // Si está “bloqueado” por ataque, no muevas
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

                    if (attackTimer <= 0f)
                    {
                        animator.SetTrigger(AttackHash);
                        attackTimer = attackCooldown;
                        attackLockTimer = attackLockTime;
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
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius + transform.position;
        randomDir.y = 0f;

        NavMeshHit hit;
        NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas);
        agent.SetDestination(hit.position);
    }

    void LookAtPlayer()
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetBool(IsChasingHash, isChasing);

        // Speed 0..1 (patrulla ~0.44, chase ~1 si chaseSpeed=4.5 y patrolSpeed=2)
        float speed01 = (chaseSpeed <= 0.01f) ? 0f : Mathf.Clamp01(agent.velocity.magnitude / chaseSpeed);

        if (agent.isStopped || agent.velocity.magnitude < 0.05f || attackLockTimer > 0f)
            speed01 = 0f;

        animator.SetFloat(SpeedHash, speed01, animDamp, Time.deltaTime);
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
