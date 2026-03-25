using UnityEngine;
using System.Collections;

public sealed class PurgeManager : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private DoorSlideDown purgeDoor;

    [Header("Rules")]
    [SerializeField] private float cooldownSeconds = 10f;
    [SerializeField] private float doorReopenDelay = 5f;

    [SerializeField] private ScoreManager score;
    [SerializeField] private int purgePenaltyPoints = 50;

    private float cooldown;

    private void Update()
    {
        if (cooldown > 0f)
            cooldown -= Time.deltaTime;
    }

    public void TryPurge()
    {
        if (cooldown > 0f)
            return;

        StartCoroutine(PurgeRoutine());
        cooldown = cooldownSeconds;
    }

    private IEnumerator PurgeRoutine()
    {
        if (purgeDoor != null)
            purgeDoor.Close();

        ExecutePurge();

        yield return new WaitForSeconds(doorReopenDelay);

        if (purgeDoor != null)
            purgeDoor.Open();
    }

    private void ExecutePurge()
    {
        Passenger[] passengers = FindObjectsByType<Passenger>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Passenger p in passengers)
        {
            if (p == null)
                continue;

            if (p.GetComponentInParent<SeatAnchor>() == null)
                continue;

            if (SeatManager.Instance != null)
                SeatManager.Instance.NotifyPassengerRemoved(p);

            if (score != null)
                score.Add(-purgePenaltyPoints);

            Destroy(p.gameObject);
        }

        Debug.Log("PURGE EXECUTED (seated only)");
    }
}