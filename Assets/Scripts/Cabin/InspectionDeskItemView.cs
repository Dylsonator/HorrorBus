using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public sealed class InspectionDeskItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image artBaseImage;
    [SerializeField] private Image artOverlayImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private RectTransform artRoot;

    [Header("Optional ID slot texts")]
    [SerializeField] private TMP_Text idNameText;
    [SerializeField] private TMP_Text idDobText;
    [SerializeField] private TMP_Text idNumberText;
    [SerializeField] private TMP_Text idExpiryText;

    private InspectionDeskUI owner;
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Vector2 templateHomeAnchoredPos;
    private bool dragging;

    public InspectionDeskItemState Data { get; private set; }
    public RectTransform Rect => rect != null ? rect : (rect = transform as RectTransform);

    public void Initialise(InspectionDeskUI newOwner, Canvas canvas, InspectionDeskItemState state, Vector2 anchoredPosition)
    {
        owner = newOwner;
        rootCanvas = canvas;
        Data = state != null ? state.Clone() : new InspectionDeskItemState();

        if (rect == null) rect = transform as RectTransform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        ForceRootRectSetup();
        ForceArtRectSetup();

        rect.anchoredPosition = anchoredPosition;
        templateHomeAnchoredPos = anchoredPosition;

        RefreshVisuals();
    }

    public void SetAnchoredPosition(Vector2 anchoredPosition)
    {
        ForceRootRectSetup();
        Rect.anchoredPosition = anchoredPosition;

        if (Data != null && Data.isTemplateSource)
            templateHomeAnchoredPos = anchoredPosition;
    }

    public void RefreshVisuals()
    {
        ForceRootRectSetup();
        ForceArtRectSetup();

        Sprite baseSprite = null;
        Sprite overlaySprite = null;
        Vector2 artSize = Vector2.zero;
        bool hideText = false;
        Color artTint = Color.white;

        bool hasArt = owner != null &&
                      owner.TryResolveArt(Data, out baseSprite, out overlaySprite, out artSize, out hideText, out artTint);

        if (hasArt)
        {
            Vector2 finalSize = artSize.sqrMagnitude > 0.01f
                ? artSize
                : new Vector2(120f, 80f);

            ApplySize(finalSize);

            if (artBaseImage != null)
            {
                artBaseImage.enabled = baseSprite != null;
                artBaseImage.sprite = baseSprite;
                artBaseImage.color = artTint;
                artBaseImage.preserveAspect = true;
                StretchChildToArtRoot(artBaseImage.rectTransform);
            }

            if (artOverlayImage != null)
            {
                artOverlayImage.enabled = overlaySprite != null;
                artOverlayImage.sprite = overlaySprite;
                artOverlayImage.color = artTint;
                artOverlayImage.preserveAspect = true;
                StretchChildToArtRoot(artOverlayImage.rectTransform);
            }

            bool useIdSlots = Data != null &&
                              Data.kind == InspectionDeskItemKind.IdCard &&
                              !Data.preferArtOnly &&
                              HasIdSlotTexts();

            if (useIdSlots)
            {
                ApplyIdSlotTexts();
                SetGenericTextsVisible(false);
            }
            else
            {
                ClearIdSlotTexts();
                SetIdSlotTextsVisible(false);

                if (titleText != null)
                {
                    titleText.text = Data != null ? Data.title : string.Empty;
                    titleText.gameObject.SetActive(!hideText);
                }

                if (subtitleText != null)
                {
                    subtitleText.text = Data != null ? Data.subtitle : string.Empty;
                    subtitleText.gameObject.SetActive(!hideText && !string.IsNullOrWhiteSpace(subtitleText.text));
                }
            }

            if (background != null)
                background.color = new Color(0f, 0f, 0f, 0f);

            return;
        }

        if (artBaseImage != null)
        {
            artBaseImage.sprite = null;
            artBaseImage.enabled = false;
        }

        if (artOverlayImage != null)
        {
            artOverlayImage.sprite = null;
            artOverlayImage.enabled = false;
        }

        Vector2 fallbackSize = (Data != null && Data.preferredSize.sqrMagnitude > 0.01f)
            ? Data.preferredSize
            : new Vector2(120f, 80f);

        ApplySize(fallbackSize);

        ClearIdSlotTexts();
        SetIdSlotTextsVisible(false);

        if (background != null)
        {
            Color tint = (Data != null && owner != null)
                ? owner.GetSuggestedTint(Data)
                : new Color(0.9f, 0.9f, 0.9f, 0.95f);

            background.color = tint;
        }

        if (titleText != null)
        {
            titleText.text = Data != null ? Data.title : string.Empty;
            titleText.gameObject.SetActive(true);
        }

        if (subtitleText != null)
        {
            subtitleText.text = Data != null ? Data.subtitle : string.Empty;
            subtitleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(subtitleText.text));
        }
    }

    private bool HasIdSlotTexts()
    {
        return idNameText != null || idDobText != null || idNumberText != null || idExpiryText != null;
    }

    private void ApplyIdSlotTexts()
    {
        string name = Data != null ? Data.title : string.Empty;
        string dob = string.Empty;
        string idNumber = string.Empty;
        string expiry = string.Empty;

        if (Data != null && !string.IsNullOrWhiteSpace(Data.subtitle))
        {
            string[] lines = Data.subtitle.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("DOB "))
                    dob = line.Substring(4).Trim();
                else if (line.StartsWith("ID "))
                    idNumber = line.Substring(3).Trim();
                else if (line.StartsWith("EXP "))
                    expiry = line.Substring(4).Trim();
            }
        }

        if (idNameText != null) idNameText.text = name;
        if (idDobText != null) idDobText.text = dob;
        if (idNumberText != null) idNumberText.text = idNumber;
        if (idExpiryText != null) idExpiryText.text = expiry;

        SetIdSlotTextsVisible(true);
    }

    private void ClearIdSlotTexts()
    {
        if (idNameText != null) idNameText.text = string.Empty;
        if (idDobText != null) idDobText.text = string.Empty;
        if (idNumberText != null) idNumberText.text = string.Empty;
        if (idExpiryText != null) idExpiryText.text = string.Empty;
    }

    private void SetIdSlotTextsVisible(bool visible)
    {
        if (idNameText != null) idNameText.gameObject.SetActive(visible);
        if (idDobText != null) idDobText.gameObject.SetActive(visible);
        if (idNumberText != null) idNumberText.gameObject.SetActive(visible);
        if (idExpiryText != null) idExpiryText.gameObject.SetActive(visible);
    }

    private void SetGenericTextsVisible(bool visible)
    {
        if (titleText != null) titleText.gameObject.SetActive(visible);
        if (subtitleText != null) subtitleText.gameObject.SetActive(visible);
    }

    private void ApplySize(Vector2 targetSize)
    {
        if (targetSize.x <= 0f || targetSize.y <= 0f)
            return;

        ForceRootRectSetup();
        ForceArtRectSetup();

        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);
        rect.sizeDelta = targetSize;

        if (artRoot != null)
        {
            artRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
            artRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);
            artRoot.sizeDelta = targetSize;
            artRoot.anchoredPosition = Vector2.zero;
        }
    }

    private void ForceRootRectSetup()
    {
        if (rect == null) rect = transform as RectTransform;
        if (rect == null) return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void ForceArtRectSetup()
    {
        if (artRoot == null)
            return;

        artRoot.anchorMin = new Vector2(0.5f, 0.5f);
        artRoot.anchorMax = new Vector2(0.5f, 0.5f);
        artRoot.pivot = new Vector2(0.5f, 0.5f);
        artRoot.anchoredPosition = Vector2.zero;
        artRoot.localScale = Vector3.one;
        artRoot.localRotation = Quaternion.identity;
    }

    private static void StretchChildToArtRoot(RectTransform child)
    {
        if (child == null)
            return;

        child.anchorMin = new Vector2(0f, 0f);
        child.anchorMax = new Vector2(1f, 1f);
        child.pivot = new Vector2(0.5f, 0.5f);
        child.anchoredPosition = Vector2.zero;
        child.offsetMin = Vector2.zero;
        child.offsetMax = Vector2.zero;
        child.localScale = Vector3.one;
        child.localRotation = Quaternion.identity;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (owner == null || Data == null)
            return;

        dragging = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.9f;
            canvasGroup.blocksRaycasts = false;
        }

        owner.NotifyBeginDrag(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (owner == null || rootCanvas == null)
            return;

        RectTransform canvasRect = owner.ItemLayer;
        if (canvasRect == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            Rect.anchoredPosition = localPoint;
            owner.ClampToCanvas(this);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (owner == null)
            return;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        owner.NotifyEndDrag(this, eventData.position);

        if (Data != null && Data.isTemplateSource)
            Rect.anchoredPosition = templateHomeAnchoredPos;

        dragging = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner == null || dragging)
            return;

        owner.HandleItemClick(this, eventData.position, null);
    }

    public void NotifyRegionClicked(InspectionDeskClickTopic topic, Vector2 screenPoint)
    {
        if (owner == null)
            return;

        owner.HandleItemClick(this, screenPoint, topic);
    }
}