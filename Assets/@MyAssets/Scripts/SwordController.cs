using UnityEngine;
using UnityEngine.InputSystem;

public class SwordController : MonoBehaviour
{
    [Header("Refs")]
    public Transform swordSocket;          // tu SwordSocket (mano real)
    public PlayerInteractUI interactUI;    // UI genérica

    [Header("Interact")]
    public string grabText = "Grab [F]";

    Transform equippedSword;
    Transform nearbySword;

    void Awake()
    {
        if (interactUI == null) interactUI = GetComponent<PlayerInteractUI>();
    }

    // Input System (Send Messages). Acción del input asset: "Interact" con tecla F.
    public void OnInteract(InputValue v)
    {
        if (!v.isPressed) return;
        if (nearbySword != null && equippedSword == null)
            EquipSword(nearbySword);
    }

    void EquipSword(Transform sword)
    {
        // apaga UI
        if (interactUI) interactUI.Hide();

        // desactiva colisiones físicas
        var col = sword.GetComponent<Collider>();
        if (col) col.enabled = false;

        var rb = sword.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // parent a socket
        sword.SetParent(swordSocket);
        sword.localPosition = Vector3.zero;
        sword.localRotation = Quaternion.identity;

        equippedSword = sword;
        nearbySword = null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SwordPick")) return;
        if (equippedSword != null) return;

        nearbySword = other.transform;
        if (interactUI) interactUI.Show(grabText);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("SwordPick")) return;

        if (nearbySword == other.transform)
        {
            nearbySword = null;
            if (interactUI) interactUI.Hide();
        }
    }

}
