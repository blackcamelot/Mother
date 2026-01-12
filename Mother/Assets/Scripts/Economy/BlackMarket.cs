using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class BlackMarket : MonoBehaviour
{
    [System.Serializable]
    public class MarketItem
    {
        public string itemName;
        public Sprite itemIcon;
        public int basePrice;
        [TextArea] public string description;
        public bool isPurchased;
        public bool isLimited = false;
        public int quantityAvailable = 1;
        public int quantityPurchased = 0;
        
        // Per la variazione dinamica dei prezzi
        [Range(0.5f, 3f)] public float priceMultiplier = 1f;
        public float demandFactor = 1f;
        
        // Metodi per calcolare il prezzo corrente
        public int GetCurrentPrice()
        {
            return Mathf.RoundToInt(basePrice * priceMultiplier * demandFactor);
        }
        
        public bool CanPurchase()
        {
            if (isLimited)
            {
                return quantityPurchased < quantityAvailable;
            }
            return !isPurchased;
        }
    }
    
    [Header("Configurazione Market")]
    [SerializeField] private List<MarketItem> marketItems = new List<MarketItem>();
    [SerializeField] private float priceUpdateInterval = 60f; // secondi
    [SerializeField] private float maxPriceMultiplier = 2.5f;
    [SerializeField] private float minPriceMultiplier = 0.6f;
    
    [Header("UI References")]
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Text playerCurrencyText;
    [SerializeField] private Text marketStatusText;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;
    
    [Header("Animazioni")]
    [SerializeField] private Animator marketAnimator;
    [SerializeField] private string openAnimation = "MarketOpen";
    [SerializeField] private string closeAnimation = "MarketClose";
    
    // Stato del mercato
    private int playerCurrency = 1000; // Sostituire con riferimento a GameManager
    private float timeSinceLastPriceUpdate = 0f;
    private bool isMarketOpen = false;
    
    // Cache UI
    private Dictionary<string, GameObject> itemUIObjects = new Dictionary<string, GameObject>();
    
    // Eventi
    public static event Action<MarketItem> OnItemPurchased;
    public static event Action OnMarketRefreshed;
    public static event Action<int> OnCurrencyChanged;
    
    private void Start()
    {
        InitializeMarket();
        UpdatePlayerCurrencyDisplay();
        
        // Setup bottoni
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshMarket);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMarket);
    }
    
    private void Update()
    {
        if (!isMarketOpen) return;
        
        // Aggiorna timer per variazione prezzi
        timeSinceLastPriceUpdate += Time.deltaTime;
        if (timeSinceLastPriceUpdate >= priceUpdateInterval)
        {
            UpdateMarketPrices();
            timeSinceLastPriceUpdate = 0f;
        }
    }
    
    private void InitializeMarket()
    {
        if (itemsContainer == null)
        {
            Debug.LogError("BlackMarket: itemsContainer non assegnato!");
            return;
        }
        
        if (itemPrefab == null)
        {
            Debug.LogError("BlackMarket: itemPrefab non assegnato!");
            return;
        }
        
        // Pulisci container
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }
        itemUIObjects.Clear();
        
        // Crea UI per ogni item
        foreach (MarketItem item in marketItems)
        {
            CreateItemUI(item);
        }
        
        // Inizializza fattori di domanda
        InitializeDemandFactors();
        
        Debug.Log($"BlackMarket inizializzato con {marketItems.Count} items");
    }
    
    private void CreateItemUI(MarketItem item)
    {
        GameObject itemObj = Instantiate(itemPrefab, itemsContainer);
        itemObj.name = $"Item_{item.itemName}";
        
        // Configura componenti UI
        ItemUI itemUI = itemObj.GetComponent<ItemUI>();
        if (itemUI == null)
        {
            Debug.LogWarning($"Item prefab non ha componente ItemUI, aggiungo componenti di base");
            itemUI = itemObj.AddComponent<ItemUI>();
            
            // Cerca componenti figlio
            Transform iconTransform = itemObj.transform.Find("Icon");
            Transform nameTransform = itemObj.transform.Find("Name");
            Transform priceTransform = itemObj.transform.Find("Price");
            Transform descTransform = itemObj.transform.Find("Description");
            Transform buttonTransform = itemObj.transform.Find("BuyButton");
            
            if (iconTransform != null) itemUI.itemIcon = iconTransform.GetComponent<Image>();
            if (nameTransform != null) itemUI.itemName = nameTransform.GetComponent<Text>();
            if (priceTransform != null) itemUI.itemPrice = priceTransform.GetComponent<Text>();
            if (descTransform != null) itemUI.itemDescription = descTransform.GetComponent<Text>();
            if (buttonTransform != null) itemUI.buyButton = buttonTransform.GetComponent<Button>();
        }
        
        // Popola UI
        if (itemUI.itemIcon != null && item.itemIcon != null)
            itemUI.itemIcon.sprite = item.itemIcon;
        
        if (itemUI.itemName != null)
            itemUI.itemName.text = item.itemName;
        
        if (itemUI.itemPrice != null)
            itemUI.itemPrice.text = $"{item.GetCurrentPrice()}G";
        
        if (itemUI.itemDescription != null)
            itemUI.itemDescription.text = item.description;
        
        // Configura bottone acquisto
        if (itemUI.buyButton != null)
        {
            // Rimuovi listener esistenti
            itemUI.buyButton.onClick.RemoveAllListeners();
            
            // Aggiungi nuovo listener
            itemUI.buyButton.onClick.AddListener(() => PurchaseItem(item.itemName));
            
            // Aggiorna stato bottone
            UpdateButtonState(itemUI.buyButton, item);
        }
        
        // Aggiungi riferimento al dizionario
        itemUIObjects[item.itemName] = itemObj;
        
        // Aggiorna stato visivo
        UpdateItemUI(item);
    }
    
    private void UpdateButtonState(Button button, MarketItem item)
    {
        if (button == null) return;
        
        bool canPurchase = item.CanPurchase() && playerCurrency >= item.GetCurrentPrice();
        button.interactable = canPurchase;
        
        // Cambia colore in base alla disponibilità
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = canPurchase ? Color.white : Color.gray;
        }
        
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = item.CanPurchase() ? "ACQUISTA" : "ESAURITO";
        }
    }
    
    private void UpdateItemUI(MarketItem item)
    {
        if (!itemUIObjects.ContainsKey(item.itemName)) return;
        
        GameObject itemObj = itemUIObjects[item.itemName];
        ItemUI itemUI = itemObj.GetComponent<ItemUI>();
        
        if (itemUI == null) return;
        
        // Aggiorna prezzo
        if (itemUI.itemPrice != null)
        {
            itemUI.itemPrice.text = $"{item.GetCurrentPrice()}G";
            
            // Animazione cambio prezzo
            if (itemUI.priceAnimator != null)
            {
                itemUI.priceAnimator.Play("PriceChange");
            }
        }
        
        // Aggiorna stato acquisto
        if (item.isLimited)
        {
            if (itemUI.quantityText != null)
            {
                itemUI.quantityText.text = $"{item.quantityPurchased}/{item.quantityAvailable}";
            }
            
            if (itemUI.purchasedIndicator != null)
            {
                itemUI.purchasedIndicator.SetActive(item.quantityPurchased >= item.quantityAvailable);
            }
        }
        else
        {
            if (itemUI.purchasedIndicator != null)
            {
                itemUI.purchasedIndicator.SetActive(item.isPurchased);
            }
        }
        
        // Aggiorna bottone
        if (itemUI.buyButton != null)
        {
            UpdateButtonState(itemUI.buyButton, item);
        }
    }
    
    private void InitializeDemandFactors()
    {
        System.Random rand = new System.Random(DateTime.Now.Millisecond);
        
        foreach (MarketItem item in marketItems)
        {
            // Inizializza con valori casuali
            item.demandFactor = (float)(0.8f + rand.NextDouble() * 0.4f); // 0.8 - 1.2
            
            // Reset purchased state per testing
            #if UNITY_EDITOR
            item.isPurchased = false;
            item.quantityPurchased = 0;
            #endif
        }
    }
    
    private void UpdateMarketPrices()
    {
        System.Random rand = new System.Random(DateTime.Now.Millisecond);
        
        foreach (MarketItem item in marketItems)
        {
            // Aggiorna domanda (simula fluttuazioni)
            float demandChange = (float)((rand.NextDouble() - 0.5f) * 0.2f); // -0.1 to +0.1
            item.demandFactor = Mathf.Clamp(item.demandFactor + demandChange, 0.5f, 2f);
            
            // Aggiorna moltiplicatore prezzo in base alla domanda
            item.priceMultiplier = Mathf.Lerp(
                item.priceMultiplier,
                Mathf.Clamp(item.demandFactor, minPriceMultiplier, maxPriceMultiplier),
                0.3f
            );
            
            // Aggiorna UI
            UpdateItemUI(item);
        }
        
        // Aggiorna testo stato
        if (marketStatusText != null)
        {
            marketStatusText.text = $"Prezzi aggiornati: {DateTime.Now:HH:mm}";
            StartCoroutine(ClearStatusText(3f));
        }
    }
    
    private System.Collections.IEnumerator ClearStatusText(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (marketStatusText != null)
        {
            marketStatusText.text = "Mercato aperto";
        }
    }
    
    public void PurchaseItem(string itemName)
    {
        MarketItem item = marketItems.Find(i => i.itemName == itemName);
        if (item == null)
        {
            Debug.LogWarning($"BlackMarket: Item {itemName} non trovato");
            return;
        }
        
        // Verifica se può essere acquistato
        if (!item.CanPurchase())
        {
            Debug.Log($"BlackMarket: {itemName} non disponibile");
            return;
        }
        
        int currentPrice = item.GetCurrentPrice();
        
        // Verifica fondi
        if (playerCurrency < currentPrice)
        {
            Debug.Log($"BlackMarket: Fondi insufficienti per {itemName}");
            ShowInsufficientFundsMessage();
            return;
        }
        
        // Effettua acquisto
        playerCurrency -= currentPrice;
        UpdatePlayerCurrencyDisplay();
        
        // Aggiorna stato item
        if (item.isLimited)
        {
            item.quantityPurchased++;
            if (item.quantityPurchased >= item.quantityAvailable)
            {
                Debug.Log($"BlackMarket: {itemName} esaurito");
            }
        }
        else
        {
            item.isPurchased = true;
        }
        
        // Aumenta domanda per questo item (rarità percepita)
        item.demandFactor = Mathf.Min(item.demandFactor * 1.1f, 2f);
        
        // Notifica acquisto
        OnItemPurchased?.Invoke(item);
        Debug.Log($"BlackMarket: Acquisto {itemName} per {currentPrice}G");
        
        // Aggiorna UI
        UpdateItemUI(item);
        
        // Salva stato acquisto (da integrare con SaveSystem)
        SavePurchase(item);
    }
    
    private void ShowInsufficientFundsMessage()
    {
        // Implementa un sistema di messaggi a comparsa
        Debug.LogWarning("Fondi insufficienti!");
        
        if (marketStatusText != null)
        {
            string originalText = marketStatusText.text;
            marketStatusText.text = "FONDI INSUFFICIENTI!";
            marketStatusText.color = Color.red;
            
            StartCoroutine(RestoreStatusText(originalText, 2f));
        }
    }
    
    private System.Collections.IEnumerator RestoreStatusText(string originalText, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (marketStatusText != null)
        {
            marketStatusText.text = originalText;
            marketStatusText.color = Color.white;
        }
    }
    
    private void SavePurchase(MarketItem item)
    {
        // DA INTEGRARE con SaveSystem
        // Usa PlayerPrefs come soluzione temporanea
        string key = $"BM_{item.itemName}_Purchased";
        PlayerPrefs.SetInt(key, item.isLimited ? item.quantityPurchased : (item.isPurchased ? 1 : 0));
        PlayerPrefs.Save();
        
        Debug.Log($"Salvato acquisto per {item.itemName}");
    }
    
    private void LoadPurchases()
    {
        // DA INTEGRARE con SaveSystem
        foreach (MarketItem item in marketItems)
        {
            string key = $"BM_{item.itemName}_Purchased";
            if (PlayerPrefs.HasKey(key))
            {
                int value = PlayerPrefs.GetInt(key);
                if (item.isLimited)
                {
                    item.quantityPurchased = Mathf.Min(value, item.quantityAvailable);
                }
                else
                {
                    item.isPurchased = value == 1;
                }
            }
        }
    }
    
    public void RefreshMarket()
    {
        // Ricarica tutti gli item
        InitializeMarket();
        
        // Notifica refresh
        OnMarketRefreshed?.Invoke();
        
        Debug.Log("BlackMarket: Mercato aggiornato");
    }
    
    public void OpenMarket()
    {
        if (isMarketOpen) return;
        
        isMarketOpen = true;
        gameObject.SetActive(true);
        
        // Animazione apertura
        if (marketAnimator != null)
        {
            marketAnimator.Play(openAnimation);
        }
        
        // Ricarica acquisti
        LoadPurchases();
        
        // Aggiorna UI
        RefreshMarketUI();
        
        Debug.Log("BlackMarket: Mercato aperto");
    }
    
    public void CloseMarket()
    {
        if (!isMarketOpen) return;
        
        isMarketOpen = false;
        
        // Animazione chiusura
        if (marketAnimator != null)
        {
            marketAnimator.Play(closeAnimation);
            StartCoroutine(DeactivateAfterAnimation(closeAnimation.length));
        }
        else
        {
            gameObject.SetActive(false);
        }
        
        Debug.Log("BlackMarket: Mercato chiuso");
    }
    
    private System.Collections.IEnumerator DeactivateAfterAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
    
    private void RefreshMarketUI()
    {
        foreach (MarketItem item in marketItems)
        {
            UpdateItemUI(item);
        }
        UpdatePlayerCurrencyDisplay();
    }
    
    private void UpdatePlayerCurrencyDisplay()
    {
        if (playerCurrencyText != null)
        {
            playerCurrencyText.text = $"{playerCurrency}G";
        }
        
        // Notifica cambio valuta
        OnCurrencyChanged?.Invoke(playerCurrency);
    }
    
    // Metodi pubblici per integrazione
    public void AddCurrency(int amount)
    {
        playerCurrency += amount;
        UpdatePlayerCurrencyDisplay();
        
        // Aggiorna UI per riflettere nuovi fondi
        RefreshMarketUI();
    }
    
    public int GetCurrency() => playerCurrency;
    
    public void SetCurrency(int amount)
    {
        playerCurrency = Mathf.Max(0, amount);
        UpdatePlayerCurrencyDisplay();
    }
    
    public List<MarketItem> GetAvailableItems()
    {
        return marketItems.FindAll(item => item.CanPurchase());
    }
    
    // Classe helper per UI items
    [System.Serializable]
    public class ItemUI : MonoBehaviour
    {
        public Image itemIcon;
        public Text itemName;
        public Text itemPrice;
        public Text itemDescription;
        public Text quantityText;
        public Button buyButton;
        public GameObject purchasedIndicator;
        public Animator priceAnimator;
    }
    
    #if UNITY_EDITOR
    // Metodi di debug per Editor
    [UnityEditor.MenuItem("Tools/BlackMarket/Reset All Purchases")]
    private static void ResetAllPurchases()
    {
        BlackMarket market = FindObjectOfType<BlackMarket>();
        if (market != null)
        {
            foreach (MarketItem item in market.marketItems)
            {
                item.isPurchased = false;
                item.quantityPurchased = 0;
                
                // Pulisci PlayerPrefs
                string key = $"BM_{item.itemName}_Purchased";
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
            market.RefreshMarket();
            
            Debug.Log("BlackMarket: Tutti gli acquisti resettati");
        }
    }
    
    [UnityEditor.MenuItem("Tools/BlackMarket/Add 1000 Currency")]
    private static void AddDebugCurrency()
    {
        BlackMarket market = FindObjectOfType<BlackMarket>();
        if (market != null)
        {
            market.AddCurrency(1000);
            Debug.Log("BlackMarket: Aggiunti 1000G");
        }
    }
    #endif
}