using UnityEngine;

/// <summary>
/// Put on a trigger collider that covers the entrance of Room 1.
/// The first time the player walks in, the countdown starts.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Room1EntryZone : MonoBehaviour
{
    private bool started = false;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (started || !other.CompareTag("Player")) return;
        started = true;

        if (GameManager.Instance != null)
            GameManager.Instance.StartTimer();
    }
}
