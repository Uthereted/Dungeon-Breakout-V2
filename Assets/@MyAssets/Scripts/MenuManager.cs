using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject hud;

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
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour cameraFollowScript;

    private CinemachineBrain cinemachineBrain;
    private bool starting = false;

    private void Start()
    {
        // menú visible, HUD oculto
        if (mainMenu != null)
            mainMenu.SetActive(true);

        if (hud != null)
            hud.SetActive(false);

        // disable Cinemachine so it doesn't override camera position
        if (mainCamera != null)
        {
            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
            if (cinemachineBrain != null)
                cinemachineBrain.enabled = false;
        }

        // colocar cámara en la posición del menú
        if (mainCamera != null && menuCameraPoint != null)
        {
            mainCamera.transform.position = menuCameraPoint.position;
            mainCamera.transform.rotation = menuCameraPoint.rotation;
        }

        // desactivar control al inicio
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (cameraFollowScript != null)
            cameraFollowScript.enabled = false;
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

        // ocultar menú, mostrar HUD
        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (hud != null)
            hud.SetActive(true);

        // re-enable Cinemachine so it takes over camera control
        if (cinemachineBrain != null)
            cinemachineBrain.enabled = true;

        // reactivar control
        if (cameraFollowScript != null)
            cameraFollowScript.enabled = true;

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;
    }

    public void OnExitPressed()
    {
        Debug.Log("Salir");
        Application.Quit();
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
