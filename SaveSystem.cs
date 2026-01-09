using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    private static string savePath = Application.persistentDataPath + "/motherhacking.save";
    
    [System.Serializable]
    public class SaveData
    {
        public int currentDay;
        public float gameTime;
        public int playerMoney;
        public int playerReputation;
        public int hackingSkill;
        public int programmingSkill;
        public int socialEngineeringSkill;
    }
    
    public static void SaveGame()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath, FileMode.Create);
        
        SaveData data = new SaveData();
        GameManager gm = GameManager.Instance;
        
        data.currentDay = gm.currentDay;
        data.gameTime = gm.gameTime;
        data.playerMoney = gm.playerMoney;
        data.playerReputation = gm.playerReputation;
        data.hackingSkill = gm.hackingSkill;
        data.programmingSkill = gm.programmingSkill;
        data.socialEngineeringSkill = gm.socialEngineeringSkill;
        
        formatter.Serialize(stream, data);
        stream.Close();
    }
    
    public static void LoadGame()
    {
        if(File.Exists(savePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(savePath, FileMode.Open);
            
            SaveData data = formatter.Deserialize(stream) as SaveData;
            stream.Close();
            
            GameManager gm = GameManager.Instance;
            
            gm.currentDay = data.currentDay;
            gm.gameTime = data.gameTime;
            gm.playerMoney = data.playerMoney;
            gm.playerReputation = data.playerReputation;
            gm.hackingSkill = data.hackingSkill;
            gm.programmingSkill = data.programmingSkill;
            gm.socialEngineeringSkill = data.socialEngineeringSkill;
            
            gm.uiManager.UpdateAllDisplays();
        }
        else
        {
            Debug.LogError("Save file not found!");
        }
    }
}