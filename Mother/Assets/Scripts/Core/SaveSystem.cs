using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SaveSystem : MonoBehaviour
{
    private static string savePath => Application.persistentDataPath + "/motherhacking.save";
    
    [System.Serializable]
    public class SaveData
    {
        public int currentDay;
        public float gameTime;
        public int playerMoney;
        public int playerCredits;
        public int playerReputation;
        public int hackingSkill;
        public int programmingSkill;
        public int socialEngineeringSkill;
        public List<string> completedMissions = new List<string>();
    }
    
    public static void SaveGame()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("Cannot save: GameManager not found!");
            return;
        }

        SaveData data = new SaveData();
        data.currentDay = gm.currentDay;
        data.gameTime = gm.gameTime;
        data.playerMoney = gm.playerMoney;
        data.playerCredits = gm.playerCredits;
        data.playerReputation = gm.playerReputation;
        data.hackingSkill = gm.hackingSkill;
        data.programmingSkill = gm.programmingSkill;
        data.socialEngineeringSkill = gm.socialEngineeringSkill;

        // Nota: MissionManager.Instance.GetCompletedMissions() non esiste nel codice corrente.
        // Dovrai implementarlo o rimuovere questa riga.
        // data.completedMissions = MissionManager.Instance.GetCompletedMissions();

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Game saved to: {savePath}");
    }
    
    public static void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogError("Save file not found!");
            return;
        }
        
        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("Cannot load: GameManager not found!");
            return;
        }
        
        gm.currentDay = data.currentDay;
        gm.gameTime = data.gameTime;
        gm.playerMoney = data.playerMoney;
        gm.playerCredits = data.playerCredits;
        gm.playerReputation = data.playerReputation;
        gm.hackingSkill = data.hackingSkill;
        gm.programmingSkill = data.programmingSkill;
        gm.socialEngineeringSkill = data.socialEngineeringSkill;
        
        // Nota: Dovrai anche caricare le missioni completate nel MissionManager.
        // MissionManager.Instance?.LoadCompletedMissions(data.completedMissions);
        
        gm.uiManager?.UpdateAllDisplays();
        Debug.Log("Game loaded successfully.");
    }

    // Metodo di utilità per cancellare il salvataggio (opzionale)
    public static void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Save file deleted.");
        }
    }
}