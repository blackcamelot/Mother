using UnityEngine;
using System.Collections.Generic;

public class HackingManager : MonoBehaviour
{
    public TerminalController terminal;
    public FileSystem fileSystem;
    
    [Header("Network")]
    public List<NetworkNode> networkNodes = new List<NetworkNode>();
    public NetworkNode currentNode;
    public NetworkNode homeComputer;
    
    [Header("Hacking Tools")]
    public bool hasPortScanner = true;
    public bool hasPasswordCracker = false;
    public bool hasFirewallBreacher = false;

    // CORREZIONE PRINCIPALE: Dichiarazione delle variabili di stato DENTRO la classe
    private int currentAttempts;
    private float remainingTime;
    private string currentNodePuzzleId; // Memorizza l'ID del nodo per il puzzle
    
    private void Start()
    {
        InitializeNetwork();
        currentNode = homeComputer;
        UpdateTerminalPrompt();
    }
    
    private void InitializeNetwork()
    {
        homeComputer = new NetworkNode()
        {
            ipAddress = "192.168.1.100",
            hostname = "home-pc",
            securityLevel = 1,
            isHacked = true,
            files = new List<VirtualFile>()
            {
                new VirtualFile("readme.txt", "Welcome to your home computer.\nType 'help' for commands."),
                new VirtualFile("notes.txt", "Target list:\n- TechCorp Inc.\n- Global Bank\n- Gov Agency")
            }
        };
        
        networkNodes.Add(homeComputer);
        GenerateRandomNodes(10);
    }
    
    private void GenerateRandomNodes(int count)
    {
        for(int i = 0; i < count; i++)
        {
            NetworkNodeGenerator.NodeType randomType = (NetworkNodeGenerator.NodeType)Random.Range(0, 6);
            NetworkNode node = NetworkNodeGenerator.GenerateRandomNode(randomType);
            networkNodes.Add(node);
        }
    }
    
    public void ExecuteCommand(string command)
    {
        string[] parts = command.Split(' ');
        string cmd = parts[0].ToLower();
        
        switch(cmd)
        {
            case "help":
                ShowHelp();
                break;
                
            case "ls":
            case "dir":
                ListFiles();
                break;
                
            case "cat":
                if(parts.Length > 1)
                    ReadFile(parts[1]);
                break;
                
            case "connect":
                if(parts.Length > 1)
                    ConnectToNode(parts[1]);
                break;
                
            case "scan":
                ScanNetwork();
                break;
                
            case "crack":
                if(parts.Length > 1)
                    CrackPassword(parts[1]);
                break;
                
            case "clear":
                terminal.ClearTerminal();
                break;
                
            case "whoami":
                terminal.PrintLine($"User: root\nLocation: {currentNode.hostname}");
                break;
                
            case "ports":
                if(parts.Length > 1)
                    ScanPorts(parts[1]);
                else
                    ScanPorts(currentNode.ipAddress);
                break;
                
            case "solve": // Aggiunto comando per risolvere puzzle di hacking
                if(parts.Length > 1)
                    AttemptHack(parts[1]);
                break;
                
            default:
                terminal.PrintLine($"Command not found: {cmd}");
                break;
        }
    }
    
    // CORREZIONE: Metodo per avviare un puzzle di hacking (da collegare con MissionManager)
    public void StartHackingPuzzle(string nodeId) {
        NetworkNode targetNode = networkNodes.Find(n => n.hostname == nodeId || n.ipAddress == nodeId);
        if (targetNode == null)
        {
            Debug.LogWarning($"Node not found for hacking puzzle: {nodeId}");
            terminal.PrintLine($"Node not found: {nodeId}");
            return;
        }

        currentNodePuzzleId = nodeId;
        currentAttempts = 0;
        remainingTime = 60f;
        terminal.PrintLine($"Hacking {nodeId}... Use 'solve [code]' to attempt. Time limit: 60s");
        
        // Avvia un timer (semplice) - in un'implementazione reale useresti una coroutine
        // StartCoroutine(HackingTimer());
    }

    // CORREZIONE: Metodo per tentare la soluzione del puzzle
    public bool AttemptHack(string solution) {
        if (string.IsNullOrEmpty(currentNodePuzzleId))
        {
            terminal.PrintLine("No active hacking puzzle. Use a mission to start one.");
            return false;
        }

        NetworkNode targetNode = networkNodes.Find(n => n.hostname == currentNodePuzzleId || n.ipAddress == currentNodePuzzleId);
        if (targetNode == null)
        {
            terminal.PrintLine("Puzzle target no longer available.");
            return false;
        }

        currentAttempts++;
        
        // Logica semplificata: il codice di sicurezza è l'ultimo ottetto dell'IP
        string[] ipParts = targetNode.ipAddress.Split('.');
        string correctCode = ipParts.Length == 4 ? ipParts[3] : "123";
        
        if(solution == correctCode) {
            terminal.PrintLine("ACCESS GRANTED! System compromised.");
            targetNode.isHacked = true;
            
            // Notifica il MissionManager se esiste
            // MissionManager.Instance?.CompleteMission(currentNodePuzzleId);
            
            currentNodePuzzleId = null; // Resetta il puzzle
            return true;
        }
        
        terminal.PrintLine($"Incorrect code. Attempts: {currentAttempts}/3");
        if (currentAttempts >= 3)
        {
            terminal.PrintLine("Maximum attempts reached. Hacking failed.");
            currentNodePuzzleId = null;
        }
        return false;
    }
    
    private void ConnectToNode(string target)
    {
        NetworkNode node = FindNode(target);
        
        if(node != null)
        {
            if(node.isHacked || node.securityLevel <= GameManager.Instance.hackingSkill)
            {
                currentNode = node;
                terminal.PrintLine($"Connected to {node.hostname} ({node.ipAddress})");
                UpdateTerminalPrompt();
            }
            else
            {
                terminal.PrintLine($"Access denied! Security level too high ({node.securityLevel})");
            }
        }
        else
        {
            terminal.PrintLine($"Could not resolve host: {target}");
        }
    }
    
    private void ScanNetwork()
    {
        terminal.PrintLine("Scanning network...");
        
        foreach(NetworkNode node in networkNodes)
        {
            if(node != currentNode && !node.isHacked)
            {
                string status = node.securityLevel <= 3 ? "[VULNERABLE]" : "[SECURE]";
                terminal.PrintLine($"{node.hostname} - {node.ipAddress} {status}");
            }
        }
    }
    
    private void ScanPorts(string target)
    {
        NetworkNode node = FindNode(target);
        
        if(node != null)
        {
            terminal.PrintLine($"Scanning ports for {node.hostname} ({node.ipAddress})...");
            
            if(node.openPorts.Count > 0)
            {
                foreach(int port in node.openPorts)
                {
                    string service = GetServiceName(port);
                    terminal.PrintLine($"Port {port}: {service} - OPEN");
                }
            }
            else
            {
                terminal.PrintLine("No open ports detected");
            }
        }
        else
        {
            terminal.PrintLine($"Host not found: {target}");
        }
    }
    
    private string GetServiceName(int port)
    {
        switch(port)
        {
            case 21: return "FTP";
            case 22: return "SSH";
            case 23: return "Telnet";
            case 25: return "SMTP";
            case 53: return "DNS";
            case 80: return "HTTP";
            case 110: return "POP3";
            case 143: return "IMAP";
            case 443: return "HTTPS";
            case 3306: return "MySQL";
            case 3389: return "RDP";
            case 8080: return "HTTP Proxy";
            default: return "Unknown";
        }
    }
    
    private void UpdateTerminalPrompt()
    {
        terminal.UpdatePrompt($"{currentNode.hostname}:~$ ");
    }
    
    private NetworkNode FindNode(string identifier)
    {
        return networkNodes.Find(n => 
            n.hostname == identifier || 
            n.ipAddress == identifier
        );
    }
    
    private void ShowHelp()
    {
        string helpText = @"Available commands:
        help - Show this help
        ls, dir - List files
        cat [file] - Read file
        connect [ip/hostname] - Connect to server
        scan - Scan network
        crack [ip] - Crack password
        clear - Clear terminal
        whoami - Show user info
        ports [ip] - Scan ports on target
        solve [code] - Solve hacking puzzle (if active)";
        
        terminal.PrintLine(helpText);
    }
    
    private void ListFiles()
    {
        foreach(VirtualFile file in currentNode.files)
        {
            terminal.PrintLine(file.name);
        }
    }
    
    private void ReadFile(string filename)
    {
        VirtualFile file = currentNode.files.Find(f => f.name == filename);
        if(file != null)
        {
            terminal.PrintLine(file.content);
        }
        else
        {
            terminal.PrintLine($"File not found: {filename}");
        }
    }
    
    private void CrackPassword(string target)
    {
        NetworkNode node = FindNode(target);
        if(node != null)
        {
            terminal.PrintLine($"Attempting to crack {node.hostname}...");
            
            int attempts = GameManager.Instance.hackingSkill * 2;
            bool success = Random.Range(0, 10) < attempts;
            
            if(success)
            {
                node.isHacked = true;
                terminal.PrintLine($"Success! Password: {node.password}");
                GameManager.Instance.IncreaseSkill("hacking");
            }
            else
            {
                terminal.PrintLine("Cracking failed! Security alerted!");
                node.securityLevel++;
            }
        }
    }
}