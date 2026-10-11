using UnityEngine;

public class AutoSitViewRoom4 : MonoBehaviour
{
    public Transform player;
    public Transform sitView;
    public Camera mainCamera;
    public Camera sitCamera;
    public SimpleFPSController playerMovement;
    public DragObject3DRoom4 dragObject3D;
    public CapsuleCollider playerCollider;
    public KeyCode exitKey = KeyCode.Q;

    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private bool isSitting = false;
    private CharacterController characterController;
    private float exitCooldown = 0f;

    private void Start()
    {
        characterController = player.GetComponent<CharacterController>();
        AutoSitView.CurrentCamera = mainCamera;
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

        if (playerCollider != null)
            playerCollider.enabled = false;

        player.position = sitView.position;
        player.rotation = sitView.rotation;

        if (mainCamera != null) mainCamera.enabled = false;
        if (sitCamera != null) sitCamera.enabled = true;
        AutoSitView.CurrentCamera = sitCamera;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (dragObject3D != null)
        {
            dragObject3D.SetCamera(sitCamera);
            dragObject3D.dragEnabled = true;
        }

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

        if (playerCollider != null)
            playerCollider.enabled = true;

        if (mainCamera != null) mainCamera.enabled = true;
        if (sitCamera != null) sitCamera.enabled = false;
        AutoSitView.CurrentCamera = mainCamera;

        if (playerMovement != null)
            playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (dragObject3D != null)
        {
            dragObject3D.SetCamera(null);
            dragObject3D.dragEnabled = false;
        }

        exitCooldown = 1.5f;
        isSitting = false;
    }
}
