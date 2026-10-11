using UnityEngine;

/// <summary>
/// Lets the player click and drag dishes during their turn of the Room4 sink task.
/// Put it on the same GameObject as the SinkTaskManager. The manager decides whether a
/// drop was correct; this script only carries the dish.
/// </summary>
[RequireComponent(typeof(SinkTaskManager))]
public class SinkDrag : MonoBehaviour
{
    [Tooltip("How far above the top of the sinks a dish is carried while dragged.")]
    public float carryHeight = 0.15f;
    [Tooltip("How quickly the dish catches up with the cursor.")]
    public float followSpeed = 20f;
    [Tooltip("How quickly the dish centres under the cursor after being grabbed. 0 keeps the grab offset.")]
    public float centeringSpeed = 6f;
    public float maxDistance = 10f;

    private SinkTaskManager manager;
    private SinkDish held;
    private Vector3 grabOffset;
    private float carryY;

    void Awake()
    {
        manager = GetComponent<SinkTaskManager>();
    }

    void Update()
    {
        // Null whenever dragging is not allowed (not seated, demo playing, dish animating, solved).
        Camera cam = manager.DragCamera;
        if (cam == null)
        {
            if (held != null)
            {
                manager.CancelDrag(held);
                held = null;
            }
            return;
        }

        if (held == null)
        {
            if (Input.GetMouseButtonDown(0)) TryGrab(cam);
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            SinkDish dropped = held;
            held = null;
            manager.OnDishReleased(dropped);
            return;
        }

        Carry(cam);
    }

    void TryGrab(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Take the nearest dish along the ray, so a sink edge or screen in front does not eat the click.
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (h1, h2) => h1.distance.CompareTo(h2.distance));

        foreach (RaycastHit hit in hits)
        {
            SinkDish dish = hit.collider.GetComponentInParent<SinkDish>();
            if (dish == null) continue;

            held = dish;
            carryY = manager.SinksTopY + carryHeight;

            // Keep the gap between the dish and the cursor point, so the dish does not jump on grab.
            grabOffset = Vector3.zero;
            Plane plane = new Plane(Vector3.up, new Vector3(0f, carryY, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                grabOffset = dish.transform.position - ray.GetPoint(enter);
                grabOffset.y = 0f;
            }

            manager.OnDishGrabbed(dish);
            return;
        }
    }

    void Carry(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, carryY, 0f));

        // Cursor above the horizon: the plane cannot be reached, so leave the dish where it is.
        if (!plane.Raycast(ray, out float enter)) return;

        // Fade the grab offset away, so what you drop is what sits under the cursor.
        grabOffset = Vector3.Lerp(grabOffset, Vector3.zero, centeringSpeed * Time.deltaTime);

        Vector3 target = ray.GetPoint(enter) + grabOffset;
        target.y = carryY;
        held.transform.position = Vector3.Lerp(held.transform.position, target, followSpeed * Time.deltaTime);
    }
}
