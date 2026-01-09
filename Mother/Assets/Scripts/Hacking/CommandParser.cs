using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

switch(command) {
    case "missions": MissionManager.Instance.ListMissions(); break;
    case "start": MissionManager.Instance.StartMission(parts[1]); break;
    case "solve": HackingManager.Instance.AttemptHack(parts[1]); break;
    case "credits": EconomyUI.Instance.ShowCredits(); break;
    case "market": BlackMarket.Instance.ShowMarket(); break;
    case "save": SaveSystem.Instance.SaveGame(); break;
    case "load": SaveSystem.Instance.LoadGame(); break;
    case "clear": TerminalUI.Instance.ClearConsole(); break;
    case "help": ShowHelp(); break;
}

public class CommandParser : MonoBehaviour
{
    [System.Serializable]
    public class Command
    {
        public string name;
        public string description;
        public string syntax;
        public List<string> aliases = new List<string>();
        public CommandCategory category;
        public int requiredSkillLevel = 0;
        public bool requiresNetworkConnection = false;
        
        public enum CommandCategory
        {
            System,
            Network,
            File,
            Hacking,
            Utility,
            Social
        }
    }
    
    [Header("Command Database")]
    public List<Command> commandDatabase = new List<Command>();
    
    [Header("Auto-complete")]
    public bool enableAutoComplete = true;
    public int maxSuggestions = 5;
    
    private Dictionary<string, Command> commandDictionary = new Dictionary<string, Command>();
    private HackingManager hackingManager;
    private TerminalController terminal;
    
    private void Start()
    {
        hackingManager = FindObjectOfType<HackingManager>();
        terminal = FindObjectOfType<TerminalController>();
        
        InitializeCommandDatabase();
        BuildCommandDictionary();
    }
    
    private void InitializeCommandDatabase()
    {
        if(commandDatabase.Count == 0)
        {
            commandDatabase.Add(new Command()
            {
                name = "help",
                description = "Display available commands",
                syntax = "help [command]",
                aliases = new List<string>{"?", "man"},
                category = Command.CommandCategory.System
            });
            
            commandDatabase.Add(new Command()
            {
                name = "clear",
                description = "Clear terminal screen",
                syntax = "clear",
                aliases = new List<string>{"cls"},
                category = Command.CommandCategory.System
            });
            
            commandDatabase.Add(new Command()
            {
                name = "whoami",
                description = "Display current user information",
                syntax = "whoami",
                category = Command.CommandCategory.System
            });
            
            commandDatabase.Add(new Command()
            {
                name = "ls",
                description = "List directory contents",
                syntax = "ls [path]",
                aliases = new List<string>{"dir", "list"},
                category = Command.CommandCategory.File
            });
            
            commandDatabase.Add(new Command()
            {
                name = "cat",
                description = "Display file contents",
                syntax = "cat <filename>",
                aliases = new List<string>{"type", "read"},
                category = Command.CommandCategory.File
            });
            
            commandDatabase.Add(new Command()
            {
                name = "connect",
                description = "Connect to remote host",
                syntax = "connect <ip/hostname>",
                aliases = new List<string>{"ssh", "telnet"},
                category = Command.CommandCategory.Network,
                requiresNetworkConnection = true
            });
            
            commandDatabase.Add(new Command()
            {
                name = "scan",
                description = "Scan network for hosts",
                syntax = "scan [options]",
                aliases = new List<string>{"nmap", "netscan"},
                category = Command.CommandCategory.Network,
                requiredSkillLevel = 1,
                requiresNetworkConnection = true
            });
            
            commandDatabase.Add(new Command()
            {
                name = "crack",
                description = "Attempt to crack password",
                syntax = "crack <target> [method]",
                aliases = new List<string>{"bruteforce"},
                category = Command.CommandCategory.Hacking,
                requiredSkillLevel = 3
            });
            
            commandDatabase.Add(new Command()
            {
                name = "portscan",
                description = "Scan for open ports",
                syntax = "portscan <ip>",
                aliases = new List<string>{"pscan"},
                category = Command.CommandCategory.Hacking,
                requiredSkillLevel = 2
            });
            
            commandDatabase.Add(new Command()
            {
                name = "echo",
                description = "Display message",
                syntax = "echo <message>",
                category = Command.CommandCategory.Utility
            });
        }
    }
    
    private void BuildCommandDictionary()
    {
        foreach(Command cmd in commandDatabase)
        {
            commandDictionary[cmd.name.ToLower()] = cmd;
            
            foreach(string alias in cmd.aliases)
            {
                commandDictionary[alias.ToLower()] = cmd;
            }
        }
    }
    
    public ParsedCommand ParseCommand(string input)
    {
        ParsedCommand parsed = new ParsedCommand();
        
        if(string.IsNullOrWhiteSpace(input))
        {
            parsed.error = "Empty command";
            return parsed;
        }
        
        string[] parts = input.Split(' ');
        parsed.rawInput = input;
        parsed.command = parts[0].ToLower();
        parsed.arguments = new List<string>();
        
        for(int i = 1; i < parts.Length; i++)
        {
            if(!string.IsNullOrWhiteSpace(parts[i]))
            {
                parsed.arguments.Add(parts[i]);
            }
        }
        
        if(commandDictionary.ContainsKey(parsed.command))
        {
            parsed.commandInfo = commandDictionary[parsed.command];
        }
        else
        {
            parsed.error = $"Command not found: {parsed.command}";
            parsed.suggestions = GetCommandSuggestions(parsed.command);
        }
        
        if(parsed.commandInfo != null)
        {
            if(parsed.commandInfo.requiredSkillLevel > 0)
            {
                int playerSkill = GameManager.Instance.hackingSkill;
                if(playerSkill < parsed.commandInfo.requiredSkillLevel)
                {
                    parsed.error = $"Insufficient skill. Required: {parsed.commandInfo.requiredSkillLevel}, Your level: {playerSkill}";
                }
            }
            
            if(parsed.commandInfo.requiresNetworkConnection)
            {
                if(hackingManager.currentNode == hackingManager.homeComputer)
                {
                    parsed.error = "Must be connected to remote host";
                }
            }
        }
        
        return parsed;
    }
    
    public List<string> GetCommandSuggestions(string partialCommand)
    {
        if(!enableAutoComplete || string.IsNullOrWhiteSpace(partialCommand))
            return new List<string>();
        
        List<string> suggestions = new List<string>();
        
        foreach(var kvp in commandDictionary)
        {
            if(kvp.Key.StartsWith(partialCommand.ToLower()) && 
               !suggestions.Contains(kvp.Value.name))
            {
                suggestions.Add(kvp.Value.name);
                
                if(suggestions.Count >= maxSuggestions)
                    break;
            }
        }
        
        return suggestions;
    }
}

[System.Serializable]
public class ParsedCommand
{
    public string rawInput;
    public string command;
    public List<string> arguments;
    public CommandParser.Command commandInfo;
    public string error;
    public List<string> suggestions;
    
    public bool IsValid()
    {
        return commandInfo != null && string.IsNullOrEmpty(error);
    }
    
    public string GetArgument(int index, string defaultValue = "")
    {
        if(index >= 0 && index < arguments.Count)
            return arguments[index];
            
        return defaultValue;
    }
    
    public bool HasArgument(string arg)
    {
        return arguments.Contains(arg);
    }

}
