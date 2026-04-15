using UnityEngine;
using UnityEngine.AI;
using DungeonBreakoutV2;

[RequireComponent(typeof(NavMeshAgent))]
public class RangedEnemyController : MonoBehaviour
{
    enum State { Patrol, Chase, Attack, Flee }

    [Header("Referencias")]
    public Transform player;
    public Animator animator;
    public Transform throwPoint;
    public GameObject stonePrefab;

    [Header("Configuracion")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4.5f;
    public float fleeSpeed = 5.5f;
    public float patrolRadius = 10f;
    public float detectionRange = 14f;
    public float loseRange = 18f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("Rangos de combate")]
    public float attackRange = 10f;
    public float fleeRange = 4f;
    public float safeRange = 9f;
    public float attackCooldown = 2.5f;
    public float fleeDelay = 1f;

    [Header("Projectile")]
    public int damage = 10;
    public float throwForce = 14f;
    public float throwDelay = 0.45f;

    [Header("Animacion")]
    public float animDamp = 0.12f;
    public float attackLockTime = 0.9f;

    public BoxCollider patrolArea;

    private NavMeshAgent agent;
    private EnemyHealth health;
    private State state = State.Patrol;
    private float waitTimer;
    private float attackTimer;
    private float attackLockTimer;
    private float fleeDelayTimer;

    int SpeedHash = Animator.StringToHash("Speed");
    int IsChasingHash = Animator.StringToHash("IsChasing");
    int AttackHash = Animator.StringToHash("Attack");

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
                agent.Warp(hit.position);
        }

        if (agent.isOnNavMesh) GoToRandomPoint();
    }

    void Update()
    {
        if (health != null && health.IsDead)
        {
            agent.isStopped = true;
            agent.ResetPath();
            CancelInvoke();
            return;
        }

        if (HealthSystem.Instance != null && HealthSystem.Instance.IsDead)
        {
            agent.isStopped = true;
            agent.ResetPath();
            state = State.Patrol;
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

        // Track how long the player has been too close
        if (dist < fleeRange && state != State.Flee)
            fleeDelayTimer += Time.deltaTime;
        else if (dist >= fleeRange)
            fleeDelayTimer = 0f;

        bool shouldFlee = dist < fleeRange && fleeDelayTimer >= fleeDelay;

        // ── State transitions ──
        switch (state)
        {
            case State.Patrol:
                if (dist < detectionRange)
                    state = State.Chase;
                break;

            case State.Chase:
                if (dist > loseRange)
                {
                    state = State.Patrol;
                    agent.stoppingDistance = 0f;
                    GoToRandomPoint();
                }
                else if (shouldFlee)
                    state = State.Flee;
                else if (dist <= attackRange)
                    state = State.Attack;
                break;

            case State.Attack:
                if (shouldFlee)
                {
                    state = State.Flee;
                }
                else if (attackLockTimer <= 0f)
                {
                    if (dist >= fleeRange)
                        state = State.Chase;
                }
                break;

            case State.Flee:
                fleeDelayTimer = 0f;
                if (dist > loseRange)
                {
                    state = State.Patrol;
                    agent.stoppingDistance = 0f;
                    GoToRandomPoint();
                }
                else if (dist >= safeRange)
                    state = State.Chase;
                break;
        }

        // ── State behavior ──
        switch (state)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;
                agent.isStopped = false;
                agent.stoppingDistance = 0f;
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0f) GoToRandomPoint();
                }
                break;

            case State.Chase:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                agent.stoppingDistance = attackRange * 0.8f;
                agent.SetDestination(player.position);
                break;

            case State.Attack:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
                LookAtPlayer();

                if (health != null && health.IsStunned) break;

                if (attackLockTimer <= 0f && attackTimer <= 0f)
                {
                    animator.SetTrigger(AttackHash);
                    attackTimer = attackCooldown;
                    attackLockTimer = attackLockTime;
                    Invoke(nameof(SpawnProjectile), throwDelay);
                }
                break;

            case State.Flee:
                agent.isStopped = false;
                agent.speed = fleeSpeed;
                agent.stoppingDistance = 0f;

                Vector3 awayDir = (transform.position - player.position).normalized;
                Vector3 fleeTarget = transform.position + awayDir * safeRange;

                if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                break;
        }

        UpdateAnimations();
    }

    void SpawnProjectile()
    {
        if (health != null && (health.IsDead || health.IsStunned)) return;
        if (stonePrefab == null || throwPoint == null || player == null) return;

        GameObject stone = Instantiate(stonePrefab, throwPoint.position, Quaternion.identity);

        StoneProjectile proj = stone.GetComponent<StoneProjectile>();
        if (proj != null)
        {
            proj.SetOwner(gameObject);
            proj.damage = damage;
        }

        Vector3 targetPos = player.position + Vector3.up * 1f;

        Rigidbody stoneRb = stone.GetComponent<Rigidbody>();
        if (stoneRb != null)
        {
            stoneRb.isKinematic = false;
            stoneRb.useGravity = true;

            Vector3 launchVel;
            if (CalculateLaunchVelocity(throwPoint.position, targetPos, throwForce, out launchVel))
                stoneRb.velocity = launchVel;
            else
            {
                // Fallback: direct shot if no valid arc
                Vector3 dir = (targetPos - throwPoint.position).normalized;
                stoneRb.velocity = dir * throwForce;
            }
        }
    }

    bool CalculateLaunchVelocity(Vector3 from, Vector3 to, float speed, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        float g = Physics.gravity.magnitude;

        Vector3 diff = to - from;
        float dx = new Vector3(diff.x, 0f, diff.z).magnitude;
        float dy = diff.y;

        float v2 = speed * speed;
        float v4 = v2 * v2;

        // Discriminant of the ballistic equation
        float disc = v4 - g * (g * dx * dx + 2f * dy * v2);
        if (disc < 0f) return false;

        // Use the lower arc (minus sqrt) for a flatter, more accurate shot
        float angle = Mathf.Atan2(v2 - Mathf.Sqrt(disc), g * dx);

        Vector3 horizontal = new Vector3(diff.x, 0f, diff.z).normalized;
        velocity = horizontal * Mathf.Cos(angle) * speed + Vector3.up * Mathf.Sin(angle) * speed;
        return true;
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
            agent.SetDestination(hit.position);
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

        animator.SetBool(IsChasingHash, state != State.Patrol);

        float speed01 = (chaseSpeed <= 0.01f)
            ? 0f
            : Mathf.Clamp01(agent.velocity.magnitude / chaseSpeed);

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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fleeRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, safeRange);
    }
}
