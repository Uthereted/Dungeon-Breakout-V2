using UnityEngine;

public class TestSpawn : MonoBehaviour
{
    public GameObject corridorPrefab;
    public Transform spawnPoint;

    void Start()
    {
        GameObject obj = Instantiate(corridorPrefab);

        Transform entrance = obj.transform.Find("Connector/Entrance");

        // mover el prefab para que el Entrance coincida con el spawnPoint
        Vector3 offset = spawnPoint.position - entrance.position;
        obj.transform.position += offset;

        // opcional: alinear rotación
        obj.transform.rotation = spawnPoint.rotation;
    }
}