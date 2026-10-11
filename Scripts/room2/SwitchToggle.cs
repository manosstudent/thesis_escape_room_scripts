using UnityEngine;

public class SwitchToggle : MonoBehaviour
{
    public bool isOn = false;
    public BinaryDisplay binaryDisplay;

    private void Start()
    {
        if (binaryDisplay == null)
            binaryDisplay = FindObjectOfType<BinaryDisplay>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = AutoSitView.CurrentCamera ?? Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // RaycastAll so the chair between camera and switches doesn't block the click
            RaycastHit[] hits = Physics.RaycastAll(ray);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                SwitchToggle clicked = hit.collider.GetComponentInParent<SwitchToggle>();
                if (clicked != null)
                {
                    if (clicked == this)
                    {
                        isOn = !isOn;
                        binaryDisplay.UpdateDisplay();
                    }
                    break; // Only the closest switch counts
                }
            }
        }
    }
}