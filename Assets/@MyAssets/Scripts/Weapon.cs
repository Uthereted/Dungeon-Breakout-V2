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
}
