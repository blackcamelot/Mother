using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(3, 10)]
    public string text;
    public float displayTime = 3f;
    public DialogueEvent dialogueEvent;
}

[System.Serializable]
public class DialogueEvent
{
    public enum EventType
    {
        None,
        AddMoney,
        AddReputation,
        UnlockTool,
        TriggerMission,
        ChangeBackground
    }
    
    public EventType eventType;
    public int intValue;
    public string stringValue;
    public bool boolValue;
}

public class DialogueSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public Text speakerText;
    public Text dialogueText;
    public Image speakerIcon;
    public Button continueButton;
    
    [Header("Settings")]
    public float textSpeed = 0.05f;
    public bool autoAdvance = true;
    
    [Header("Audio")]
    public AudioClip typingSound;
    public AudioClip dialogueOpenSound;
    public AudioClip dialogueCloseSound;
    
    [Header("Speaker Icons")]
    public List<SpeakerIcon> iconList = new List<SpeakerIcon>();
    
    [System.Serializable]
    public class SpeakerIcon
    {
        public string speakerName;
        public Sprite icon;
    }
    
    private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
    private Coroutine typingCoroutine;
    private AudioSource audioSource;
    private bool isDialogueActive = false;
    private Dictionary<string, Sprite> speakerIcons = new Dictionary<string, Sprite>();
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if(audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        continueButton.onClick.AddListener(ContinueDialogue);
        dialoguePanel.SetActive(false);
        
        foreach(var icon in iconList)
        {
            speakerIcons[icon.speakerName] = icon.icon;
        }
    }
    
    public void StartDialogue(List<DialogueLine> dialogueLines)
    {
        if(dialogueLines == null || dialogueLines.Count == 0)
            return;
            
        dialogueQueue.Clear();
        
        foreach(var line in dialogueLines)
        {
            dialogueQueue.Enqueue(line);
        }
        
        dialoguePanel.SetActive(true);
        isDialogueActive = true;
        continueButton.gameObject.SetActive(!autoAdvance);
        
        PlaySound(dialogueOpenSound);
        DisplayNextLine();
    }
    
    public void StartDialogue(DialogueLine singleLine)
    {
        List<DialogueLine> lines = new List<DialogueLine> { singleLine };
        StartDialogue(lines);
    }
    
    private void DisplayNextLine()
    {
        if(dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }
        
        DialogueLine currentLine = dialogueQueue.Dequeue();
        
        speakerText.text = currentLine.speaker;
        
        if(speakerIcons.ContainsKey(currentLine.speaker))
        {
            speakerIcon.sprite = speakerIcons[currentLine.speaker];
            speakerIcon.gameObject.SetActive(true);
        }
        else
        {
            speakerIcon.gameObject.SetActive(false);
        }
        
        if(typingCoroutine != null)
            StopCoroutine(typingCoroutine);
            
        typingCoroutine = StartCoroutine(TypeText(currentLine.text));
        
        if(currentLine.dialogueEvent != null)
        {
            ProcessDialogueEvent(currentLine.dialogueEvent);
        }
        
        if(autoAdvance)
        {
            Invoke("DisplayNextLine", currentLine.displayTime);
        }
    }
    
    private IEnumerator TypeText(string text)
    {
        dialogueText.text = "";
        
        foreach(char c in text.ToCharArray())
        {
            dialogueText.text += c;
            
            if(typingSound != null && c != ' ')
            {
                audioSource.PlayOneShot(typingSound, 0.1f);
            }
            
            yield return new WaitForSeconds(textSpeed);
        }
        
        typingCoroutine = null;
    }
    
    public void ContinueDialogue()
    {
        if(typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            if(dialogueQueue.Count > 0)
                dialogueText.text = dialogueQueue.Peek().text;
        }
        else
        {
            DisplayNextLine();
        }
    }
    
    private void ProcessDialogueEvent(DialogueEvent dialogueEvent)
    {
        if(dialogueEvent == null) return;
        
        switch(dialogueEvent.eventType)
        {
            case DialogueEvent.EventType.AddMoney:
                GameManager.Instance.AddMoney(dialogueEvent.intValue);
                break;
                
            case DialogueEvent.EventType.AddReputation:
                GameManager.Instance.playerReputation += dialogueEvent.intValue;
                GameManager.Instance.uiManager.UpdateAllDisplays();
                break;
                
            case DialogueEvent.EventType.UnlockTool:
                UnlockHackingTool(dialogueEvent.stringValue);
                break;
                
            case DialogueEvent.EventType.TriggerMission:
                TriggerMission(dialogueEvent.stringValue);
                break;
        }
    }
    
    private void UnlockHackingTool(string toolName)
    {
        HackingManager hackingManager = FindObjectOfType<HackingManager>();
        if(hackingManager == null) return;
        
        switch(toolName.ToLower())
        {
            case "portscanner":
                hackingManager.hasPortScanner = true;
                break;
            case "passwordcracker":
                hackingManager.hasPasswordCracker = true;
                break;
            case "firewallbreacher":
                hackingManager.hasFirewallBreacher = true;
                break;
        }
        
        if(GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.ShowNotification($"Unlocked: {toolName}");
        }
    }
    
    private void TriggerMission(string missionId)
    {
        Debug.Log($"Triggering mission: {missionId}");
    }
    
    private void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        PlaySound(dialogueCloseSound);
    }
    
    private void PlaySound(AudioClip clip)
    {
        if(clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
    
    public void ShowMessage(string speaker, string message, float displayTime = 3f)
    {
        DialogueLine line = new DialogueLine()
        {
            speaker = speaker,
            text = message,
            displayTime = displayTime
        };
        
        StartDialogue(line);
    }
    
    public void ShowTutorialMessage(string message)
    {
        ShowMessage("TUTORIAL", message, 4f);
    }
    
    public void ShowSystemMessage(string message)
    {
        ShowMessage("SYSTEM", message, 3f);
    }
    
    public void ShowHackerMessage(string message)
    {
        ShowMessage("ANONYMOUS", message, 3f);
    }
}