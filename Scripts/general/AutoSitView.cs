using UnityEngine;

public class AutoSitView : MonoBehaviour
{
    public static Camera CurrentCamera;

    public Transform player;
    public Transform sitView;
    public Camera mainCamera;
    public Camera sitCamera;
    public SimpleFPSController playerMovement;
    public DragObject3D dragObject3D;
    public KeyCode exitKey = KeyCode.Q;

    [Tooltip("Optional. Shown only while the player is sitting (e.g. instruction screens on the wall).")]
    public GameObject[] sitInstructions;

    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private bool isSitting = false;

    /// <summary>True while the player is seated and looking through sitCamera.</summary>
    public bool IsSitting => isSitting;
    private CharacterController characterController;
    private float exitCooldown = 0f;

    private void Start()
    {
        characterController = player.GetComponent<CharacterController>();
        CurrentCamera = mainCamera;

        SetInstructionsVisible(false);
    }

    private void SetInstructionsVisible(bool visible)
    {
        if (sitInstructions == null) return;

        foreach (GameObject screen in sitInstructions)
            if (screen != null)
                screen.SetActive(visible);
    }

    private void Update()
    {
        if (exitCooldown > 0f)
            exitCooldown -= Time.deltaTime;

        if (!isSitting) return;
        if (Input.GetKeyDown(exitKey))
            ExitSitView();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isSitting) return;
        if (exitCooldown > 0f) return;
        if (!other.CompareTag("Player")) return;

        savedPosition = player.position;
        savedRotation = player.rotation;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (characterController != null)
            characterController.enabled = false;

        player.position = sitView.position;
        player.rotation = sitView.rotation;

        if (mainCamera != null) mainCamera.enabled = false;
        if (sitCamera != null) sitCamera.enabled = true;
        CurrentCamera = sitCamera;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (dragObject3D != null)
        {
            dragObject3D.SetCamera(sitCamera);
            dragObject3D.dragEnabled = true;
        }

        SetInstructionsVisible(true);

        isSitting = true;
    }

    private void ExitSitView()
    {
        if (characterController != null)
            characterController.enabled = false;

        player.position = savedPosition;
        player.rotation = savedRotation;

        if (characterController != null)
            characterController.enabled = true;

        if (mainCamera != null) mainCamera.enabled = true;
        if (sitCamera != null) sitCamera.enabled = false;
        CurrentCamera = mainCamera;

        if (playerMovement != null)
            playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (dragObject3D != null)
        {
            dragObject3D.SetCamera(null);
            dragObject3D.dragEnabled = false;
        }

        SetInstructionsVisible(false);

        exitCooldown = 1.5f;
        isSitting = false;
    }
}