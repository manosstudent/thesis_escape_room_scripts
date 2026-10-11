using UnityEngine;

public class DragObject3D : MonoBehaviour
{
    [Header("Drag Settings")]
    public float smoothSpeed = 15f;
    public float maxDistance = 10f;
    public float liftSpeed = 5f;
    public float minHeight = 0.5f;
    public float maxHeight = 10f;

    [Header("Filter")]
    public string draggableTag = "Draggable";
    public LayerMask raycastMask = ~0;
    public bool dragEnabled = false;

    [Header("Bounds")]
    [Tooltip("Optional. If set, dragged objects cannot be moved outside this collider's volume.")]
    public Collider dragBounds;

    [Header("Fixed height (optional, e.g. Room 1)")]
    [Tooltip("If on, an object rises to fixedLocalHeight as soon as you pick it up, and the Up/Down arrows do nothing.")]
    public bool useFixedHeight = false;
    [Tooltip("Height an object is held at when Use Fixed Height is on, as a LOCAL Y relative to the " +
             "dragged object's own parent (the same value you see in its Transform's Position Y in the Inspector).")]
    public float fixedLocalHeight = 0.215f;

    [HideInInspector] public Camera cam;
    private Transform dragged;
    private Rigidbody draggedRb;
    private bool isDragging;
    private float dragY;
    private float fixedPlaneY;
    private Vector3 grabOffset;
    private bool dragOnScreenPlane;
    private Plane screenPlane;

    public void SetCamera(Camera camera)
    {
        cam = camera;
    }

    void LateUpdate()
    {
        if (!dragEnabled || cam == null) return;
        if (Input.GetMouseButtonDown(0)) TryPick();
        if (Input.GetMouseButtonUp(0)) Release();
        if (isDragging && dragged != null) DragMove();
    }

    void TryPick()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Take the nearest draggable along the ray, not just the nearest collider. Otherwise a
        // non-draggable collider in front of the object (e.g. the rim of the box) swallows the click.
        // Triggers (e.g. AllowedArea) are ignored entirely.
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, raycastMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        Transform tagged = null;
        foreach (RaycastHit hit in hits)
        {
            tagged = FindTaggedParent(hit.collider.transform);
            if (tagged != null) break;
        }

        if (tagged == null) return;

        dragged = tagged;
        draggedRb = dragged.GetComponent<Rigidbody>();
        dragY = dragged.position.y;
        fixedPlaneY = dragY;
        grabOffset = Vector3.zero;
        dragOnScreenPlane = false;

        if (useFixedHeight)
            dragY = LocalHeightToWorldY(dragged, fixedLocalHeight);

        // With bounds set, always drag along the floor of the allowed area. Otherwise an object
        // picked up from an unusual height (e.g. resting on the rim of the box) puts the drag plane
        // near eye level, and the mouse ray hits it right in front of the camera.
        if (dragBounds != null)
        {
            Bounds b = dragBounds.bounds;
            fixedPlaneY = b.min.y;
            dragY = Mathf.Clamp(dragY, b.min.y, b.max.y);

            // The plane is now below the object, so the mouse ray meets it further back than the
            // object itself. Remember that gap, so the object stays put on grab instead of being
            // pulled back into the box and sliding along its rim.
            Plane grabPlane = new Plane(Vector3.up, new Vector3(0, fixedPlaneY, 0));
            if (grabPlane.Raycast(ray, out float grabEnter))
            {
                grabOffset = dragged.position - ray.GetPoint(grabEnter);
                grabOffset.y = 0f;
            }
            else
            {
                // The object is above eye level (e.g. on the rim of the box): the mouse ray points
                // upwards and can never meet a plane at table height. Drag it on a plane facing the
                // camera instead, so it follows the cursor on screen and can be lowered to the table.
                dragOnScreenPlane = true;
                screenPlane = new Plane(-cam.transform.forward, dragged.position);
                if (screenPlane.Raycast(ray, out float screenEnter))
                    grabOffset = dragged.position - ray.GetPoint(screenEnter);
            }
        }

        isDragging = true;
        if (draggedRb != null)
        {
            draggedRb.useGravity = false;
            draggedRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            if (!draggedRb.isKinematic)
                draggedRb.velocity = Vector3.zero;
        }
    }

    void DragMove()
    {
        if (draggedRb != null && draggedRb.isKinematic) return;

        if (!useFixedHeight)
        {
            if (Input.GetKey(KeyCode.UpArrow))
                dragY += liftSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow))
                dragY -= liftSpeed * Time.deltaTime;
            dragY = Mathf.Clamp(dragY, minHeight, maxHeight);
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPos;

        if (dragOnScreenPlane)
        {
            if (!screenPlane.Raycast(ray, out float screenEnter)) { HoldStill(); return; }
            // Height comes from the cursor here, so the object can be brought down onto the table.
            targetPos = ray.GetPoint(screenEnter) + grabOffset;
        }
        else
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0, fixedPlaneY, 0));
            if (!plane.Raycast(ray, out float enter)) { HoldStill(); return; }
            targetPos = ray.GetPoint(enter) + grabOffset;
            targetPos.y = dragY;
        }

        // Never aim outside the allowed volume, so the object stops at the edge
        // instead of fighting a collider and being thrown around.
        if (dragBounds != null)
        {
            Bounds b = dragBounds.bounds;
            targetPos.x = Mathf.Clamp(targetPos.x, b.min.x, b.max.x);
            targetPos.y = Mathf.Clamp(targetPos.y, b.min.y, b.max.y);
            targetPos.z = Mathf.Clamp(targetPos.z, b.min.z, b.max.z);
        }

        if (draggedRb != null)
        {
            Vector3 direction = targetPos - draggedRb.position;
            draggedRb.velocity = direction * smoothSpeed;
        }
        else
        {
            dragged.position = Vector3.Lerp(dragged.position, targetPos,
                                            Time.deltaTime * smoothSpeed);
        }
    }

    // Used when the cursor points where the drag plane cannot be reached. Without this the object
    // keeps the velocity from the previous frame and drifts away on its own.
    void HoldStill()
    {
        if (draggedRb != null && !draggedRb.isKinematic)
            draggedRb.velocity = Vector3.zero;
    }

    void Release()
    {
        if (draggedRb != null)
        {
            draggedRb.useGravity = true;
            draggedRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            if (!draggedRb.isKinematic)
            {
                draggedRb.velocity = Vector3.zero;
                // Also kill the spin, otherwise round objects keep rolling away after release.
                draggedRb.angularVelocity = Vector3.zero;
            }
        }
        isDragging = false;
        dragged = null;
        draggedRb = null;
    }

    // Converts a local Y (relative to the object's own parent) to the equivalent world Y, so the
    // Inspector value can be copied straight from an object's Transform even if the room sits inside
    // a rotated/offset parent group.
    static float LocalHeightToWorldY(Transform obj, float localY)
    {
        if (obj.parent == null) return localY;

        Vector3 local = obj.parent.InverseTransformPoint(obj.position);
        local.y = localY;
        return obj.parent.TransformPoint(local).y;
    }

    Transform FindTaggedParent(Transform start)
    {
        Transform t = start;
        while (t != null)
        {
            if (t.tag == "Draggable") return t;
            t = t.parent;
        }
        return null;
    }
}