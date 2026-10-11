using UnityEngine;

/// <summary>
/// Put on a thin trigger collider that covers the floor of Room 5.
/// If one of the listed cubes falls off the table and touches it, the cube goes back
/// to the position and rotation it had when the game started.
/// Objects that are not in the list (the player, doors, ...) are ignored.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Room5FloorReset : MonoBehaviour
{
    [Tooltip("The cubes that must stay on the table. Drag the objects that have the Rigidbody.")]
    public Transform[] objects;

    private Vector3[] homePositions;
    private Quaternion[] homeRotations;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        homePositions = new Vector3[objects.Length];
        homeRotations = new Quaternion[objects.Length];

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null) continue;
            homePositions[i] = objects[i].position;
            homeRotations[i] = objects[i].rotation;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null) continue;
            if (other.transform != objects[i] && !other.transform.IsChildOf(objects[i])) continue;

            SendHome(i);
            return;
        }
    }

    void SendHome(int i)
    {
        Transform t = objects[i];

        Rigidbody rb = t.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        t.SetPositionAndRotation(homePositions[i], homeRotations[i]);

        if (rb != null)
        {
            rb.position = homePositions[i];
            rb.rotation = homeRotations[i];
        }
    }
}
