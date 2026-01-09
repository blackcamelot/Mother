using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    [Header("Initialization Settings")]
    public bool initializeOnStart = true;
    public string firstSceneName = "MainMenu";
    
    [Header("Prefabs to Instantiate")]
    public GameObject gameManagerPrefab;
    public GameObject audioManagerPrefab;
    public GameObject sceneCleanupPrefab;
    
    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeGame();
        }
    }
    
    public void InitializeGame()
    {
        Debug.Log("Initializing game systems...");
        
        // Ensure GameManager exists
        if (GameManager.Instance == null && gameManagerPrefab != null)
        {
            Instantiate(gameManagerPrefab);
            Debug.Log("GameManager instantiated");
        }
        
        // Ensure AudioManager exists
        if (AudioManager.Instance == null && audioManagerPrefab != null)
        {
            Instantiate(audioManagerPrefab);
            Debug.Log("AudioManager instantiated");
        }
        
        // Ensure SceneCleanup exists
        if (sceneCleanupPrefab != null)
        {
            SceneCleanup existingCleanup = FindObjectOfType<SceneCleanup>();
            if (existingCleanup == null)
            {
                Instantiate(sceneCleanupPrefab);
                Debug.Log("SceneCleanup instantiated");
            }
        }
        
        // Load first scene if not already loaded
        if (!string.IsNullOrEmpty(firstSceneName) && 
            SceneManager.GetActiveScene().name != firstSceneName)
        {
            SceneManager.LoadScene(firstSceneName);
        }
        
        Debug.Log("Game initialization complete");
    }
    
    public void ResetGame()
    {
        Debug.Log("Resetting game...");
        
        // Destroy existing managers
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            Destroy(gameManager.gameObject);
        }
        
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            Destroy(audioManager.gameObject);
        }
        
        // Re-initialize
        InitializeGame();
    }
    
    public void QuitToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
        
        // Clean up gameplay objects
        HackingManager hackingManager = FindObjectOfType<HackingManager>();
        if (hackingManager != null)
        {
            Destroy(hackingManager.gameObject);
        }
        
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            Destroy(uiManager.gameObject);
        }
        
        Debug.Log("Returned to main menu");
    }
}