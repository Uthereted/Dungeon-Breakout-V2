using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonEscape : MonoBehaviour
{
    public static DungeonEscape Instance { get; private set; }

    [Header("Goal")]
    public int chestsRequired = 2;

    [Header("Escape")]
    public string escapeSceneName = "";
    public bool reloadCurrentScene = true;

    int chestsLooted;

    // auto-discovered at runtime
    GameObject portalBlocker;

    public int ChestsLooted => chestsLooted;
    public int ChestsRequired => chestsRequired;
    public bool PortalUnlocked => chestsLooted >= chestsRequired;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// Called by ProceduralDungeonGenerator after all rooms are placed.
    public void Setup()
    {
        StartCoroutine(SetupRoutine());
    }

    IEnumerator SetupRoutine()
    {
        // wait one frame so all DungeonPiece.Awake have run
        yield return null;

        // find all exit rooms and lock their portals
        var allPieces = FindObjectsByType<DungeonPiece>(FindObjectsSortMode.None);

        foreach (var piece in allPieces)
        {
            if (piece.specialRoomType == SpecialRoomType.Exit)
            {
                // look for a child named "PortalBlocker" to disable when unlocked
                Transform blocker = piece.transform.Find("PortalBlocker");
                if (blocker != null)
                {
                    portalBlocker = blocker.gameObject;
                    portalBlocker.SetActive(true);
                }

                // add EscapePortal trigger if not already present
                Transform portalTrigger = piece.transform.Find("PortalTrigger");
                if (portalTrigger != null && portalTrigger.GetComponent<EscapePortal>() == null)
                    portalTrigger.gameObject.AddComponent<EscapePortal>();
            }
        }

        Debug.Log($"[DungeonEscape] Ready — need {chestsRequired} chests to unlock portal");
    }

    public void ChestLooted()
    {
        chestsLooted++;
        Debug.Log($"[DungeonEscape] Chest looted! {chestsLooted}/{chestsRequired}");

        if (chestsLooted >= chestsRequired)
            UnlockPortal();
    }

    void UnlockPortal()
    {
        Debug.Log("[DungeonEscape] Portal unlocked!");
        if (portalBlocker) portalBlocker.SetActive(false);
    }

    public void Escape()
    {
        if (!PortalUnlocked) return;

        Debug.Log("[DungeonEscape] Escaped the dungeon!");

        if (reloadCurrentScene)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        else if (!string.IsNullOrEmpty(escapeSceneName))
            SceneManager.LoadScene(escapeSceneName);
    }
}
