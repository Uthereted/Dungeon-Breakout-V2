using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordController : MonoBehaviour
{
    [Header("Refs")]
    public Transform swordSocket;
    public PlayerInteractUI interactUI;
    public Animator animator;
    public PlayerController player;

    [Header("Grab Anim")]
    public string grabTrigger = "Grab";
    public float grabDuration = 1.0f;
    public string grabText = "Grab [F]";

    Transform equippedSword, nearbySword;
    bool grabbing;

    public bool HasSword => equippedSword != null;
    public bool IsGrabbing => grabbing;
    public Transform EquippedSword => equippedSword;

    void Awake()
    {
        if (!interactUI) interactUI = GetComponent<PlayerInteractUI>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!player) player = GetComponentInParent<PlayerController>();
    }

    public void OnInteract(InputValue v)
    {
        if (!v.isPressed || grabbing) return;
        if (nearbySword == null || equippedSword != null) return;
        StartCoroutine(GrabRoutine());
    }

    IEnumerator GrabRoutine()
    {
        grabbing = true;
        if (interactUI) interactUI.Hide();
        if (player) player.movementLocked = true;
        if (animator) animator.SetTrigger(grabTrigger);
        yield return new WaitForSeconds(grabDuration);
        EquipSword(nearbySword);
        grabbing = false;
        if (player) player.movementLocked = false;
    }

    void EquipSword(Transform sword)
    {
        if (!sword) return;

        var rb = sword.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        SphereCollider grabCol = sword.GetComponent<SphereCollider>();
        if (!grabCol) grabCol = sword.GetComponentInChildren<SphereCollider>(true);
        if (grabCol) grabCol.enabled = false;

        BoxCollider hitCol = sword.GetComponent<BoxCollider>();
        if (!hitCol) hitCol = sword.GetComponentInChildren<BoxCollider>(true);
        if (hitCol)
        {
            hitCol.isTrigger = true;
            hitCol.enabled = false;
        }

        sword.SetParent(swordSocket);
        sword.localPosition = Vector3.zero;
        sword.localRotation = Quaternion.identity;
        equippedSword = sword;
        nearbySword = null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SwordPick")) return;
        if (equippedSword != null || grabbing) return;
        nearbySword = other.transform;
        if (interactUI) interactUI.Show(grabText);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("SwordPick")) return;
        if (nearbySword != other.transform) return;
        nearbySword = null;
        if (interactUI) interactUI.Hide();
    }
}