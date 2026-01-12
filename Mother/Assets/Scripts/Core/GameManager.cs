using UnityEngine;
using System;
using System.Collections.Generic;

public enum GameState
{
    Initializing,
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Loading
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Configurazione")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool vSyncEnabled = false;
    
    [Header("Stato corrente")]
    [SerializeField] private GameState currentState = GameState.Initializing;
    private GameState previousState;
    
    // Eventi
    public static event Action<GameState, GameState> OnGameStateChanged;
    public static event Action OnGameStarted;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action OnGameOver;
    
    // Dati di gioco
    private int currentScore = 0;
    private int highScore = 0;
    private float playTime = 0f;
    
    private const string HIGH_SCORE_KEY = "HighScore";
    
    private void Awake()
    {
        // Singleton pattern thread-safe
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Initialize();
    }
    
    private void Initialize()
    {
        // Configura qualità
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
        
        ApplyHardwareSpecificSettings();

        // Carica high score
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        
        // Stato iniziale
        ChangeState(GameState.MainMenu);
    }

    private void ApplyHardwareSpecificSettings()
    {
        // Intel HD Graphics detection
        string gpu = SystemInfo.graphicsDeviceName.ToLower();
        int vendorId = SystemInfo.graphicsDeviceVendorID;
    
        // Intel = vendor ID 0x8086
        if (vendorId == 0x8086 && (gpu.Contains("hd") || gpu.Contains("graphics")))
        {
            Debug.LogWarning($"Intel integrated GPU detected: {SystemInfo.graphicsDeviceName}");
        
            // Override delle impostazioni per compatibilità
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        
            // Solo se siamo nel primo avvio o nel menu principale
            if (currentState == GameState.Initializing || currentState == GameState.MainMenu)
            {
                Screen.SetResolution(1280, 720, false);
            }
        
            // Ottimizzazioni aggiuntive
            QualitySettings.shadowDistance = 20f;
            QualitySettings.pixelLightCount = 2;
        }
    }
    
    private void Update()
    {
        // Aggiorna timer di gioco
        if (currentState == GameState.Playing)
        {
            playTime += Time.deltaTime;
        }
        
        // Gestione input globale (es: Escape per pausa)
        HandleGlobalInput();
    }
    
    private void HandleGlobalInput()
    {
       if (Input.GetKeyDown(KeyCode.Escape))
       {
           switch (currentState)
           {
               case GameState.Playing: PauseGame(); break;
               case GameState.Paused: ResumeGame(); break;
               case GameState.GameOver: ReturnToMainMenu(); break; // Nuova funzionalità
           }
       }
    }  
    
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
        
        previousState = currentState;
        currentState = newState;
        
        Debug.Log($"Cambio stato: {previousState} -> {currentState}");
        
        // Gestisci transizioni di stato
        HandleStateTransition(previousState, newState);
        
        // Notifica listeners
        OnGameStateChanged?.Invoke(previousState, newState);
    }
    
    private void HandleStateTransition(GameState fromState, GameState toState)
   {
       switch (toState)
       {
            case GameState.Playing:
                Time.timeScale = 1f; // IMPORTANTE: Imposta sempre a 1
                if (fromState == GameState.Paused) OnGameResumed?.Invoke();
                else OnGameStarted?.Invoke();
                break;
                
            case GameState.Paused:
                Time.timeScale = 0f;
                OnGamePaused?.Invoke();
                break;
                
            case GameState.GameOver:
                Time.timeScale = 0f;
                SaveHighScore();
                OnGameOver?.Invoke();
                break;
                
            case GameState.MainMenu:
                Time.timeScale = 1f;
                ResetGameData();
                break;
        }
    }
    
    // Metodi pubblici per controllo gioco
    public void StartGame()
    {
        ChangeState(GameState.Playing);
    }
    
    public void PauseGame()
    {
        ChangeState(GameState.Paused);
    }
    
    public void ResumeGame()
    {
        ChangeState(GameState.Playing);
    }
    
    public void GameOver()
    {
        ChangeState(GameState.GameOver);
    }
    
    public void ReturnToMainMenu()
    {
        ChangeState(GameState.MainMenu);
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    // Gestione punteggio
    public void AddScore(int points)
    {
        if (currentState != GameState.Playing) return;
        currentScore += points;
        if (currentScore > highScore)
        {
            highScore = currentScore;
        }
    }
    
    public void ResetScore()
    {
        currentScore = 0;
    }
    
    private void SaveHighScore()
    {
        if (currentScore > PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0))
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, currentScore);
            PlayerPrefs.Save();
        }
    }

    private void SaveHighScore()
   {
       int savedHighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
       // Salva solo se il punteggio corrente è MAGGIORE di quello salvato
       if (currentScore > savedHighScore)
       {
           PlayerPrefs.SetInt(HIGH_SCORE_KEY, currentScore);
           PlayerPrefs.Save();
           highScore = currentScore; // Aggiorna la variabile locale solo dopo il salvataggio
       }
   }
    
    private void ResetGameData()
    {
        currentScore = 0;
        playTime = 0f;
    }
    
    // Properties
    public GameState CurrentState => currentState;
    public GameState PreviousState => previousState;
    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public float PlayTime => playTime;
    
    // Metodo per ripristinare stato precedente
    public void RestorePreviousState()
    {
        ChangeState(previousState);
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            
            // Salva dati quando il gioco viene chiuso
            SaveHighScore();
        }
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Validazione in Editor
        if (targetFrameRate < 30) targetFrameRate = 30;
        if (targetFrameRate > 144) targetFrameRate = 144;
    }
    #endif

}
