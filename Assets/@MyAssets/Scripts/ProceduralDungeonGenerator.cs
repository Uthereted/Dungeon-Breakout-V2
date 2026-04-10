using System.Collections.Generic;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class SpecialRoomRule
{
    public SpecialRoomType type;
    public GameObject[] prefabs;
    public int maxCount = 1;
}

public class ProceduralDungeonGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] corridorPrefabs;
    public GameObject[] roomPrefabs;

    [Header("Configuración")]
    public Transform[] startPoints;
    public int maxPieces = 20;

    [Tooltip("0 = aleatorio cada vez")]
    public int seed = 0;

    [Range(0f, 1f)]
    public float roomChance = 0.4f;

    [Header("Colisiones")]
    public LayerMask dungeonLayer;
    public int maxPlacementAttempts = 6;
    public float overlapShrink = 0.1f;

    [Tooltip("La sala principal de la escena, se registra como obstáculo antes de generar")]
    public GameObject mainRoom;

    [Header("NavMesh")]
    public Transform NavRoot;
    public NavMeshSurface navSurface;

    private readonly List<Queue<(ConnectorPoint connector, bool mustBeCorridor, StairDirection lastStair)>> branchQueues
        = new List<Queue<(ConnectorPoint, bool, StairDirection)>>();

    private int placedCount = 0;
    private int activeBranch = 0;

    [Header("Enemigos")]
    public GameObject[] enemyPrefabs;
    public int minEnemiesPerPiece = 1;
    public int maxEnemiesPerPiece = 2;
    [Range(0f, 1f)] public float enemySpawnChance = 0.7f;
    private readonly List<EnemySetup> enemyRooms = new List<EnemySetup>();

    [Header("Salas especiales")]
    public SpecialRoomRule[] specialRoomRules;
    private readonly Dictionary<SpecialRoomType, int> specialRoomCounts = new Dictionary<SpecialRoomType, int>();

    [Header("Decoraciones")]
    public GameObject[] decorationPrefabs;
    [Range(0f, 1f)] public float decoSpawnChance = 0.8f;
    public LayerMask decoCollisionLayer;
    private readonly List<List<Transform>> decoSpawnPoints = new List<List<Transform>>();


    // ---------------------------------------------------------------
    void Awake()
    {
        if (seed != 0) Random.InitState(seed);
        Generate();
        StartCoroutine(BakeNavNextFrame());
    }

    // ---------------------------------------------------------------
    void Generate()
    {
        int effectiveMax = maxPieces + 2;

        if (mainRoom != null && NavRoot != null && mainRoom.transform.parent != NavRoot)
        {
            mainRoom.transform.SetParent(NavRoot, true);
        }

        // Registrar la sala principal como obstáculo fijo
        if (mainRoom != null)
        {
            SetupBoundsCheck(mainRoom);
            Physics.SyncTransforms();
        }

        // Generar una pieza inicial desde cada punto de inicio, cada una con su propia cola
        foreach (Transform sp in startPoints)
        {
            if (sp == null) continue;

            var queue = new Queue<(ConnectorPoint, bool, StairDirection)>();
            branchQueues.Add(queue);

            GameObject first = TrySpawnAndAlign(corridorPrefabs, sp);
            if (first == null)
            {
                Debug.LogWarning($"[DungeonGen] No se pudo colocar pieza inicial en {sp.name}");
                continue;
            }

            var firstPiece = first.GetComponent<DungeonPiece>();
            RegisterExits(firstPiece, mustBeCorridor: false, lastStair: firstPiece.stairDirection);
        }

        int safety = 0;

        // Round-robin: procesar un conector de cada rama por turno
        while (placedCount < effectiveMax && BranchesHaveConnectors())
        {
            if (++safety > 5000) { Debug.LogWarning("DungeonGen: safety break"); break; }

            // Buscar la siguiente rama que tenga conectores
            var queue = NextActiveBranch();
            if (queue == null) break;

            var (connector, mustBeCorridor, lastStair) = queue.Dequeue();
            if (connector.isConnected) continue;

            bool isLastSlot = placedCount >= effectiveMax - 1;

            if (isLastSlot)
            {
                CloseWithRoom(connector, mustBeCorridor);
                continue;
            }

            bool placeRoom = !mustBeCorridor && Random.value < roomChance;
            GameObject[] corridorPool = FilterCorridorsByStair(corridorPrefabs, lastStair);
            GameObject[] pool = placeRoom ? roomPrefabs : corridorPool;

            GameObject spawned = TrySpawnAndAlign(pool, connector.transform);

            // Si falla con el tipo preferido intenta el otro
            if (spawned == null)
            {
                GameObject[] fallback = placeRoom ? corridorPool : roomPrefabs;
                spawned = TrySpawnAndAlign(fallback, connector.transform);
                placeRoom = !placeRoom;
            }

            // Si sigue sin poder, descartar conector
            if (spawned == null)
            {
                Debug.LogWarning($"[DungeonGen] Conector descartado por colisiones.");
                connector.isConnected = true;
                continue;
            }

            connector.isConnected = true;
            var piece = spawned.GetComponent<DungeonPiece>();
            if (piece.entrance != null) piece.entrance.isConnected = true;

            StairDirection newStair = piece.stairDirection;
            RegisterExits(piece, mustBeCorridor: placeRoom, lastStair: newStair);
        }

        // Recoger todos los conectores abiertos
        var openConnectors = new List<(ConnectorPoint connector, bool mustBeCorridor)>();
        foreach (var queue in branchQueues)
        {
            while (queue.Count > 0)
            {
                var (connector2, mustBeCorridor2, _) = queue.Dequeue();
                if (!connector2.isConnected)
                    openConnectors.Add((connector2, mustBeCorridor2));
            }
        }

        // Barajar para elegir al azar cuáles serán salas especiales
        for (int i = openConnectors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (openConnectors[i], openConnectors[j]) = (openConnectors[j], openConnectors[i]);
        }

        // Reservar conectores para salas especiales y colocarlas primero
        int reserveCount = 0;
        if (specialRoomRules != null)
            foreach (var rule in specialRoomRules)
                reserveCount += rule.maxCount;

        var specialConnectors = openConnectors.GetRange(0, Mathf.Min(reserveCount, openConnectors.Count));
        var normalConnectors = openConnectors.GetRange(specialConnectors.Count, openConnectors.Count - specialConnectors.Count);

        // Cerrar conectores normales con salas normales
        foreach (var (conn, mustCorr) in normalConnectors)
        {
            if (!conn.isConnected)
                CloseWithRoom(conn, mustCorr);
        }

        // Colocar salas especiales en los conectores reservados
        PlaceSpecialRooms(specialConnectors);

    }
    IEnumerator BakeNavNextFrame()
    {
        yield return null; // Esperar un frame para asegurarse de que todas las piezas estén colocadas

        Physics.SyncTransforms();

        if (navSurface != null)
            navSurface.BuildNavMesh();

        SpawnDecorations();
        SpawnEnemiesInPieces();

        // Para el enemigo de la sala principal no dar warning por NavMeshAgent sin NavMesh
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        foreach (EnemyController enemy in enemies)
        {
            NavMeshAgent enemyAgent = enemy.GetComponent<NavMeshAgent>();
            if (enemyAgent != null && !enemyAgent.enabled)
                enemyAgent.enabled = true;
        }

        RangedEnemyController[] rangedEnemies = FindObjectsByType<RangedEnemyController>(FindObjectsSortMode.None);

        foreach (RangedEnemyController enemy in rangedEnemies)
        {
            NavMeshAgent enemyAgent = enemy.GetComponent<NavMeshAgent>();
            if (enemyAgent != null && !enemyAgent.enabled)
                enemyAgent.enabled = true;
        }
    }

    void SpawnEnemiesInPieces()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        foreach (EnemySetup room in enemyRooms)
        {
            if (room == null || room.spawnPoints == null || room.spawnPoints.Length == 0) continue;
            if (Random.value > enemySpawnChance) continue;

            int amount = Random.Range(minEnemiesPerPiece, maxEnemiesPerPiece + 1);
            amount = Mathf.Min(amount, room.spawnPoints.Length);

            int[] indices = ShuffledIndices(room.spawnPoints.Length);

            for (int i = 0; i < amount; i++)
            {
                Transform spawnPoint = room.spawnPoints[indices[i]];
                GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

                Vector3 spawnPos = spawnPoint.position;
                Quaternion spawnRot = spawnPoint.rotation;

                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                }

                GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, spawnRot, NavRoot);

                EnemyController enemyController = enemyObj.GetComponent<EnemyController>();
                if (enemyController != null)
                {
                    enemyController.patrolArea = room.patrolArea;
                }

                RangedEnemyController rangedController = enemyObj.GetComponent<RangedEnemyController>();
                if (rangedController != null)
                {
                    rangedController.patrolArea = room.patrolArea;
                }
            }
        }
    }

    void SpawnDecorations()
    {
        if (decorationPrefabs == null || decorationPrefabs.Length == 0) return;

        foreach (List<Transform> points in decoSpawnPoints)
        {
            foreach (Transform point in points)
            {
                if (point == null) continue;
                if (Random.value > decoSpawnChance) continue;

                // Barajar prefabs para probar varios si hay colisión
                int[] prefabIndices = ShuffledIndices(decorationPrefabs.Length);
                bool placed = false;

                for (int i = 0; i < decorationPrefabs.Length; i++)
                {
                    GameObject prefab = decorationPrefabs[prefabIndices[i]];
                    GameObject obj = Instantiate(prefab, point.position, point.rotation, point);

                    // Asignar layer de decoración para que el OverlapBox los detecte entre sí
                    int decoLayer = LayerMaskToLayer(decoCollisionLayer);
                    SetLayerRecursive(obj, decoLayer);

                    Physics.SyncTransforms();

                    if (DecoHasOverlap(obj))
                    {
                        Destroy(obj);
                        Physics.SyncTransforms();
                        continue;
                    }

                    // Añadir NavMeshObstacle para que los enemigos esquiven la decoración
                    BoxCollider bc = obj.GetComponent<BoxCollider>();
                    if (bc == null) bc = obj.GetComponentInChildren<BoxCollider>();
                    if (bc != null)
                    {
                        NavMeshObstacle obstacle = obj.AddComponent<NavMeshObstacle>();
                        obstacle.shape = NavMeshObstacleShape.Box;
                        obstacle.center = bc.center;
                        obstacle.size = bc.size * 0.6f;
                        obstacle.carving = true;
                    }

                    placed = true;
                    break;
                }

                if (!placed)
                    Debug.Log($"[DungeonGen] Decoración descartada en {point.name}: todas colisionan");
            }
        }
    }

    bool DecoHasOverlap(GameObject obj)
    {
        BoxCollider bc = obj.GetComponent<BoxCollider>();
        if (bc == null) bc = obj.GetComponentInChildren<BoxCollider>();
        if (bc == null) return false;

        Vector3 worldCenter = bc.transform.TransformPoint(bc.center);
        Vector3 worldExtents = Vector3.Scale(bc.size * 0.5f, bc.transform.lossyScale);

        // Reducir un poco para evitar falsos positivos con superficies adyacentes
        worldExtents -= Vector3.one * 0.02f;
        worldExtents = new Vector3(
            Mathf.Max(worldExtents.x, 0.01f),
            Mathf.Max(worldExtents.y, 0.01f),
            Mathf.Max(worldExtents.z, 0.01f)
        );

        Collider[] hits = Physics.OverlapBox(
            worldCenter,
            worldExtents,
            bc.transform.rotation,
            decoCollisionLayer,
            QueryTriggerInteraction.Ignore
        );

        foreach (var hit in hits)
        {
            // Ignorar el propio collider
            if (hit.transform.IsChildOf(obj.transform) || hit.transform == obj.transform)
                continue;
            return true;
        }

        return false;
    }

    // ---------------------------------------------------------------
    GameObject TrySpawnAndAlign(GameObject[] pool, Transform target)
    {
        int[] indices = ShuffledIndices(pool.Length);

        for (int attempt = 0; attempt < Mathf.Min(maxPlacementAttempts, pool.Length); attempt++)
        {
            GameObject prefab = pool[indices[attempt]];
            GameObject obj = Instantiate(prefab, NavRoot);
            var piece = obj.GetComponent<DungeonPiece>();

            if (piece == null || piece.entrance == null)
            {
                Debug.LogError($"[DungeonGen] {prefab.name}: DungeonPiece o entrance no encontrado.");
                Destroy(obj);
                continue;
            }

            AlignToConnector(obj.transform, piece.entrance.transform, target);

            // Forzar sync del motor de físicas ANTES de comprobar colisiones
            // Sin esto Unity no ve los colliders recién instanciados
            Physics.SyncTransforms();

            if (HasOverlap(obj))
            {
                Destroy(obj);
                continue;
            }

            SetupBoundsCheck(obj);

            // Sync de nuevo para que la pieza confirmada sea visible
            // a las comprobaciones de las siguientes piezas
            Physics.SyncTransforms();

            placedCount++;

            EnemySetup roomSetup = obj.GetComponent<EnemySetup>();
            if (roomSetup != null)
            {
                enemyRooms.Add(roomSetup);
                Debug.Log($"[DungeonGen] Pieza con setup de enemigos detectada: {obj.name}");
            }

            // Recoger puntos de decoración (DecorationSpawn/Deco1, Deco2, Deco3...)
            List<Transform> decos = new List<Transform>();
            Transform decoParent = obj.transform.Find("DecorationSpawn");
            if (decoParent != null)
            {
                for (int d = 1; ; d++)
                {
                    Transform deco = decoParent.Find($"Deco{d}");
                    if (deco == null) break;
                    decos.Add(deco);
                }
            }
            if (decos.Count > 0)
            {
                decoSpawnPoints.Add(decos);
                Debug.Log($"[DungeonGen] {obj.name}: {decos.Count} puntos de decoración encontrados");
            }

            return obj;
        }

        return null;
    }

    // ---------------------------------------------------------------
    // Asigna el layer correcto al BoundsCheck y lo convierte en trigger
    void SetupBoundsCheck(GameObject obj)
    {
        Transform t = obj.transform.Find("BoundsCheck");
        if (t == null)
        {
            Debug.LogWarning($"[DungeonGen] {obj.name}: no tiene hijo 'BoundsCheck'");
            return;
        }

        t.gameObject.layer = LayerMaskToLayer(dungeonLayer);

        var bc = t.GetComponent<BoxCollider>();
        if (bc == null)
        {
            Debug.LogWarning($"[DungeonGen] {obj.name}: 'BoundsCheck' no tiene BoxCollider");
            return;
        }

        // Tiene que ser trigger para que OverlapBox lo detecte
        bc.isTrigger = true;

        if (overlapShrink > 0f)
        {
            bc.size = new Vector3(
                Mathf.Max(bc.size.x - overlapShrink, 0.1f),
                Mathf.Max(bc.size.y - overlapShrink, 0.1f),
                Mathf.Max(bc.size.z - overlapShrink, 0.1f)
            );
        }

        // Sync inmediato para que el collider sea detectable al momento
        Physics.SyncTransforms();
    }

    // ---------------------------------------------------------------
    BoxCollider GetBoundsCollider(GameObject obj)
    {
        Transform t = obj.transform.Find("BoundsCheck");
        if (t == null)
        {
            Debug.LogWarning($"[DungeonGen] {obj.name}: no tiene hijo 'BoundsCheck'");
            return null;
        }

        var bc = t.GetComponent<BoxCollider>();
        if (bc == null)
            Debug.LogWarning($"[DungeonGen] {obj.name}: 'BoundsCheck' no tiene BoxCollider");

        return bc;
    }

    // ---------------------------------------------------------------
    bool HasOverlap(GameObject obj)
    {
        BoxCollider bc = GetBoundsCollider(obj);
        if (bc == null) return false;

        Vector3 worldCenter = bc.transform.TransformPoint(bc.center);
        Vector3 worldExtents = Vector3.Scale(bc.size * 0.5f, bc.transform.lossyScale);

        worldExtents -= Vector3.one * (overlapShrink * 0.5f);
        worldExtents = new Vector3(
            Mathf.Max(worldExtents.x, 0.05f),
            Mathf.Max(worldExtents.y, 0.05f),
            Mathf.Max(worldExtents.z, 0.05f)
        );

        Collider[] hits = Physics.OverlapBox(
            worldCenter,
            worldExtents,
            obj.transform.rotation,
            dungeonLayer,
            QueryTriggerInteraction.Collide
        );

        foreach (var hit in hits)
        {
            if (hit.transform.root == obj.transform) continue;
            Debug.Log($"[DungeonGen] {obj.name} colisiona con {hit.transform.root.name}");
            return true;
        }

        return false;
    }


    // ---------------------------------------------------------------
    void CloseWithRoom(ConnectorPoint connector, bool mustBeCorridor)
    {
        ConnectorPoint targetForRoom = connector;

        if (mustBeCorridor)
        {
            GameObject corridorObj = TrySpawnAndAlign(corridorPrefabs, connector.transform);
            if (corridorObj == null) return;

            connector.isConnected = true;
            var cp = corridorObj.GetComponent<DungeonPiece>();
            if (cp.entrance != null) cp.entrance.isConnected = true;

            if (cp.exits == null || cp.exits.Length == 0) return;
            targetForRoom = cp.exits[0];
        }

        GameObject roomObj = TrySpawnAndAlign(roomPrefabs, targetForRoom.transform);
        if (roomObj == null) return;

        targetForRoom.isConnected = true;
        var rp = roomObj.GetComponent<DungeonPiece>();
        if (rp.entrance != null) rp.entrance.isConnected = true;

        TrackSpecialRoom(rp);

        if (rp.exits != null)
            foreach (var exit in rp.exits)
                if (exit != null) exit.isConnected = true;
    }

    // ---------------------------------------------------------------
    static void AlignToConnector(Transform root, Transform entrance, Transform target)
    {
        Quaternion desiredEntranceRot = Quaternion.LookRotation(target.forward, Vector3.up);
        Quaternion entranceLocalRot = Quaternion.Inverse(root.rotation) * entrance.rotation;
        root.rotation = desiredEntranceRot * Quaternion.Inverse(entranceLocalRot);
        root.position += target.position - entrance.position;
    }

    // ---------------------------------------------------------------
    void RegisterExits(DungeonPiece piece, bool mustBeCorridor, StairDirection lastStair)
    {
        if (piece.exits == null) return;
        if (branchQueues.Count == 0) return;

        var queue = branchQueues[activeBranch % branchQueues.Count];
        foreach (var exit in piece.exits)
            if (exit != null && !exit.isConnected)
                queue.Enqueue((exit, mustBeCorridor, lastStair));
    }

    // ---------------------------------------------------------------
    bool BranchesHaveConnectors()
    {
        foreach (var q in branchQueues)
            if (q.Count > 0) return true;
        return false;
    }

    Queue<(ConnectorPoint, bool, StairDirection)> NextActiveBranch()
    {
        int count = branchQueues.Count;
        for (int i = 0; i < count; i++)
        {
            activeBranch = (activeBranch + 1) % count;
            if (branchQueues[activeBranch].Count > 0)
                return branchQueues[activeBranch];
        }
        return null;
    }

    // ---------------------------------------------------------------
    static int LayerMaskToLayer(LayerMask mask)
    {
        int val = mask.value;
        for (int i = 0; i < 32; i++)
            if ((val & (1 << i)) != 0) return i;
        return 0;
    }

    static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    static int[] ShuffledIndices(int count)
    {
        int[] arr = new int[count];
        for (int i = 0; i < count; i++) arr[i] = i;
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr;
    }


    // ---------------------------------------------------------------
    // Salas especiales
    // ---------------------------------------------------------------

    void TrackSpecialRoom(DungeonPiece piece)
    {
        if (piece.specialRoomType == SpecialRoomType.None) return;

        if (!specialRoomCounts.ContainsKey(piece.specialRoomType))
            specialRoomCounts[piece.specialRoomType] = 0;

        specialRoomCounts[piece.specialRoomType]++;
        Debug.Log($"[DungeonGen] Sala especial colocada: {piece.specialRoomType} (total: {specialRoomCounts[piece.specialRoomType]})");
    }

    void PlaceSpecialRooms(List<(ConnectorPoint connector, bool mustBeCorridor)> connectors)
    {
        if (specialRoomRules == null || specialRoomRules.Length == 0) return;

        int connIndex = 0;

        foreach (var rule in specialRoomRules)
        {
            if (rule.prefabs == null || rule.prefabs.Length == 0)
            {
                Debug.LogWarning($"[DungeonGen] No hay prefabs para sala especial: {rule.type}");
                continue;
            }

            int placed = 0;
            while (placed < rule.maxCount && connIndex < connectors.Count)
            {
                var (conn, mustCorr) = connectors[connIndex];
                connIndex++;

                if (conn.isConnected) continue;

                ConnectorPoint target = conn;

                // Colocar corredor intermedio si es necesario
                if (mustCorr)
                {
                    GameObject corridorObj = TrySpawnAndAlign(corridorPrefabs, conn.transform);
                    if (corridorObj == null) continue;

                    conn.isConnected = true;
                    var cp = corridorObj.GetComponent<DungeonPiece>();
                    if (cp.entrance != null) cp.entrance.isConnected = true;
                    if (cp.exits == null || cp.exits.Length == 0) continue;
                    target = cp.exits[0];
                }

                GameObject roomObj = TrySpawnAndAlign(rule.prefabs, target.transform);
                if (roomObj == null) continue;

                target.isConnected = true;
                conn.isConnected = true;
                var rp = roomObj.GetComponent<DungeonPiece>();
                if (rp.entrance != null) rp.entrance.isConnected = true;

                TrackSpecialRoom(rp);
                placed++;
            }

            if (placed == 0)
                Debug.LogWarning($"[DungeonGen] No se pudo colocar sala especial: {rule.type}");
        }
    }

    // Filtra pasillos para evitar StairsUp→StairsDown o StairsDown→StairsUp
    GameObject[] FilterCorridorsByStair(GameObject[] pool, StairDirection lastStair)
    {
        if (lastStair == StairDirection.None) return pool;

        StairDirection forbidden = lastStair == StairDirection.Up
            ? StairDirection.Down
            : StairDirection.Up;

        var filtered = new List<GameObject>();
        foreach (var prefab in pool)
        {
            var piece = prefab.GetComponent<DungeonPiece>();
            if (piece != null && piece.stairDirection == forbidden) continue;
            filtered.Add(prefab);
        }

        // Si todo quedó filtrado, devolver el pool original para no bloquear la generación
        return filtered.Count > 0 ? filtered.ToArray() : pool;
    }

    static GameObject GetRandomPrefab(GameObject[] arr)
        => arr[Random.Range(0, arr.Length)];
}