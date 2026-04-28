using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Gameplay")]
    [SerializeField] private TMP_Text grainCounterText;
    [SerializeField] private Image[] heartImages;

    [Header("Escenas")]
    [SerializeField] private string winSceneName = "WinScene";

    [Header("Salud del Jugador")]
    [SerializeField] private int maxLives = 3;

    public static float LastRunSeconds { get; private set; }

    private int grainCount = 0;
    private int totalGrain = 0;
    private int currentLives;
    private float startTime;
    private bool gameEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        startTime = Time.time;
        totalGrain = GameObject.FindGameObjectsWithTag("Grain").Length;
        currentLives = Mathf.Max(maxLives, 1);

        UpdateGrainUI();
        UpdateLivesUI();
        SendRemainingToFpga();
    }

    public void AddGrain(int amount = 1)
    {
        if (gameEnded) return;

        grainCount += amount;
        UpdateGrainUI();
        SendRemainingToFpga();

        if (totalGrain > 0 && grainCount >= totalGrain)
        {
            WinGame();
        }
    }

    public void LoseLife()
    {
        if (gameEnded) return;

        currentLives--;
        UpdateLivesUI();

        if (currentLives <= 0)
        {
            GameOver();
        }
    }

    private void UpdateGrainUI()
    {
        if (grainCounterText != null)
        {
            grainCounterText.text = "Grain: " + grainCount + " / " + totalGrain;
        }
    }

    private void UpdateLivesUI()
    {
        if (heartImages == null)
        {
            return;
        }

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null)
            {
                continue;
            }

            heartImages[i].enabled = i < currentLives;
        }
    }

    private void SendRemainingToFpga()
    {
        if (SerialController.Instance == null)
        {
            return;
        }

        int remaining = Mathf.Max(totalGrain - grainCount, 0);
        SerialController.Instance.SendRemainingGrains(remaining);
    }

    private void WinGame()
    {
        gameEnded = true;
        LastRunSeconds = Time.time - startTime;
        SceneManager.LoadScene(winSceneName);
    }

    private void GameOver()
    {
        gameEnded = true;
        Debug.Log("Out of lives! Restarting level...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
