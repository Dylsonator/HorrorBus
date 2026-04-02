
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

    private readonly List<Button> runtimeButtons = new List<Button>();
    private Action<InspectionDeskQuestionOption> currentCallback;

    private void Awake()
    {
        if (root == null) root = gameObject;
        if (popupRect == null) popupRect = transform as RectTransform;
        if (popupParent == null && popupRect != null) popupParent = popupRect.parent as RectTransform;
        Hide();
    }

    public void ShowAtScreenPoint(Canvas canvas, Camera eventCamera, Vector2 screenPoint, string title, IReadOnlyList<InspectionDeskQuestionOption> options, Action<InspectionDeskQuestionOption> callback)
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

                button.onClick.AddListener(() =>
                {
                    Action<InspectionDeskQuestionOption> cb = currentCallback;
                    Hide();
                    cb?.Invoke(option);
                });
            }
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(popupParent, screenPoint, eventCamera, out Vector2 localPoint))
            popupRect.anchoredPosition = localPoint;

        root.SetActive(true);
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
}
