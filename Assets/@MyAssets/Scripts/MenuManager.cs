using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject hud;
    [SerializeField] private GameObject pauseMenu;

    [Header("Pausa")]
    [SerializeField] private string mainMenuScene = "TitleScreen";

    [Header("Interact Popup")]
    [SerializeField] private GameObject interactRoot;
    [SerializeField] private TMP_Text interactLabel;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector3 worldOffset = new Vector3(1f, 2f, 0f);

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform menuCameraPoint;
    [SerializeField] private Transform startCameraPoint;
    [SerializeField] private float moveDuration = 2f;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;

    private CinemachineBrain cinemachineBrain;
    private bool starting;
    private bool isPaused;
    private bool gameStarted;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);
        if (hud != null) hud.SetActive(false);

        if (mainCamera != null)
        {
            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
            if (cinemachineBrain != null) cinemachineBrain.enabled = false;
        }

        if (mainCamera != null && menuCameraPoint != null)
        {
            mainCamera.transform.position = menuCameraPoint.position;
            mainCamera.transform.rotation = menuCameraPoint.rotation;
        }

        if (playerController != null) playerController.enabled = false;
    }

    public void OnStartPressed()
    {
        if (starting) return;
        StartCoroutine(MoveCameraToStart());
    }

    private IEnumerator MoveCameraToStart()
    {
        starting = true;

        Vector3 initialPos = mainCamera.transform.position;
        Quaternion initialRot = mainCamera.transform.rotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            mainCamera.transform.position = Vector3.Lerp(initialPos, startCameraPoint.position, t);
            mainCamera.transform.rotation = Quaternion.Slerp(initialRot, startCameraPoint.rotation, t);
            yield return null;
        }

        if (mainMenu != null) mainMenu.SetActive(false);
        if (hud != null) hud.SetActive(true);

        SetGameplayActive(true);
        gameStarted = true;
    }

    public void OnExitPressed()
    {
        Application.Quit();
    }

    // --- Pausa ---

    private void Update()
    {
        if (gameStarted && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) OnResumePressed();
            else Pause();
        }
    }

    void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetGameplayActive(false);

        if (pauseMenu != null) pauseMenu.SetActive(true);
        if (hud != null) hud.SetActive(false);
    }

    public void OnResumePressed()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetGameplayActive(true);

        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (hud != null) hud.SetActive(true);
    }

    public void OnRetryPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnPauseExitPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    void SetGameplayActive(bool active)
    {
        if (playerController != null) playerController.enabled = active;
        if (cinemachineBrain != null) cinemachineBrain.enabled = active;

        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !active;
    }

    // --- Interact Popup ---

    public void ShowInteract(string text)
    {
        if (interactLabel) interactLabel.text = text;
        if (interactRoot) interactRoot.SetActive(true);
    }

    public void HideInteract()
    {
        if (interactRoot) interactRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        if (interactRoot == null || !interactRoot.activeSelf) return;
        if (playerTransform == null || mainCamera == null) return;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(playerTransform.position + worldOffset);
        if (screenPos.z > 0)
            interactRoot.transform.position = screenPos;
    }
}
