using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Serializable]
    public class LevelConfig
    {
        public string levelName = "Level 1";
        public int grainCount = 20;
        public int lives = 3;
        public int bombCount = 3;
        public float bombSpeed = 1.5f;
    }

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay 1";
    [SerializeField] private string winSceneName = "WinScene";
    [SerializeField] private string loseSceneName = "LoseScene";

    [Header("Levels")]
    [SerializeField] private LevelConfig[] levels = new LevelConfig[3]
    {
        new LevelConfig { levelName = "Level 1", grainCount = 20, lives = 3, bombCount = 2, bombSpeed = 1.5f },
        new LevelConfig { levelName = "Level 2", grainCount = 20, lives = 3, bombCount = 4, bombSpeed = 2.2f },
        new LevelConfig { levelName = "Level 3", grainCount = 20, lives = 3, bombCount = 6, bombSpeed = 3.0f }
    };

    [Header("Runtime")]
    [SerializeField] private int currentLevelIndex = 0;
    [SerializeField] private LevelSpawner levelSpawner;

    public int CurrentLevelIndex => currentLevelIndex;
    public LevelConfig CurrentConfig => GetCurrentConfig();

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
        ApplyCurrentLevelIfGameplayScene();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Solo aplicar nivel cuando se carga en modo Single (no aditivo como pausa)
        if (mode == LoadSceneMode.Single)
        {
            ApplyCurrentLevelIfGameplayScene();
        }
    }

    public void StartGame()
    {
        currentLevelIndex = 0;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnAllGrainsCollected()
    {
        currentLevelIndex++;

        if (currentLevelIndex >= levels.Length)
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.CompleteGame();
            }
            else
            {
                SceneManager.LoadScene(winSceneName);
            }
            return;
        }

        ApplyCurrentLevelIfGameplayScene();
    }

    public void ResetToFirstLevel()
    {
        currentLevelIndex = 0;
        ApplyCurrentLevelIfGameplayScene();
    }

    public void GoToLoseScene()
    {
        SceneManager.LoadScene(loseSceneName);
    }

    private void ApplyCurrentLevelIfGameplayScene()
    {
        if (!IsGameplayScene())
        {
            return;
        }

        if (levelSpawner == null)
        {
            levelSpawner = FindObjectOfType<LevelSpawner>();
        }

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            return;
        }

        LevelConfig config = GetCurrentConfig();
        if (levelSpawner != null)
        {
            levelSpawner.SetupLevel(config);
        }

        gameManager.BeginLevel(config.grainCount, config.lives, config.levelName);
        Debug.Log("Loaded " + config.levelName + ": grains=" + config.grainCount + ", lives=" + config.lives + ", bombs=" + config.bombCount + ", bombSpeed=" + config.bombSpeed);
    }

    private bool IsGameplayScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName == gameplaySceneName;
    }

    private LevelConfig GetCurrentConfig()
    {
        if (levels == null || levels.Length == 0)
        {
            return new LevelConfig();
        }

        int index = Mathf.Clamp(currentLevelIndex, 0, levels.Length - 1);
        return levels[index];
    }
}
