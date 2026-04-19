using UnityEngine;
using TMPro;
using DungeonBreakoutV2;

public class GameTimer : MonoBehaviour
{
    [Header("Duración (segundos)")]
    [SerializeField] private float totalSeconds = 900f; // 15 min

    [Header("UI")]
    [SerializeField] private TMP_Text timerLabel;

    private float remaining;
    private bool running;

    void Awake()
    {
        remaining = totalSeconds;
        UpdateLabel();
    }

    public void StartTimer()
    {
        running = true;
    }

    public void StopTimer()
    {
        running = false;
    }

    void Update()
    {
        if (!running) return;

        remaining -= Time.deltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            running = false;
            UpdateLabel();

            if (HealthSystem.Instance != null)
                HealthSystem.Instance.TakeDamage(HealthSystem.Instance.maxHitPoint);
            return;
        }

        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (timerLabel == null) return;

        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        timerLabel.text = $"{minutes:00}:{seconds:00}";
    }
}
