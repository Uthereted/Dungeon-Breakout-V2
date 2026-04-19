using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Refs")]
    public Transform weaponSocket;
    public MenuManager menuManager;
    public Animator animator;
    public PlayerController player;

    [Header("Grab Anim")]
    public string grabTrigger = "Grab";
    public float grabDuration = 1.0f;
    public float turnSpeed = 5f;
    public string grabText = "Grab [F]";

    Transform equippedWeaponTransform, nearbyWeapon;
    Weapon equippedWeapon;
    Chest nearbyChest;
    bool grabbing;

    public bool HasWeapon => equippedWeaponTransform != null;
    public bool IsGrabbing => grabbing;
    public Transform EquippedWeaponTransform => equippedWeaponTransform;
    public Weapon EquippedWeapon => equippedWeapon;

    void Awake()
    {
        if (!menuManager) menuManager = FindAnyObjectByType<MenuManager>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!player) player = GetComponentInParent<PlayerController>();
    }

    public void OnInteract(InputValue v)
    {
        if (!v.isPressed || grabbing) return;

        if (nearbyChest != null && !nearbyChest.IsOpened)
            StartCoroutine(InteractRoutine(nearbyChest.transform, () => nearbyChest?.Open()));
        else if (nearbyWeapon != null && equippedWeaponTransform == null)
        {
            Transform weapon = nearbyWeapon;
            StartCoroutine(InteractRoutine(weapon, () => EquipWeapon(weapon)));
        }
    }

    IEnumerator InteractRoutine(Transform target, System.Action onComplete)
    {
        grabbing = true;
        nearbyWeapon = null;
        nearbyChest = null;

        if (menuManager) menuManager.HideInteract();
        if (player) player.movementLocked = true;

        if (target) yield return TurnTo(target.position);

        if (animator) animator.SetTrigger(grabTrigger);
        yield return new WaitForSeconds(grabDuration);

        onComplete?.Invoke();

        grabbing = false;
        if (player) player.movementLocked = false;
    }

    IEnumerator TurnTo(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) yield break;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRot;
    }

    void EquipWeapon(Transform weapon)
    {
        if (!weapon) return;

        // Disable physics
        var rb = weapon.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // Disable pickup trigger
        var pickCol = weapon.GetComponentInChildren<SphereCollider>(true);
        if (pickCol) pickCol.enabled = false;

        // Prepare hit collider (off until attacking)
        var hitCol = weapon.GetComponentInChildren<BoxCollider>(true);
        if (hitCol)
        {
            hitCol.isTrigger = true;
            hitCol.enabled = false;
        }

        // Attach to hand using weapon's grip offset
        Weapon w = weapon.GetComponent<Weapon>();
        weapon.SetParent(weaponSocket);
        weapon.localPosition = w ? w.gripPosition : Vector3.zero;
        weapon.localRotation = w ? Quaternion.Euler(w.gripRotation) : Quaternion.identity;

        // Swap animations if this weapon has its own override
        if (w && w.animatorOverride && animator)
            animator.runtimeAnimatorController = w.animatorOverride;

        equippedWeaponTransform = weapon;
        equippedWeapon = w;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SwordPick") && equippedWeaponTransform == null && !grabbing)
        {
            nearbyWeapon = other.transform;
            if (menuManager) menuManager.ShowInteract(grabText);
        }
        else if (other.CompareTag("Chest"))
        {
            var chest = other.GetComponentInParent<Chest>();
            if (chest && !chest.IsOpened)
            {
                nearbyChest = chest;
                if (menuManager) menuManager.ShowInteract("Loot [F]");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SwordPick") && !grabbing && nearbyWeapon == other.transform)
        {
            nearbyWeapon = null;
            if (menuManager) menuManager.HideInteract();
        }
        else if (other.CompareTag("Chest"))
        {
            var chest = other.GetComponentInParent<Chest>();
            if (chest == nearbyChest)
            {
                nearbyChest = null;
                if (menuManager) menuManager.HideInteract();
            }
        }
    }
}
