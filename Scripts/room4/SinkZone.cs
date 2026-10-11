using UnityEngine;

/// <summary>
/// The area of one sink in the Room4 sink task, and where its dishes stack.
/// Put one on the wash sink and one on the dry sink, and size the Box Collider to the basin.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class SinkZone : MonoBehaviour
{
    [Tooltip("Optional. Where the bottom dish sits. If empty, the bottom centre of the Box Collider is used.")]
    public Transform stackBase;

    [Tooltip("Vertical gap between two stacked dishes (roughly the thickness of a dish).")]
    public float dishSpacing = 0.04f;

    private BoxCollider area;

    void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void Awake()
    {
        area = GetComponent<BoxCollider>();
        // Only the volume is used; it should not block the player or anything else.
        area.isTrigger = true;
    }

    /// <summary>Top of the sink area, used to decide how high dishes are carried.</summary>
    public float TopY => area.bounds.max.y;

    /// <summary>True if a point is above or inside the basin, ignoring height.</summary>
    public bool ContainsXZ(Vector3 point)
    {
        Bounds b = area.bounds;
        bool result = point.x >= b.min.x && point.x <= b.max.x
                   && point.z >= b.min.z && point.z <= b.max.z;
        Debug.Log("[SINK-TEMP] " + name + " bounds min=" + b.min + " max=" + b.max
                  + " | point=" + point + " | result=" + result);   // TEMP
        return result;
    }

    /// <summary>World position of the dish at the given height in the stack (0 = bottom).</summary>
    public Vector3 SlotPosition(int index)
    {
        Vector3 bottom;
        if (stackBase != null)
        {
            bottom = stackBase.position;
        }
        else
        {
            Bounds b = area.bounds;
            bottom = new Vector3(b.center.x, b.min.y, b.center.z);
        }
        return bottom + Vector3.up * dishSpacing * index;
    }
}
