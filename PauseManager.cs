using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Escenas")]
    [SerializeField] private string pauseSceneName = "PauseScene";
    [SerializeField] private string startMenuSceneName = "StartMenu";

    private bool isPaused = false;
    private bool isTransitioning = false;
    private string gameplaySceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameplaySceneName = SceneManager.GetActiveScene().name;
    }

    private void Update()
    {
        if (SerialController.PauseToggleRequested > 0)
        {
            SerialController.PauseToggleRequested--;
            TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void PauseGame()
    {
        if (isPaused || isTransitioning) return;
        StartCoroutine(PauseRoutine());
    }

    public void ResumeGame()
    {
        if (!isPaused || isTransitioning) return;
        StartCoroutine(ResumeRoutine());
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    private IEnumerator PauseRoutine()
    {
        isTransitioning = true;

        Time.timeScale = 0f;
        AudioListener.pause = true;

        AsyncOperation op = SceneManager.LoadSceneAsync(pauseSceneName, LoadSceneMode.Additive);
        yield return op;

        isPaused = true;
        isTransitioning = false;
    }

    private IEnumerator ResumeRoutine()
    {
        isTransitioning = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        AsyncOperation op = SceneManager.UnloadSceneAsync(pauseSceneName);
        yield return op;

        isPaused = false;
        isTransitioning = false;
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(startMenuSceneName, LoadSceneMode.Single);
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
