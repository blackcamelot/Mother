using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneCleanup : MonoBehaviour
{
    private static SceneCleanup instance;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CleanupResources();
    }
    
    private void CleanupResources()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        
        Debug.Log($"Scene cleanup completed for: {SceneManager.GetActiveScene().name}");
    }
    
    public static void ForceCleanup()
    {
        if (instance != null)
        {
            instance.CleanupResources();
        }
    }
    
    public static void CleanupBeforeSceneLoad()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}