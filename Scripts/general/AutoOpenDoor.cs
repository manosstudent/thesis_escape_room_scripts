using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AutoOpenDoor : MonoBehaviour
{
    public Transform door;            // The door object to rotate
    public float openAngle = 90f;     // How much the door opens
    public float openSpeed = 3f;      // Speed of opening/closing
    public float autoCloseDelay = 3f; // Wait time before closing
    private bool isOpen = false;
    private Quaternion closedRot;
    private Quaternion openedRot;
    void Start()
    {
        closedRot = door.rotation;
        openedRot = Quaternion.Euler(
            door.rotation.eulerAngles.x,
            door.rotation.eulerAngles.y + openAngle,
            door.rotation.eulerAngles.z
        );
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isOpen)
        {
            StartCoroutine(OpenAndClose());
        }
    }
    IEnumerator OpenAndClose()
    {
        isOpen = true;
        // --- Open Door ---
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            door.rotation = Quaternion.Slerp(closedRot, openedRot, t);
            yield return null;
        }
        // Wait for X seconds
        yield return new WaitForSeconds(autoCloseDelay);
        // --- Close Door ---
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            door.rotation = Quaternion.Slerp(openedRot, closedRot, t);
            yield return null;
        }
        isOpen = false;
    }
}
