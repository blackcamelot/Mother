using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Mission
{
    public string id;
    public string title;
    public string description;
    public string targetIP;
    public int reward;
    public int difficulty;
    public bool isCompleted;
    public MissionType type;
    public List<MissionObjective> objectives;
    
    public enum MissionType
    {
        Hack,
        StealData,
        PlantVirus,
        TransferMoney,
        SocialEngineering,
        Surveillance
    }
}

[System.Serializable]
public class MissionObjective
{
    public string description;
    public bool isCompleted;
    public ObjectiveType type;
    
    public enum ObjectiveType
    {
        ConnectToTarget,
        CrackPassword,
        DownloadFile,
        UploadFile,
        DeleteFile,
        ScanPorts,
        BypassFirewall
    }
}

public class MissionManager : MonoBehaviour
{
    [Header("Mission Settings")]
    public List<Mission> availableMissions = new List<Mission>();
    public List<Mission> completedMissions = new List<Mission>();
    public int maxActiveMissions = 5;
    
    [Header("Mission Generation")]
    public string[] missionTitles = {
        "Corporate Espionage",
        "Bank Transfer",
        "Data Theft",
        "System Takeover",
        "Email Hack",
        "Database Breach",
        "Firewall Penetration",
        "Social Engineering",
        "Surveillance Operation",
        "Data Recovery"
    };
    
    public string[] missionDescriptions = {
        "Infiltrate {0} and steal financial documents",
        "Transfer ${1} from {0} to offshore account",
        "Download customer database from {0}",
        "Gain root access to {0} main server",
        "Compromise email server at {0}",
        "Extract sensitive data from {0}",
        "Bypass firewall of {0} security system",
        "Manipulate employee at {0} for access",
        "Monitor network traffic at {0}",
        "Recover deleted files from {0}"
    };
    
    private HackingManager hackingManager;
    private UIManager uiManager;
    
    private void Start()
    {
        hackingManager = FindObjectOfType<HackingManager>();
        uiManager = FindObjectOfType<UIManager>();
        
        GenerateNewMissions();
    }
    
    public void GenerateNewMissions()
    {
        availableMissions.Clear();
        
        int missionCount = Random.Range(3, 6);
        for(int i = 0; i < missionCount; i++)
        {
            Mission mission = new Mission()
            {
                id = $"MISSION_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
                title = missionTitles[Random.Range(0, missionTitles.Length)],
                description = string.Format(
                    missionDescriptions[Random.Range(0, missionDescriptions.Length)],
                    GenerateRandomCompany(),
                    Random.Range(1000, 10000)
                ),
                targetIP = GenerateRandomIP(),
                reward = Random.Range(500, 5000),
                difficulty = Random.Range(1, 10),
                type = (Mission.MissionType)Random.Range(0, 6),
                isCompleted = false,
                objectives = GenerateObjectives()
            };
            
            availableMissions.Add(mission);
        }
        
        if(uiManager != null)
        {
            uiManager.RefreshMissions();
        }
    }
    
    private List<MissionObjective> GenerateObjectives()
    {
        List<MissionObjective> objectives = new List<MissionObjective>();
        int objectiveCount = Random.Range(1, 4);
        
        for(int i = 0; i < objectiveCount; i++)
        {
            objectives.Add(new MissionObjective()
            {
                description = GetRandomObjectiveDescription(),
                isCompleted = false,
                type = (MissionObjective.ObjectiveType)Random.Range(0, 7)
            });
        }
        
        return objectives;
    }
    
    private string GetRandomObjectiveDescription()
    {
        string[] descriptions = {
            "Connect to target server",
            "Crack administrator password",
            "Download confidential files",
            "Upload backdoor program",
            "Delete system logs",
            "Scan all open ports",
            "Bypass firewall protection"
        };
        
        return descriptions[Random.Range(0, descriptions.Length)];
    }
    
    public void CompleteMission(Mission mission)
    {
        if(availableMissions.Contains(mission) && !mission.isCompleted)
        {
            mission.isCompleted = true;
            availableMissions.Remove(mission);
            completedMissions.Add(mission);
            
            GameManager.Instance.AddMoney(mission.reward);
            GameManager.Instance.playerReputation += mission.difficulty;
            
            if(uiManager != null)
            {
                uiManager.ShowNotification($"Mission completed! +${mission.reward}");
                uiManager.RefreshMissions();
            }
            
            CheckForNewMissionUnlocks();
        }
    }
    
    private void CheckForNewMissionUnlocks()
    {
        int completedCount = completedMissions.Count;
        
        if(completedCount % 3 == 0 && completedCount > 0)
        {
            GenerateNewMissions();
            
            if(uiManager != null)
            {
                uiManager.ShowNotification("New missions available!");
            }
        }
    }
    
    public void AcceptMission(Mission mission)
    {
        Debug.Log($"Mission accepted: {mission.title}");
        
        if(uiManager != null)
        {
            uiManager.ShowNotification($"Mission '{mission.title}' accepted!");
        }
    }
    
    public void UpdateMissionProgress(string targetIP, MissionObjective.ObjectiveType objectiveType)
    {
        foreach(Mission mission in availableMissions)
        {
            if(mission.targetIP == targetIP && !mission.isCompleted)
            {
                foreach(MissionObjective objective in mission.objectives)
                {
                    if(objective.type == objectiveType && !objective.isCompleted)
                    {
                        objective.isCompleted = true;
                        CheckMissionCompletion(mission);
                        return;
                    }
                }
            }
        }
    }
    
    private void CheckMissionCompletion(Mission mission)
    {
        bool allObjectivesComplete = true;
        
        foreach(MissionObjective objective in mission.objectives)
        {
            if(!objective.isCompleted)
            {
                allObjectivesComplete = false;
                break;
            }
        }
        
        if(allObjectivesComplete)
        {
            CompleteMission(mission);
        }
    }
    
    private string GenerateRandomCompany()
    {
        string[] companies = {
            "TechCorp Inc.",
            "Global Bank",
            "CyberSec Solutions",
            "DataFlow Systems",
            "SecureNet Ltd.",
            "Quantum Computing Inc.",
            "Neural Networks Corp",
            "Digital Fortress",
            "OmniCorp",
            "Apex Technologies"
        };
        
        return companies[Random.Range(0, companies.Length)];
    }
    
    private string GenerateRandomIP()
    {
        return $"{Random.Range(10, 255)}.{Random.Range(0, 255)}.{Random.Range(0, 255)}.{Random.Range(1, 255)}";
    }
    
    public List<Mission> GetAvailableMissions()
    {
        return availableMissions;
    }
    
    public List<Mission> GetCompletedMissions()
    {
        return completedMissions;
    }
    
    public int GetTotalEarnings()
    {
        int total = 0;
        foreach(Mission mission in completedMissions)
        {
            total += mission.reward;
        }
        return total;
    }
}