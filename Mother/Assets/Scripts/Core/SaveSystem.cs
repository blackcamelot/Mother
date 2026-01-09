using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    public int highScore;
    public float masterVolume;
    public bool[] unlockedLevels;
    public int totalPlayTime;
    public DateTime lastSaveTime;
    
    // Aggiungi qui altri dati da salvare
    public Dictionary<string, bool> achievements;
    public Dictionary<string, int> inventory;
    
    public GameData()
    {
        highScore = 0;
        masterVolume = 0.7f;
        unlockedLevels = new bool[10]; // Esempio: 10 livelli
        unlockedLevels[0] = true; // Primo livello sbloccato
        totalPlayTime = 0;
        lastSaveTime = DateTime.Now;
        achievements = new Dictionary<string, bool>();
        inventory = new Dictionary<string, int>();
    }
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }
    
    [Header("Configurazione")]
    [SerializeField] private string saveFileName = "save.dat";
    [SerializeField] private string backupFileName = "save_backup.dat";
    [SerializeField] private int maxBackups = 3;
    [SerializeField] private bool useEncryption = true;
    
    [Header("Auto Save")]
    [SerializeField] private bool enableAutoSave = true;
    [SerializeField] private float autoSaveInterval = 300f; // 5 minuti
    
    private GameData currentGameData;
    private string saveFilePath;
    private string backupFolderPath;
    private float timeSinceLastSave = 0f;
    
    // Encryption
    private byte[] encryptionKey;
    private readonly byte[] salt = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee };
    
    public event Action OnGameSaved;
    public event Action OnGameLoaded;
    public event Action<string> OnSaveError;
    
    private void Awake()
    {
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
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        backupFolderPath = Path.Combine(Application.persistentDataPath, "Backups");
        
        // Crea directory backup se non esiste
        if (!Directory.Exists(backupFolderPath))
        {
            Directory.CreateDirectory(backupFolderPath);
        }
        
        // Genera o carica chiave di encryption
        InitializeEncryption();
        
        Debug.Log($"SaveSystem inizializzato. Path: {saveFilePath}");
    }
    
    private void InitializeEncryption()
    {
        string keyPrefsKey = "EncryptionKey";
        
        if (PlayerPrefs.HasKey(keyPrefsKey))
        {
            string savedKey = PlayerPrefs.GetString(keyPrefsKey);
            encryptionKey = Convert.FromBase64String(savedKey);
        }
        else
        {
            // Genera nuova chiave
            using (var deriveBytes = new Rfc2898DeriveBytes("game_save_salt", salt, 10000))
            {
                encryptionKey = deriveBytes.GetBytes(32);
                PlayerPrefs.SetString(keyPrefsKey, Convert.ToBase64String(encryptionKey));
                PlayerPrefs.Save();
            }
        }
    }
    
    private void Update()
    {
        if (enableAutoSave && currentGameData != null)
        {
            timeSinceLastSave += Time.deltaTime;
            if (timeSinceLastSave >= autoSaveInterval)
            {
                timeSinceLastSave = 0f;
                SaveGameAsync();
            }
        }
    }
    
    public async Task<bool> SaveGameAsync()
    {
        try
        {
            if (currentGameData == null)
            {
                currentGameData = new GameData();
            }
            
            // Aggiorna timestamp
            currentGameData.lastSaveTime = DateTime.Now;
            
            // Crea backup prima di sovrascrivere
            if (File.Exists(saveFilePath))
            {
                CreateBackup();
            }
            
            // Serializza dati
            string jsonData = JsonUtility.ToJson(currentGameData, true);
            
            // Encrypt se necessario
            byte[] dataToSave = useEncryption 
                ? Encrypt(Encoding.UTF8.GetBytes(jsonData)) 
                : Encoding.UTF8.GetBytes(jsonData);
            
            // Salva su file
            await File.WriteAllBytesAsync(saveFilePath, dataToSave);
            
            Debug.Log($"Gioco salvato: {saveFilePath}");
            OnGameSaved?.Invoke();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore salvataggio: {e.Message}");
            OnSaveError?.Invoke(e.Message);
            
            // Ripristina backup se disponibile
            await RestoreFromBackup();
            
            return false;
        }
    }
    
    public async Task<bool> LoadGameAsync()
    {
        try
        {
            if (!File.Exists(saveFilePath))
            {
                Debug.Log("Nessun file di salvataggio trovato, crea dati nuovi");
                currentGameData = new GameData();
                OnGameLoaded?.Invoke();
                return false;
            }
            
            // Leggi file
            byte[] savedData = await File.ReadAllBytesAsync(saveFilePath);
            
            // Decrypt se necessario
            byte[] decryptedData = useEncryption 
                ? Decrypt(savedData) 
                : savedData;
            
            // Deserializza
            string jsonData = Encoding.UTF8.GetString(decryptedData);
            currentGameData = JsonUtility.FromJson<GameData>(jsonData);
            
            // Valida dati
            if (!ValidateGameData(currentGameData))
            {
                Debug.LogWarning("Dati di gioco non validi, ripristino valori default");
                currentGameData = new GameData();
            }
            
            Debug.Log("Gioco caricato con successo");
            OnGameLoaded?.Invoke();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore caricamento: {e.Message}");
            
            // Prova a caricare da backup
            if (await LoadFromLatestBackup())
            {
                return true;
            }
            
            // Fallback a dati nuovi
            currentGameData = new GameData();
            OnGameLoaded?.Invoke();
            
            return false;
        }
    }
    
    private void CreateBackup()
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(backupFolderPath, $"backup_{timestamp}.dat");
            
            File.Copy(saveFilePath, backupPath, true);
            
            // Mantieni solo gli ultimi N backup
            CleanupOldBackups();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Errore creazione backup: {e.Message}");
        }
    }
    
    private void CleanupOldBackups()
    {
        try
        {
            var backupFiles = Directory.GetFiles(backupFolderPath, "backup_*.dat");
            if (backupFiles.Length > maxBackups)
            {
                Array.Sort(backupFiles);
                for (int i = 0; i < backupFiles.Length - maxBackups; i++)
                {
                    File.Delete(backupFiles[i]);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Errore pulizia backup: {e.Message}");
        }
    }
    
    private async Task<bool> RestoreFromBackup()
    {
        try
        {
            var backupFiles = Directory.GetFiles(backupFolderPath, "backup_*.dat");
            if (backupFiles.Length == 0)
            {
                return false;
            }
            
            Array.Sort(backupFiles);
            string latestBackup = backupFiles[backupFiles.Length - 1];
            
            await File.WriteAllBytesAsync(saveFilePath, await File.ReadAllBytesAsync(latestBackup));
            Debug.Log($"Ripristinato da backup: {latestBackup}");
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore ripristino backup: {e.Message}");
            return false;
        }
    }
    
    private async Task<bool> LoadFromLatestBackup()
    {
        try
        {
            var backupFiles = Directory.GetFiles(backupFolderPath, "backup_*.dat");
            if (backupFiles.Length == 0)
            {
                return false;
            }
            
            Array.Sort(backupFiles);
            string latestBackup = backupFiles[backupFiles.Length - 1];
            
            byte[] savedData = await File.ReadAllBytesAsync(latestBackup);
            byte[] decryptedData = useEncryption ? Decrypt(savedData) : savedData;
            
            string jsonData = Encoding.UTF8.GetString(decryptedData);
            currentGameData = JsonUtility.FromJson<GameData>(jsonData);
            
            Debug.Log($"Caricato da backup: {latestBackup}");
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private bool ValidateGameData(GameData data)
    {
        if (data == null) return false;
        
        // Validazioni base
        if (data.highScore < 0) return false;
        if (data.masterVolume < 0f || data.masterVolume > 1f) return false;
        if (data.unlockedLevels == null || data.unlockedLevels.Length == 0) return false;
        
        // Assicura che almeno il primo livello sia sbloccato
        if (data.unlockedLevels.Length > 0 && !data.unlockedLevels[0])
        {
            data.unlockedLevels[0] = true;
        }
        
        return true;
    }
    
    // Encryption methods
    private byte[] Encrypt(byte[] data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = encryptionKey;
            aes.IV = new byte[16]; // IV fisso per semplicità (in produzione usa IV random)
            
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }
    
    private byte[] Decrypt(byte[] data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = encryptionKey;
            aes.IV = new byte[16];
            
            using (MemoryStream ms = new MemoryStream(data))
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (MemoryStream output = new MemoryStream())
            {
                cs.CopyTo(output);
                return output.ToArray();
            }
        }
    }
    
    // Metodi pubblici per accedere ai dati
    public GameData GetGameData()
    {
        if (currentGameData == null)
        {
            currentGameData = new GameData();
        }
        return currentGameData;
    }
    
    public void UpdateGameData(GameData newData)
    {
        if (newData != null)
        {
            currentGameData = newData;
        }
    }
    
    public void DeleteSaveFile()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("File di salvataggio eliminato");
            }
            
            // Pulisci anche i backup
            var backupFiles = Directory.GetFiles(backupFolderPath, "backup_*.dat");
            foreach (string backupFile in backupFiles)
            {
                File.Delete(backupFile);
            }
            
            currentGameData = new GameData();
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore eliminazione salvataggio: {e.Message}");
        }
    }
    
    // Metodi per test
    public string GetSavePath() => saveFilePath;
    public bool HasSaveFile() => File.Exists(saveFilePath);
    
    private void OnApplicationQuit()
    {
        // Auto-save all'uscita
        if (enableAutoSave && currentGameData != null)
        {
            _ = SaveGameAsync(); // Fire and forget
        }
    }
}