using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

// Strutture per serializzazione Dictionary
[System.Serializable]
public class SerializableDictionary<TKey, TValue>
{
    public List<TKey> keys = new List<TKey>();
    public List<TValue> values = new List<TValue>();
    
    public Dictionary<TKey, TValue> ToDictionary()
    {
        var dict = new Dictionary<TKey, TValue>();
        for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
        {
            dict[keys[i]] = values[i];
        }
        return dict;
    }
    
    public void FromDictionary(Dictionary<TKey, TValue> dict)
    {
        keys.Clear();
        values.Clear();
        foreach (var kvp in dict)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }
}

[System.Serializable]
public class GameData
{
    public int highScore;
    public float masterVolume;
    public bool[] unlockedLevels;
    public int totalPlayTime;
    public DateTime lastSaveTime;
    
    // Dizionari serializzabili
    public SerializableDictionary<string, bool> serializableAchievements;
    public SerializableDictionary<string, int> serializableInventory;
    
    // Cache per accesso rapido (non serializzato)
    [System.NonSerialized] private Dictionary<string, bool> achievementsCache;
    [System.NonSerialized] private Dictionary<string, int> inventoryCache;
    
    public GameData()
    {
        highScore = 0;
        masterVolume = 0.7f;
        unlockedLevels = new bool[10];
        unlockedLevels[0] = true;
        totalPlayTime = 0;
        lastSaveTime = DateTime.Now;
        
        serializableAchievements = new SerializableDictionary<string, bool>();
        serializableInventory = new SerializableDictionary<string, int>();
        
        InitializeCaches();
    }
    
    private void InitializeCaches()
    {
        achievementsCache = serializableAchievements?.ToDictionary() ?? new Dictionary<string, bool>();
        inventoryCache = serializableInventory?.ToDictionary() ?? new Dictionary<string, int>();
    }
    
    // Metodi per accedere ai dizionari
    public Dictionary<string, bool> GetAchievements()
    {
        if (achievementsCache == null) InitializeCaches();
        return achievementsCache;
    }
    
    public Dictionary<string, int> GetInventory()
    {
        if (inventoryCache == null) InitializeCaches();
        return inventoryCache;
    }
    
    public void SetAchievement(string id, bool unlocked)
    {
        if (achievementsCache == null) InitializeCaches();
        achievementsCache[id] = unlocked;
        serializableAchievements.FromDictionary(achievementsCache);
    }
    
    public void SetInventoryItem(string itemId, int quantity)
    {
        if (inventoryCache == null) InitializeCaches();
        inventoryCache[itemId] = quantity;
        serializableInventory.FromDictionary(inventoryCache);
    }
    
    public void OnAfterDeserialize()
    {
        InitializeCaches();
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
    [SerializeField] private float autoSaveInterval = 300f;
    [SerializeField] private bool saveOnApplicationPause = true;
    
    private GameData currentGameData;
    private string saveFilePath;
    private string backupFolderPath;
    private float timeSinceLastSave = 0f;
    private bool isSaving = false;
    
    // Encryption - chiave derivata in modo unico
    private byte[] encryptionKey;
    private byte[] encryptionIV;
    private readonly byte[] salt = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee };
    
    public event Action OnGameSaved;
    public event Action OnGameLoaded;
    public event Action<string> OnSaveError;
    public event Action OnSaveStarted;
    
    private void Awake()
    {
        // Singleton robusto
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
            try
            {
                Directory.CreateDirectory(backupFolderPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Impossibile creare directory backup: {e.Message}");
            }
        }
        
        // Inizializza encryption
        InitializeEncryption();
        
        Debug.Log($"SaveSystem inizializzato. Path: {saveFilePath}");
        
        // Carica dati all'avvio
        _ = LoadGameAsync();
    }
    
    private void InitializeEncryption()
    {
        try
        {
            // Deriva chiave in modo unico per questa installazione
            // Usa una combinazione di identificatori unici
            string uniqueSeed = $"{Application.productName}_{SystemInfo.deviceUniqueIdentifier}_{Application.companyName}";
            
            using (var deriveBytes = new Rfc2898DeriveBytes(uniqueSeed, salt, 10000, HashAlgorithmName.SHA256))
            {
                // Deriva sia la chiave che l'IV dallo stesso seed ma con parametri diversi
                encryptionKey = deriveBytes.GetBytes(32); // 256 bit
                
                // Per l'IV, usa un'altra derivazione con salt modificato
                byte[] modifiedSalt = new byte[salt.Length];
                Array.Copy(salt, modifiedSalt, salt.Length);
                modifiedSalt[0] ^= 0xFF; // Modifica il salt per l'IV
                
                using (var ivDeriveBytes = new Rfc2898DeriveBytes(uniqueSeed, modifiedSalt, 10000, HashAlgorithmName.SHA256))
                {
                    encryptionIV = ivDeriveBytes.GetBytes(16); // 128 bit
                }
            }
            
            // NON salvare la chiave da nessuna parte
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore inizializzazione encryption: {e.Message}");
            // Fallback a encryption disabilitata
            useEncryption = false;
        }
    }
    
    private void Update()
    {
        if (enableAutoSave && currentGameData != null && !isSaving)
        {
            timeSinceLastSave += Time.deltaTime;
            if (timeSinceLastSave >= autoSaveInterval)
            {
                timeSinceLastSave = 0f;
                _ = SaveGameAsync();
            }
        }
    }
    
    public async Task<bool> SaveGameAsync()
    {
        if (isSaving)
        {
            Debug.LogWarning("Salvataggio già in corso, ignoro richiesta");
            return false;
        }
        
        isSaving = true;
        OnSaveStarted?.Invoke();
        
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
                if (!await CreateBackupAsync())
                {
                    Debug.LogWarning("Backup non creato, procedo comunque con salvataggio");
                }
            }
            
            // Serializza dati
            string jsonData = JsonUtility.ToJson(currentGameData, true);
            
            // Encrypt se necessario
            byte[] dataToSave;
            if (useEncryption && encryptionKey != null && encryptionIV != null)
            {
                dataToSave = await EncryptAsync(Encoding.UTF8.GetBytes(jsonData));
            }
            else
            {
                dataToSave = Encoding.UTF8.GetBytes(jsonData);
            }
            
            // Salva su file
            await File.WriteAllBytesAsync(saveFilePath, dataToSave);
            
            // Verifica che il file sia stato scritto correttamente
            if (File.Exists(saveFilePath))
            {
                var fileInfo = new FileInfo(saveFilePath);
                if (fileInfo.Length > 0)
                {
                    Debug.Log($"Gioco salvato con successo: {saveFilePath} ({fileInfo.Length} bytes)");
                    OnGameSaved?.Invoke();
                    isSaving = false;
                    return true;
                }
            }
            
            throw new IOException("File salvato ma vuoto o non accessibile");
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore salvataggio: {e.Message}");
            OnSaveError?.Invoke(e.Message);
            
            // Ripristina backup se disponibile
            bool restored = await RestoreFromLatestBackupAsync();
            if (!restored)
            {
                Debug.LogWarning("Impossibile ripristinare da backup");
            }
            
            isSaving = false;
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
            
            if (savedData.Length == 0)
            {
                throw new IOException("File di salvataggio vuoto");
            }
            
            // Decrypt se necessario
            byte[] decryptedData;
            if (useEncryption && encryptionKey != null && encryptionIV != null)
            {
                decryptedData = await DecryptAsync(savedData);
            }
            else
            {
                decryptedData = savedData;
            }
            
            // Deserializza
            string jsonData = Encoding.UTF8.GetString(decryptedData);
            currentGameData = JsonUtility.FromJson<GameData>(jsonData);
            
            // Assicurati che i cache siano inizializzati
            if (currentGameData != null)
            {
                // Chiama il metodo di post-deserializzazione
                currentGameData.OnAfterDeserialize();
                
                // Valida dati
                if (!ValidateGameData(currentGameData))
                {
                    Debug.LogWarning("Dati di gioco non validi, ripristino valori default");
                    currentGameData = new GameData();
                }
            }
            else
            {
                throw new InvalidDataException("Dati deserializzati nulli");
            }
            
            Debug.Log("Gioco caricato con successo");
            OnGameLoaded?.Invoke();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore caricamento: {e.Message}");
            
            // Prova a caricare da backup
            if (await LoadFromLatestBackupAsync())
            {
                return true;
            }
            
            // Fallback a dati nuovi
            Debug.LogWarning("Creazione nuovo salvataggio con valori default");
            currentGameData = new GameData();
            OnGameLoaded?.Invoke();
            
            return false;
        }
    }
    
    private async Task<bool> CreateBackupAsync()
    {
        try
        {
            if (!File.Exists(saveFilePath)) return false;
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(backupFolderPath, $"backup_{timestamp}.dat");
            
            // Copia asincrona
            byte[] fileData = await File.ReadAllBytesAsync(saveFilePath);
            await File.WriteAllBytesAsync(backupPath, fileData);
            
            // Mantieni solo gli ultimi N backup
            await CleanupOldBackupsAsync();
            
            Debug.Log($"Backup creato: {backupPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Errore creazione backup: {e.Message}");
            return false;
        }
    }
    
    private async Task CleanupOldBackupsAsync()
    {
        try
        {
            if (!Directory.Exists(backupFolderPath)) return;
            
            var backupFiles = Directory.GetFiles(backupFolderPath, "backup_*.dat");
            if (backupFiles.Length > maxBackups)
            {
                // Ordina per data di creazione
                var sortedFiles = backupFiles
                    .Select(f => new FileInfo(f))
                    .OrderBy(f => f.CreationTime)
                    .ToArray();
                
                // Elimina i più vecchi
                for (int i = 0; i < sortedFiles.Length - maxBackups; i++)
                {
                    await Task.Run(() => sortedFiles[i].Delete());
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Errore pulizia backup: {e.Message}");
        }
    }
    
    private async Task<bool> RestoreFromLatestBackupAsync()
    {
        try
        {
            if (!Directory.Exists(backupFolderPath)) return false;
            
            var backupFiles = Directory.GetFiles(backupFolderPath, "backup_*.dat");
            if (backupFiles.Length == 0) return false;
            
            // Trova il backup più recente
            var latestFile = backupFiles
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .FirstOrDefault();
            
            if (latestFile == null) return false;
            
            // Ripristina
            byte[] backupData = await File.ReadAllBytesAsync(latestFile.FullName);
            await File.WriteAllBytesAsync(saveFilePath, backupData);
            
            Debug.Log($"Ripristinato da backup: {latestFile.Name}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore ripristino backup: {e.Message}");
            return false;
        }
    }
    
    private async Task<bool> LoadFromLatestBackupAsync()
    {
        try
        {
            if (!Directory.Exists(backupFolderPath)) return false;
            
            var backupFiles = Directory.GetFiles(backupFolderPath, "backup_*.dat");
            if (backupFiles.Length == 0) return false;
            
            // Trova il backup più recente
            var latestFile = backupFiles
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .FirstOrDefault();
            
            if (latestFile == null) return false;
            
            byte[] savedData = await File.ReadAllBytesAsync(latestFile.FullName);
            
            // Decrypt
            byte[] decryptedData;
            if (useEncryption && encryptionKey != null && encryptionIV != null)
            {
                decryptedData = await DecryptAsync(savedData);
            }
            else
            {
                decryptedData = savedData;
            }
            
            // Deserializza
            string jsonData = Encoding.UTF8.GetString(decryptedData);
            currentGameData = JsonUtility.FromJson<GameData>(jsonData);
            
            if (currentGameData != null)
            {
                currentGameData.OnAfterDeserialize();
                if (!ValidateGameData(currentGameData))
                {
                    currentGameData = new GameData();
                }
            }
            
            Debug.Log($"Caricato da backup: {latestFile.Name}");
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
        
        try
        {
            // Validazioni base
            if (data.highScore < 0) 
            {
                Debug.LogWarning("HighScore negativo, reset a 0");
                data.highScore = 0;
            }
            
            if (data.masterVolume < 0f || data.masterVolume > 1f)
            {
                Debug.LogWarning($"MasterVolume {data.masterVolume} fuori range, reset a 0.7");
                data.masterVolume = 0.7f;
            }
            
            if (data.unlockedLevels == null || data.unlockedLevels.Length == 0)
            {
                Debug.LogWarning("unlockedLevels nullo o vuoto, ricreo array");
                data.unlockedLevels = new bool[10];
                data.unlockedLevels[0] = true;
            }
            
            // Assicura che almeno il primo livello sia sbloccato
            if (!data.unlockedLevels[0])
            {
                Debug.LogWarning("Primo livello non sbloccato, correggo");
                data.unlockedLevels[0] = true;
            }
            
            // Valida timestamp
            if (data.lastSaveTime > DateTime.Now.AddDays(1) || data.lastSaveTime < DateTime.Now.AddYears(-10))
            {
                Debug.LogWarning($"Timestamp salvataggio sospetto: {data.lastSaveTime}");
                data.lastSaveTime = DateTime.Now;
            }
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore validazione dati: {e.Message}");
            return false;
        }
    }
    
    // Encryption methods asincrone
    private async Task<byte[]> EncryptAsync(byte[] data)
    {
        return await Task.Run(() =>
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.IV = encryptionIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        });
    }
    
    private async Task<byte[]> DecryptAsync(byte[] data)
    {
        return await Task.Run(() =>
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.IV = encryptionIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                using (MemoryStream ms = new MemoryStream(data))
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    cs.CopyTo(output);
                    return output.ToArray();
                }
            }
        });
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
    
    public async void DeleteSaveFile()
    {
        try
        {
            isSaving = true;
            
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("File di salvataggio eliminato");
            }
            
            // Pulisci anche i backup
            if (Directory.Exists(backupFolderPath))
            {
                var backupFiles = Directory.GetFiles(backupFolderPath, "backup_*.dat");
                foreach (string backupFile in backupFiles)
                {
                    await Task.Run(() => File.Delete(backupFile));
                }
            }
            
            currentGameData = new GameData();
            
            // Salva immediatamente il nuovo stato
            await SaveGameAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore eliminazione salvataggio: {e.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }
    
    // Metodo per forzare salvataggio immediato (usare con cautela)
    public async Task<bool> ForceSaveAsync()
    {
        timeSinceLastSave = autoSaveInterval; // Forza salvataggio
        return await SaveGameAsync();
    }
    
    // Metodi per test
    public string GetSavePath() => saveFilePath;
    public bool HasSaveFile() => File.Exists(saveFilePath);
    public bool IsSaving() => isSaving;
    
    // Gestione eventi applicazione
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && enableAutoSave && saveOnApplicationPause && !isSaving)
        {
            Debug.Log("Applicazione in pausa, salvataggio automatico...");
            _ = SaveGameAsync();
        }
    }
    
    private void OnApplicationQuit()
    {
        if (enableAutoSave && currentGameData != null && !isSaving)
        {
            Debug.Log("Applicazione in chiusura, salvataggio finale...");
            // Salvataggio sincrono per sicurezza
            var task = SaveGameAsync();
            task.Wait(1000); // Timeout di 1 secondo
        }
    }
    
    private void OnDisable()
    {
        if (enableAutoSave && currentGameData != null && !isSaving)
        {
            Debug.Log("SaveSystem disabilitato, tentativo di salvataggio...");
            var task = SaveGameAsync();
            task.Wait(500); // Timeout più breve
        }
    }
    
    #if UNITY_EDITOR
    // Metodi di debug per Editor
    [UnityEditor.MenuItem("Tools/Save System/Show Save Path")]
    private static void ShowSavePath()
    {
        if (Instance != null)
        {
            UnityEditor.EditorUtility.RevealInFinder(Instance.GetSavePath());
        }
    }
    
    [UnityEditor.MenuItem("Tools/Save System/Delete All Saves")]
    private static void DeleteAllSaves()
    {
        if (Instance != null)
        {
            Instance.DeleteSaveFile();
        }
    }
    #endif
}