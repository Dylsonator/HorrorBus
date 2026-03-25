using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class PassengerInspectUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text stopsText;
    [SerializeField] private TMP_Text paidText;

    [Header("ID Images (Full Size, Stacked)")]
    [SerializeField] private Image idBaseImage;
    [SerializeField] private Image idTopImage;

    [Header("Real ID Sprites")]
    [SerializeField] private Sprite trueBaseSprite;   // layer 0
    [SerializeField] private Sprite trueTopSprite;    // layer 4

    [Header("Fake Full Card Sprites")]
    [SerializeField] private Sprite obviousFakeSprite;
    [SerializeField] private Sprite fakeAlt1Sprite;
    [SerializeField] private Sprite fakeAlt2Sprite;
    [SerializeField] private Sprite fakeAlt3Sprite;

    private Passenger current;

    public Passenger Current => current;
    public bool IsVisible => root != null && root.activeSelf;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        Hide();
    }

    public void Show(Passenger passenger)
    {
        if (passenger == null)
            return;

        current = passenger;
        root.SetActive(true);

        if (nameText != null)
            nameText.text = passenger.PassengerName;

        if (stopsText != null)
            stopsText.text = $"Stops Left: {passenger.ClaimedStopsRemaining}";

        if (paidText != null)
            paidText.text = $"Paid: {passenger.PaidAmount} / Fare: {passenger.ExpectedFare}";

        ApplyCardVisuals(passenger);
    }

    private void ApplyCardVisuals(Passenger passenger)
    {
        if (idBaseImage == null || idTopImage == null)
            return;

        switch (passenger.IdVisual)
        {
            case PassengerIdVisual.Real:
                SetLayeredCard(trueBaseSprite, trueTopSprite);
                break;

            case PassengerIdVisual.ObviousFake:
                SetFullFake(obviousFakeSprite);
                break;

            case PassengerIdVisual.FakeAlt1:
                SetFullFake(fakeAlt1Sprite);
                break;

            case PassengerIdVisual.FakeAlt2:
                SetFullFake(fakeAlt2Sprite);
                break;

            case PassengerIdVisual.FakeAlt3:
                SetFullFake(fakeAlt3Sprite);
                break;

            default:
                SetLayeredCard(trueBaseSprite, trueTopSprite);
                break;
        }
    }

    private void SetLayeredCard(Sprite baseSprite, Sprite topSprite)
    {
        if (idBaseImage != null)
        {
            idBaseImage.enabled = baseSprite != null;
            idBaseImage.sprite = baseSprite;
            idBaseImage.preserveAspect = true;
        }

        if (idTopImage != null)
        {
            idTopImage.enabled = topSprite != null;
            idTopImage.sprite = topSprite;
            idTopImage.preserveAspect = true;
        }
    }

    private void SetFullFake(Sprite fakeSprite)
    {
        if (idBaseImage != null)
            idBaseImage.enabled = false;

        if (idTopImage != null)
        {
            idTopImage.enabled = fakeSprite != null;
            idTopImage.sprite = fakeSprite;
            idTopImage.preserveAspect = true;
        }
    }

    public void Hide()
    {
        current = null;

        if (root != null)
            root.SetActive(false);
    }
}