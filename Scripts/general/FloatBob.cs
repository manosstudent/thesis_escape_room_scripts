using UnityEngine;

/// <summary>
/// Makes an object gently float up and down in place, e.g. a "Sit here!" sign above a crystal.
/// Put it on the object itself; it moves relative to wherever the object starts.
/// </summary>
public class FloatBob : MonoBehaviour
{
    [Tooltip("How far up and down the object moves, in metres.")]
    public float amplitude = 0.05f;
    [Tooltip("How fast it bobs. Higher = faster.")]
    public float speed = 1.5f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = startPos + Vector3.up * offset;
    }
}
