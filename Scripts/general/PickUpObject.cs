using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickUpObject : MonoBehaviour
{
    private Rigidbody rb;
    private Collider[] objectColliders;
    private bool isHeld = false;
    private Transform holdPoint;

    [Header("Follow Settings")]
    public float moveSpeed = 15f;
    public float rotateSpeed = 15f;
    [Tooltip("Maximum speed while carried. Lower values stop the object from tunneling through thin colliders.")]
    public float maxCarrySpeed = 6f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        objectColliders = GetComponentsInChildren<Collider>();
    }

    private void FixedUpdate()
    {
        if (!isHeld || holdPoint == null) return;

        Vector3 targetPos = holdPoint.position;
        Vector3 direction = targetPos - transform.position;
        rb.velocity = Vector3.ClampMagnitude(direction * moveSpeed, maxCarrySpeed);

        Quaternion targetRot = holdPoint.rotation;
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRotation);
    }

    public void PickUp(Transform holdTransform, Collider[] playerColliders)
    {
        isHeld = true;
        holdPoint = holdTransform;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        foreach (Collider objCol in objectColliders)
            foreach (Collider playerCol in playerColliders)
                if (objCol != null && playerCol != null)
                    Physics.IgnoreCollision(objCol, playerCol, true);
    }

    public void Drop(Collider[] playerColliders)
    {
        isHeld = false;
        holdPoint = null;
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        foreach (Collider objCol in objectColliders)
            foreach (Collider playerCol in playerColliders)
                if (objCol != null && playerCol != null)
                    Physics.IgnoreCollision(objCol, playerCol, false);
    }

    public bool IsHeld()
    {
        return isHeld;
    }
}