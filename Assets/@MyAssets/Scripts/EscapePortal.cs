using UnityEngine;

public class EscapePortal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (DungeonEscape.Instance == null) return;

        DungeonEscape.Instance.Escape();
    }
}
