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
    [SerializeField] private string loseSceneName = "LoseScene";

    [Header("Salud del Jugador")]
    [SerializeField] private int maxLives = 3;

    public static float LastRunSeconds { get; private set; }

    private int grainCount = 0;
    private int totalGrain = 0;
    private int currentLives;
    private float sessionStartTime;
    private bool gameEnded = false;
    private bool levelActive = false;
    private bool sessionStarted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log("GameManager.Start() called. levelActive = " + levelActive);
        if (LevelManager.Instance == null && !levelActive)
        {
            Debug.Log("Calling BeginLevel from Start()");
            BeginLevel(GameObject.FindGameObjectsWithTag("Grain").Length, maxLives, "Level");
        }
    }

    public void BeginLevel(int grainsInLevel, int lives, string levelName)
    {
        if (!sessionStarted)
        {
            sessionStartTime = Time.time;
            sessionStarted = true;
        }

        totalGrain = Mathf.Max(grainsInLevel, 0);
        grainCount = 0;
        currentLives = Mathf.Max(lives, 1);
        gameEnded = false;
        levelActive = true;

        UpdateGrainUI();
        UpdateLivesUI();
        SendRemainingToFpga();

        Debug.Log("Starting " + levelName + " | grains=" + totalGrain + " lives=" + currentLives);
    }

    public void AddGrain(int amount = 1)
    {
        if (gameEnded) return;

        grainCount += amount;
        UpdateGrainUI();
        SendRemainingToFpga();

        if (totalGrain > 0 && grainCount >= totalGrain)
        {
            LevelManager.Instance?.OnAllGrainsCollected();
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
        levelActive = false;
        LastRunSeconds = Time.time - sessionStartTime;
        SceneManager.LoadScene(winSceneName);
    }

    public void CompleteGame()
    {
        if (gameEnded)
        {
            return;
        }

        WinGame();
    }

    private void GameOver()
    {
        gameEnded = true;
        levelActive = false;
        Debug.Log("Out of lives! Loading lose scene...");
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.GoToLoseScene();
        }
        else
        {
            SceneManager.LoadScene(loseSceneName);
        }
    }
}
