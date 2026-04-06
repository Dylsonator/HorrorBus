using UnityEngine;

public sealed class WorldInspectableImage : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private Sprite expandedSprite;
    [SerializeField] private string inspectTitle = "Inspect";
    [SerializeField][TextArea] private string inspectBody;

    [Header("Hint")]
    [SerializeField] private bool showHintOnStart = true;
    [SerializeField][TextArea] private string hintMessage = "Click signs, maps, and passengers to inspect them. Press ESC to close.";

    private bool hintShown;

    private void Start()
    {
        if (showHintOnStart && !hintShown && WorldInspectViewerUI.Instance != null)
        {
            WorldInspectViewerUI.Instance.ShowHint(hintMessage);
            hintShown = true;
        }
    }

    public void Inspect()
    {
        if (WorldInspectViewerUI.Instance == null)
            return;

        WorldInspectViewerUI.Instance.Show(expandedSprite, inspectTitle, inspectBody);
    }
}