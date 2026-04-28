using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuNavigation : MonoBehaviour
{
    [Header("Nombres de escena")]
    [SerializeField] private string startMenuScene = "StartMenu";
    [SerializeField] private string gameplayScene = "Gameplay 1";
    [SerializeField] private string creditsScene = "Credits";

    public void StartGame()
    {
        SceneManager.LoadScene(gameplayScene);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(startMenuScene);
    }

    public void OpenCredits()
    {
        SceneManager.LoadScene(creditsScene);
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(gameplayScene);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("QuitGame ejecutado (en editor no cierra, en build sí).");
    }
}
