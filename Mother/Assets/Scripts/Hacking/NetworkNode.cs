using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[System.Serializable]
public class NetworkNode
{
    // AGGIUNTA: Proprietà 'id' richiesta dal codice HackingManager
    public string id;
    public string ipAddress;
    public string securityCode; // Usata per i puzzle di hacking
    public int difficultyLevel;
    public bool isFirewallActive;
    public string hostname;
    public string domain;
    public int securityLevel;
    public bool isHacked;
    public bool isFirewalled;
    public string password;
    public List<int> openPorts;
    public List<VirtualFile> files;
    public List<NetworkService> services;
    public List<Vulnerability> vulnerabilities;
    public NodeType nodeType;
    public string owner;
    public int dataValue;
    public bool isOnline = true;
    
    public enum NodeType
    {
        PersonalComputer,
        CorporateServer,
        GovernmentServer,
        BankServer,
        WebServer,
        DatabaseServer,
        Router,
        Firewall,
        IoTDevice,
        MobileDevice
    }
    
    public NetworkNode()
    {
        id = System.Guid.NewGuid().ToString().Substring(0, 8); // Genera un ID univoco di default
        files = new List<VirtualFile>();
        services = new List<NetworkService>();
        vulnerabilities = new List<Vulnerability>();
        openPorts = new List<int>();
    }
    
    
}
    
    public string GetSystemInfo()
    {
        StringBuilder info = new StringBuilder();
        
        info.AppendLine($"Hostname: {hostname}");
        info.AppendLine($"IP Address: {ipAddress}");
        info.AppendLine($"Domain: {domain}");
        info.AppendLine($"Security Level: {securityLevel}");
        info.AppendLine($"Type: {nodeType}");
        info.AppendLine($"Owner: {owner}");
        
        info.Append("Status: ");
        info.AppendLine(isOnline ? "Online" : "Offline");
        
        info.Append("Hacked: ");
        info.AppendLine(isHacked ? "Yes" : "No");
        
        info.Append("Firewalled: ");
        info.AppendLine(isFirewalled ? "Yes" : "No");
        
        if(isHacked)
        {
            info.AppendLine($"Password: {password}");
        }
        
        return info.ToString();
    }
    
    public string GetPortInfo()
    {
        if(openPorts.Count == 0)
            return "No open ports detected";
            
        StringBuilder info = new StringBuilder();
        info.AppendLine("Open Ports:");
        
        foreach(int port in openPorts)
        {
            string serviceName = GetServiceByPort(port);
            info.AppendLine($"{port} - {serviceName}");
        }
        
        return info.ToString();
    }
    
    private string GetServiceByPort(int port)
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
    
    public int CalculateDataValue()
    {
        int value = 0;
        
        switch(nodeType)
        {
            case NodeType.BankServer:
                value += 5000;
                break;
            case NodeType.GovernmentServer:
                value += 4000;
                break;
            case NodeType.CorporateServer:
                value += 3000;
                break;
            case NodeType.DatabaseServer:
                value += 2500;
                break;
            case NodeType.WebServer:
                value += 1500;
                break;
            default:
                value += 500;
                break;
        }
        
        value += files.Count * 100;
        
        foreach(VirtualFile file in files)
        {
            if(file.content.Contains("password") || 
               file.content.Contains("confidential") ||
               file.content.Contains("secret"))
            {
                value += 500;
            }
        }
        
        dataValue = value;
        return value;
    }
}

[System.Serializable]
public class NetworkService
{
    public string name;
    public int port;
    public string version;
    public bool isRunning;
    public ServiceType type;
    
    public enum ServiceType
    {
        WebServer,
        Database,
        FileServer,
        MailServer,
        SSH,
        FTP,
        DNS,
        VPN
    }
}

[System.Serializable]
public class Vulnerability
{
    public string name;
    public string description;
    public int severity;
    public VulnerabilityType type;
    public string exploitCommand;
    public int requiredSkillLevel;
    
    public enum VulnerabilityType
    {
        BufferOverflow,
        SQLInjection,
        XSS,
        CSRF,
        PrivilegeEscalation,
        ZeroDay,
        Misconfiguration,
        WeakPassword
    }
}