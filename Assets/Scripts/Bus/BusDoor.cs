using UnityEngine;

public sealed class BusDoor : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private Vector3 openRotation = new Vector3(0f, -90f, 0f);
    [SerializeField] private float rotateSpeed = 5f;

    [Header("References")]
    [SerializeField] private RouteStops routeStops;

    private Quaternion closedRot;
    private Quaternion openRot;
    private bool isOpen;

    private void Awake()
    {
        closedRot = transform.localRotation;
        openRot = Quaternion.Euler(openRotation) * closedRot;
    }

    private void OnEnable()
    {
        if (routeStops == null)
            routeStops = FindFirstObjectByType<RouteStops>();

        if (routeStops != null)
        {
            routeStops.ArrivedAtStop += OpenDoor;
            routeStops.LeavingStop += CloseDoor;
        }
    }

    private void OnDisable()
    {
        if (routeStops != null)
        {
            routeStops.ArrivedAtStop -= OpenDoor;
            routeStops.LeavingStop -= CloseDoor;
        }
    }

    private void Update()
    {
        Quaternion target = isOpen ? openRot : closedRot;
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            target,
            Time.deltaTime * rotateSpeed
        );
    }

    private void OpenDoor()
    {
        isOpen = true;
    }

    private void CloseDoor()
    {
        isOpen = false;
    }
}
