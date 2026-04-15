using UnityEngine;

public class Chest : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string openTrigger = "Open";

    bool opened;

    public bool IsOpened => opened;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    public void Open()
    {
        if (opened) return;
        opened = true;

        if (animator) animator.SetTrigger(openTrigger);

        if (DungeonEscape.Instance != null)
            DungeonEscape.Instance.ChestLooted();
    }
}
