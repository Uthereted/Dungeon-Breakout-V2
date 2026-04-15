using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField, Range(0, 1)] private float initialRatio = 1f;

    public float MaxHealth { get => maxHealth; set => maxHealth = value; }
    public float CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0f;

    public UnityAction<float> OnHealthChanged;
    public UnityAction OnHealthEmpty;

    void Awake()
    {
        CurrentHealth = maxHealth * initialRatio;
    }

    public void ApplyDamage(float damage)
    {
        if (!IsAlive) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, maxHealth);
        OnHealthChanged?.Invoke(-damage);

        if (CurrentHealth <= 0f)
            OnHealthEmpty?.Invoke();
    }

    public void AddHealth(float amount)
    {
        if (!IsAlive) return;

        float prev = CurrentHealth;
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, maxHealth);

        float change = CurrentHealth - prev;
        if (change > 0f)
            OnHealthChanged?.Invoke(change);
    }

    public void SetHealth(float hp)
    {
        CurrentHealth = Mathf.Clamp(hp, 0, maxHealth);
    }
}
