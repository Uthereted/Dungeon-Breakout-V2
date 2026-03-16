using UnityEngine;
using System.Collections.Generic;

public class ProceduralDungeonGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] corridorPrefabs;
    public GameObject[] roomPrefabs;

    [Header("Configuración")]
    public Transform startPoint;
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

    private readonly Queue<(ConnectorPoint connector, bool mustBeCorridor)> openConnectors
        = new Queue<(ConnectorPoint, bool)>();

    private int placedCount = 0;

    // ---------------------------------------------------------------
    void Start()
    {
        if (seed != 0) Random.InitState(seed);
        Generate();
    }

    // ---------------------------------------------------------------
    void Generate()
    {
        int effectiveMax = maxPieces + 2;

        // Registrar la sala principal como obstáculo fijo
        if (mainRoom != null)
        {
            SetupBoundsCheck(mainRoom);
            Physics.SyncTransforms();
        }

        GameObject first = TrySpawnAndAlign(corridorPrefabs, startPoint);
        if (first == null) { Debug.LogError("[DungeonGen] No se pudo colocar la pieza inicial."); return; }

        RegisterExits(first.GetComponent<DungeonPiece>(), mustBeCorridor: false);

        int safety = 0;

        while (openConnectors.Count > 0 && placedCount < effectiveMax)
        {
            if (++safety > 5000) { Debug.LogWarning("DungeonGen: safety break"); break; }

            var (connector, mustBeCorridor) = openConnectors.Dequeue();
            if (connector.isConnected) continue;

            bool isLastSlot = placedCount >= effectiveMax - 1;

            if (isLastSlot)
            {
                CloseWithRoom(connector, mustBeCorridor);
                continue;
            }

            bool placeRoom = !mustBeCorridor && Random.value < roomChance;
            GameObject[] pool = placeRoom ? roomPrefabs : corridorPrefabs;

            GameObject spawned = TrySpawnAndAlign(pool, connector.transform);

            // Si falla con el tipo preferido intenta el otro
            if (spawned == null)
            {
                GameObject[] fallback = placeRoom ? corridorPrefabs : roomPrefabs;
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

            RegisterExits(piece, mustBeCorridor: placeRoom);
        }

        // Cerrar conectores abiertos restantes
        while (openConnectors.Count > 0)
        {
            var (connector, mustBeCorridor) = openConnectors.Dequeue();
            if (!connector.isConnected)
                CloseWithRoom(connector, mustBeCorridor);
        }
    }

    // ---------------------------------------------------------------
    GameObject TrySpawnAndAlign(GameObject[] pool, Transform target)
    {
        int[] indices = ShuffledIndices(pool.Length);

        for (int attempt = 0; attempt < Mathf.Min(maxPlacementAttempts, pool.Length); attempt++)
        {
            GameObject prefab = pool[indices[attempt]];
            GameObject obj = Instantiate(prefab);
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
    void RegisterExits(DungeonPiece piece, bool mustBeCorridor)
    {
        if (piece.exits == null) return;
        foreach (var exit in piece.exits)
            if (exit != null && !exit.isConnected)
                openConnectors.Enqueue((exit, mustBeCorridor));
    }

    // ---------------------------------------------------------------
    static int LayerMaskToLayer(LayerMask mask)
    {
        int val = mask.value;
        for (int i = 0; i < 32; i++)
            if ((val & (1 << i)) != 0) return i;
        return 0;
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


    static GameObject GetRandomPrefab(GameObject[] arr)
        => arr[Random.Range(0, arr.Length)];
}