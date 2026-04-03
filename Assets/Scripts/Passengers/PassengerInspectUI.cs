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

    [Header("Subtle Fake Top Layers (1..20)")]
    [SerializeField] private Sprite[] fakeTopSprites;

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

            default:
                {
                    int fakeIndex = GetFakeVariantIndex(passenger.IdVisual);
                    if (fakeIndex >= 0 && fakeTopSprites != null && fakeIndex < fakeTopSprites.Length && fakeTopSprites[fakeIndex] != null)
                        SetLayeredCard(trueBaseSprite, fakeTopSprites[fakeIndex]);
                    else
                        SetLayeredCard(trueBaseSprite, trueTopSprite);
                    break;
                }
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

    private int GetFakeVariantIndex(PassengerIdVisual visual)
    {
        return visual switch
        {
            PassengerIdVisual.FakeAlt1 => 0,
            PassengerIdVisual.FakeAlt2 => 1,
            PassengerIdVisual.FakeAlt3 => 2,
            PassengerIdVisual.FakeAlt4 => 3,
            PassengerIdVisual.FakeAlt5 => 4,
            PassengerIdVisual.FakeAlt6 => 5,
            PassengerIdVisual.FakeAlt7 => 6,
            PassengerIdVisual.FakeAlt8 => 7,
            PassengerIdVisual.FakeAlt9 => 8,
            PassengerIdVisual.FakeAlt10 => 9,
            PassengerIdVisual.FakeAlt11 => 10,
            PassengerIdVisual.FakeAlt12 => 11,
            PassengerIdVisual.FakeAlt13 => 12,
            PassengerIdVisual.FakeAlt14 => 13,
            PassengerIdVisual.FakeAlt15 => 14,
            PassengerIdVisual.FakeAlt16 => 15,
            PassengerIdVisual.FakeAlt17 => 16,
            PassengerIdVisual.FakeAlt18 => 17,
            PassengerIdVisual.FakeAlt19 => 18,
            PassengerIdVisual.FakeAlt20 => 19,
            _ => -1
        };
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