using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public WeaponController sword;

    [Header("Triggers")]
    public string lightTrigger = "LightNext";
    public string heavyTrigger = "HeavyNext";

    [Header("State Names (exactos del Animator)")]
    public string light1State = "Cha_Attack_Light_1";
    public string light2State = "Cha_Attack_Light_2";
    public string heavy1State = "Cha_Attack_Heavy_1";
    public string heavy2State = "Cha_Attack_Heavy_2";

    [Header("Combo")]
    [Range(0f, 1f)] public float chainWindow = 0.65f;
    public float inputBufferTime = 0.35f;

    [Header("Damage (fallback if weapon has no Weapon script)")]
    public int defaultLightDamage = 15;
    public int defaultHeavyDamage = 30;

    bool queuedLight, queuedHeavy;
    float queuedLightUntil, queuedHeavyUntil;
    int lastAttackStateHash;

    PlayerController playerController;
    PotionSystem potionSystem;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!sword) sword = GetComponent<WeaponController>();
        if (!sword && transform.parent) sword = GetComponentInParent<WeaponController>();
        playerController = GetComponent<PlayerController>();
        potionSystem = GetComponent<PotionSystem>();
    }

    void Update()
    {
        if (!animator) return;
        var st = animator.GetCurrentAnimatorStateInfo(0);

        // Expirar buffer
        if (queuedLight && Time.time > queuedLightUntil) queuedLight = false;
        if (queuedHeavy && Time.time > queuedHeavyUntil) queuedHeavy = false;

        // Combo chains
        if (queuedLight && IsLightState(st) && st.normalizedTime >= chainWindow)
        {
            queuedLight = false;
            animator.SetTrigger(lightTrigger);
        }
        if (queuedHeavy && IsHeavyState(st) && st.normalizedTime >= chainWindow)
        {
            queuedHeavy = false;
            animator.SetTrigger(heavyTrigger);
        }

        // Play weapon SFX exactly once per attack-state entry
        if (IsAnyAttackState(st))
        {
            if (st.shortNameHash != lastAttackStateHash)
            {
                lastAttackStateHash = st.shortNameHash;
                PlayWeaponSfx();
            }
        }
        else
        {
            lastAttackStateHash = 0;
        }

        // Activar/desactivar hitbox según estado de animación
        UpdateHitbox(st);

        // Apply weapon attack speed to animator
        if (sword && sword.HasWeapon && IsAnyAttackState(st))
        {
            var weapon = sword.EquippedWeapon;
            animator.speed = weapon ? weapon.attackSpeed : 1f;
        }
        else if (!IsAnyAttackState(st) && Mathf.Abs(animator.speed - 1f) > 0.01f)
        {
            animator.speed = 1f;
        }

        // Lock movement during attack animations (don't override if drinking)
        bool drinking = potionSystem != null && potionSystem.isDrinking;
        if (playerController != null && !drinking)
            playerController.movementLocked = IsAnyAttackState(st);
        else if (playerController != null && drinking)
            playerController.movementLocked = true;
    }

    void UpdateHitbox(AnimatorStateInfo st)
    {
        if (!sword || !sword.HasWeapon) return;
        var hitbox = sword.EquippedWeaponTransform.GetComponentInChildren<WeaponHitbox>(true);
        if (!hitbox) return;

        bool inAttack = IsAnyAttackState(st);

        var weapon = sword.EquippedWeapon;
        int light = weapon ? weapon.lightDamage : defaultLightDamage;
        int heavy = weapon ? weapon.heavyDamage : defaultHeavyDamage;
        int dmg = IsHeavyState(st) ? heavy : light;
        float kb = weapon ? weapon.knockbackForce : 0f;

        hitbox.SetActive(inAttack, dmg, kb, transform, st.shortNameHash);
    }

    public void OnLightAttack(InputValue v)
    {
        if (!v.isPressed || !CanAttack()) return;
        var st = animator.GetCurrentAnimatorStateInfo(0);

        if (!IsAnyAttackState(st))
        {
            animator.SetTrigger(lightTrigger);
            return;
        }
        if (IsLightState(st))
        {
            queuedLight = true;
            queuedLightUntil = Time.time + inputBufferTime;
        }
    }

    public void OnHeavyAttack(InputValue v)
    {
        if (!v.isPressed || !CanAttack()) return;
        var st = animator.GetCurrentAnimatorStateInfo(0);

        if (!IsAnyAttackState(st))
        {
            animator.SetTrigger(heavyTrigger);
            return;
        }
        if (IsHeavyState(st))
        {
            queuedHeavy = true;
            queuedHeavyUntil = Time.time + inputBufferTime;
        }
    }

    void PlayWeaponSfx()
    {
        if (sword == null) return;
        var weapon = sword.EquippedWeapon;
        if (weapon == null || weapon.attackSfx == null) return;

        Vector3 pos = sword.EquippedWeaponTransform != null
            ? sword.EquippedWeaponTransform.position
            : transform.position;

        AudioSource.PlayClipAtPoint(weapon.attackSfx, pos, weapon.attackSfxVolume);
    }

    bool CanAttack() => sword != null && sword.HasWeapon && !sword.IsGrabbing
        && (potionSystem == null || !potionSystem.isDrinking);
    bool IsAnyAttackState(AnimatorStateInfo st) =>
        st.IsName(light1State) || st.IsName(light2State) ||
        st.IsName(heavy1State) || st.IsName(heavy2State);
    bool IsLightState(AnimatorStateInfo st) =>
        st.IsName(light1State) || st.IsName(light2State);
    bool IsHeavyState(AnimatorStateInfo st) =>
        st.IsName(heavy1State) || st.IsName(heavy2State);
}