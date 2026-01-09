using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class MarketItem
{
    public string id;
    public string name;
    public string description;
    public ItemType type;
    public int price;
    public int quantity;
    public int requiredLevel;
    public bool isUnlocked;
    public bool isLimited;
    public DateTime restockTime;
    public Sprite icon;
    
    public enum ItemType
    {
        HackingTool,
        Software,
        Hardware,
        Service,
        Information,
        Cosmetic
    }
}

[System.Serializable]
public class MarketOrder
{
    public string id;
    public DateTime orderDate;
    public string itemId;
    public int quantity;
    public int totalPrice;
    public OrderStatus status;
    
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }
}

public class BlackMarket : MonoBehaviour
{
    public static BlackMarket Instance;
    
    [Header("Market Items")]
    public List<MarketItem> availableItems = new List<MarketItem>();
    public List<MarketItem> limitedTimeOffers = new List<MarketItem>();
    
    [Header("Player Inventory")]
    public List<MarketItem> playerInventory = new List<MarketItem>();
    public List<MarketOrder> orderHistory = new List<MarketOrder>();
    
    [Header("Market Settings")]
    public float priceVolatility = 0.1f; // 10% price change chance
    public int restockHours = 24;
    public int maxInventorySize = 50;
    
    [Header("Reputation System")]
    public int marketReputation = 50; // 0-100
    public int successfulTransactions = 0;
    public int failedTransactions = 0;
    
    [Header("UI References")]
    public GameObject marketPanel;
    public Text reputationText;
    public Text balanceText;
    
    private DateTime lastMarketUpdate;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMarketItems();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        LoadMarketData();
        UpdateMarketPrices();
        GenerateLimitedOffers();
    }
    
    private void InitializeMarketItems()
    {
        if (availableItems.Count == 0)
        {
            // Hacking Tools
            availableItems.Add(new MarketItem()
            {
                id = "TOOL_PORTSCANNER",
                name = "Advanced Port Scanner",
                description = "Scan for open ports with 200% efficiency",
                type = MarketItem.ItemType.HackingTool,
                price = 500,
                quantity = 1,
                requiredLevel = 5,
                isUnlocked = true
            });
            
            availableItems.Add(new MarketItem()
            {
                id = "TOOL_PASSWORDCRACKER",
                name = "Quantum Password Cracker",
                description = "Crack passwords 3x faster",
                type = MarketItem.ItemType.HackingTool,
                price = 1500,
                quantity = 1,
                requiredLevel = 10,
                isUnlocked = false
            });
            
            availableItems.Add(new MarketItem()
            {
                id = "TOOL_FIREWALLBREACHER",
                name = "Firewall Breacher MK II",
                description = "Bypass firewalls with 90% success rate",
                type = MarketItem.ItemType.HackingTool,
                price = 3000,
                quantity = 1,
                requiredLevel = 15,
                isUnlocked = false
            });
            
            // Software
            availableItems.Add(new MarketItem()
            {
                id = "SOFT_VIRUS",
                name = "Stealth Virus v2.0",
                description = "Remotely install undetectable malware",
                type = MarketItem.ItemType.Software,
                price = 800,
                quantity = 5,
                requiredLevel = 8,
                isUnlocked = true
            });
            
            availableItems.Add(new MarketItem()
            {
                id = "SOFT_KEYLOGGER",
                name = "Advanced Keylogger",
                description = "Record all keystrokes on target system",
                type = MarketItem.ItemType.Software,
                price = 1200,
                quantity = 3,
                requiredLevel = 12,
                isUnlocked = false
            });
            
            // Hardware
            availableItems.Add(new MarketItem()
            {
                id = "HARD_ANTENNA",
                name = "High-Gain Antenna",
                description = "Increase wireless range by 500%",
                type = MarketItem.ItemType.Hardware,
                price = 2500,
                quantity = 1,
                requiredLevel = 20,
                isUnlocked = false
            });
            
            availableItems.Add(new MarketItem()
            {
                id = "HARD_SSD",
                name = "Encrypted SSD 1TB",
                description = "Secure storage for sensitive data",
                type = MarketItem.ItemType.Hardware,
                price = 800,
                quantity = 10,
                requiredLevel = 3,
                isUnlocked = true
            });
            
            // Services
            availableItems.Add(new MarketItem()
            {
                id = "SERV_IDFORGERY",
                name = "ID Forgery Service",
                description = "Create fake digital identities",
                type = MarketItem.ItemType.Service,
                price = 5000,
                quantity = 1,
                requiredLevel = 25,
                isUnlocked = false
            });
            
            availableItems.Add(new MarketItem()
            {
                id = "SERV_LAUNDERING",
                name = "Money Laundering",
                description = "Clean dirty money (90% return)",
                type = MarketItem.ItemType.Service,
                price = 10000,
                quantity = 1,
                requiredLevel = 30,
                isUnlocked = false
            });
            
            // Information
            availableItems.Add(new MarketItem()
            {
                id = "INFO_CORPDATA",
                name = "Corporate Database Access",
                description = "Login credentials for major corps",
                type = MarketItem.ItemType.Information,
                price = 2000,
                quantity = 1,
                requiredLevel = 18,
                isUnlocked = false
            });
            
            // Cosmetic
            availableItems.Add(new MarketItem()
            {
                id = "COSM_TERMINALSKIN",
                name = "Neon Terminal Skin",
                description = "Cosmetic skin for your terminal",
                type = MarketItem.ItemType.Cosmetic,
                price = 300,
                quantity = 999,
                requiredLevel = 1,
                isUnlocked = true
            });
        }
    }
    
    public bool PurchaseItem(string itemId, int quantity = 1)
    {
        MarketItem item = availableItems.Find(i => i.id == itemId);
        if (item == null || !item.isUnlocked || quantity <= 0)
            return false;
        
        // Check player level
        if (GameManager.Instance.playerLevel < item.requiredLevel)
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowNotification($"Required Level: {item.requiredLevel}");
            }
            return false;
        }
        
        // Check inventory space
        if (playerInventory.Count >= maxInventorySize)
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowNotification("Inventory full!");
            }
            return false;
        }
        
        // Check player funds
        int totalCost = item.price * quantity;
        if (GameManager.Instance.playerMoney < totalCost)
            return false;
        
        // Check item availability
        if (item.quantity < quantity && item.quantity != 999) // 999 = unlimited
            return false;
        
        // Check reputation requirement for expensive items
        if (totalCost > 5000 && marketReputation < 70)
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowNotification("Need higher market reputation!");
            }
            return false;
        }
        
        // Process purchase
        GameManager.Instance.AddMoney(-totalCost);
        
        if (item.quantity != 999)
            item.quantity -= quantity;
        
        // Add to inventory
        MarketItem purchasedItem = new MarketItem()
        {
            id = item.id,
            name = item.name,
            description = item.description,
            type = item.type,
            price = item.price,
            quantity = quantity,
            requiredLevel = item.requiredLevel,
            isUnlocked = true
        };
        
        playerInventory.Add(purchasedItem);
        
        // Record order
        RecordOrder(itemId, quantity, totalCost);
        
        // Update reputation
        marketReputation = Mathf.Min(100, marketReputation + 1);
        successfulTransactions++;
        
        // Apply item effects
        ApplyItemEffects(item);
        
        // Save and update UI
        SaveMarketData();
        UpdateMarketUI();
        
        // Show notification
        UIManager uiManager2 = FindObjectOfType<UIManager>();
        if (uiManager2 != null)
        {
            uiManager2.ShowNotification($"Purchased: {item.name} x{quantity}");
        }
        
        return true;
    }
    
    public bool SellItem(string itemId, int quantity = 1)
    {
        MarketItem item = playerInventory.Find(i => i.id == itemId);
        if (item == null || item.quantity < quantity)
            return false;
        
        // Calculate sell price (80% of purchase price)
        int sellPrice = Mathf.RoundToInt(item.price * 0.8f) * quantity;
        
        // Remove from inventory
        item.quantity -= quantity;
        if (item.quantity <= 0)
        {
            playerInventory.Remove(item);
        }
        
        // Add money
        GameManager.Instance.AddMoney(sellPrice);
        
        // Update reputation
        marketReputation = Mathf.Max(0, marketReputation - 1);
        
        // Save and update UI
        SaveMarketData();
        UpdateMarketUI();
        
        return true;
    }
    
    private void ApplyItemEffects(MarketItem item)
    {
        HackingManager hackingManager = FindObjectOfType<HackingManager>();
        if (hackingManager == null) return;
        
        switch (item.id)
        {
            case "TOOL_PORTSCANNER":
                hackingManager.hasPortScanner = true;
                break;
                
            case "TOOL_PASSWORDCRACKER":
                hackingManager.hasPasswordCracker = true;
                break;
                
            case "TOOL_FIREWALLBREACHER":
                hackingManager.hasFirewallBreacher = true;
                break;
                
            case "SOFT_VIRUS":
                // Unlock virus command
                break;
                
            case "COSM_TERMINALSKIN":
                // Apply cosmetic skin
                TerminalUI terminalUI = FindObjectOfType<TerminalUI>();
                if (terminalUI != null)
                {
                    terminalUI.ApplyNeonTheme();
                }
                break;
        }
    }
    
    public void ListItemForSale(MarketItem item, int price)
    {
        // Player can list items for sale (future feature)
        // This would add to a player marketplace
    }
    
    private void UpdateMarketPrices()
    {
        foreach (MarketItem item in availableItems)
        {
            // Random price fluctuation
            if (UnityEngine.Random.value < priceVolatility)
            {
                float change = UnityEngine.Random.Range(-0.2f, 0.3f); // -20% to +30%
                item.price = Mathf.RoundToInt(item.price * (1 + change));
                item.price = Mathf.Max(50, item.price); // Minimum price
            }
        }
        
        lastMarketUpdate = DateTime.Now;
    }
    
    private void GenerateLimitedOffers()
    {
        limitedTimeOffers.Clear();
        
        // Generate 3 random limited offers
        for (int i = 0; i < 3; i++)
        {
            if (availableItems.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableItems.Count);
                MarketItem original = availableItems[randomIndex];
                
                MarketItem limitedOffer = new MarketItem()
                {
                    id = original.id + "_LIMITED",
                    name = original.name + " (SALE)",
                    description = original.description + "\nLimited time offer!",
                    type = original.type,
                    price = Mathf.RoundToInt(original.price * 0.7f), // 30% off
                    quantity = 1,
                    requiredLevel = original.requiredLevel,
                    isUnlocked = original.isUnlocked,
                    isLimited = true,
                    restockTime = DateTime.Now.AddHours(6) // Available for 6 hours
                };
                
                limitedTimeOffers.Add(limitedOffer);
            }
        }
    }
    
    private void RecordOrder(string itemId, int quantity, int totalPrice)
    {
        MarketOrder order = new MarketOrder()
        {
            id = Guid.NewGuid().ToString(),
            orderDate = DateTime.Now,
            itemId = itemId,
            quantity = quantity,
            totalPrice = totalPrice,
            status = MarketOrder.OrderStatus.Processing
        };
        
        orderHistory.Add(order);
        
        // Simulate delivery after delay
        StartCoroutine(DeliverOrder(order));
    }
    
    private IEnumerator DeliverOrder(MarketOrder order)
    {
        // Simulate delivery time (5-30 seconds)
        float deliveryTime = UnityEngine.Random.Range(5f, 30f);
        yield return new WaitForSeconds(deliveryTime);
        
        order.status = MarketOrder.OrderStatus.Delivered;
        
        // 10% chance of "seized by authorities"
        if (UnityEngine.Random.value < 0.1f)
        {
            order.status = MarketOrder.OrderStatus.Cancelled;
            marketReputation = Mathf.Max(0, marketReputation - 10);
            failedTransactions++;
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowNotification("WARNING: Package seized by authorities!");
            }
        }
        
        SaveMarketData();
    }


    public List<MarketItem> availableItems = new List<MarketItem>();
    public void ShowMarket() {
        TerminalUI.Instance.AddMessage("=== MERCATO NERO ===");
        foreach(var item in availableItems) {
            TerminalUI.Instance.AddMessage($"{item.name} - {item.price} crediti");
        }
    }
    
    public void UpdateMarketUI()
    {
        if (reputationText != null)
        {
            reputationText.text = $"Reputation: {marketReputation}/100";
            reputationText.color = GetReputationColor(marketReputation);
        }
        
        if (balanceText != null)
        {
            balanceText.text = $"Balance: ${GameManager.Instance.playerMoney}";
        }
    }
    
    private Color GetReputationColor(int reputation)
    {
        if (reputation >= 80) return Color.green;
        if (reputation >= 60) return Color.yellow;
        if (reputation >= 40) return new Color(1, 0.5f, 0); // Orange
        return Color.red;
    }
    
    public List<MarketItem> GetAvailableItems()
    {
        List<MarketItem> items = new List<MarketItem>();
        
        foreach (var item in availableItems)
        {
            if (item.isUnlocked && item.quantity > 0)
            {
                items.Add(item);
            }
        }
        
        items.AddRange(limitedTimeOffers);
        
        return items;
    }
    
    public List<MarketItem> GetPlayerInventory()
    {
        return playerInventory;
    }
    
    public void UnlockItemByLevel(int playerLevel)
    {
        foreach (var item in availableItems)
        {
            if (!item.isUnlocked && playerLevel >= item.requiredLevel)
            {
                item.isUnlocked = true;
                
                UIManager uiManager = FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    uiManager.ShowNotification($"Unlocked: {item.name}");
                }
            }
        }
        
        SaveMarketData();
    }
    
    public void SaveMarketData()
    {
        PlayerPrefs.SetString("MarketData", JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }
    
    public void LoadMarketData()
    {
        if (PlayerPrefs.HasKey("MarketData"))
        {
            JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString("MarketData"), this);
        }
    }
    
    public void ResetMarketData()
    {
        playerInventory.Clear();
        orderHistory.Clear();
        marketReputation = 50;
        successfulTransactions = 0;
        failedTransactions = 0;
        
        foreach (var item in availableItems)
        {
            item.quantity = item.type == MarketItem.ItemType.Cosmetic ? 999 : 1;
        }
        
        SaveMarketData();
        UpdateMarketUI();
    }

}
