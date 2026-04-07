using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class PauseMenuUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Optional")]
    [SerializeField] private string mainMenuSceneName = "Menu";
    [SerializeField] private bool pauseTimeScale = true;
    [SerializeField] private bool useEscapeToggle = true;
    [SerializeField] private bool debugLogs = true;

    [Header("Optional gameplay refs")]
    [SerializeField] private CabinPeek cabinPeek;
    [SerializeField] private InspectionDeskUI deskUI;
    [SerializeField] private SeatedRequestionUI seatedRequestionUI;

    public bool IsPaused => root != null && root.activeSelf;

    private void Awake()
    {
        if (cabinPeek == null)
            cabinPeek = FindFirstObjectByType<CabinPeek>();

        if (deskUI == null)
            deskUI = FindFirstObjectByType<InspectionDeskUI>(FindObjectsInactive.Include);

        if (seatedRequestionUI == null)
            seatedRequestionUI = FindFirstObjectByType<SeatedRequestionUI>(FindObjectsInactive.Include);

        if (root != null)
            root.SetActive(false);

        if (debugLogs)
            Debug.Log("[PauseMenuUI] Awake");
    }

    private void Update()
    {
        if (!useEscapeToggle)
            return;

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (debugLogs)
            Debug.Log("[PauseMenuUI] ESC pressed");

        if (!IsPaused)
        {
            if (WorldInspectViewerUI.Instance != null && WorldInspectViewerUI.Instance.IsOpen)
            {
                if (debugLogs) Debug.Log("[PauseMenuUI] Blocked by WorldInspectViewerUI");
                return;
            }

            if (deskUI != null && deskUI.IsOpen)
            {
                if (debugLogs) Debug.Log("[PauseMenuUI] Blocked by InspectionDeskUI");
                return;
            }

            if (seatedRequestionUI != null && seatedRequestionUI.IsOpen)
            {
                if (debugLogs) Debug.Log("[PauseMenuUI] Blocked by SeatedRequestionUI");
                return;
            }

            Pause();
        }
        else
        {
            Resume();
        }
    }

    public void Pause()
    {
        if (debugLogs)
            Debug.Log("[PauseMenuUI] Pause()");

        if (root != null)
            root.SetActive(true);
        else if (debugLogs)
            Debug.LogWarning("[PauseMenuUI] root is NULL");

        if (pauseTimeScale)
            Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cabinPeek != null)
            cabinPeek.SetLookEnabled(false);
    }

    public void Resume()
    {
        if (debugLogs)
            Debug.Log("[PauseMenuUI] Resume()");

        if (root != null)
            root.SetActive(false);

        if (pauseTimeScale)
            Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cabinPeek != null)
            cabinPeek.SetLookEnabled(true);
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.name);
    }

    public void GoToMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning("[PauseMenuUI] Main menu scene name is empty.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        Debug.Log("[PauseMenuUI] Quit only works in a build.");
#else
        Application.Quit();
#endif
    }
}