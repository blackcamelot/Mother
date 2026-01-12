using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class GameInitializer : MonoBehaviour
{
    [Header("Configurazione")]
    [SerializeField] private string firstSceneName = "MainMenu";
    [SerializeField] private float minLoadTime = 1.5f; // Tempo minimo per evitare flash
    
    [Header("UI")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private UnityEngine.UI.Slider progressBar;
    [SerializeField] private UnityEngine.UI.Text progressText;
    
    public static event Action OnGameInitialized;
    
    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        StartCoroutine(InitializeGameRoutine());
    }

    private IEnumerator InitializeGameRoutine()
    {
        float startTime = Time.time;
        
        // Mostra loading screen se disponibile
        if (loadingScreen != null)
            loadingScreen.SetActive(true);
        
        // Fase 1: Inizializza sistemi base
        yield return InitializeCoreSystems();
        UpdateProgress(0.3f, "Inizializzazione sistemi...");
        
        // Fase 2: Carica dati salvati
        yield return LoadSavedData();
        UpdateProgress(0.6f, "Caricamento dati...");
        
        // Fase 3: Verifica integrità
        yield return VerifyGameIntegrity();
        UpdateProgress(0.9f, "Verifica integrità...");
        
        // Attendi tempo minimo
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minLoadTime)
        {
            yield return new WaitForSeconds(minLoadTime - elapsedTime);
        }
        
        UpdateProgress(1f, "Completato!");
        yield return new WaitForSeconds(0.5f);
        
        // Notifica completamento
        OnGameInitialized?.Invoke();
        
        // Carica scena iniziale
        LoadFirstScene();
    }

    private IEnumerator InitializeCoreSystems()
    {
        try
        {
            
             // INIZIALIZZAZIONE GRAFICA - AGGIUNGI QUI
            InitializeGraphicsSettings();            
            
            
            // Inizializza AudioManager se presente
            AudioManager audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null)
            {
                GameObject audioObj = new GameObject("AudioManager");
                audioObj.AddComponent<AudioManager>();
                DontDestroyOnLoad(audioObj);
            }
            
            // Inizializza altri sistemi...
            
            yield return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore inizializzazione sistemi: {e.Message}");
            throw;
        }
    }

    private void InitializeGraphicsSettings()
    {
        // Rileva GPU Intel HD
        string gpuName = SystemInfo.graphicsDeviceName.ToLower();
        bool isIntelHD = gpuName.Contains("intel") && 
                        (gpuName.Contains("hd") || gpuName.Contains("graphics"));
    
        if (isIntelHD)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.SetResolution(1280, 720, false);
        
            Debug.Log($"Intel HD Graphics detected: {SystemInfo.graphicsDeviceName}");
            Debug.Log("Applied compatibility settings: 720p, 60 FPS, VSync off");
        }
    }

    private IEnumerator LoadSavedData()
    {
        try
        {
            // Carica dati salvati
            SaveSystem saveSystem = FindObjectOfType<SaveSystem>();
            if (saveSystem != null)
            {
                yield return saveSystem.LoadGameAsync();
            }
            
            yield return null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Errore caricamento dati: {e.Message}");
            // Continua comunque con valori di default
        }
    }

    private IEnumerator VerifyGameIntegrity()
    {
        // Verifica che le scene esistano
        if (!SceneExists(firstSceneName))
        {
            Debug.LogError($"Scena iniziale non trovata: {firstSceneName}");
            // Potresti voler gestire questo errore diversamente
        }
        
        // Altre verifiche...
        
        yield return null;
    }

    private bool SceneExists(string sceneName)
    {
       // Controlla in tutte le scene in build (funziona anche in build)
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(scenePath) == sceneName)
                return true;
        }
        return false;
    }

    private void UpdateProgress(float progress, string message)
    {
        if (progressBar != null)
            progressBar.value = progress;
        
        if (progressText != null)
            progressText.text = $"{message} {(progress * 100):F0}%";
    }

    private void LoadFirstScene()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    
        try 
        {
            SceneManager.LoadScene(firstSceneName);
        } 
        catch (System.Exception e) 
        {
            Debug.LogError($"Impossibile caricare la scena '{firstSceneName}': {e.Message}");
           // Qui potresti caricare una scena di fallback o mostrare un messaggio all'utente
        }
    }

    // Metodo pubblico per riavviare l'inizializzazione
    public void RestartInitialization()
    {
        StopAllCoroutines();
        StartCoroutine(InitializeGameRoutine());
    }

}
