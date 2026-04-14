using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using DungeonBreakoutV2;

public class PotionSystem : MonoBehaviour
{
    [Header("Potion")]
    public GameObject potionPrefab;
    public Transform potionSocket;
    public int potionCount = 3;
    public float healAmount = 40f;

    [Header("Animation")]
    public Animator animator;
    public string drinkTrigger = "Drink";
    public float drinkDuration = 2f;

    [Header("UI")]
    public TMP_Text potionCountText;

    [HideInInspector] public bool isDrinking;

    private PlayerController playerController;
    private GameObject currentPotion;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        UpdateUI();
    }

    public void OnUseItem(InputValue v)
    {
        if (!v.isPressed) return;
        if (isDrinking) return;
        if (potionCount <= 0) return;
        if (playerController != null && playerController.movementLocked) return;

        StartCoroutine(DrinkRoutine());
    }

    IEnumerator DrinkRoutine()
    {
        isDrinking = true;

        if (playerController != null)
            playerController.movementLocked = true;

        // Spawn potion in hand
        if (potionPrefab != null && potionSocket != null)
        {
            currentPotion = Instantiate(potionPrefab, potionSocket);
            currentPotion.transform.localPosition = Vector3.zero;
            currentPotion.transform.localRotation = Quaternion.identity;

            // Disable physics/colliders on the held potion
            var rb = currentPotion.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;
            foreach (var col in currentPotion.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        if (animator != null)
            animator.SetTrigger(drinkTrigger);

        yield return new WaitForSeconds(drinkDuration);

        // Heal
        if (HealthSystem.Instance != null)
            HealthSystem.Instance.HealDamage(healAmount);

        // Remove potion from hand
        if (currentPotion != null)
            Destroy(currentPotion);

        potionCount--;
        UpdateUI();

        isDrinking = false;

        if (playerController != null)
            playerController.movementLocked = false;
    }

    void UpdateUI()
    {
        if (potionCountText != null)
            potionCountText.text = "x" + potionCount;
    }
}
