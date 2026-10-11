using UnityEngine;
using System.Collections.Generic;

public class StackZone : MonoBehaviour
{
    public enum ZoneType { Push, Stack, Pop }
    public ZoneType zoneType;

    private List<GameObject> plates = new List<GameObject>();

    public List<GameObject> GetPlates() { return plates; }

    public GameObject GetTopPlate()
    {
        if (plates.Count == 0) return null;

        GameObject top = plates[0];
        foreach (GameObject p in plates)
            if (p.transform.position.y > top.transform.position.y)
                top = p;
        return top;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PickUpObject>() != null)
        {
            GameObject plate = other.GetComponentInParent<PickUpObject>().gameObject;
            if (!plates.Contains(plate))
            {
                plates.Add(plate);
                StackManager.Instance?.OnZoneChanged(this);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PickUpObject>() != null)
        {
            GameObject plate = other.GetComponentInParent<PickUpObject>().gameObject;
            plates.Remove(plate);
            StackManager.Instance?.OnZoneChanged(this);
        }
    }
}
