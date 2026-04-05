using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InspectionDeskQuestionPopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private RectTransform popupParent;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;

    [Header("Positioning")]
    [SerializeField] private Vector2 clickOffset = new Vector2(18f, -18f);
    [SerializeField] private float edgePadding = 12f;

    private readonly List<Button> runtimeButtons = new List<Button>();
    private Action<InspectionDeskQuestionOption> currentCallback;

    private void Awake()
    {
        if (root == null) root = gameObject;
        if (popupRect == null) popupRect = transform as RectTransform;
        if (popupParent == null && popupRect != null) popupParent = popupRect.parent as RectTransform;

        ForcePopupRectSetup();
        Hide();
    }

    public void ShowAtScreenPoint(
        Canvas canvas,
        Camera eventCamera,
        Vector2 screenPoint,
        string title,
        IReadOnlyList<InspectionDeskQuestionOption> options,
        Action<InspectionDeskQuestionOption> callback)
    {
        if (root == null || popupRect == null || popupParent == null || buttonPrefab == null || buttonContainer == null)
            return;

        ClearButtons();

        if (titleText != null)
            titleText.text = title;

        currentCallback = callback;

        if (options != null)
        {
            for (int i = 0; i < options.Count; i++)
            {
                InspectionDeskQuestionOption option = options[i];
                if (option == null)
                    continue;

                Button button = Instantiate(buttonPrefab, buttonContainer);
                runtimeButtons.Add(button);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = option.label;

                InspectionDeskQuestionOption captured = option;
                button.onClick.AddListener(() =>
                {
                    Action<InspectionDeskQuestionOption> cb = currentCallback;
                    Hide();
                    cb?.Invoke(captured);
                });
            }
        }

        ForcePopupRectSetup();

        if (root != null)
            root.SetActive(true);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonContainer as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
        Canvas.ForceUpdateCanvases();

        Camera camToUse = eventCamera;
        if (camToUse == null && canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camToUse = canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(popupParent, screenPoint, camToUse, out Vector2 localPoint))
        {
            Vector2 target = localPoint + clickOffset;
            popupRect.anchoredPosition = ClampAnchoredPositionToParent(target);
        }
    }

    public void Hide()
    {
        ClearButtons();

        if (root != null)
            root.SetActive(false);
    }

    private void ClearButtons()
    {
        for (int i = 0; i < runtimeButtons.Count; i++)
        {
            if (runtimeButtons[i] != null)
                Destroy(runtimeButtons[i].gameObject);
        }

        runtimeButtons.Clear();
        currentCallback = null;
    }

    private void ForcePopupRectSetup()
    {
        if (popupRect == null)
            return;

        // Keep popup positioning independent from weird inspector anchor setups.
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0f, 1f);
        popupRect.localScale = Vector3.one;
        popupRect.localRotation = Quaternion.identity;
    }

    private Vector2 ClampAnchoredPositionToParent(Vector2 target)
    {
        if (popupRect == null || popupParent == null)
            return target;

        Rect parentRect = popupParent.rect;
        Vector2 size = popupRect.rect.size;
        Vector2 pivot = popupRect.pivot;

        float minX = parentRect.xMin + edgePadding;
        float maxX = parentRect.xMax - size.x - edgePadding;
        float minY = parentRect.yMin + size.y + edgePadding;
        float maxY = parentRect.yMax - edgePadding;

        target.x = Mathf.Clamp(target.x, minX, maxX);
        target.y = Mathf.Clamp(target.y, minY, maxY);

        return target;
    }
}