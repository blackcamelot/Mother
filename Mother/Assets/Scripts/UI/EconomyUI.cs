using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class EconomyUI : MonoBehaviour
{
    [Header("Main References")]
    public GameObject economyPanel;
    public Button economyButton;
    
    [Header("Tabs")]
    public Button cryptoTabButton;
    public Button marketTabButton;
    public Button inventoryTabButton;
    public Button tradingTabButton;
    
    public GameObject cryptoPanel;
    public GameObject marketPanel;
    public GameObject inventoryPanel;
    public GameObject tradingPanel;
    
    [Header("Crypto UI")]
    public Transform cryptoListContent;
    public GameObject cryptoItemPrefab;
    public Text walletBalanceText;
    public Text portfolioValueText;
    public InputField buyAmountInput;
    public Dropdown cryptoDropdown;
    public Button buyButton;
    public Button sellButton;
    public Button miningButton;
    public Text miningStatusText;
    
    [Header("Market UI")]
    public Transform marketItemsContent;
    public GameObject marketItemPrefab;
    public Text marketReputationText;
    public Text playerBalanceText;
    public InputField quantityInput;
    
    [Header("Inventory UI")]
    public Transform inventoryContent;
    public GameObject inventoryItemPrefab;
    public Text inventorySpaceText;
    
    [Header("Trading UI")]
    public Transform tradingHistoryContent;
    public GameObject tradeRecordPrefab;
    public Text totalTradesText;
    public Text successRateText;
    
    private CryptocurrencySystem cryptoSystem;
    private BlackMarket marketSystem;
    private int currentTab = 0;
    
    private void Start()
    {
        cryptoSystem = CryptocurrencySystem.Instance;
        marketSystem = BlackMarket.Instance;
        
        InitializeUI();
        UpdateAllDisplays();
    }
    
    private void InitializeUI()
    {
        // Button listeners
        if (economyButton != null)
        {
            economyButton.onClick.AddListener(ToggleEconomyPanel);
        }
        
        // Tab listeners
        if (cryptoTabButton != null)
            cryptoTabButton.onClick.AddListener(() => SwitchTab(0));
        
        if (marketTabButton != null)
            marketTabButton.onClick.AddListener(() => SwitchTab(1));
        
        if (inventoryTabButton != null)
            inventoryTabButton.onClick.AddListener(() => SwitchTab(2));
        
        if (tradingTabButton != null)
            tradingTabButton.onClick.AddListener(() => SwitchTab(3));
        
        // Crypto UI listeners
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyCrypto);
        
        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellCrypto);
        
        if (miningButton != null)
            miningButton.onClick.AddListener(ToggleMining);
        
        // Initialize dropdown
        InitializeCryptoDropdown();
        
        // Hide panel initially
        if (economyPanel != null)
        {
            economyPanel.SetActive(false);
        }
        
        // Default to crypto tab
        SwitchTab(0);
    }

    public void UpdateCreditDisplay(int credits) {
        creditText.text = $"CREDITI: {credits}";
    }


    
    public void ToggleEconomyPanel()
    {
        if (economyPanel != null)
        {
            bool newState = !economyPanel.activeSelf;
            economyPanel.SetActive(newState);
            
            if (newState)
            {
                UpdateAllDisplays();
            }
        }
    }
    
    private void SwitchTab(int tabIndex)
    {
        currentTab = tabIndex;
        
        // Update tab visuals
        UpdateTabButtons(tabIndex);
        
        // Show/hide panels
        if (cryptoPanel != null)
            cryptoPanel.SetActive(tabIndex == 0);
        
        if (marketPanel != null)
            marketPanel.SetActive(tabIndex == 1);
        
        if (inventoryPanel != null)
            inventoryPanel.SetActive(tabIndex == 2);
        
        if (tradingPanel != null)
            tradingPanel.SetActive(tabIndex == 3);
        
        // Update content for active tab
        switch (tabIndex)
        {
            case 0: UpdateCryptoDisplay(); break;
            case 1: UpdateMarketDisplay(); break;
            case 2: UpdateInventoryDisplay(); break;
            case 3: UpdateTradingDisplay(); break;
        }
    }
    
    private void UpdateTabButtons(int activeTab)
    {
        Color activeColor = new Color(0, 0.5f, 0); // Dark green
        Color inactiveColor = Color.gray;
        
        if (cryptoTabButton != null)
            UpdateButtonColor(cryptoTabButton, activeTab == 0 ? activeColor : inactiveColor);
        
        if (marketTabButton != null)
            UpdateButtonColor(marketTabButton, activeTab == 1 ? activeColor : inactiveColor);
        
        if (inventoryTabButton != null)
            UpdateButtonColor(inventoryTabButton, activeTab == 2 ? activeColor : inactiveColor);
        
        if (tradingTabButton != null)
            UpdateButtonColor(tradingTabButton, activeTab == 3 ? activeColor : inactiveColor);
    }
    
    private void UpdateButtonColor(Button button, Color color)
    {
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }
    }
    
    // CRYPTO TAB
    
    private void InitializeCryptoDropdown()
    {
        if (cryptoDropdown != null && cryptoSystem != null)
        {
            cryptoDropdown.ClearOptions();
            List<string> options = new List<string>();
            
            foreach (var crypto in cryptoSystem.cryptocurrencies)
            {
                options.Add($"{crypto.symbol} - {crypto.name}");
            }
            
            cryptoDropdown.AddOptions(options);
        }
    }
    
    private void UpdateCryptoDisplay()
    {
        if (cryptoSystem == null) return;
        
        // Update wallet info
        if (walletBalanceText != null)
            walletBalanceText.text = $"Wallet: ${cryptoSystem.walletBalance:F2}";
        
        if (portfolioValueText != null)
        {
            double portfolioValue = cryptoSystem.GetPortfolioValue();
            portfolioValueText.text = $"Portfolio: ${portfolioValue:F2}";
        }
        
        // Update crypto list
        UpdateCryptoList();
        
        // Update mining status
        if (miningStatusText != null)
        {
            miningStatusText.text = cryptoSystem.canMine ? "MINING: ACTIVE" : "MINING: INACTIVE";
            miningStatusText.color = cryptoSystem.canMine ? Color.green : Color.red;
        }
        
        if (miningButton != null)
        {
            miningButton.GetComponentInChildren<Text>().text = cryptoSystem.canMine ? "STOP MINING" : "START MINING";
        }
    }
    
    private void UpdateCryptoList()
    {
        if (cryptoListContent == null || cryptoItemPrefab == null || cryptoSystem == null) return;
        
        // Clear existing items
        foreach (Transform child in cryptoListContent)
        {
            Destroy(child.gameObject);
        }
        
        // Create crypto items
        foreach (var crypto in cryptoSystem.cryptocurrencies)
        {
            GameObject cryptoItem = Instantiate(cryptoItemPrefab, cryptoListContent);
            CryptoUIItem uiItem = cryptoItem.GetComponent<CryptoUIItem>();
            
            if (uiItem != null)
            {
                double change24h = cryptoSystem.Get24HourChange(crypto.symbol);
                uiItem.Initialize(crypto, change24h);
            }
        }
    }
    
    private void OnBuyCrypto()
    {
        if (cryptoSystem == null || cryptoDropdown == null || buyAmountInput == null) return;
        
        string selectedOption = cryptoDropdown.options[cryptoDropdown.value].text;
        string symbol = selectedOption.Split(' ')[0];
        
        if (double.TryParse(buyAmountInput.text, out double amount))
        {
            bool success = cryptoSystem.BuyCryptocurrency(symbol, amount);
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                if (success)
                {
                    uiManager.ShowNotification($"Bought {symbol} for ${amount:F2}");
                }
                else
                {
                    uiManager.ShowNotification("Purchase failed!");
                }
            }
            
            UpdateCryptoDisplay();
        }
    }
    
    private void OnSellCrypto()
    {
        if (cryptoSystem == null || cryptoDropdown == null || buyAmountInput == null) return;
        
        string selectedOption = cryptoDropdown.options[cryptoDropdown.value].text;
        string symbol = selectedOption.Split(' ')[0];
        
        if (double.TryParse(buyAmountInput.text, out double amount))
        {
            bool success = cryptoSystem.SellCryptocurrency(symbol, amount);
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                if (success)
                {
                    uiManager.ShowNotification($"Sold {symbol} for ${amount:F2}");
                }
                else
                {
                    uiManager.ShowNotification("Sale failed!");
                }
            }
            
            UpdateCryptoDisplay();
        }
    }
    
    private void ToggleMining()
    {
        if (cryptoSystem == null) return;
        
        if (cryptoSystem.canMine)
        {
            cryptoSystem.StopMining();
        }
        else
        {
            if (cryptoDropdown != null)
            {
                string selectedOption = cryptoDropdown.options[cryptoDropdown.value].text;
                string symbol = selectedOption.Split(' ')[0];
                cryptoSystem.StartMining(symbol);
            }
        }
        
        UpdateCryptoDisplay();
    }
    
    // MARKET TAB
    
    private void UpdateMarketDisplay()
    {
        if (marketSystem == null) return;
        
        // Update reputation and balance
        if (marketReputationText != null)
        {
            marketReputationText.text = $"Reputation: {marketSystem.marketReputation}/100";
            marketReputationText.color = GetReputationColor(marketSystem.marketReputation);
        }
        
        if (playerBalanceText != null)
        {
            playerBalanceText.text = $"Balance: ${GameManager.Instance.playerMoney}";
        }
        
        // Update market items
        UpdateMarketItems();
    }
    
    private void UpdateMarketItems()
    {
        if (marketItemsContent == null || marketItemPrefab == null || marketSystem == null) return;
        
        // Clear existing items
        foreach (Transform child in marketItemsContent)
        {
            Destroy(child.gameObject);
        }
        
        // Get available items
        List<MarketItem> items = marketSystem.GetAvailableItems();
        
        // Create market items
        foreach (var item in items)
        {
            GameObject marketItem = Instantiate(marketItemPrefab, marketItemsContent);
            MarketUIItem uiItem = marketItem.GetComponent<MarketUIItem>();
            
            if (uiItem != null)
            {
                uiItem.Initialize(item, OnMarketItemClicked);
            }
        }
    }
    
    private void OnMarketItemClicked(MarketItem item)
    {
        // Show purchase dialog
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            string message = $"Buy {item.name} for ${item.price}?";
            uiManager.ShowConfirmationDialog(message, () => {
                bool success = marketSystem.PurchaseItem(item.id, 1);
                if (success)
                {
                    UpdateMarketDisplay();
                    UpdateInventoryDisplay();
                }
            });
        }
    }
    
    // INVENTORY TAB
    
    private void UpdateInventoryDisplay()
    {
        if (marketSystem == null) return;
        
        // Update inventory space
        if (inventorySpaceText != null)
        {
            int used = marketSystem.playerInventory.Count;
            int total = marketSystem.maxInventorySize;
            inventorySpaceText.text = $"Inventory: {used}/{total}";
        }
        
        // Update inventory items
        UpdateInventoryItems();
    }
    
    private void UpdateInventoryItems()
    {
        if (inventoryContent == null || inventoryItemPrefab == null || marketSystem == null) return;
        
        // Clear existing items
        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }
        
        // Get player inventory
        List<MarketItem> inventory = marketSystem.GetPlayerInventory();
        
        // Create inventory items
        foreach (var item in inventory)
        {
            GameObject inventoryItem = Instantiate(inventoryItemPrefab, inventoryContent);
            InventoryUIItem uiItem = inventoryItem.GetComponent<InventoryUIItem>();
            
            if (uiItem != null)
            {
                uiItem.Initialize(item, OnInventoryItemClicked);
            }
        }
    }
    
    private void OnInventoryItemClicked(MarketItem item)
    {
        // Show item options (use/sell)
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowItemOptionsDialog(item, () => {
                // Use item
                marketSystem.ApplyItemEffects(item);
            }, () => {
                // Sell item
                bool success = marketSystem.SellItem(item.id, 1);
                if (success)
                {
                    UpdateInventoryDisplay();
                    UpdateMarketDisplay();
                }
            });
        }
    }
    
    // TRADING TAB
    
    private void UpdateTradingDisplay()
    {
        if (marketSystem == null) return;
        
        // Update trade stats
        if (totalTradesText != null)
        {
            int total = marketSystem.successfulTransactions + marketSystem.failedTransactions;
            totalTradesText.text = $"Total Trades: {total}";
        }
        
        if (successRateText != null)
        {
            int total = marketSystem.successfulTransactions + marketSystem.failedTransactions;
            float rate = total > 0 ? (float)marketSystem.successfulTransactions / total * 100 : 0;
            successRateText.text = $"Success Rate: {rate:F1}%";
            successRateText.color = rate >= 90 ? Color.green : rate >= 70 ? Color.yellow : Color.red;
        }
        
        // Update trade history
        UpdateTradeHistory();
    }
    
    private void UpdateTradeHistory()
    {
        if (tradingHistoryContent == null || tradeRecordPrefab == null || marketSystem == null) return;
        
        // Clear existing records
        foreach (Transform child in tradingHistoryContent)
        {
            Destroy(child.gameObject);
        }
        
        // Get trade history (last 20)
        List<MarketOrder> history = marketSystem.orderHistory;
        int startIndex = Mathf.Max(0, history.Count - 20);
        
        for (int i = startIndex; i < history.Count; i++)
        {
            GameObject tradeRecord = Instantiate(tradeRecordPrefab, tradingHistoryContent);
            TradeRecordUI uiRecord = tradeRecord.GetComponent<TradeRecordUI>();
            
            if (uiRecord != null)
            {
                uiRecord.Initialize(history[i]);
            }
        }
    }
    
    // HELPER METHODS
    
    private Color GetReputationColor(int reputation)
    {
        if (reputation >= 80) return Color.green;
        if (reputation >= 60) return Color.yellow;
        if (reputation >= 40) return new Color(1, 0.5f, 0); // Orange
        return Color.red;
    }
    
    public void UpdateAllDisplays()
    {
        UpdateCryptoDisplay();
        UpdateMarketDisplay();
        UpdateInventoryDisplay();
        UpdateTradingDisplay();
    }
    
    public void RefreshCurrentTab()
    {
        SwitchTab(currentTab);
    }
}

// UI Components

public class CryptoUIItem : MonoBehaviour
{
    public Text symbolText;
    public Text nameText;
    public Text priceText;
    public Text amountText;
    public Text valueText;
    public Text changeText;
    public Image trendArrow;
    
    public void Initialize(Cryptocurrency crypto, double change24h)
    {
        if (symbolText != null)
            symbolText.text = crypto.symbol;
        
        if (nameText != null)
            nameText.text = crypto.name;
        
        if (priceText != null)
            priceText.text = $"${crypto.valuePerCoin:F2}";
        
        if (amountText != null)
            amountText.text = $"{crypto.amount:F6}";
        
        if (valueText != null)
            valueText.text = $"${crypto.TotalValue:F2}";
        
        if (changeText != null)
        {
            changeText.text = $"{change24h:+#.##%;-#.##%;0%}";
            changeText.color = change24h >= 0 ? Color.green : Color.red;
        }
        
        if (trendArrow != null)
        {
            trendArrow.gameObject.SetActive(true);
            trendArrow.color = change24h >= 0 ? Color.green : Color.red;
            trendArrow.transform.rotation = Quaternion.Euler(0, 0, change24h >= 0 ? 0 : 180);
        }
    }
}

public class MarketUIItem : MonoBehaviour
{
    public Text nameText;
    public Text descriptionText;
    public Text priceText;
    public Text levelText;
    public Text quantityText;
    public Button buyButton;
    public Image iconImage;
    
    private MarketItem item;
    private Action<MarketItem> onClick;
    
    public void Initialize(MarketItem marketItem, Action<MarketItem> clickCallback)
    {
        item = marketItem;
        onClick = clickCallback;
        
        if (nameText != null)
            nameText.text = marketItem.name;
        
        if (descriptionText != null)
            descriptionText.text = marketItem.description;
        
        if (priceText != null)
            priceText.text = $"${marketItem.price}";
        
        if (levelText != null)
        {
            levelText.text = $"Level {marketItem.requiredLevel}+";
            levelText.color = GameManager.Instance.playerLevel >= marketItem.requiredLevel ? Color.green : Color.red;
        }
        
        if (quantityText != null)
        {
            quantityText.text = marketItem.quantity == 999 ? "Unlimited" : $"Stock: {marketItem.quantity}";
            quantityText.color = marketItem.quantity > 0 ? Color.white : Color.red;
        }
        
        if (buyButton != null)
        {
            buyButton.interactable = marketItem.quantity > 0 && 
                                   GameManager.Instance.playerLevel >= marketItem.requiredLevel;
            buyButton.onClick.AddListener(OnBuyClicked);
        }
        
        if (iconImage != null && marketItem.icon != null)
        {
            iconImage.sprite = marketItem.icon;
        }
    }
    
    private void OnBuyClicked()
    {
        onClick?.Invoke(item);
    }
}

public class InventoryUIItem : MonoBehaviour
{
    public Text nameText;
    public Text descriptionText;
    public Text quantityText;
    public Button useButton;
    public Button sellButton;
    public Image iconImage;
    
    private MarketItem item;
    private Action<MarketItem> onClick;
    
    public void Initialize(MarketItem marketItem, Action<MarketItem> clickCallback)
    {
        item = marketItem;
        onClick = clickCallback;
        
        if (nameText != null)
            nameText.text = marketItem.name;
        
        if (descriptionText != null)
            descriptionText.text = marketItem.description;
        
        if (quantityText != null)
            quantityText.text = $"x{marketItem.quantity}";
        
        if (useButton != null)
        {
            useButton.onClick.AddListener(() => onClick?.Invoke(item));
        }
        
        if (sellButton != null)
        {
            sellButton.onClick.AddListener(() => {
                // Direct sell without dialog
                BlackMarket.Instance?.SellItem(item.id, 1);
            });
        }
        
        if (iconImage != null && marketItem.icon != null)
        {
            iconImage.sprite = marketItem.icon;
        }
    }
}

public class TradeRecordUI : MonoBehaviour
{
    public Text dateText;
    public Text itemText;
    public Text amountText;
    public Text priceText;
    public Text statusText;
    public Image statusIcon;
    
    public void Initialize(MarketOrder order)
    {
        if (dateText != null)
            dateText.text = order.orderDate.ToString("MM/dd HH:mm");
        
        if (itemText != null)
            itemText.text = order.itemId;
        
        if (amountText != null)
            amountText.text = $"x{order.quantity}";
        
        if (priceText != null)
            priceText.text = $"${order.totalPrice}";
        
        if (statusText != null)
        {
            statusText.text = order.status.ToString();
            statusText.color = GetStatusColor(order.status);
        }
        
        if (statusIcon != null)
        {
            statusIcon.color = GetStatusColor(order.status);
        }
    }
    
    private Color GetStatusColor(MarketOrder.OrderStatus status)
    {
        switch (status)
        {
            case MarketOrder.OrderStatus.Delivered: return Color.green;
            case MarketOrder.OrderStatus.Processing: return Color.yellow;
            case MarketOrder.OrderStatus.Shipped: return Color.blue;
            case MarketOrder.OrderStatus.Cancelled: return Color.red;
            default: return Color.gray;
        }
    }

}
