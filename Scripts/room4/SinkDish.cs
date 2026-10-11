using UnityEngine;

/// <summary>
/// One dish of the Room4 sink task. Put it on every dish; the dish also needs a Collider
/// so it can be clicked. Dishes are never moved by physics, only by the task scripts.
/// </summary>
public class SinkDish : MonoBehaviour
{
    [Tooltip("Must match the colour names used in the task steps, e.g. Red, Blue, Green.")]
    public string colorName;

    // Where the dish sits on the stool at the start. Dishes return here between the demo and the task.
    public Vector3 HomePosition { get; private set; }
    public Quaternion HomeRotation { get; private set; }

    void Awake()
    {
        HomePosition = transform.position;
        HomeRotation = transform.rotation;

        // The task places dishes exactly; physics would only knock the stacks over.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}
