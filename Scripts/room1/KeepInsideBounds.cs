using UnityEngine;

/// <summary>
/// Keeps this object inside the volume of a reference collider.
/// Put it on each draggable object and assign the invisible bounds collider.
/// The bounds collider should have "Is Trigger" checked so it does not push objects around.
/// </summary>
public class KeepInsideBounds : MonoBehaviour
{
    [Tooltip("The invisible box collider that defines the allowed area.")]
    public Collider bounds;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (bounds == null) return;

        Bounds b = bounds.bounds;
        Vector3 p = transform.position;

        Vector3 clamped = new Vector3(
            Mathf.Clamp(p.x, b.min.x, b.max.x),
            Mathf.Clamp(p.y, b.min.y, b.max.y),
            Mathf.Clamp(p.z, b.min.z, b.max.z)
        );

        if (clamped != p)
        {
            transform.position = clamped;

            // Kill the velocity that was pushing it out, so it does not keep fighting the limit.
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
