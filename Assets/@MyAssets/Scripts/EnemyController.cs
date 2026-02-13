using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;        // Arrastra aquí a tu Jugador

    [Header("Velocidades")]
    public float patrolSpeed = 2f;  // Velocidad tranquila al patrullar
    public float chaseSpeed = 4.5f; // Velocidad rápida al perseguir

    [Header("Comportamiento Patrulla")]
    public float patrolRadius = 10f;    // Cuánto se aleja del punto de inicio
    public float minWaitTime = 1f;      // Tiempo mínimo de espera al llegar
    public float maxWaitTime = 3f;      // Tiempo máximo de espera

    [Header("Comportamiento Persecución")]
    public float detectionRange = 8f;   // A qué distancia te ve

    // Variables internas
    private NavMeshAgent agent;
    private float waitTimer;      // Contador para la espera
    private bool isChasing;       // Para saber en qué estado estamos

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Al empezar, le damos un destino aleatorio inicial
        GoToRandomPoint();
    }

    void Update()
    {
        if (player == null) return;

        // 1. Calcular distancia con el jugador
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // --- LÓGICA DE DECISIÓN ---
        if (distanceToPlayer < detectionRange)
        {
            EngageChase(); // MODO PERSECUCIÓN
        }
        else
        {
            EngagePatrol(); // MODO PATRULLA
        }
    }

    void EngageChase()
    {
        isChasing = true;
        agent.speed = chaseSpeed; // Ponemos el turbo
        agent.stoppingDistance = 1.5f; // Para no chocarse contigo

        // Actualizamos el destino constantemente a donde esté el jugador
        agent.SetDestination(player.position);
    }

    void EngagePatrol()
    {
        // Si acabamos de salir de una persecución, reseteamos la velocidad
        if (isChasing)
        {
            isChasing = false;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = 0f; // Al patrullar queremos llegar justo al punto
        }

        // --- LÓGICA DE LLEGADA Y ESPERA ---

        // Comprobamos si ha llegado a su destino
        // (!pathPending es importante para que no crea que ha llegado mientras calcula la ruta)
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // Si ha llegado, restamos tiempo
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0)
            {
                // Si el tiempo se acabó, buscamos nuevo punto y reseteamos timer
                GoToRandomPoint();
            }
        }
    }

    void GoToRandomPoint()
    {
        // Elegimos un tiempo de espera aleatorio para la PRÓXIMA vez que pare
        waitTimer = Random.Range(minWaitTime, maxWaitTime);

        // Buscamos un punto en el mapa
        Vector3 randomPoint = GetRandomNavMeshPoint();
        agent.SetDestination(randomPoint);
    }

    // Función auxiliar para encontrar un punto válido en el suelo azul (NavMesh)
    Vector3 GetRandomNavMeshPoint()
    {
        // Buscamos un punto aleatorio dentro de una esfera
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        // SamplePosition intenta encontrar el punto válido más cercano en el suelo
        NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1);

        return hit.position;
    }

    // Dibujos para ver los rangos en el Editor
    void OnDrawGizmosSelected()
    {
        // Círculo ROJO = Rango de visión (Detection)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Círculo AMARILLO = Zona de patrulla
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}