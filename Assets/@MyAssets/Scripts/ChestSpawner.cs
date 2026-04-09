using UnityEngine;

public class ChestSpawner : MonoBehaviour
{
    public GameObject[] chestPrefabs;
    public Transform chestSpawnPoint;

    void Start()
    {
        if (chestPrefabs == null || chestPrefabs.Length == 0) return;
        if (chestSpawnPoint == null)
        {
            chestSpawnPoint = transform.Find("ChestSpawn");
            if (chestSpawnPoint == null) return;
        }

        GameObject prefab = chestPrefabs[Random.Range(0, chestPrefabs.Length)];
        Instantiate(prefab, chestSpawnPoint.position, chestSpawnPoint.rotation, chestSpawnPoint);
    }
}
