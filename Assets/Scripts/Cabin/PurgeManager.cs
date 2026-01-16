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
        if (cooldown > 0f) return;

        StartCoroutine(PurgeRoutine());
        cooldown = cooldownSeconds;
    }

    private IEnumerator PurgeRoutine()
    {
        // 1) Close door
        if (purgeDoor != null)
            purgeDoor.Close();

        // 2) Purge seated passengers immediately
        ExecutePurge();

        // 3) Wait, then reopen
        yield return new WaitForSeconds(doorReopenDelay);

        if (purgeDoor != null)
            purgeDoor.Open();
    }

    private void ExecutePurge()
    {
        var passengers = FindObjectsOfType<Passenger>(true);

        foreach (var p in passengers)
        {
            if (p == null) continue;

            // ONLY seated passengers
            if (p.GetComponentInParent<SeatAnchor>() == null)
                continue;
            if (score != null) score.Add(-purgePenaltyPoints);

            Destroy(p.gameObject);
        }

        Debug.Log("PURGE EXECUTED (seated only)");
    }
}
