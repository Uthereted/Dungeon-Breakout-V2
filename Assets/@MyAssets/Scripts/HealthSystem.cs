using UnityEngine;
using UnityEngine.UI;

namespace DungeonBreakoutV2
{
    public class HealthSystem : MonoBehaviour
    {
        public static HealthSystem Instance;

        [Header("Health Bar")]
        public Image currentHealthBar;
        public Text healthText;

        public float hitPoint = 100f;
        public float maxHitPoint = 100f;

        [Header("Regeneration")]
        public bool regenerate = false;
        public float regen = 0.1f;
        public float regenUpdateInterval = 1f;
        private float timeLeft = 0f;

        public bool godMode = false;

        [Header("Death")]
        public GameObject player;
        public string dieTrigger = "Die";
        public float groundOffset = 0.02f;
        public LayerMask groundMask = ~0;

        public bool IsDead { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            UpdateGraphics();
            timeLeft = regenUpdateInterval;
        }

        private void Update()
        {
            if (regenerate && !IsDead)
                Regen();
        }

        private void Regen()
        {
            timeLeft -= Time.deltaTime;

            if (timeLeft <= 0f)
            {
                if (godMode)
                    HealDamage(maxHitPoint);
                else
                    HealDamage(regen);

                timeLeft = regenUpdateInterval;
            }
        }

        private void UpdateHealthBar()
        {
            if (currentHealthBar != null)
            {
                float ratio = hitPoint / maxHitPoint;
                ratio = Mathf.Clamp01(ratio);

                currentHealthBar.rectTransform.localPosition = new Vector3(
                    currentHealthBar.rectTransform.rect.width * ratio - currentHealthBar.rectTransform.rect.width,
                    0,
                    0
                );
            }

            if (healthText != null)
            {
                healthText.text = hitPoint.ToString("0") + "/" + maxHitPoint.ToString("0");
            }
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            hitPoint -= damage;

            if (hitPoint < 0f)
                hitPoint = 0f;

            UpdateGraphics();

            if (hitPoint <= 0f)
                PlayerDied();
        }

        public void HealDamage(float heal)
        {
            if (IsDead) return;

            hitPoint += heal;

            if (hitPoint > maxHitPoint)
                hitPoint = maxHitPoint;

            UpdateGraphics();
        }

        public void SetMaxHealth(float max)
        {
            maxHitPoint += maxHitPoint * max / 100f;

            if (hitPoint > maxHitPoint)
                hitPoint = maxHitPoint;

            UpdateGraphics();
        }

        private void UpdateGraphics()
        {
            UpdateHealthBar();
        }

        private void PlayerDied()
        {
            IsDead = true;

            if (player == null) return;

            // Lock player movement
            var pc = player.GetComponent<PlayerController>();
            if (pc) pc.movementLocked = true;

            // Snap player to ground
            var cap = player.GetComponent<CapsuleCollider>();
            if (cap)
            {
                Vector3 origin = cap.bounds.center + Vector3.up * 0.5f;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f, groundMask))
                {
                    float bottom = cap.bounds.min.y;
                    float delta = (hit.point.y + groundOffset) - bottom;
                    player.transform.position += new Vector3(0f, delta, 0f);
                }
            }

            // Stop physics
            var rb = player.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Disable collider so player doesn't float
            if (cap) cap.enabled = false;

            // Play death animation
            var animator = player.GetComponentInChildren<Animator>();
            if (animator) animator.SetTrigger(dieTrigger);
        }
    }
}