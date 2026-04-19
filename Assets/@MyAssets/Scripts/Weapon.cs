using UnityEngine;

public enum WeaponType
{
    Sword,
    Axe,
    Hammer,
    Dagger,
    Spear
}

public class Weapon : MonoBehaviour
{
    [Header("Info")]
    public string weaponName = "Sword";
    public WeaponType weaponType = WeaponType.Sword;

    [Header("Damage")]
    public int lightDamage = 15;
    public int heavyDamage = 30;

    [Header("Speed")]
    [Tooltip("Multiplier on attack animation speed (1 = normal)")]
    public float attackSpeed = 1f;

    [Header("Knockback")]
    public float knockbackForce = 0f;

    [Header("Hand Grip Offset")]
    [Tooltip("Local position offset relative to the hand socket")]
    public Vector3 gripPosition = Vector3.zero;
    [Tooltip("Local euler rotation offset relative to the hand socket")]
    public Vector3 gripRotation = Vector3.zero;

    [Header("Animations")]
    [Tooltip("Optional: override controller for this weapon's animations. Leave empty to use the player's default animator.")]
    public AnimatorOverrideController animatorOverride;
}
