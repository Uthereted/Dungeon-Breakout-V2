using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class MenuManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject mainMenu;

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
        // menú visible
        if (mainMenu != null)
            mainMenu.SetActive(true);

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

        // ocultar menú
        if (mainMenu != null)
            mainMenu.SetActive(false);

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
}
