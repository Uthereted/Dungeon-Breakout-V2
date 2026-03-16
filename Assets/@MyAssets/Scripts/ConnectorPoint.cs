using UnityEngine;

public class ConnectorPoint : MonoBehaviour
{
    [HideInInspector] public bool isConnected = false;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = isConnected ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position, 0.15f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 0.8f);
    }
#endif
}
