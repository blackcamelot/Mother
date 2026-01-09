using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public interface ICommandHandler
{
    string CommandName { get; }
    string Description { get; }
    string Syntax { get; }
    string[] Aliases { get; }
    CommandCategory Category { get; }
    int RequiredSkill { get; }
    bool RequiresConnection { get; }
    
    CommandResult Execute(ParsedCommand command);
    ValidationResult Validate(ParsedCommand command);
}

public enum CommandCategory
{
    System,
    Network,
    File,
    Hacking,
    Utility,
    Social,
    Mission,
    Economy
}

public struct CommandResult
{
    public bool Success;
    public string Message;
    public object Data;
    public Color? ColorOverride;
    
    public static CommandResult SuccessResult(string message, object data = null) => 
        new CommandResult { Success = true, Message = message, Data = data };
    
    public static CommandResult ErrorResult(string message) => 
        new CommandResult { Success = false, Message = message };
}

public struct ValidationResult
{
    public bool IsValid;
    public string ErrorMessage;
    
    public static ValidationResult Valid => new ValidationResult { IsValid = true };
    public static ValidationResult Invalid(string error) => 
        new ValidationResult { IsValid = false, ErrorMessage = error };
}

[System.Serializable]
public class CommandRegistry
{
    private Dictionary<string, ICommandHandler> _handlers = 
        new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<CommandCategory, List<ICommandHandler>> _categorizedHandlers = 
        new Dictionary<CommandCategory, List<ICommandHandler>>();
    
    public void RegisterHandler(ICommandHandler handler)
    {
        if (handler == null) return;
        
        _handlers[handler.CommandName] = handler;
        
        foreach (var alias in handler.Aliases ?? Array.Empty<string>())
        {
            if (!string.IsNullOrEmpty(alias))
                _handlers[alias] = handler;
        }
        
        if (!_categorizedHandlers.ContainsKey(handler.Category))
            _categorizedHandlers[handler.Category] = new List<ICommandHandler>();
        
        _categorizedHandlers[handler.Category].Add(handler);
    }
    
    public bool TryGetHandler(string command, out ICommandHandler handler) => 
        _handlers.TryGetValue(command, out handler);
    
    public IEnumerable<ICommandHandler> GetHandlersByCategory(CommandCategory category)
    {
        if (_categorizedHandlers.TryGetValue(category, out var handlers))
            return handlers;
        return Enumerable.Empty<ICommandHandler>();
    }
    
    public IEnumerable<string> GetSuggestions(string partial, int max = 5)
    {
        return _handlers.Keys
            .Where(key => key.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            .Take(max);
    }
}

public class ParsedCommand
{
    public string RawInput { get; private set; }
    public string Command { get; private set; }
    public List<string> Arguments { get; private set; }
    public Dictionary<string, string> NamedArguments { get; private set; }
    public ICommandHandler Handler { get; set; }
    
    public ParsedCommand(string input)
    {
        RawInput = input?.Trim() ?? string.Empty;
        Arguments = new List<string>();
        NamedArguments = new Dictionary<string, string>();
        ParseInput();
    }
    
    private void ParseInput()
    {
        if (string.IsNullOrWhiteSpace(RawInput))
            return;
            
        var tokens = Tokenize(RawInput);
        if (tokens.Count == 0)
            return;
            
        Command = tokens[0];
        
        for (int i = 1; i < tokens.Count; i++)
        {
            var token = tokens[i];
            
            // Named argument: --flag=value or -f value
            if (token.StartsWith("--") && token.Contains('='))
            {
                var parts = token.Substring(2).Split('=', 2);
                if (parts.Length == 2)
                    NamedArguments[parts[0]] = parts[1];
            }
            else if (token.StartsWith("--"))
            {
                NamedArguments[token.Substring(2)] = "true";
            }
            else if (token.StartsWith("-") && token.Length > 1)
            {
                NamedArguments[token.Substring(1)] = i + 1 < tokens.Count ? tokens[++i] : "true";
            }
            else
            {
                Arguments.Add(token);
            }
        }
    }
    
    private List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var currentToken = new StringBuilder();
        bool inQuotes = false;
        char quoteChar = '\0';
        
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            
            if (c == '\\' && i + 1 < input.Length)
            {
                // Escape character
                currentToken.Append(input[++i]);
            }
            else if ((c == '"' || c == '\'') && (inQuotes == false || c == quoteChar))
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else
                {
                    inQuotes = false;
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                }
            }
            else if (c == ' ' && !inQuotes)
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                }
            }
            else
            {
                currentToken.Append(c);
            }
        }
        
        if (currentToken.Length > 0)
            tokens.Add(currentToken.ToString());
            
        return tokens;
    }
    
    public string GetArgument(int index, string defaultValue = "") => 
        index >= 0 && index < Arguments.Count ? Arguments[index] : defaultValue;
    
    public bool HasArgument(string arg) => Arguments.Contains(arg);
    public bool HasFlag(string flag) => NamedArguments.ContainsKey(flag);
    public string GetFlag(string flag, string defaultValue = "") => 
        NamedArguments.TryGetValue(flag, out var value) ? value : defaultValue;
}

public class CommandParser : MonoBehaviour
{
    public static CommandParser Instance { get; private set; }
    
    [SerializeField] private CommandRegistry _registry = new CommandRegistry();
    [SerializeField] private List<CommandHandlerConfig> _defaultHandlers = new List<CommandHandlerConfig>();
    
    [Header("Auto-complete")]
    [SerializeField] private bool _enableAutoComplete = true;
    [SerializeField] private int _maxSuggestions = 5;
    
    public event Action<CommandResult> OnCommandExecuted;
    public event Action<ParsedCommand, ValidationResult> OnCommandValidated;
    
    private Dictionary<Type, ICommandHandler> _handlerInstances = new Dictionary<Type, ICommandHandler>();
    private PlayerProgression _playerProgression;
    private NetworkManager _networkManager;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }
    
    private void Initialize()
    {
        _playerProgression = FindObjectOfType<PlayerProgression>();
        _networkManager = FindObjectOfType<NetworkManager>();
        
        // Register default handlers
        foreach (var config in _defaultHandlers)
        {
            var handler = CreateHandler(config);
            if (handler != null)
                _registry.RegisterHandler(handler);
        }
        
        // Register built-in handlers
        RegisterHandler<HelpCommandHandler>();
        RegisterHandler<ClearCommandHandler>();
        RegisterHandler<FileSystemCommandHandler>();
        RegisterHandler<NetworkCommandHandler>();
        RegisterHandler<HackingCommandHandler>();
    }
    
    private ICommandHandler CreateHandler(CommandHandlerConfig config)
    {
        // Factory method for creating handlers from config
        // Implementation depends on your serialization approach
        return null;
    }
    
    private void RegisterHandler<T>() where T : ICommandHandler, new()
    {
        var handler = new T();
        _handlerInstances[typeof(T)] = handler;
        _registry.RegisterHandler(handler);
    }
    
    public ParsedCommand Parse(string input)
    {
        var parsed = new ParsedCommand(input);
        
        if (!string.IsNullOrEmpty(parsed.Command))
        {
            if (_registry.TryGetHandler(parsed.Command, out var handler))
                parsed.Handler = handler;
        }
        
        return parsed;
    }
    
    public CommandResult Execute(string input)
    {
        var parsed = Parse(input);
        
        if (parsed.Handler == null)
            return CommandResult.ErrorResult($"Unknown command: {parsed.Command}");
        
        // Validate command
        var validation = parsed.Handler.Validate(parsed);
        OnCommandValidated?.Invoke(parsed, validation);
        
        if (!validation.IsValid)
            return CommandResult.ErrorResult(validation.ErrorMessage);
        
        // Execute command
        var result = parsed.Handler.Execute(parsed);
        OnCommandExecuted?.Invoke(result);
        
        return result;
    }
    
    public List<string> GetSuggestions(string partial)
    {
        if (!_enableAutoComplete || string.IsNullOrWhiteSpace(partial))
            return new List<string>();
        
        return _registry.GetSuggestions(partial, _maxSuggestions).ToList();
    }
    
    public IEnumerable<ICommandHandler> GetAvailableCommands(CommandCategory? filter = null)
    {
        if (filter.HasValue)
            return _registry.GetHandlersByCategory(filter.Value);
        
        return _registry.GetHandlersByCategory(CommandCategory.System)
            .Concat(_registry.GetHandlersByCategory(CommandCategory.File))
            .Concat(_registry.GetHandlersByCategory(CommandCategory.Network))
            .Concat(_registry.GetHandlersByCategory(CommandCategory.Hacking));
    }
    
    public void RegisterCustomHandler(ICommandHandler handler)
    {
        if (handler != null)
            _registry.RegisterHandler(handler);
    }
}

// Example handler implementations
public class HelpCommandHandler : ICommandHandler
{
    public string CommandName => "help";
    public string Description => "Display available commands";
    public string Syntax => "help [command]";
    public string[] Aliases => new[] { "?", "man", "commands" };
    public CommandCategory Category => CommandCategory.System;
    public int RequiredSkill => 0;
    public bool RequiresConnection => false;
    
    public CommandResult Execute(ParsedCommand command)
    {
        var parser = CommandParser.Instance;
        
        if (command.Arguments.Count > 0)
        {
            // Show help for specific command
            var cmdName = command.Arguments[0];
            if (parser.GetAvailableCommands().FirstOrDefault(h => 
                h.CommandName.Equals(cmdName, StringComparison.OrdinalIgnoreCase) || 
                h.Aliases?.Any(a => a.Equals(cmdName, StringComparison.OrdinalIgnoreCase)) == true) 
                is ICommandHandler specificHandler)
            {
                return CommandResult.SuccessResult(
                    $"{specificHandler.CommandName}: {specificHandler.Description}\n" +
                    $"Syntax: {specificHandler.Syntax}\n" +
                    $"Category: {specificHandler.Category}\n" +
                    $"Required Skill: {specificHandler.RequiredSkill}");
            }
            
            return CommandResult.ErrorResult($"Command not found: {cmdName}");
        }
        
        // Show all commands grouped by category
        var sb = new StringBuilder("Available commands:\n\n");
        
        foreach (CommandCategory category in Enum.GetValues(typeof(CommandCategory)))
        {
            var handlers = parser.GetAvailableCommands(category).ToList();
            if (handlers.Any())
            {
                sb.AppendLine($"[{category}]");
                foreach (var handler in handlers)
                {
                    sb.AppendLine($"  {handler.CommandName,-15} - {handler.Description}");
                }
                sb.AppendLine();
            }
        }
        
        return CommandResult.SuccessResult(sb.ToString().TrimEnd());
    }
    
    public ValidationResult Validate(ParsedCommand command)
    {
        return ValidationResult.Valid;
    }
}

public class ClearCommandHandler : ICommandHandler
{
    public string CommandName => "clear";
    public string Description => "Clear terminal screen";
    public string Syntax => "clear";
    public string[] Aliases => new[] { "cls", "reset" };
    public CommandCategory Category => CommandCategory.System;
    public int RequiredSkill => 0;
    public bool RequiresConnection => false;
    
    public CommandResult Execute(ParsedCommand command)
    {
        TerminalController.Instance?.ClearTerminal();
        return CommandResult.SuccessResult("");
    }
    
    public ValidationResult Validate(ParsedCommand command)
    {
        return ValidationResult.Valid;
    }
}

[System.Serializable]
public class CommandHandlerConfig
{
    public string TypeName;
    public string CommandName;
    public string Description;
    public string Syntax;
    public string[] Aliases;
    public CommandCategory Category;
    public int RequiredSkill;
    public bool RequiresConnection;
}