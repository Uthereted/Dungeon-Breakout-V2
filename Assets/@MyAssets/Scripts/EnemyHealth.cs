using UnityEngine;
using UnityEngine.UI;
using CanvasScaler = UnityEngine.UI.CanvasScaler;

[RequireComponent(typeof(Health))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Stun")]
    public float stunDuration = 2.0f;
    float stunUntil;

    [Header("Animator")]
    public Animator animator;
    public string hitTrigger = "Hit";
    public string dieTrigger = "Die";
    public string isDeadBool = "IsDead";

    [Header("Hit FX")]
    public GameObject bloodPrefab;
    public Vector3 bloodOffset = new Vector3(0f, 1.2f, 0f);
    public float bloodLifetime = 2f;

    [Header("Healthbar")]
    public Vector3 barOffset = new Vector3(0f, 2.2f, 0f);
    public Vector2 barSize = new Vector2(1f, 0.12f);
    public Color barBackground = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color barColorHigh = Color.green;
    public Color barColorLow = Color.red;
    public float animSpeed = 100f;
    public bool hideWhenFull = true;

    Health health;
    bool dead;

    // healthbar runtime refs
    Canvas barCanvas;
    Image fillImage;
    RectTransform fillRect;
    float displayedHealth;

    public bool IsDead => dead;
    public bool IsStunned => Time.time < stunUntil;

    void Awake()
    {
        health = GetComponent<Health>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        CreateHealthbar();
    }

    void OnEnable()
    {
        health.OnHealthEmpty += Die;
    }

    void OnDisable()
    {
        health.OnHealthEmpty -= Die;
    }

    void Start()
    {
        displayedHealth = health.CurrentHealth;

        // hide if full at start
        if (hideWhenFull && barCanvas)
            barCanvas.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (barCanvas == null) return;

        // billboard: face camera
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        if (cam != null)
        {
            barCanvas.transform.position = transform.position + barOffset;
            barCanvas.transform.LookAt(barCanvas.transform.position + cam.forward);
        }

        // animate fill
        displayedHealth = Mathf.MoveTowards(displayedHealth, health.CurrentHealth, animSpeed * Time.deltaTime);
        float ratio = health.MaxHealth > 0 ? displayedHealth / health.MaxHealth : 0f;

        // drain from right to left
        fillRect.anchorMin = new Vector2(1f - ratio, 0f);
        fillImage.color = Color.Lerp(barColorLow, barColorHigh, ratio);

        // visibility
        if (ratio <= 0f)
        {
            barCanvas.gameObject.SetActive(false);
        }
        else if (hideWhenFull && Mathf.Approximately(ratio, 1f))
        {
            barCanvas.gameObject.SetActive(false);
        }
        else
        {
            if (!barCanvas.gameObject.activeSelf)
                barCanvas.gameObject.SetActive(true);
        }
    }

    void CreateHealthbar()
    {
        // Canvas
        GameObject canvasGO = new GameObject("EnemyHealthbar");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = barOffset;

        barCanvas = canvasGO.AddComponent<Canvas>();
        barCanvas.renderMode = RenderMode.WorldSpace;

        // Scaler so the pixel-sized images render correctly
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barSize.x * 100f, barSize.y * 100f);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = barBackground;
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform, false);
        fillImage = fillGO.AddComponent<Image>();
        fillImage.color = barColorHigh;
        fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    public void TakeDamage(int dmg)
    {
        if (dead) return;

        health.ApplyDamage(dmg);
        stunUntil = Time.time + stunDuration;

        Debug.Log($"{gameObject.name} took {dmg} damage → {health.CurrentHealth}/{health.MaxHealth}");

        if (!dead && health.IsAlive)
        {
            if (animator) animator.SetTrigger(hitTrigger);
            SpawnBloodFX();
        }
    }

    void SpawnBloodFX()
    {
        if (bloodPrefab == null) return;

        Vector3 pos = transform.position + transform.TransformVector(bloodOffset);
        GameObject fx = Instantiate(bloodPrefab, pos, Quaternion.identity, transform);
        if (bloodLifetime > 0f) Destroy(fx, bloodLifetime);
    }

    void Die()
    {
        if (dead) return;
        dead = true;

        gameObject.layer = 0;
        foreach (Transform child in GetComponentsInChildren<Transform>())
            child.gameObject.layer = 0;

        if (animator)
        {
            if (!string.IsNullOrEmpty(isDeadBool)) animator.SetBool(isDeadBool, true);
            animator.SetTrigger(dieTrigger);
        }

        // disable all colliders so the corpse can't block anything
        foreach (var c in GetComponentsInChildren<Collider>())
            if (c) c.enabled = false;

        // stop physics
        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // stop AI
        var enemy = GetComponent<EnemyController>();
        if (enemy) enemy.CancelInvoke();

        var rangedEnemy = GetComponent<RangedEnemyController>();
        if (rangedEnemy) rangedEnemy.CancelInvoke();
    }
}
