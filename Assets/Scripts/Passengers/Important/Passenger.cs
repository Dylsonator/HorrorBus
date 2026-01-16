using UnityEngine;

public class Passenger : MonoBehaviour
{
    public enum StopInfoAccuracy
    {
        Correct,
        Incorrect,
        Unknown,
        AccidentalMistake,
        IntentionalLie
    }

    [Header("Identity")]
    [SerializeField] private string passengerName;

    [Header("Anomaly Flag (hidden from player)")]
    [SerializeField] private bool isAnomaly;

    [Header("Stops")]
    [SerializeField] private int dropOffStopIndex = -1;

    // Spawner-provided stop info (your spawner uses ints, not string)
    [SerializeField] private int stopsInfoA;
    [SerializeField] private int stopsInfoB;
    [SerializeField] private StopInfoAccuracy stopsInfoAccuracy = StopInfoAccuracy.Unknown;

    [Header("Observation (for gaze/anomaly system)")]
    public Transform Head;
    public bool IsObserved { get; internal set; }

    public bool IsAnomaly => isAnomaly;
    public int DropOffStopIndex => dropOffStopIndex;
    public string PassengerName => passengerName;

    public int StopsInfoA => stopsInfoA;
    public int StopsInfoB => stopsInfoB;
    public StopInfoAccuracy StopsAccuracy => stopsInfoAccuracy;
    public int ClaimedStopsRemaining => stopsInfoA;
    private void OnEnable()
    {
        PassengerRegistry.Register(this);
    }

    private void OnDisable()
    {
        PassengerRegistry.Unregister(this);
    }

    private void Awake()
    {
        if (Head == null) Head = transform;
    }

    public void SetPassengerName(string newName) => passengerName = newName;
    public void SetDropOffStopIndex(int index) => dropOffStopIndex = index;

    // MATCHES PassengerSpawner.cs call shape: (int, int, StopInfoAccuracy)
    public void SetStopsInfo(int valueA, int valueB, StopInfoAccuracy accuracy)
    {
        stopsInfoA = valueA;
        stopsInfoB = valueB;
        stopsInfoAccuracy = accuracy;
    }

    public void SetIsAnomaly(bool value) => isAnomaly = value;
    private bool lastObserved;

    private void Update()
    {
        if (IsObserved != lastObserved)
        {
            Debug.Log($"{PassengerName} observed = {IsObserved}");
            lastObserved = IsObserved;
        }
    }

    public int ExpectedFare { get; private set; }
    public int PaidAmount { get; private set; }

    public void SetFare(int expected, int paid)
    {
        ExpectedFare = Mathf.Max(0, expected);
        PaidAmount = Mathf.Max(0, paid);
    }


}
