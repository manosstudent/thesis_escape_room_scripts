using UnityEngine;
using System.Collections;

public class Room5AddressZone : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [Tooltip("The correct \"next\" value for this node, e.g. \"0x3A\" or \"NULL\".")]
    public string correctAddress;

    [Header("Snap")]
    public Transform snapPoint;
    public float snapSpeed = 8f;

    public bool HasCube { get; private set; }
    public bool IsCorrect { get; private set; }

    private Room5AddressCube currentCube;

    // OnTriggerStay instead of Enter: the cube enters the zone while still being dragged
    // (useGravity=false) and Enter does not fire again when it is released inside the zone.
    void OnTriggerStay(Collider other)
    {
        if (currentCube != null) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || !rb.useGravity) return; // still being held/dragged

        Room5AddressCube cube = other.GetComponentInParent<Room5AddressCube>();
        if (cube == null) return;

        currentCube = cube;
        HasCube = true;
        IsCorrect = cube.addressValue == correctAddress;

        if (IsCorrect)
            Room5ListManager.Instance?.ShowCorrectFeedback();

        if (!IsCorrect)
        {
            Room5ListManager.Instance?.ShowWrongFeedback();

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.RemovePoints(1);
        }

        if (snapPoint != null)
            StartCoroutine(SnapCube(cube.transform));

        Room5ListManager.Instance?.OnZoneChanged();
    }

    void OnTriggerExit(Collider other)
    {
        Room5AddressCube cube = other.GetComponentInParent<Room5AddressCube>();
        if (cube == null || cube != currentCube) return;

        currentCube = null;
        HasCube = false;
        IsCorrect = false;

        Room5ListManager.Instance?.OnZoneChanged();
    }

    IEnumerator SnapCube(Transform cube)
    {
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Vector3 target = new Vector3(snapPoint.position.x, cube.position.y, snapPoint.position.z);
        while (cube != null && Vector3.Distance(
            new Vector3(cube.position.x, 0f, cube.position.z),
            new Vector3(target.x, 0f, target.z)) > 0.01f)
        {
            cube.position = Vector3.Lerp(cube.position, target, Time.deltaTime * snapSpeed);
            target = new Vector3(snapPoint.position.x, cube.position.y, snapPoint.position.z);
            yield return null;
        }

        if (cube != null)
            cube.position = target;
    }
}
