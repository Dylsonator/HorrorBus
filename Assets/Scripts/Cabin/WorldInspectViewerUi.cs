using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WorldInspectViewerUI : MonoBehaviour
{
    public static WorldInspectViewerUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Image")]
    [SerializeField] private Image displayImage;
    [SerializeField] private bool preserveAspect = true;

    [Header("Optional Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Optional Hint Popup")]
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private float hintDuration = 3f;

    private float hintTimer;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        Instance = this;

        if (root == null)
            root = gameObject;

        HideImmediate();
        HideHintImmediate();
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Hide();

        if (hintRoot != null && hintRoot.activeSelf)
        {
            hintTimer -= Time.unscaledDeltaTime;
            if (hintTimer <= 0f)
                HideHintImmediate();
        }
    }

    public void Show(Sprite sprite, string title = "", string body = "")
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

        if (bodyText != null)
        {
            bodyText.text = body;
            bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(body));
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

    public void ShowHint(string message)
    {
        if (hintRoot == null || hintText == null || string.IsNullOrWhiteSpace(message))
            return;

        hintRoot.SetActive(true);
        hintText.text = message;
        hintTimer = hintDuration;
    }

    public void HideImmediate()
    {
        if (root != null)
            root.SetActive(false);

        isOpen = false;
    }

    private void HideHintImmediate()
    {
        if (hintRoot != null)
            hintRoot.SetActive(false);
    }
}