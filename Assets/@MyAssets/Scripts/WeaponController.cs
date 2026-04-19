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

        // chest interaction
        if (nearbyChest != null && !nearbyChest.IsOpened)
        {
            StartCoroutine(LootRoutine());
            return;
        }

        // weapon pickup
        if (nearbyWeapon == null || equippedWeaponTransform != null) return;
        StartCoroutine(GrabRoutine());
    }

    IEnumerator GrabRoutine()
    {
        grabbing = true;
        Transform target = nearbyWeapon;   // cache so OnTriggerExit can't null it
        nearbyWeapon = null;

        if (menuManager) menuManager.HideInteract();
        if (player) player.movementLocked = true;

        // smoothly rotate player to face the weapon before the animation starts
        if (target)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.rotation = targetRot;
            }
        }

        if (animator) animator.SetTrigger(grabTrigger);
        yield return new WaitForSeconds(grabDuration);
        EquipWeapon(target);
        grabbing = false;
        if (player) player.movementLocked = false;
    }

    IEnumerator LootRoutine()
    {
        grabbing = true;
        Chest chest = nearbyChest;
        nearbyChest = null;

        if (menuManager) menuManager.HideInteract();
        if (player) player.movementLocked = true;

        // turn toward chest
        if (chest)
        {
            Vector3 dir = chest.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.rotation = targetRot;
            }
        }

        // play pickup animation
        if (animator) animator.SetTrigger(grabTrigger);
        yield return new WaitForSeconds(grabDuration);

        // open chest after animation
        if (chest) chest.Open();

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

        Weapon w = sword.GetComponent<Weapon>();
        if (w != null)
        {
            sword.localPosition = w.gripPosition;
            sword.localRotation = Quaternion.Euler(w.gripRotation);
        }
        else
        {
            sword.localPosition = Vector3.zero;
            sword.localRotation = Quaternion.identity;
        }

        equippedWeaponTransform = sword;
        equippedWeapon = w;
        nearbyWeapon = null;

        // Swap animations if this weapon has its own override controller
        if (w != null && w.animatorOverride != null && animator != null)
            animator.runtimeAnimatorController = w.animatorOverride;
    }

    void OnTriggerEnter(Collider other)
    {
        // weapon pickup
        if (other.CompareTag("SwordPick"))
        {
            if (equippedWeaponTransform != null || grabbing) return;
            nearbyWeapon = other.transform;
            if (menuManager) menuManager.ShowInteract(grabText);
            return;
        }

        // chest interaction
        if (other.CompareTag("Chest"))
        {
            var chest = other.GetComponentInParent<Chest>();
            if (chest && !chest.IsOpened)
            {
                nearbyChest = chest;
                if (menuManager) menuManager.ShowInteract("Loot [F]");
            }
            return;
        }
    }

    void OnTriggerExit(Collider other)
    {
        // weapon
        if (other.CompareTag("SwordPick"))
        {
            if (grabbing) return;
            if (nearbyWeapon != other.transform) return;
            nearbyWeapon = null;
            if (menuManager) menuManager.HideInteract();
            return;
        }

        // chest
        if (other.CompareTag("Chest"))
        {
            var chest = other.GetComponentInParent<Chest>();
            if (chest == nearbyChest)
            {
                nearbyChest = null;
                if (menuManager) menuManager.HideInteract();
            }
            return;
        }
    }
}