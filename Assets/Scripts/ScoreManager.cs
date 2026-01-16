using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    [SerializeField] private int score = 0;
    public int Score => score;

    public void Add(int amount)
    {
        score += amount;
        Debug.Log($"[SCORE] {(amount >= 0 ? "+" : "")}{amount} -> {score}");
    }

    public void Set(int value)
    {
        score = value;
        _textMeshPro.text = ($"[SCORE] -> {score}");
    }
}
