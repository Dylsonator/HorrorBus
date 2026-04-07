using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuUI : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string playSceneName = "Map";
    [SerializeField] private string quitFallbackMessage = "Quit only works in a build.";

    public void Play()
    {
        if (string.IsNullOrWhiteSpace(playSceneName))
        {
            Debug.LogWarning("[MainMenuUI] Play scene name is empty.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(playSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        Debug.Log($"[MainMenuUI] {quitFallbackMessage}");
#else
        Application.Quit();
#endif
    }
}