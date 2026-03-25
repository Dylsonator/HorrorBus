using UnityEngine;

public enum DecisionButtonType { Accept, Reject }

public sealed class DecisionButton : MonoBehaviour
{
    [SerializeField] private PassengerInspection inspection;
    [SerializeField] private DecisionButtonType type;

    private void Awake()
    {
        if (inspection == null)
            inspection = FindFirstObjectByType<PassengerInspection>();
    }

    public void Press()
    {
        if (inspection == null) return;

        if (type == DecisionButtonType.Accept) inspection.AcceptCurrent();
        else inspection.RejectCurrent();
    }
}
