using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// 1. Classi Data (separate e serializzabili)
[System.Serializable]
public class MarketItemData
{
    public string id;
    public string name;
    public string description;
    public ItemType type;
    public int basePrice;
    public int stockQuantity; // -1 per illimitato
    public int requiredLevel;
    public int requiredReputation;
    public string effectId; // Riferimento a un effetto configurato

    public enum ItemType { HackingTool, Software, Hardware, Service, Information, Cosmetic }
}

[System.Serializable]
public class PlayerInventoryItem
{
    public string itemId;
    public int quantity;
}

[System.Serializable]
public class MarketOrderData
{
    public string orderId;
    public DateTime orderDate;
    public string itemId;
    public int quantity;
    public int totalPrice;
    public OrderStatus status;
    public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled }
}

[System.Serializable]
public class BlackMarketSaveData
{
    public int marketReputation;
    public int successfulTransactions;
    public List<PlayerInventoryItem> playerInventory = new List<PlayerInventoryItem>();
    public List<MarketOrderData> orderHistory = new List<MarketOrderData>();
    public DateTime lastMarketUpdate;
}

// 2. Interfaccia per Item Effects (Dependency Inversion)
public interface IItemEffectHandler
{
    void ApplyEffect(string itemId);
}

// 3. Classe Manager Principale (Singleton Corretto)
public class BlackMarket : MonoBehaviour
{
    // Singleton thread-safe con proprietà
    public static BlackMarket Instance { get; private set; }
    
    // Configurazione esposta in Inspector
    [Header("Market Configuration")]
    [SerializeField] private List<MarketItemData> _itemDatabase = new List<MarketItemData>();
    [SerializeField] private float _priceVolatility = 0.1f;
    [SerializeField] private int _restockIntervalHours = 24;
    [SerializeField] private int _maxPlayerInventorySlots = 50;
    
    // Riferimenti via Inspector (non FindObjectOfType)
    [Header("External References")]
    [SerializeField] private EconomyManager _economyManager;
    [SerializeField] private PlayerProgression _playerProgression;
    [SerializeField] private IItemEffectHandler _itemEffectHandler;
    
    // Stato corrente (separato dalla configurazione)
    private Dictionary<string, MarketItemData> _itemDatabaseDict = new Dictionary<string, MarketItemData>();
    private Dictionary<string, int> _currentPrices = new Dictionary<string, int>();
    private Dictionary<string, int> _currentStock = new Dictionary<string, int>();
    private List<MarketItemData> _limitedTimeOffers = new List<MarketItemData>();
    
    // Dati di sessione
    private BlackMarketSaveData _currentSaveData = new BlackMarketSaveData();
    
    // Eventi per comunicazione
    public static event Action<int> OnReputationChanged;
    public static event Action<List<PlayerInventoryItem>> OnInventoryUpdated;
    public static event Action OnMarketPricesUpdated;
    
    private void Awake()
    {
        // Singleton robusto
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeMarketSystem();
    }
    
    private void InitializeMarketSystem()
    {
        // Converti lista in dizionario per accesso O(1)
        _itemDatabaseDict = _itemDatabase.ToDictionary(item => item.id, item => item);
        
        // Inizializza prezzi e stock
        foreach (var item in _itemDatabase)
        {
            _currentPrices[item.id] = item.basePrice;
            _currentStock[item.id] = item.stockQuantity;
        }
        
        // Carica dati salvati
        LoadMarketData();
        
        // Aggiorna prezzi se necessario
        if ((DateTime.Now - _currentSaveData.lastMarketUpdate).TotalHours >= _restockIntervalHours)
        {
            UpdateMarketPrices();
            GenerateLimitedOffers();
        }
    }
    
    // 4. Metodo di Acquisto Corretto e Sicuro
    public PurchaseResult PurchaseItem(string itemId, int quantity = 1)
    {
        // Validazione input
        if (!_itemDatabaseDict.TryGetValue(itemId, out MarketItemData itemData))
            return PurchaseResult.ItemNotFound;
        
        if (quantity <= 0)
            return PurchaseResult.InvalidQuantity;
        
        // Controlli requisiti
        if (!ValidatePurchaseRequirements(itemData, quantity, out string validationMessage))
            return new PurchaseResult(false, validationMessage);
        
        // Calcola costo
        int totalCost = GetCurrentPrice(itemId) * quantity;
        
        // Processa transazione economica (via EconomyManager)
        if (!_economyManager.TrySpendMoney(totalCost))
            return PurchaseResult.InsufficientFunds;
        
        // Aggiorna stock
        if (_currentStock[itemId] > 0) // -1 = illimitato
        {
            _currentStock[itemId] -= quantity;
        }
        
        // Aggiungi all'inventario
        AddToPlayerInventory(itemId, quantity);
        
        // Registra ordine
        RecordOrder(itemId, quantity, totalCost);
        
        // Aggiorna reputazione
        UpdateReputation(1);
        _currentSaveData.successfulTransactions++;
        
        // Applica effetti tramite handler
        _itemEffectHandler?.ApplyEffect(itemId);
        
        // Salva e notifica
        SaveMarketData();
        NotifyPurchaseSuccess(itemData.name, quantity);
        
        return PurchaseResult.Success;
    }
    
    // 5. Validazione Centralizzata
    private bool ValidatePurchaseRequirements(MarketItemData item, int quantity, out string message)
    {
        message = string.Empty;
        
        if (_playerProgression.CurrentLevel < item.requiredLevel)
        {
            message = $"Livello richiesto: {item.requiredLevel}";
            return false;
        }
        
        if (_currentSaveData.marketReputation < item.requiredReputation)
        {
            message = "Reputazione insufficiente";
            return false;
        }
        
        if (GetCurrentStock(item.id) > 0 && GetCurrentStock(item.id) < quantity)
        {
            message = "Quantità insufficiente in stock";
            return false;
        }
        
        if (GetTotalInventoryItems() + quantity > _maxPlayerInventorySlots)
        {
            message = "Spazio inventario insufficiente";
            return false;
        }
        
        return true;
    }
    
    // 6. Gestione Inventario Efficiente (Dictionary-based)
    private void AddToPlayerInventory(string itemId, int quantity)
    {
        var existingItem = _currentSaveData.playerInventory.Find(i => i.itemId == itemId);
        
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            _currentSaveData.playerInventory.Add(new PlayerInventoryItem
            {
                itemId = itemId,
                quantity = quantity
            });
        }
        
        OnInventoryUpdated?.Invoke(_currentSaveData.playerInventory);
    }
    
    // 7. Sistema di Prezzi Dinamici
    private void UpdateMarketPrices()
    {
        System.Random rng = new System.Random();
        
        foreach (var itemId in _currentPrices.Keys.ToList())
        {
            if (rng.NextDouble() < _priceVolatility)
            {
                // Variazione più realistica basata su domanda/offerta
                float volatilityFactor = 0.8f + (float)rng.NextDouble() * 0.4f; // 0.8-1.2
                int basePrice = _itemDatabaseDict[itemId].basePrice;
                
                // Considera stock limitato
                if (GetCurrentStock(itemId) < 5 && GetCurrentStock(itemId) > 0)
                {
                    volatilityFactor *= 1.3f; // +30% per scarsità
                }
                
                _currentPrices[itemId] = Mathf.Clamp(
                    Mathf.RoundToInt(basePrice * volatilityFactor),
                    basePrice / 2, // Minimo 50%
                    basePrice * 2  // Massimo 200%
                );
            }
        }
        
        _currentSaveData.lastMarketUpdate = DateTime.Now;
        OnMarketPricesUpdated?.Invoke();
    }
    
    // 8. Gestione Dati Persistente
    private void SaveMarketData()
    {
        SaveSystem.Instance.SaveSubsystemData("blackmarket", _currentSaveData);
    }
    
    private void LoadMarketData()
    {
        var savedData = SaveSystem.Instance.LoadSubsystemData<BlackMarketSaveData>("blackmarket");
        if (savedData != null)
        {
            _currentSaveData = savedData;
        }
    }
    
    // 9. Metodi di utilità pubblici
    public int GetCurrentPrice(string itemId) => 
        _currentPrices.TryGetValue(itemId, out int price) ? price : 0;
    
    public int GetCurrentStock(string itemId) => 
        _currentStock.TryGetValue(itemId, out int stock) ? stock : 0;
    
    public int GetTotalInventoryItems() => 
        _currentSaveData.playerInventory.Sum(item => item.quantity);
    
    public int GetPlayerReputation() => _currentSaveData.marketReputation;
    
    // 10. Classe per risultato acquisto
    public class PurchaseResult
    {
        public bool Success { get; }
        public string Message { get; }
        
        public static readonly PurchaseResult Success = new PurchaseResult(true, "Acquisto completato");
        public static readonly PurchaseResult ItemNotFound = new PurchaseResult(false, "Oggetto non trovato");
        public static readonly PurchaseResult InvalidQuantity = new PurchaseResult(false, "Quantità non valida");
        public static readonly PurchaseResult InsufficientFunds = new PurchaseResult(false, "Fondi insufficienti");
        
        public PurchaseResult(bool success, string message = "")
        {
            Success = success;
            Message = message;
        }
    }
    
    // Metodo helper per aggiornamento reputazione
    private void UpdateReputation(int change)
    {
        _currentSaveData.marketReputation = Mathf.Clamp(
            _currentSaveData.marketReputation + change, 0, 100);
        
        OnReputationChanged?.Invoke(_currentSaveData.marketReputation);
    }
    
    // Metodo per generare offerte limitate (migliorato)
    private void GenerateLimitedOffers()
    {
        _limitedTimeOffers.Clear();
        var rng = new System.Random();
        
        // Seleziona oggetti rari (alto requisito, basso stock)
        var rareItems = _itemDatabase
            .Where(item => item.requiredLevel >= 15 && GetCurrentStock(item.id) < 10)
            .ToList();
        
        for (int i = 0; i < Mathf.Min(3, rareItems.Count); i++)
        {
            var original = rareItems[rng.Next(rareItems.Count)];
            rareItems.Remove(original); // Evita duplicati
            
            var limitedOffer = new MarketItemData
            {
                id = $"{original.id}_LIMITED_{DateTime.Now:yyyyMMdd}",
                name = $"{original.name} [OFFERTA LIMITATA]",
                description = $"{original.description}\n\n• Disponibile per 6 ore\n• Sconto 40%",
                type = original.type,
                basePrice = Mathf.RoundToInt(original.basePrice * 0.6f), // 40% off
                stockQuantity = 1,
                requiredLevel = original.requiredLevel,
                requiredReputation = original.requiredReputation,
                effectId = original.effectId
            };
            
            _limitedTimeOffers.Add(limitedOffer);
            _currentStock[limitedOffer.id] = 1;
            _currentPrices[limitedOffer.id] = limitedOffer.basePrice;
        }
    }
    
    // Metodo per vendita oggetti
    public bool SellItem(string itemId, int quantity = 1)
    {
        var inventoryItem = _currentSaveData.playerInventory.Find(i => i.itemId == itemId);
        
        if (inventoryItem == null || inventoryItem.quantity < quantity)
            return false;
        
        // Prezzo di vendita basato su prezzo attuale
        int sellPrice = Mathf.RoundToInt(GetCurrentPrice(itemId) * 0.7f) * quantity;
        
        // Rimuovi dall'inventario
        inventoryItem.quantity -= quantity;
        if (inventoryItem.quantity <= 0)
        {
            _currentSaveData.playerInventory.Remove(inventoryItem);
        }
        
        // Aggiungi denaro
        _economyManager.AddMoney(sellPrice);
        
        // Leggera penalità reputazione per vendite frequenti
        UpdateReputation(-1);
        
        // Notifica UI
        OnInventoryUpdated?.Invoke(_currentSaveData.playerInventory);
        SaveMarketData();
        
        return true;
    }
    
    // Pulizia alla distruzione
    private void OnDestroy()
    {
        if (Instance == this)
        {
            SaveMarketData();
            Instance = null;
        }
    }
}