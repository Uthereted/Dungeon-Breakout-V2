using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Refs")]
    public Transform weaponSocket;
    public PlayerInteractUI interactUI;
    public Animator animator;
    public PlayerController player;

    [Header("Grab Anim")]
    public string grabTrigger = "Grab";
    public float grabDuration = 1.0f;
    public string grabText = "Grab [F]";

    Transform equippedWeaponTransform, nearbyWeapon;
    Weapon equippedWeapon;
    bool grabbing;

    public bool HasWeapon => equippedWeaponTransform != null;
    public bool IsGrabbing => grabbing;
    public Transform EquippedWeaponTransform => equippedWeaponTransform;
    public Weapon EquippedWeapon => equippedWeapon;

    void Awake()
    {
        if (!interactUI) interactUI = GetComponent<PlayerInteractUI>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!player) player = GetComponentInParent<PlayerController>();
    }

    public void OnInteract(InputValue v)
    {
        if (!v.isPressed || grabbing) return;
        if (nearbyWeapon == null || equippedWeaponTransform != null) return;
        StartCoroutine(GrabRoutine());
    }

    IEnumerator GrabRoutine()
    {
        grabbing = true;
        if (interactUI) interactUI.Hide();
        if (player) player.movementLocked = true;
        if (animator) animator.SetTrigger(grabTrigger);
        yield return new WaitForSeconds(grabDuration);
        EquipWeapon(nearbyWeapon);
        grabbing = false;
        if (player) player.movementLocked = false;
    }

    void EquipWeapon(Transform sword)
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

        sword.SetParent(weaponSocket);
        sword.localPosition = Vector3.zero;
        sword.localRotation = Quaternion.identity;
        equippedWeaponTransform = sword;
        equippedWeapon = sword.GetComponent<Weapon>();
        nearbyWeapon = null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SwordPick")) return;
        if (equippedWeaponTransform != null || grabbing) return;
        nearbyWeapon = other.transform;
        if (interactUI) interactUI.Show(grabText);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("SwordPick")) return;
        if (nearbyWeapon != other.transform) return;
        nearbyWeapon = null;
        if (interactUI) interactUI.Hide();
    }
}