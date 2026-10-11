using UnityEngine;

/// <summary>
/// The portal in front of the sinks. When the player steps on it, movement is frozen, the view
/// switches to the seat camera and the sink task starts. Press the stand-up key to leave.
/// Needs a Collider with "Is Trigger" checked on the SAME GameObject as this script.
/// </summary>
public class SinkSeat : MonoBehaviour
{
    [Header("Task")]
    public SinkTaskManager manager;

    [Header("Player")]
    public Transform player;
    public SimpleFPSController playerMovement;
    public PlayerPickUpDrop playerPickUp;

    [Header("Cameras")]
    public Camera mainCamera;
    [Tooltip("Fixed camera looking at the sinks and the stool. Must NOT be tagged MainCamera.")]
    public Camera seatCamera;

    [Header("Options")]
    [Tooltip("Optional. Where the player is moved while on the portal. Leave empty to keep them where they are.")]
    public Transform seatPoint;
    public KeyCode standUpKey = KeyCode.Q;
    [Tooltip("Optional. Shown only while the player is on the portal (e.g. instruction screens).")]
    public GameObject[] seatInstructions;

    public bool IsSeated { get; private set; }

    private CharacterController characterController;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    // After standing up the player is still on the portal; wait until they step off before it works again.
    private bool waitForExit;

    void Start()
    {
        if (player != null) characterController = player.GetComponent<CharacterController>();
        SetCameraActive(seatCamera, false);
        SetInstructionsVisible(false);
    }

    void Update()
    {
        if (IsSeated && Input.GetKeyDown(standUpKey))
            StandUp();
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsSeated || waitForExit || !other.CompareTag("Player")) return;
        Sit();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            waitForExit = false;
    }

    void Sit()
    {
        IsSeated = true;

        savedPosition = player.position;
        savedRotation = player.rotation;

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerPickUp != null) playerPickUp.enabled = false;

        if (seatPoint != null) Teleport(seatPoint.position, seatPoint.rotation);

        SetCameraActive(mainCamera, false);
        SetCameraActive(seatCamera, true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetInstructionsVisible(true);

        if (manager != null) manager.OnSeated();
    }

    void StandUp()
    {
        IsSeated = false;
        waitForExit = true;

        if (seatPoint != null) Teleport(savedPosition, savedRotation);

        SetCameraActive(seatCamera, false);
        SetCameraActive(mainCamera, true);

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerPickUp != null) playerPickUp.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetInstructionsVisible(false);

        if (manager != null) manager.OnStood();
    }

    // The CharacterController ignores direct position changes, so switch it off while moving the player.
    void Teleport(Vector3 position, Quaternion rotation)
    {
        if (characterController != null) characterController.enabled = false;
        player.SetPositionAndRotation(position, rotation);
        if (characterController != null) characterController.enabled = true;
    }

    // Also toggles the camera's Audio Listener, so two listeners are never active at once.
    static void SetCameraActive(Camera cam, bool active)
    {
        if (cam == null) return;
        cam.enabled = active;
        AudioListener listener = cam.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = active;
    }

    void SetInstructionsVisible(bool visible)
    {
        if (seatInstructions == null) return;
        foreach (GameObject screen in seatInstructions)
            if (screen != null)
                screen.SetActive(visible);
    }
}
