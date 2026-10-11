using UnityEngine;
using System.Collections;

/// <summary>
/// Puts the player back at the starting point if they fall off the world.
/// Attach to the Player GameObject.
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    [Tooltip("If the player's Y goes below this, the fall is detected.")]
    public float fallThreshold = -5f;

    [Tooltip("Seconds to wait before putting the player back.")]
    public float respawnDelay = 2f;

    [Tooltip("Optional. If empty, the player's position at Start is used.")]
    public Transform respawnPoint;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private CharacterController characterController;
    private bool isRespawning;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (isRespawning) return;

        if (transform.position.y < fallThreshold)
            StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;

        yield return new WaitForSeconds(respawnDelay);

        Vector3 targetPos = respawnPoint != null ? respawnPoint.position : startPosition;
        Quaternion targetRot = respawnPoint != null ? respawnPoint.rotation : startRotation;

        // The CharacterController overrides transform moves, so switch it off while teleporting.
        if (characterController != null) characterController.enabled = false;

        transform.position = targetPos;
        transform.rotation = targetRot;

        if (characterController != null) characterController.enabled = true;

        isRespawning = false;
    }
}
