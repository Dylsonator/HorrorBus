using UnityEngine;

public enum FareChangeButtonAction
{
    AddDenomination,
    RemoveLast,
    ClearAll,
    AutoExact
}

public sealed class FareChangeButton : MonoBehaviour
{
    [SerializeField] private PassengerInspection inspection;
    [SerializeField] private FareChangeButtonAction action;
    [SerializeField] private int denominationPence = 20;

    private void Awake()
    {
        if (inspection == null)
            inspection = FindFirstObjectByType<PassengerInspection>();
    }

    public void Press()
    {
        if (inspection == null)
            return;

        switch (action)
        {
            case FareChangeButtonAction.AddDenomination:
                inspection.AddChangeDenomination(denominationPence);
                break;

            case FareChangeButtonAction.RemoveLast:
                inspection.RemoveLastChangeDenomination();
                break;

            case FareChangeButtonAction.ClearAll:
                inspection.ClearSelectedChange();
                break;

            case FareChangeButtonAction.AutoExact:
                inspection.AutoSelectCorrectChange();
                break;
        }
    }
}
