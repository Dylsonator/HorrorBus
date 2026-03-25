using TMPro;
using UnityEngine;

public sealed class ScoreUI : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TMP_Text text;
    [SerializeField] private string prefix = "Points: ";

    private int lastScore = int.MinValue;

    private void Awake()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    private void Update()
    {
        if (scoreManager == null || text == null) return;

        int s = scoreManager.Score;
        if (s == lastScore) return;

        lastScore = s;
        text.text = $"{prefix}{s}";
    }
}
