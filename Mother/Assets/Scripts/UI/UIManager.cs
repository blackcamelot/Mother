using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Main UI Panels")]
    public GameObject terminalPanel;
    public GameObject missionPanel;
    public GameObject inventoryPanel;
    public GameObject skillsPanel;
    public GameObject fileSystemPanel;
    
    [Header("Status Display")]
    public Text moneyText;
    public Text dayText;
    public Text timeText;
    public Text reputationText;
    public Text skillHackingText;
    public Text skillProgrammingText;
    public Text skillSocialText;
    
    [Header("Notifications")]
    public GameObject notificationPanel;
    public Text notificationText;
    public float notificationDuration = 3f;
    
    [Header("Terminal References")]
    public InputField terminalInput;
    public Button terminalSendButton;
    
    [Header("Mission UI")]
    public Transform missionContent;
    public GameObject missionPrefab;
    
    [Header("File System UI")]
    public Text currentPathText;
    public Text directoryContentsText;
    public InputField fileSystemInput;
    
    private GameManager gameManager;
    private HackingManager hackingManager;
    private MissionManager missionManager;
    private FileSystem fileSystem;
    
    private void Start()
    {
        gameManager = GameManager.Instance;
        hackingManager = FindObjectOfType<HackingManager>();
        missionManager = FindObjectOfType<MissionManager>();
        fileSystem = FindObjectOfType<FileSystem>();
        
        // Verifica che tutti i componenti necessari esistano
        if (gameManager == null)
            Debug.LogError("GameManager non trovato!");
        if (hackingManager == null)
            Debug.LogWarning("HackingManager non trovato, alcune funzionalità saranno limitate");
            
        InitializeUI();
        UpdateAllDisplays();
    }
    
    private void InitializeUI()
    {
        // Set initial panel states
        terminalPanel.SetActive(true);
        missionPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        skillsPanel.SetActive(false);
        fileSystemPanel.SetActive(false);
        notificationPanel.SetActive(false);
        
        // Setup button listeners
        if (terminalSendButton != null && terminalInput != null)
        {
            terminalSendButton.onClick.AddListener(() => 
            {
                if (!string.IsNullOrEmpty(terminalInput.text))
                {
                    hackingManager.ExecuteCommand(terminalInput.text);
                    terminalInput.text = "";
                    terminalInput.ActivateInputField();
                }
            });
        }
    }
    
    private void Update()
    {
        if (gameManager != null)
        {
            UpdateTimeDisplay();
        }
    }
    
    public void UpdateAllDisplays()
    {
        UpdateMoneyDisplay(gameManager.playerMoney);
        UpdateDayDisplay(gameManager.currentDay);
        UpdateReputationDisplay(gameManager.playerReputation);
        UpdateSkillsDisplay();
    }
    
    public void UpdateMoneyDisplay(int money)
    {
        if (moneyText != null)
            moneyText.text = $"$ {money}";
    }
    
    private void UpdateDayDisplay(int day)
    {
        if (dayText != null)
            dayText.text = $"Day: {day}";
    }
    
    private void UpdateTimeDisplay()
    {
        if (timeText != null)
        {
            int hours = Mathf.FloorToInt(gameManager.gameTime);
            int minutes = Mathf.FloorToInt((gameManager.gameTime - hours) * 60);
            timeText.text = $"{hours:00}:{minutes:00}";
        }
    }
    
    private void UpdateReputationDisplay(int reputation)
    {
        if (reputationText != null)
            reputationText.text = $"Rep: {reputation}";
    }
    
    private void UpdateSkillsDisplay()
    {
        if (skillHackingText != null)
            skillHackingText.text = $"Hacking: {gameManager.hackingSkill}";
        if (skillProgrammingText != null)
            skillProgrammingText.text = $"Programming: {gameManager.programmingSkill}";
        if (skillSocialText != null)
            skillSocialText.text = $"Social: {gameManager.socialEngineeringSkill}";
    }
    
    public void ShowNotification(string message)
    {
        if (notificationPanel == null || notificationText == null)
            return;
            
        notificationText.text = message;
        notificationPanel.SetActive(true);
        
        StopAllCoroutines();
        StartCoroutine(HideNotificationAfterDelay());
    }
    
    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        notificationPanel.SetActive(false);
    }
    
    // UI Button Functions
    public void ToggleTerminal()
    {
        if (terminalPanel != null)
        {
            bool newState = !terminalPanel.activeSelf;
            terminalPanel.SetActive(newState);
            
            if (newState && terminalInput != null)
            {
                terminalInput.ActivateInputField();
            }
        }
    }
    
    public void ToggleMissions()
    {
        if (missionPanel != null)
        {
            missionPanel.SetActive(!missionPanel.activeSelf);
            
            if (missionPanel.activeSelf && missionManager != null)
            {
                UpdateMissionsDisplay();
            }
        }
    }
    
    public void ToggleInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }
    
    public void ToggleSkills()
    {
        if (skillsPanel != null)
        {
            skillsPanel.SetActive(!skillsPanel.activeSelf);
            
            if (skillsPanel.activeSelf)
            {
                UpdateSkillsDisplay();
            }
        }
    }
    
    public void ToggleFileSystem()
    {
        if (fileSystemPanel != null)
        {
            fileSystemPanel.SetActive(!fileSystemPanel.activeSelf);
            
            if (fileSystemPanel.activeSelf && fileSystem != null)
            {
                UpdateFileSystemDisplay();
            }
        }
    }
    
    private List<MissionUI> missionPool = new List<MissionUI>();
    private void UpdateMissionsDisplay()
    {
        if (missionContent == null || missionPrefab == null || missionManager == null)
            return;
            
        // Disabilita tutte le missioni esistenti
        foreach (Transform child in missionContent)
        {
            child.gameObject.SetActive(false);
        }
        
        // Riutilizza o crea nuove missioni UI
        for (int i = 0; i < missionManager.availableMissions.Count; i++)
        {
            MissionUI missionUI;
            if (i < missionContent.childCount)
            {
                missionUI = missionContent.GetChild(i).GetComponent<MissionUI>();
                missionUI.gameObject.SetActive(true);
            }
            else
            {
                GameObject missionObj = Instantiate(missionPrefab, missionContent);
                missionUI = missionObj.GetComponent<MissionUI>();
            }
            
            if (missionUI != null)
            {
                missionUI.Setup(missionManager.availableMissions[i], this);
            }
        }
    }
    
    private void UpdateFileSystemDisplay()
    {
        if (fileSystem == null || currentPathText == null || directoryContentsText == null)
            return;
            
        currentPathText.text = $"Path: {fileSystem.GetCurrentPath()}";
        directoryContentsText.text = fileSystem.ListDirectoryContents();
    }
    
    public void ExecuteFileSystemCommand()
    {
        if (fileSystemInput == null || fileSystem == null || string.IsNullOrEmpty(fileSystemInput.text))
            return;
            
        string command = fileSystemInput.text.Trim();
        fileSystemInput.text = "";
        
        // Simple command parsing for file system
        if (command.StartsWith("cd "))
        {
            string path = command.Substring(3);
            string result = fileSystem.ChangeDirectory(path);
            ShowNotification(result);
        }
        else if (command.StartsWith("cat ") || command.StartsWith("read "))
        {
            string filename = command.Contains(" ") ? command.Substring(command.IndexOf(' ') + 1) : "";
            string content = fileSystem.ReadFile(filename);
            directoryContentsText.text = content;
        }
        else if (command == "ls" || command == "dir")
        {
            directoryContentsText.text = fileSystem.ListDirectoryContents();
        }
        else
        {
            ShowNotification($"Unknown command: {command}");
        }
        
        UpdateFileSystemDisplay();
    }
    
    public void SaveGame()
    {
        SaveSystem.SaveGame();
        ShowNotification("Game saved!");
    }
    
    public void LoadGame()
    {
        SaveSystem.LoadGame();
        ShowNotification("Game loaded!");
        UpdateAllDisplays();
    }
    
    public void QuickSave()
    {
        SaveSystem.SaveGame();
        ShowNotification("Quick save complete!");
    }
    
    public void QuickLoad()
    {
        SaveSystem.LoadGame();
        ShowNotification("Quick load complete!");
        UpdateAllDisplays();
    }
    
    // Public method to update mission display from external scripts
    public void RefreshMissions()
    {
        if (missionPanel != null && missionPanel.activeSelf)
        {
            UpdateMissionsDisplay();
        }
    }
    
    // Method to handle file system navigation
    public void NavigateFileSystem(string path)
    {
        if (fileSystem != null)
        {
            string result = fileSystem.ChangeDirectory(path);
            ShowNotification(result);
            UpdateFileSystemDisplay();
        }
    }
}

// Mission UI component for mission panel
public class MissionUI : MonoBehaviour
{
    public Text missionTitle;
    public Text missionDescription;
    public Text missionReward;
    public Button acceptButton;
    
    private Mission mission;
    private UIManager uiManager;
    
    public void Setup(Mission missionData, UIManager manager)
    {
        mission = missionData;
        uiManager = manager;
        
        missionTitle.text = mission.title;
        missionDescription.text = mission.description;
        missionReward.text = $"Reward: ${mission.reward}";
        
        acceptButton.onClick.AddListener(AcceptMission);
    }
    
    private void AcceptMission()
    {
        // Implementation depends on your mission system
        Debug.Log($"Mission accepted: {mission.title}");
        uiManager.ShowNotification($"Mission '{mission.title}' accepted!");
        
        // You would typically move this to the mission manager
        Destroy(gameObject);
    }
}