using UnityEngine;

public class LeverSwitch : MonoBehaviour
{
    [Header("Settings")]
    public int screenIndex;
    public int answerIndex;

    [Header("Animation")]
    public float pullAngle = 45f;
    public float pullSpeed = 5f;

    private bool isPulled = false;
    private Quaternion originalRotation;
    private Quaternion pulledRotation;
    private bool isAnimating = false;

    void Start()
    {
        originalRotation = transform.localRotation;
        pulledRotation = originalRotation * Quaternion.Euler(pullAngle, 0f, 0f);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 5f))
            {
                if (hit.transform == transform && !isPulled)
                {
                    Pull();
                }
            }
        }

        if (isAnimating)
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                pulledRotation,
                Time.deltaTime * pullSpeed
            );

            if (Quaternion.Angle(transform.localRotation, pulledRotation) < 0.5f)
            {
                transform.localRotation = pulledRotation;
                isAnimating = false;
            }
        }
    }

    void Pull()
    {
        isPulled = true;
        isAnimating = true;
PuzzleManager.Instance.OnLeverPulled(screenIndex, answerIndex);
    }
}