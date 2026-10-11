using UnityEngine;

public class ScreenInteraction : MonoBehaviour
{
    [Header("References")]
    public ScreenController screenController;
    public Transform povPoint;
    public GameObject pressKText;

    [Header("Settings")]
    public float raycastDistance = 5f;
    public float proximityDistance = 3f;
    public KeyCode interactKey = KeyCode.K;

    private bool isLocked = false;
    private Transform playerCamera;
    private SimpleFPSController fpsController;

    private Vector3 originalCamPosition;
    private Quaternion originalCamRotation;

    void Start()
    {
        playerCamera = Camera.main.transform;
        fpsController = FindObjectOfType<SimpleFPSController>();
        pressKText.SetActive(false);
    }

    void Update()
    {
        if (isLocked)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Unlock();
            return;
        }

        if (Camera.main == null) return;
        float distance = Vector3.Distance(transform.position, Camera.main.transform.position);

        if (distance > proximityDistance)
        {
            pressKText.SetActive(false);
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            if (hit.collider.gameObject == screenController.gameObject &&
                screenController.GetCurrentState() == ScreenController.ScreenState.Active)
            {
                pressKText.SetActive(true);

                if (Input.GetKeyDown(interactKey))
                    Lock();
            }
            else
            {
                pressKText.SetActive(false);
            }
        }
        else
        {
            pressKText.SetActive(false);
        }
    }

    void Lock()
    {
        isLocked = true;
        originalCamPosition = playerCamera.position;
        originalCamRotation = playerCamera.rotation;

        playerCamera.position = povPoint.position;
        playerCamera.rotation = povPoint.rotation;

        fpsController.enabled = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Unlock()
    {
        isLocked = false;
        playerCamera.position = originalCamPosition;
        playerCamera.rotation = originalCamRotation;

        fpsController.enabled = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        pressKText.SetActive(false);
    }
}