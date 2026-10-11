using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float openAngleY = 90f;   // πόσες μοίρες να στραφεί
    public float duration = 1.5f;    // διάρκεια animation

    private bool isOpen = false;
    private bool isAnimating = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = Quaternion.Euler(
            transform.localEulerAngles.x,
            transform.localEulerAngles.y + openAngleY,
            transform.localEulerAngles.z
        );
    }

    [ContextMenu("Test Open Door")]
    public void OpenDoor()
    {
        if (!isOpen && !isAnimating)
        {
            DisableOldAutoOpeners();
            StartCoroutine(AnimateDoor(closedRotation, openRotation));
        }
    }

    // Το παλιό AutoOpenDoor ξανακλείνει την πόρτα μετά από autoCloseDelay.
    // Το σβήνουμε από τον κώδικα, ώστε να μη μετράει τι δείχνει το checkbox στο Inspector.
    private void DisableOldAutoOpeners()
    {
        foreach (var auto in FindObjectsOfType<AutoOpenDoor>())
        {
            if (auto.door == null) continue;
            if (auto.door == transform || auto.door.IsChildOf(transform) || transform.IsChildOf(auto.door))
            {
                // Το enabled=false δεν αρκεί: το OnTriggerEnter καλείται και σε απενεργοποιημένο script.
                Destroy(auto);
            }
        }
    }

    private IEnumerator AnimateDoor(Quaternion from, Quaternion to)
    {
        isAnimating = true;
        isOpen = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.localRotation = Quaternion.Lerp(from, to, t);
            yield return null;
        }

        transform.localRotation = to;
        isAnimating = false;
    }
}