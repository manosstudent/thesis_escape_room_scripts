using UnityEngine;

/// <summary>
/// The portal after the last room. The game is won when the player stands on it.
/// Instead of trigger events (a CharacterController does not report OnTriggerStay reliably),
/// this checks every frame where the player's feet are, which is exact and predictable.
/// Needs a Collider on the same GameObject; keep it a thin slab at ground level.
/// </summary>
public class FinishZone : MonoBehaviour
{
    [Tooltip("Only finish while the player is standing on the ground, not jumping through.")]
    public bool requireOnGround = true;

    [Tooltip("How far above or below the slab the feet may be and still count, in metres.")]
    public float feetTolerance = 0.15f;

    private Collider area;
    private Transform player;
    private Collider playerCollider;
    private CharacterController playerController;

    void Awake()
    {
        area = GetComponent<Collider>();
    }

    void Update()
    {
        if (GameManager.Instance == null || area == null) return;

        if (player == null && !FindPlayer()) return;

        // Feet = bottom centre of the player's own collider.
        Vector3 feet = playerCollider != null
            ? new Vector3(playerCollider.bounds.center.x, playerCollider.bounds.min.y, playerCollider.bounds.center.z)
            : player.position;

        Bounds zone = area.bounds;

        bool overThePortal = feet.x >= zone.min.x && feet.x <= zone.max.x
                          && feet.z >= zone.min.z && feet.z <= zone.max.z;
        if (!overThePortal) return;

        bool standingOnIt = feet.y >= zone.min.y - feetTolerance
                         && feet.y <= zone.max.y + feetTolerance;
        if (!standingOnIt) return;

        if (requireOnGround && playerController != null && !playerController.isGrounded) return;

        GameManager.Instance.Win();
    }

    bool FindPlayer()
    {
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found == null) return false;

        player = found.transform;
        playerCollider = found.GetComponent<Collider>();
        playerController = found.GetComponent<CharacterController>();
        return true;
    }
}
