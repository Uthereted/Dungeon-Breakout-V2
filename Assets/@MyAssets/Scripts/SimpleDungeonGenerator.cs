using System.Collections.Generic;
using UnityEngine;

public class SimpleDungeonGenerator : MonoBehaviour
{
    public Transform startPoint;
    public List<GameObject> piecePrefabs;
    public int piecesToSpawn = 10;
    public LayerMask dungeonLayer;
    public int maxAttemptsPerPiece = 10;
    public float boundsShrink = 0.9f;

    private Transform currentExit;
    private GameObject lastPlacedPiece;

    void Start()
    {
        currentExit = startPoint;
        lastPlacedPiece = null;

        for (int i = 0; i < piecesToSpawn; i++)
        {
            bool placed = SpawnNextPiece();

            if (!placed)
            {
                Debug.Log("No se pudo colocar más piezas.");
                break;
            }
        }
    }

    bool SpawnNextPiece()
    {
        for (int attempt = 0; attempt < maxAttemptsPerPiece; attempt++)
        {
            GameObject prefab = piecePrefabs[Random.Range(0, piecePrefabs.Count)];
            GameObject piece = Instantiate(prefab);

            Transform entrance = piece.transform.Find("Connector/Entrance");
            Transform nextExit = GetRandomExit(piece.transform);

            if (entrance == null || nextExit == null)
            {
                Debug.LogError("Falta Entrance o Exit en " + prefab.name);
                Destroy(piece);
                return false;
            }

            SetBoundsLayer(piece, LayerMask.NameToLayer("Default"));
            AlignPieceToExit(piece.transform, entrance, currentExit);

            if (HasCollision(piece, lastPlacedPiece))
            {
                Destroy(piece);
                continue;
            }

            SetBoundsLayer(piece, LayerMask.NameToLayer("DungeonPiece"));

            lastPlacedPiece = piece;
            currentExit = nextExit;
            return true;
        }

        return false;
    }

    void AlignPieceToExit(Transform pieceRoot, Transform entrance, Transform targetExit)
    {
        Quaternion rotationOffset = targetExit.rotation * Quaternion.Inverse(entrance.rotation);
        pieceRoot.rotation = rotationOffset * pieceRoot.rotation;

        Vector3 positionOffset = targetExit.position - entrance.position;
        pieceRoot.position += positionOffset;
    }

    Transform GetRandomExit(Transform pieceRoot)
    {
        Transform connector = pieceRoot.Find("Connector");
        if (connector == null) return null;

        List<Transform> exits = new List<Transform>();

        foreach (Transform child in connector)
        {
            if (child.name.StartsWith("Exit"))
                exits.Add(child);
        }

        if (exits.Count == 0) return null;

        return exits[Random.Range(0, exits.Count)];
    }

    void SetBoundsLayer(GameObject piece, int layer)
    {
        Transform boundsObj = piece.transform.Find("BoundsCheck");
        if (boundsObj != null)
            boundsObj.gameObject.layer = layer;
    }

    bool HasCollision(GameObject piece, GameObject pieceToIgnore)
    {
        Transform boundsObj = piece.transform.Find("BoundsCheck");
        if (boundsObj == null) return true;

        BoxCollider box = boundsObj.GetComponent<BoxCollider>();
        if (box == null) return true;

        Vector3 center = box.bounds.center;
        Vector3 halfExtents = box.bounds.extents * boundsShrink;
        Quaternion rotation = boundsObj.rotation;

        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            rotation,
            dungeonLayer,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(piece.transform))
                continue;

            if (pieceToIgnore != null && hit.transform.IsChildOf(pieceToIgnore.transform))
                continue;

            Debug.Log($"COLISION REAL: {piece.name} choca con {hit.transform.root.name}");
            return true;
        }

        return false;
    }
}