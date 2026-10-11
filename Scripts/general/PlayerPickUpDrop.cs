using UnityEngine;

public class PlayerPickUpDrop : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform holdPoint;

    [Header("Pickup Settings")]
    public float pickupRange = 5f;
    public float dropMaxDistance = 10f;

    private PickUpObject heldObject;
    private Collider[] playerColliders;
    private PickUpObject hoveredObject;
    private SimpleFPSController fpsController;

    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    private Color originalColor;
    private Renderer hoveredRenderer;

    private bool pickupMode = false;

    private void Awake()
    {
        playerColliders = GetComponentsInChildren<Collider>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        fpsController = GetComponent<SimpleFPSController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            pickupMode = !pickupMode;
            SetPickupMode(pickupMode);
        }

        if (pickupMode)
        {
            if (heldObject == null)
                HandleHover();

            if (Input.GetMouseButtonDown(0))
            {
                if (heldObject == null)
                    TryPickUp();
                else
                    DropAtClickPoint();
            }
        }
    }

    private void SetPickupMode(bool enabled)
    {
        pickupMode = enabled;
        fpsController.canRotate = !enabled;
        Cursor.visible = enabled;
        Cursor.lockState = enabled ? CursorLockMode.None : CursorLockMode.Locked;

        if (!enabled)
        {
            RemoveHighlight();
            hoveredObject = null;
            if (heldObject != null)
            {
                heldObject.Drop(playerColliders);
                heldObject = null;
            }
        }
    }

    private Camera ActiveCamera => AutoSitView.CurrentCamera != null ? AutoSitView.CurrentCamera : playerCamera;

    private void HandleHover()
    {
        Ray ray = ActiveCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            PickUpObject pickable = hit.collider.GetComponentInParent<PickUpObject>();

            if (pickable != null && !pickable.IsHeld())
            {
                if (pickable != hoveredObject)
                {
                    RemoveHighlight();
                    hoveredObject = pickable;
                    ApplyHighlight(hoveredObject);
                }
            }
            else
            {
                RemoveHighlight();
                hoveredObject = null;
            }
        }
        else
        {
            RemoveHighlight();
            hoveredObject = null;
        }
    }

    private void TryPickUp()
    {
        if (hoveredObject == null) return;

        heldObject = hoveredObject;
        RemoveHighlight();
        hoveredObject = null;
        heldObject.PickUp(holdPoint, playerColliders);
    }

    private void DropAtClickPoint()
    {
        if (heldObject == null) return;

        Ray ray = ActiveCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, dropMaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            heldObject.transform.position = hit.point + Vector3.up * 0.5f;
        }

        heldObject.Drop(playerColliders);
        heldObject = null;
    }

    private void ApplyHighlight(PickUpObject obj)
    {
        hoveredRenderer = obj.GetComponentInChildren<Renderer>();
        if (hoveredRenderer != null)
        {
            originalColor = hoveredRenderer.material.color;
            hoveredRenderer.material.color = highlightColor;
        }
    }

    private void RemoveHighlight()
    {
        if (hoveredRenderer != null)
        {
            hoveredRenderer.material.color = originalColor;
            hoveredRenderer = null;
        }
    }
}