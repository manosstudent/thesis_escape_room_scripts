using UnityEngine;
using System.Collections;

public class DropZone : MonoBehaviour
{
    public string draggableTag = "Draggable";
    public string keyObjectName = "Sphere";

    [Header("Entry Validation")]
    [Tooltip("Πόσο κάτω από το άνω άκρο του trigger επιτρέπεται να είναι το αντικείμενο κατά την είσοδο (σε world units).")]
    public float topEntryTolerance = 0.05f;

    public Transform door;
    public float openAngle = 90f;
    public float openSpeed = 3f;
    private bool doorOpened = false;
    private Quaternion closedRot;
    private Quaternion openedRot;

    void Start()
    {
        if (door != null)
        {
            closedRot = door.rotation;
            openedRot = Quaternion.Euler(
                door.rotation.eulerAngles.x,
                door.rotation.eulerAngles.y + openAngle,
                door.rotation.eulerAngles.z
            );
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Απόρριψη αν το object κρατιέται ακόμα (useGravity=false → dragging)
        Rigidbody otherRb = other.attachedRigidbody;
        if (otherRb != null && !otherRb.useGravity)
            return;

        // Απόρριψη αν δεν έρχεται από πάνω
        Collider myCol = GetComponent<Collider>();
        if (myCol != null)
        {
            float pipeTopY = myCol.bounds.max.y;
            if (other.transform.position.y < pipeTopY - topEntryTolerance)
                return;
        }

        Transform t = other.transform;
        while (t != null)
        {
            if (t.CompareTag(draggableTag)) break;
            t = t.parent;
        }
        if (t == null) return;

        Rigidbody rb = t.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        Collider col = t.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        t.gameObject.SetActive(false);

        // Ο Room1QuizManager αποφασίζει αν είναι σωστό, κρατάει τον γύρο και ανοίγει την πόρτα.
        if (Room1QuizManager.Instance != null)
        {
            Room1QuizManager.Instance.OnObjectDropped(t.gameObject);
            return;
        }

        // Fallback: παλιά συμπεριφορά ενός γύρου, αν δεν υπάρχει manager στη σκηνή.
        if (!doorOpened && t.name == keyObjectName && door != null)
        {
            doorOpened = true;
            StartCoroutine(OpenDoor());
        }
        else
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.RemovePoints(1);
        }
    }

    IEnumerator OpenDoor()
    {
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openSpeed;
            door.rotation = Quaternion.Slerp(closedRot, openedRot, elapsed);
            yield return null;
        }
    }
}