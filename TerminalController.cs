using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TerminalController : MonoBehaviour
{
    [Header("UI References")]
    public InputField commandInput;
    public Text terminalOutput;
    public ScrollRect scrollRect;
    
    [Header("Terminal Settings")]
    public int maxLines = 100;
    public Color outputColor = Color.green;
    public Color errorColor = Color.red;
    public Color inputColor = Color.cyan;
    
    private List<string> commandHistory = new List<string>();
    private int historyIndex = 0;
    private HackingManager hackingManager;
    
    private void Start()
    {
        hackingManager = FindObjectOfType<HackingManager>();
        commandInput.onEndEdit.AddListener(OnCommandEntered);
        
        PrintLine("Mother Hacking Simulation v1.0");
        PrintLine("Type 'help' for available commands");
        PrintLine("");
    }
    
    public void OnCommandEntered(string command)
    {
        if(string.IsNullOrEmpty(command) || string.IsNullOrWhiteSpace(command))
            return;
            
        PrintLine($"> {command}", inputColor);
        hackingManager.ExecuteCommand(command);
        
        commandHistory.Add(command);
        historyIndex = commandHistory.Count;
        
        commandInput.text = "";
        commandInput.ActivateInputField();
        
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
    
    public void PrintLine(string text, Color? color = null)
    {
        Color textColor = color ?? outputColor;
        
        string coloredText = $"<color=#{ColorUtility.ToHtmlStringRGB(textColor)}>{text}</color>";
        
        if(terminalOutput.text.Split('\n').Length >= maxLines)
        {
            string[] lines = terminalOutput.text.Split('\n');
            terminalOutput.text = string.Join("\n", lines, 1, lines.Length - 1);
        }
        
        terminalOutput.text += coloredText + "\n";
    }
    
    public void ClearTerminal()
    {
        terminalOutput.text = "";
    }
    
    public void UpdatePrompt(string prompt)
    {
        // Prompt display update if needed
    }
    
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.UpArrow))
        {
            if(commandHistory.Count > 0 && historyIndex > 0)
            {
                historyIndex--;
                commandInput.text = commandHistory[historyIndex];
                commandInput.caretPosition = commandInput.text.Length;
            }
        }
        else if(Input.GetKeyDown(KeyCode.DownArrow))
        {
            if(historyIndex < commandHistory.Count - 1)
            {
                historyIndex++;
                commandInput.text = commandHistory[historyIndex];
            }
            else
            {
                historyIndex = commandHistory.Count;
                commandInput.text = "";
            }
        }
    }
}