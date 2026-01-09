using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    public float gameTime = 9.0f;
    public float timeScale = 60f;
    public int playerMoney = 0;
    public int playerCredits = 1000;
    public int playerReputation = 0;
    public int currentDay = 1;
    
    [Header("Player Skills")]
    public int hackingSkill = 1;
    public int programmingSkill = 1;
    public int socialEngineeringSkill = 1;
    
    [Header("References")]
    public HackingManager hackingManager;
    public UIManager uiManager;
    public MissionManager missionManager;
    public DialogueSystem dialogueSystem;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    private void Start()
    {
        FindReferences();
        Debug.Log($"Starting Day {currentDay}");
        
        if (uiManager != null)
        {
            uiManager.ShowNotification($"Day {currentDay} - New missions available!");
        }
        // Nota: StartNewDay() viene chiamato da Update() quando gameTime >= 24
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindReferences();
    }
    
    private void FindReferences()
    {
        hackingManager = FindObjectOfType<HackingManager>();
        uiManager = FindObjectOfType<UIManager>();
        missionManager = FindObjectOfType<MissionManager>();
        dialogueSystem = FindObjectOfType<DialogueSystem>();
    }
    
    private void Update()
    {
        if (hackingManager != null)
        {
            gameTime += Time.deltaTime / 60f * timeScale;
            
            if (gameTime >= 24f)
            {
                gameTime = 0f;
                currentDay++;
                StartNewDay();
            }
        }
    }
    
    private void StartNewDay()
    {
        Debug.Log($"Starting Day {currentDay}");
        
        if (missionManager != null)
        {
            missionManager.GenerateNewMissions();
        }
        
        if (uiManager != null)
        {
            uiManager.ShowNotification($"Day {currentDay} - New missions available!");
        }
    }
    
    public void AddMoney(int amount)
    {
        playerMoney += amount;
        
        if (uiManager != null)
        {
            uiManager.UpdateMoneyDisplay(playerMoney);
        }
    }

    public void AddCredits(int amount)
    {
        playerCredits += amount;
        // Nota: EconomyUI.Instance non esiste nel codice corrente.
        // Dovrai collegare l'UI dei crediti tramite UIManager o un riferimento diretto.
        // Esempio: if(uiManager != null) uiManager.UpdateCreditDisplay(playerCredits);
    }
    
    public void IncreaseSkill(string skill, int amount = 1)
    {
        switch(skill.ToLower())
        {
            case "hacking":
                hackingSkill += amount;
                break;
            case "programming":
                programmingSkill += amount;
                break;
            case "social":
                socialEngineeringSkill += amount;
                break;
        }
    }
    
    public void ClearNullReferences()
    {
        if (hackingManager == null) hackingManager = FindObjectOfType<HackingManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (missionManager == null) missionManager = FindObjectOfType<MissionManager>();
        if (dialogueSystem == null) dialogueSystem = FindObjectOfType<DialogueSystem>();
    }
}