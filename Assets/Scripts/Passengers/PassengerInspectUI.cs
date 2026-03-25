using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class PassengerInspectUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("ID Slot Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dobText;
    [SerializeField] private TMP_Text idNumberText;
    [SerializeField] private TMP_Text expiryText;

    [Header("ID Images (Full Size, Stacked)")]
    [SerializeField] private Image idBaseImage;
    [SerializeField] private Image idTopImage;

    [Header("Real ID Sprites")]
    [SerializeField] private Sprite trueBaseSprite;
    [SerializeField] private Sprite trueTopSprite;

    [Header("Subtle Fake Top Layers")]
    [SerializeField] private Sprite fakeAlt1TopSprite;
    [SerializeField] private Sprite fakeAlt2TopSprite;
    [SerializeField] private Sprite fakeAlt3TopSprite;

    [Header("Obvious Fake Full Card")]
    [SerializeField] private Sprite obviousFakeFullSprite;

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

        ApplyCardVisuals(passenger);
        ApplyText(passenger);
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
                SetFullFake(obviousFakeFullSprite);
                break;

            case PassengerIdVisual.FakeAlt1:
                SetLayeredCard(trueBaseSprite, fakeAlt1TopSprite != null ? fakeAlt1TopSprite : trueTopSprite);
                break;

            case PassengerIdVisual.FakeAlt2:
                SetLayeredCard(trueBaseSprite, fakeAlt2TopSprite != null ? fakeAlt2TopSprite : trueTopSprite);
                break;

            case PassengerIdVisual.FakeAlt3:
                SetLayeredCard(trueBaseSprite, fakeAlt3TopSprite != null ? fakeAlt3TopSprite : trueTopSprite);
                break;

            default:
                SetLayeredCard(trueBaseSprite, trueTopSprite);
                break;
        }
    }

    private void ApplyText(Passenger passenger)
    {
        bool obviousFake = passenger.IdVisual == PassengerIdVisual.ObviousFake;

        if (obviousFake)
        {
            if (nameText != null) nameText.text = string.Empty;
            if (dobText != null) dobText.text = string.Empty;
            if (idNumberText != null) idNumberText.text = string.Empty;
            if (expiryText != null) expiryText.text = string.Empty;
            return;
        }

        if (nameText != null)
            nameText.text = passenger.PassengerName;

        if (dobText != null)
            dobText.text = passenger.DateOfBirth;

        if (idNumberText != null)
            idNumberText.text = passenger.IdNumber;

        if (expiryText != null)
            expiryText.text = passenger.ExpiryDate;
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

        if (nameText != null) nameText.text = string.Empty;
        if (dobText != null) dobText.text = string.Empty;
        if (idNumberText != null) idNumberText.text = string.Empty;
        if (expiryText != null) expiryText.text = string.Empty;

        if (root != null)
            root.SetActive(false);
    }
}