using UnityEngine;
using TMPro;

public sealed class PassengerInspectUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text stopsText;

    private void Awake()
    {
        if (root == null) root = gameObject;
        Hide();
    }

    public void Show(Passenger passenger)
    {
        if (passenger == null) return;

        root.SetActive(true);

        if (nameText != null)
            nameText.text = passenger.PassengerName;

        if (stopsText != null)
            stopsText.text = $"Stops: {passenger.ClaimedStopsRemaining}";
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public bool IsVisible => root != null && root.activeSelf;
}
