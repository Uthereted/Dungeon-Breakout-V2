using UnityEngine;

public class TestDecorationSpawner : MonoBehaviour
{
    public GameObject prefabDecoracion;
    public Transform[] puntosSpawn;
    public bool generarAlIniciar = true;

    private void Start()
    {
        if (generarAlIniciar)
            GenerarUno();
    }

    [ContextMenu("Generar uno")]
    public void GenerarUno()
    {
        if (prefabDecoracion == null || puntosSpawn == null || puntosSpawn.Length == 0)
        {
            Debug.LogWarning("Falta el prefab o los puntos de spawn.");
            return;
        }

        int index = Random.Range(0, puntosSpawn.Length);
        Transform punto = puntosSpawn[index];

        Instantiate(prefabDecoracion, punto.position, punto.rotation);
    }
}