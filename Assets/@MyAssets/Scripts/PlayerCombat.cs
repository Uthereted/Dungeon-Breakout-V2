using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public SwordController sword;

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

    [Header("Damage")]
    public int lightDamage = 1;
    public int heavyDamage = 2;

    bool queuedLight, queuedHeavy;
    float queuedLightUntil, queuedHeavyUntil;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!sword) sword = GetComponent<SwordController>();
        if (!sword && transform.parent) sword = GetComponentInParent<SwordController>();
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

        // Activar/desactivar hitbox según estado de animación
        UpdateHitbox(st);
    }

    void UpdateHitbox(AnimatorStateInfo st)
    {
        if (!sword || !sword.HasSword) return;
        var hitbox = sword.EquippedSword.GetComponentInChildren<SwordHitbox>(true);
        if (!hitbox) return;

        bool inAttack = IsAnyAttackState(st);
        int dmg = IsHeavyState(st) ? heavyDamage : lightDamage;

        hitbox.SetActive(inAttack, dmg);
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

    bool CanAttack() => sword != null && sword.HasSword && !sword.IsGrabbing;
    bool IsAnyAttackState(AnimatorStateInfo st) =>
        st.IsName(light1State) || st.IsName(light2State) ||
        st.IsName(heavy1State) || st.IsName(heavy2State);
    bool IsLightState(AnimatorStateInfo st) =>
        st.IsName(light1State) || st.IsName(light2State);
    bool IsHeavyState(AnimatorStateInfo st) =>
        st.IsName(heavy1State) || st.IsName(heavy2State);
}