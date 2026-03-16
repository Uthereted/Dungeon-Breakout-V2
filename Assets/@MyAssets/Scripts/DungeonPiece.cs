using UnityEngine;
using System.Collections.Generic;

public enum PieceType { Corridor, Room }

public class DungeonPiece : MonoBehaviour
{
    public PieceType pieceType;

    // Se rellenan automáticamente en Awake, no hace falta arrastrar nada
    [HideInInspector] public ConnectorPoint entrance;
    [HideInInspector] public ConnectorPoint[] exits;

    void Awake()
    {
        entrance = GetConnector("Connector/Entrance");

        if (pieceType == PieceType.Corridor)
        {
            // Pasillo: un solo exit
            var exit = GetConnector("Connector/Exit");
            exits = exit != null ? new[] { exit } : new ConnectorPoint[0];
        }
        else
        {
            // Sala: hasta 3 exits numerados
            var found = new List<ConnectorPoint>();
            for (int i = 1; i <= 3; i++)
            {
                var exit = GetConnector($"Connector/Exit{i}");
                if (exit != null) found.Add(exit);
            }
            exits = found.ToArray();
        }

        if (entrance == null)
            Debug.LogError($"[DungeonPiece] {name}: no se encontró Connector/Entrance");

        if (exits.Length == 0)
            Debug.LogWarning($"[DungeonPiece] {name}: no se encontró ningún exit en Connector/");
    }

    ConnectorPoint GetConnector(string path)
    {
        Transform t = transform.Find(path);
        if (t == null) return null;

        var cp = t.GetComponent<ConnectorPoint>();
        if (cp == null)
            Debug.LogWarning($"[DungeonPiece] {name}: {path} existe pero no tiene ConnectorPoint");

        return cp;
    }

#if UNITY_EDITOR
    // Preview en editor sin entrar en Play
    void OnValidate()
    {
        // Mostrar en inspector qué ha encontrado (solo lectura)
        entrance = transform.Find("Connector/Entrance")?.GetComponent<ConnectorPoint>();
    }
#endif
}