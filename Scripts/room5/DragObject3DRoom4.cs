using UnityEngine;

public class DragObject3DRoom4 : MonoBehaviour
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
    public bool ignoreOtherDraggables = true;

    [Header("Fixed height (optional, e.g. Room 5)")]
    [Tooltip("If on, held objects hover at a constant height and the Up/Down arrows do nothing.")]
    public bool useFixedHeight = false;
    [Tooltip("Height held objects are held at when Use Fixed Height is on, as a LOCAL Y relative to the " +
             "dragged object's own parent (the same value you see in its Transform's Position Y in the Inspector).")]
    public float fixedLocalHeight = 0.16f;

    [HideInInspector] public Camera cam;

    private Transform dragged;
    private Rigidbody draggedRb;
    private bool isDragging;
    private float dragY;
    private float fixedPlaneY;
    private Vector3 grabOffset;
    private Vector3 targetPos;
    private bool hasTarget;
    private Collider[] draggedColliders;
    private System.Collections.Generic.List<Collider> ignoredColliders = new System.Collections.Generic.List<Collider>();
    private RigidbodyConstraints originalConstraints;

    public void SetCamera(Camera camera)
    {
        cam = camera;
    }

    void LateUpdate()
    {
        if (!dragEnabled || cam == null) return;
        if (Input.GetMouseButtonDown(0)) TryPick();
        if (Input.GetMouseButtonUp(0)) Release();
        if (isDragging && dragged != null) ComputeTarget();
    }

    void FixedUpdate()
    {
        if (!isDragging || dragged == null || !hasTarget) return;

        if (draggedRb != null)
        {
            // Ταχύτητα (όχι kinematic teleport) ώστε να συνεχίζει να μπλοκάρεται από τοίχους/τραπέζι/πάτωμα,
            // ενώ αγνοεί μόνο τα άλλα Draggable αντικείμενα (βλ. TryPick).
            Vector3 next = Vector3.Lerp(draggedRb.position, targetPos, Time.fixedDeltaTime * smoothSpeed);
            draggedRb.velocity = (next - draggedRb.position) / Time.fixedDeltaTime;
            draggedRb.angularVelocity = Vector3.zero;
        }
        else
        {
            dragged.position = Vector3.Lerp(dragged.position, targetPos, Time.fixedDeltaTime * smoothSpeed);
        }
    }

    void TryPick()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        // Αγνόησε triggers (π.χ. StackZone) ώστε το raycast να βρίσκει πάντα το ίδιο το πιάτο, όχι τη ζώνη γύρω του
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastMask, QueryTriggerInteraction.Ignore))
            return;

        Transform tagged = FindTaggedParent(hit.collider.transform);
        if (tagged == null) return;

        dragged = tagged;
        draggedRb = dragged.GetComponent<Rigidbody>();
        fixedPlaneY = dragged.position.y;   // used only to project the mouse ray to XZ, keep it at the pick-up height
        dragY = useFixedHeight ? LocalHeightToWorldY(dragged, fixedLocalHeight) : fixedPlaneY;

        // Διατηρεί το σημείο που "έπιασες" το αντικείμενο αντί να το κολλάει στο κέντρο του κέρσορα
        Plane plane = new Plane(Vector3.up, new Vector3(0, fixedPlaneY, 0));
        Vector3 hitOnPlane = plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : hit.point;
        grabOffset = dragged.position - hitOnPlane;

        isDragging = true;
        hasTarget = false;

        if (draggedRb != null)
        {
            draggedRb.useGravity = false;
            draggedRb.velocity = Vector3.zero;
            draggedRb.angularVelocity = Vector3.zero;
            draggedRb.interpolation = RigidbodyInterpolation.Interpolate;
            // Κλείδωμα περιστροφής όσο το κρατάς, ώστε να μη στριφογυρνάει στον αέρα
            originalConstraints = draggedRb.constraints;
            draggedRb.constraints = originalConstraints | RigidbodyConstraints.FreezeRotation;
        }

        // Αγνόησε συγκρούσεις με τα υπόλοιπα Draggable αντικείμενα (π.χ. στοίβα πιάτων) όσο το κρατάς,
        // αλλιώς η ξαφνική κίνηση ενώ ακουμπάνε πάνω του προκαλεί βίαιο "pop" απ' το physics engine.
        // Προαιρετικό (ignoreOtherDraggables) ώστε άλλα δωμάτια (π.χ. room5) να επιτρέπουν κανονική σύγκρουση μεταξύ τους.
        if (ignoreOtherDraggables)
        {
            draggedColliders = dragged.GetComponentsInChildren<Collider>();
            GameObject[] others = GameObject.FindGameObjectsWithTag(draggableTag);
            foreach (GameObject go in others)
            {
                if (go == dragged.gameObject) continue;
                foreach (Collider otherCol in go.GetComponentsInChildren<Collider>())
                {
                    foreach (Collider ownCol in draggedColliders)
                        Physics.IgnoreCollision(ownCol, otherCol, true);
                    ignoredColliders.Add(otherCol);
                }
            }
        }
    }

    void ComputeTarget()
    {
        if (!useFixedHeight)
        {
            if (Input.GetKey(KeyCode.UpArrow))
                dragY += liftSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow))
                dragY -= liftSpeed * Time.deltaTime;
            dragY = Mathf.Clamp(dragY, minHeight, maxHeight);
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0, fixedPlaneY, 0));
        if (!plane.Raycast(ray, out float enter)) return;

        Vector3 point = ray.GetPoint(enter) + grabOffset;
        point.y = dragY;
        targetPos = point;
        hasTarget = true;
    }

    void Release()
    {
        if (draggedRb != null)
        {
            draggedRb.useGravity = true;
            draggedRb.velocity = Vector3.zero;
            draggedRb.angularVelocity = Vector3.zero;
            draggedRb.constraints = originalConstraints;
        }

        if (draggedColliders != null)
        {
            foreach (Collider otherCol in ignoredColliders)
            {
                if (otherCol == null) continue;
                foreach (Collider ownCol in draggedColliders)
                    if (ownCol != null)
                        Physics.IgnoreCollision(ownCol, otherCol, false);
            }
        }
        ignoredColliders.Clear();
        draggedColliders = null;

        isDragging = false;
        hasTarget = false;
        dragged = null;
        draggedRb = null;
    }

    // Converts a local Y (relative to the object's own parent) to the equivalent world Y, so the
    // Inspector value can be copied straight from a cube's Transform even if the room sits inside
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
            if (t.CompareTag(draggableTag)) return t;
            t = t.parent;
        }
        return null;
    }
}
