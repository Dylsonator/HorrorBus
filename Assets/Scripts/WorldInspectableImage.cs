using UnityEngine;

public sealed class WorldInspectableImage : MonoBehaviour
{
    [SerializeField] private Sprite expandedSprite;
    [SerializeField] private string inspectTitle = "Inspect";

    [Header("Hint")]
    [SerializeField] private bool showHintOnce = true;
    [SerializeField][TextArea] private string hintMessage = "You can click maps, notes, and passengers to inspect them. Press ESC to close.";

    private bool shownHint;

    private void Start()
    {
        if (showHintOnce && !shownHint && WorldInspectViewerUI.Instance != null)
        {
            WorldInspectViewerUI.Instance.ShowHint(hintMessage);
            shownHint = true;
        }
    }

    public void Inspect()
    {
        if (WorldInspectViewerUI.Instance == null)
            return;

        WorldInspectViewerUI.Instance.Show(expandedSprite, inspectTitle);
    }
}