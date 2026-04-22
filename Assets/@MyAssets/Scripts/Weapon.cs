using UnityEngine;

public enum WeaponType { Sword, Spear, Axe }

public class Weapon : MonoBehaviour
{
    [Header("Info")]
    public string weaponName = "Sword";
    public WeaponType weaponType = WeaponType.Sword;

    [Header("Combat")]
    public int lightDamage = 15;
    public int heavyDamage = 30;
    public float attackSpeed = 1f;
    public float knockbackForce = 0f;

    [Header("Hand Grip")]
    public Vector3 gripPosition = Vector3.zero;
    public Vector3 gripRotation = Vector3.zero;

    [Header("Animations")]
    [Tooltip("Optional override controller. Leave empty to use the default animator.")]
    public AnimatorOverrideController animatorOverride;

    [Header("SFX")]
    public AudioClip attackSfx;
    [Range(0f, 1f)] public float attackSfxVolume = 1f;
}
