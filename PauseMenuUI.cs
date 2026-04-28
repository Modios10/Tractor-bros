using UnityEngine;

public class PauseMenuUI : MonoBehaviour
{
    public void OnResumePressed()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame();
    }

    public void OnMenuPressed()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.GoToMenu();
    }

    public void OnRetryPressed()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.RetryLevel();
    }
}
