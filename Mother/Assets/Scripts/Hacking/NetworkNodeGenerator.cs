using UnityEngine;
using System.Collections.Generic;
using System.Text;

public static class NetworkNodeGenerator
{
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
    
    private static string[] personalNames = {"john", "sarah", "mike", "lisa", "david", "emma"};
    private static string[] corporateNames = {"corp", "inc", "llc", "ltd", "enterprises"};
    private static string[] domains = {"com", "org", "net", "edu", "gov", "io"};
    
    public static NetworkNode GenerateRandomNode(NodeType type = NodeType.PersonalComputer)
    {
        NetworkNode node = new NetworkNode();
        
        node.nodeType = (NetworkNode.NodeType)type;
        node.ipAddress = GenerateIP();
        node.hostname = GenerateHostname(type);
        node.domain = GenerateDomain();
        node.securityLevel = GetBaseSecurityLevel(type) + Random.Range(-2, 3);
        node.securityLevel = Mathf.Clamp(node.securityLevel, 1, 10);
        node.password = GeneratePassword(Random.Range(6, 12));
        node.openPorts = GenerateOpenPorts(type);
        node.services = GenerateServices(node.openPorts);
        node.files = GenerateFiles(type);
        node.vulnerabilities = GenerateVulnerabilities(type, node.securityLevel);
        node.owner = GenerateOwner(type);
        node.isFirewalled = Random.Range(0, 100) < (node.securityLevel * 10);
        node.isOnline = Random.Range(0, 100) < 90;
        
        node.CalculateDataValue();
        return node;
    }
    
    private static string GenerateIP()
    {
        return $"{Random.Range(10, 255)}.{Random.Range(0, 255)}.{Random.Range(0, 255)}.{Random.Range(1, 255)}";
    }
    
    private static string GenerateHostname(NodeType type)
    {
        switch(type)
        {
            case NodeType.PersonalComputer:
                return $"{personalNames[Random.Range(0, personalNames.Length)]}-pc";
            case NodeType.CorporateServer:
                return $"server{Random.Range(1, 100)}.{corporateNames[Random.Range(0, corporateNames.Length)]}";
            case NodeType.GovernmentServer:
                return $"gov-server-{Random.Range(1, 50)}";
            case NodeType.BankServer:
                return $"bank-srv-{Random.Range(1, 20)}";
            case NodeType.WebServer:
                return $"web{Random.Range(1, 100)}";
            case NodeType.DatabaseServer:
                return $"db{Random.Range(1, 50)}";
            default:
                return $"node{Random.Range(1000, 9999)}";
        }
    }
    
    private static string GenerateDomain()
    {
        string[] prefixes = {"example", "test", "secure", "data", "net"};
        return $"{prefixes[Random.Range(0, prefixes.Length)]}.{domains[Random.Range(0, domains.Length)]}";
    }
    
    private static int GetBaseSecurityLevel(NodeType type)
    {
        switch(type)
        {
            case NodeType.GovernmentServer:
            case NodeType.BankServer:
                return 8;
            case NodeType.CorporateServer:
                return 6;
            case NodeType.DatabaseServer:
                return 5;
            case NodeType.WebServer:
                return 4;
            case NodeType.PersonalComputer:
                return 3;
            case NodeType.IoTDevice:
            case NodeType.MobileDevice:
                return 2;
            default:
                return 3;
        }
    }
    
    private static string GeneratePassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        StringBuilder password = new StringBuilder();
        
        for(int i = 0; i < length; i++)
        {
            password.Append(chars[Random.Range(0, chars.Length)]);
        }
        
        return password.ToString();
    }
    
    private static List<int> GenerateOpenPorts(NodeType type)
    {
        List<int> ports = new List<int>();
        Dictionary<int, int> portChances = new Dictionary<int, int>()
        {
            {21, 30}, {22, 40}, {23, 20}, {25, 50},
            {53, 60}, {80, 100}, {110, 40}, {143, 40},
            {443, 80}, {3306, 30}, {3389, 20}, {8080, 30}
        };
        
        foreach(var port in portChances)
        {
            if(Random.Range(0, 100) < port.Value)
            {
                ports.Add(port.Key);
            }
        }
        
        if(ports.Count == 0)
        {
            if(type == NodeType.WebServer)
                ports.Add(80);
            else
                ports.Add(22);
        }
        
        return ports;
    }
    
    private static List<NetworkService> GenerateServices(List<int> ports)
    {
        List<NetworkService> services = new List<NetworkService>();
        Dictionary<int, string> portServices = new Dictionary<int, string>()
        {
            {21, "FTP Server"}, {22, "SSH Daemon"}, {23, "Telnet"},
            {25, "SMTP Mail"}, {53, "DNS Server"}, {80, "Apache Web"},
            {110, "POP3 Mail"}, {143, "IMAP Mail"}, {443, "SSL Web"},
            {3306, "MySQL DB"}, {3389, "Remote Desktop"}, {8080, "Proxy Server"}
        };
        
        foreach(int port in ports)
        {
            if(portServices.ContainsKey(port))
            {
                NetworkService service = new NetworkService()
                {
                    name = portServices[port],
                    port = port,
                    version = $"{Random.Range(1, 4)}.{Random.Range(0, 10)}.{Random.Range(0, 100)}",
                    isRunning = true
                };
                
                services.Add(service);
            }
        }
        
        return services;
    }
    
    private static List<VirtualFile> GenerateFiles(NodeType type)
    {
        List<VirtualFile> files = new List<VirtualFile>();
        int fileCount = Random.Range(1, type == NodeType.PersonalComputer ? 8 : 15);
        
        for(int i = 0; i < fileCount; i++)
        {
            string filename = GenerateFilename(type, i);
            string content = GenerateFileContent(type, filename);
            files.Add(new VirtualFile(filename, content));
        }
        
        return files;
    }
    
    private static string GenerateFilename(NodeType type, int index)
    {
        string[] personalFiles = {"resume.txt", "photos.zip", "diary.txt", "projects.doc", "passwords.txt"};
        string[] corporateFiles = {"financial_report.pdf", "employee_data.db", "client_list.csv", "meeting_notes.txt"};
        string[] governmentFiles = {"classified_document.txt", "surveillance_logs.db", "agent_profiles.pdf"};
        
        switch(type)
        {
            case NodeType.PersonalComputer:
                return personalFiles[Random.Range(0, personalFiles.Length)];
            case NodeType.CorporateServer:
            case NodeType.BankServer:
                return corporateFiles[Random.Range(0, corporateFiles.Length)];
            case NodeType.GovernmentServer:
                return governmentFiles[Random.Range(0, governmentFiles.Length)];
            default:
                return $"file{index + 1}.txt";
        }
    }
    
    private static string GenerateFileContent(NodeType type, string filename)
    {
        if(filename.Contains("password"))
        {
            return $"Username: admin\nPassword: {GeneratePassword(8)}";
        }
        else if(filename.Contains("financial") || filename.Contains("report"))
        {
            return $"Financial Report\nRevenue: ${Random.Range(100000, 10000000)}\nExpenses: ${Random.Range(50000, 5000000)}";
        }
        else
        {
            return "Document content placeholder.";
        }
    }
    
    private static List<Vulnerability> GenerateVulnerabilities(NodeType type, int securityLevel)
    {
        List<Vulnerability> vulnerabilities = new List<Vulnerability>();
        int maxVulns = Mathf.Max(0, 5 - securityLevel);
        int vulnCount = Random.Range(0, maxVulns + 1);
        
        for(int i = 0; i < vulnCount; i++)
        {
            if(Random.Range(0, 100) < 60)
            {
                vulnerabilities.Add(new Vulnerability()
                {
                    name = "Weak Password",
                    description = "System uses default or weak password",
                    severity = 3,
                    requiredSkillLevel = 1
                });
            }
        }
        
        return vulnerabilities;
    }

    public NetworkNode GetNode(string nodeId) {
        return new NetworkNode {
            id = nodeId,
            securityCode = GenerateRandomCode(),
            difficultyLevel = 1,
            isFirewallActive = true
        };
    }

    
    
    private static string GenerateOwner(NodeType type)
    {
        switch(type)
        {
            case NodeType.PersonalComputer:
                return personalNames[Random.Range(0, personalNames.Length)];
            case NodeType.CorporateServer:
                return $"{corporateNames[Random.Range(0, corporateNames.Length)]} Corporation";
            case NodeType.GovernmentServer:
                return "Government Agency";
            case NodeType.BankServer:
                return "Global Bank Inc.";
            default:
                return "Unknown";
        }
    }

}
