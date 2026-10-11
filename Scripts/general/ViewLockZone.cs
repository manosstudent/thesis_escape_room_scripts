using UnityEngine;

/// <summary>
/// Locks the player's view to a fixed camera while they stand near something, for example
/// a crystal in front of an instruction board. Press the exit key to walk away again.
/// Needs a Collider with "Is Trigger" checked on the SAME GameObject as this script.
/// </summary>
public class ViewLockZone : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public SimpleFPSController playerMovement;
    public PlayerPickUpDrop playerPickUp;

    [Header("Cameras")]
    public Camera mainCamera;
    [Tooltip("Fixed camera aimed at what the player should read. Must NOT be tagged MainCamera.")]
    public Camera viewCamera;

    [Header("Options")]
    public KeyCode exitKey = KeyCode.Q;
    [Tooltip("Optional. Objects shown only while the view is locked.")]
    public GameObject[] showWhileLocked;
    [Tooltip("Stop the countdown while reading. Leave off, or the player can freeze the clock here.")]
    public bool pauseTimerWhileLocked = false;

    [Header("Door to open on exit (optional, use one of the two)")]
    [Tooltip("For a door that already has a DoorController component.")]
    public DoorController doorController;
    [Tooltip("For any other door: the transform that should swing open.")]
    public Transform doorTransform;
    public float doorOpenAngle = 90f;
    public float doorOpenSpeed = 3f;

    [Header("Instructions sign (optional)")]
    [Tooltip("Tick this on the instruction crystal. Leaving it with Q switches the sign from \"See the instructions first!\" to the blinking \"Welcome!\".")]
    public bool marksInstructionsRead = false;

    private bool doorOpened;

    public bool IsLocked { get; private set; }

    private CharacterController characterController;
    // After leaving, the player is still inside the trigger: wait until they step out.
    private bool waitForExit;

    void Start()
    {
        if (player != null) characterController = player.GetComponent<CharacterController>();
        SetCameraActive(viewCamera, false);
        SetExtrasVisible(false);
    }

    void Update()
    {
        if (IsLocked && Input.GetKeyDown(exitKey))
            Unlock();
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsLocked || waitForExit || !other.CompareTag("Player")) return;
        Lock();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            waitForExit = false;
    }

    void Lock()
    {
        IsLocked = true;

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerPickUp != null) playerPickUp.enabled = false;

        SetCameraActive(mainCamera, false);
        SetCameraActive(viewCamera, true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetExtrasVisible(true);

        if (pauseTimerWhileLocked && GameManager.Instance != null)
            GameManager.Instance.SetTimerPaused(true);
    }

    void Unlock()
    {
        IsLocked = false;
        waitForExit = true;

        SetCameraActive(viewCamera, false);
        SetCameraActive(mainCamera, true);

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerPickUp != null) playerPickUp.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetExtrasVisible(false);

        if (pauseTimerWhileLocked && GameManager.Instance != null)
            GameManager.Instance.SetTimerPaused(false);

        if (marksInstructionsRead && GameManager.Instance != null)
            GameManager.Instance.InstructionsRead();

        OpenDoorOnce();
    }

    void OpenDoorOnce()
    {
        if (doorOpened) return;

        if (doorController != null)
        {
            doorOpened = true;
            doorController.OpenDoor();
        }
        else if (doorTransform != null)
        {
            doorOpened = true;
            StartCoroutine(SwingDoor());
        }
    }

    System.Collections.IEnumerator SwingDoor()
    {
        Quaternion closed = doorTransform.rotation;
        Quaternion open = Quaternion.Euler(
            doorTransform.eulerAngles.x,
            doorTransform.eulerAngles.y + doorOpenAngle,
            doorTransform.eulerAngles.z);

        for (float t = 0f; t < 1f; t += Time.deltaTime * doorOpenSpeed)
        {
            doorTransform.rotation = Quaternion.Slerp(closed, open, t);
            yield return null;
        }
        doorTransform.rotation = open;
    }

    // Also toggles the camera's Audio Listener, so two listeners are never active at once.
    static void SetCameraActive(Camera cam, bool active)
    {
        if (cam == null) return;
        cam.enabled = active;
        AudioListener listener = cam.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = active;
    }

    void SetExtrasVisible(bool visible)
    {
        if (showWhileLocked == null) return;
        foreach (GameObject item in showWhileLocked)
            if (item != null)
                item.SetActive(visible);
    }
}
