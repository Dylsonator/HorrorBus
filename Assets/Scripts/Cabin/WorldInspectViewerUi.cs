using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WorldInspectViewerUI : MonoBehaviour
{
    public static WorldInspectViewerUI Instance { get; private set; }

    [SerializeField] private GameObject root;
    [SerializeField] private Image displayImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private GameObject hintRoot;

    [SerializeField] private bool preserveAspect = true;
    [SerializeField] private float hintDuration = 4f;

    private float hintTimer;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        Instance = this;

        if (root == null)
            root = gameObject;

        if (root != null)
            root.SetActive(false);

        if (hintRoot != null)
            hintRoot.SetActive(false);
    }

    private void Update()
    {
        if (hintRoot != null && hintRoot.activeSelf)
        {
            hintTimer -= Time.unscaledDeltaTime;
            if (hintTimer <= 0f)
                hintRoot.SetActive(false);
        }
    }

    public void Show(Sprite sprite, string title = "")
    {
        if (root == null || displayImage == null)
            return;

        root.SetActive(true);
        isOpen = true;

        displayImage.sprite = sprite;
        displayImage.preserveAspect = preserveAspect;
        displayImage.enabled = sprite != null;

        if (titleText != null)
        {
            titleText.text = title;
            titleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(title));
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        isOpen = false;
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowHint(string text)
    {
        if (hintRoot == null || hintText == null || string.IsNullOrWhiteSpace(text))
            return;

        hintRoot.SetActive(true);
        hintText.text = text;
        hintTimer = hintDuration;
    }
}