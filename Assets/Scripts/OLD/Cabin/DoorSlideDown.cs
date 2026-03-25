using UnityEngine;

public sealed class DoorSlideDown : MonoBehaviour
{
    [SerializeField] private float dropDistance = 5f;
    [SerializeField] private float duration = 0.6f;

    private Vector3 openLocalPos;
    private Vector3 closedLocalPos;

    private float t;
    private bool moving;
    private bool closing; // true = closing, false = opening

    private void Awake()
    {
        openLocalPos = transform.localPosition;
        closedLocalPos = openLocalPos + Vector3.down * dropDistance;
    }

    public void Close()
    {
        closing = true;
        moving = true;
        t = 0f;
    }

    public void Open()
    {
        closing = false;
        moving = true;
        t = 0f;
    }

    private void Update()
    {
        if (!moving) return;

        t += Time.deltaTime / duration;
        float a = Mathf.Clamp01(t);

        if (closing)
            transform.localPosition = Vector3.Lerp(openLocalPos, closedLocalPos, a);
        else
            transform.localPosition = Vector3.Lerp(closedLocalPos, openLocalPos, a);

        if (t >= 1f)
            moving = false;
    }
}
