using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FileSystem : MonoBehaviour
{
    [System.Serializable]
    public class VirtualDirectory
    {
        public string name;
        public string path;
        public List<VirtualFile> files;
        public List<VirtualDirectory> subdirectories;
        public bool isHidden;
        public bool isLocked;
        public int lockLevel;
        
        public VirtualDirectory(string name, string path)
        {
            this.name = name;
            this.path = path;
            files = new List<VirtualFile>();
            subdirectories = new List<VirtualDirectory>();
            isHidden = false;
            isLocked = false;
            lockLevel = 0;
        }
        
        public void AddFile(VirtualFile file)
        {
            files.Add(file);
        }
        
        public void AddDirectory(VirtualDirectory directory)
        {
            subdirectories.Add(directory);
        }
        
        public VirtualFile GetFile(string filename)
        {
            return files.Find(f => f.name.ToLower() == filename.ToLower());
        }
        
        public VirtualDirectory GetDirectory(string dirname)
        {
            return subdirectories.Find(d => d.name.ToLower() == dirname.ToLower());
        }
        
        public bool DeleteFile(string filename)
        {
            VirtualFile file = GetFile(filename);
            if(file != null)
            {
                files.Remove(file);
                return true;
            }
            return false;
        }
        
        public string GetFullPath()
        {
            return path + "/" + name;
        }
    }
    
    [Header("File System Root")]
    public VirtualDirectory rootDirectory;
    public VirtualDirectory currentDirectory;
    
    private void Start()
    {
        InitializeFileSystem();
    }
    
    private void InitializeFileSystem()
    {
        rootDirectory = new VirtualDirectory("root", "");
        currentDirectory = rootDirectory;
        CreateDefaultDirectories();
        CreateDefaultFiles();
    }
    
    private void CreateDefaultDirectories()
    {
        VirtualDirectory home = new VirtualDirectory("home", "/root");
        VirtualDirectory system = new VirtualDirectory("system", "/root");
        VirtualDirectory logs = new VirtualDirectory("logs", "/root");
        VirtualDirectory temp = new VirtualDirectory("temp", "/root");
        
        VirtualDirectory documents = new VirtualDirectory("documents", "/root/home");
        VirtualDirectory downloads = new VirtualDirectory("downloads", "/root/home");
        VirtualDirectory desktop = new VirtualDirectory("desktop", "/root/home");
        
        rootDirectory.AddDirectory(home);
        rootDirectory.AddDirectory(system);
        rootDirectory.AddDirectory(logs);
        rootDirectory.AddDirectory(temp);
        
        home.AddDirectory(documents);
        home.AddDirectory(downloads);
        home.AddDirectory(desktop);
    }
    
    private void CreateDefaultFiles()
    {
        CreateFile("/root/system", "readme.txt", "Welcome to the hacking simulation system.\nType 'help' for available commands.");
        CreateFile("/root/system", "config.ini", "[System]\nVersion=1.0.0\nSecurityLevel=1\nAutoUpdate=false");
        CreateFile("/root/logs", "system.log", "2023-10-01 09:00: System started\n2023-10-01 09:05: User login successful");
        CreateFile("/root/home/documents", "notes.txt", "Important notes:\n1. Change default passwords\n2. Update firewall rules");
        
        VirtualFile hiddenFile = new VirtualFile(".hidden_config", "Secret configuration data");
        hiddenFile.isHidden = true;
        AddFileToPath("/root/system", hiddenFile);
    }
    
    public bool CreateFile(string path, string filename, string content)
    {
        VirtualDirectory dir = GetDirectoryByPath(path);
        if(dir != null)
        {
            VirtualFile file = new VirtualFile(filename, content);
            dir.AddFile(file);
            return true;
        }
        return false;
    }
    
    public VirtualDirectory GetDirectoryByPath(string path)
    {
        if(string.IsNullOrEmpty(path) || path == "/" || path == "/root")
            return rootDirectory;
            
        string[] parts = path.Split('/');
        VirtualDirectory current = rootDirectory;
        
        foreach(string part in parts)
        {
            if(string.IsNullOrEmpty(part) || part == "root")
                continue;
                
            current = current.GetDirectory(part);
            if(current == null)
                return null;
        }
        
        return current;
    }
    
    public VirtualFile GetFileByPath(string filepath)
    {
        string directoryPath = Path.GetDirectoryName(filepath).Replace("\\", "/");
        string filename = Path.GetFileName(filepath);
        
        VirtualDirectory dir = GetDirectoryByPath(directoryPath);
        if(dir != null)
        {
            return dir.GetFile(filename);
        }
        
        return null;
    }
    
    public string ListDirectoryContents(string path = "")
    {
        VirtualDirectory dir;
        
        if(string.IsNullOrEmpty(path))
        {
            dir = currentDirectory;
        }
        else
        {
            dir = GetDirectoryByPath(path);
            if(dir == null)
                return $"Directory not found: {path}";
        }
        
        string output = $"Contents of {dir.GetFullPath()}:\n\n";
        
        foreach(VirtualDirectory subdir in dir.subdirectories)
        {
            string hiddenMark = subdir.isHidden ? "(hidden) " : "";
            string lockedMark = subdir.isLocked ? "[LOCKED] " : "";
            output += $"[DIR]  {lockedMark}{hiddenMark}{subdir.name}/\n";
        }
        
        foreach(VirtualFile file in dir.files)
        {
            string hiddenMark = file.isHidden ? "(hidden) " : "";
            string lockedMark = file.isLocked ? "[LOCKED] " : "";
            string encryptedMark = file.isEncrypted ? "[ENCRYPTED] " : "";
            output += $"[FILE] {lockedMark}{encryptedMark}{hiddenMark}{file.name}\n";
        }
        
        if(dir.subdirectories.Count == 0 && dir.files.Count == 0)
        {
            output += "Directory is empty.\n";
        }
        
        return output;
    }
    
    public string ChangeDirectory(string path)
    {
        if (path == ".." || path == "../")
        {
            // Se non siamo alla root, torna al parent
            if (currentDirectory != rootDirectory)
            {
                // Troviamo il directory parent
                string currentPath = currentDirectory.GetFullPath();
                string parentPath = Path.GetDirectoryName(currentPath)?.Replace("\\", "/");
            
                if (!string.IsNullOrEmpty(parentPath))
                {
                    VirtualDirectory parentDir = GetDirectoryByPath(parentPath);
                    if (parentDir != null)
                    {
                        currentDirectory = parentDir;
                        return $"Changed to directory: {currentDirectory.GetFullPath()}";
                    }
                }
            
                currentDirectory = rootDirectory;
                return $"Changed to directory: {currentDirectory.GetFullPath()}";
            }
            else
            {
                return "Already at root directory";
            }
        }
    
    public string ReadFile(string filepath)
    {
        VirtualFile file = GetFileByPath(filepath);
        
        if(file == null)
        {
            file = currentDirectory.GetFile(filepath);
        }
        
        if(file != null)
        {
            if(file.isLocked)
            {
                return $"File is locked. Required skill level: {file.lockLevel}";
            }
            
            if(file.isEncrypted)
            {
                return $"File is encrypted.\nUse 'decrypt {filepath}' to attempt decryption.";
            }
            
            return file.content;
        }
        
        return $"File not found: {filepath}";
    }

    public VirtualFile GetFileByPath(string filepath)
    {
        if (string.IsNullOrEmpty(filepath))
            return null;
        
        string directoryPath = Path.GetDirectoryName(filepath);
    
        // Controlla se è un file nella directory corrente
        if (string.IsNullOrEmpty(directoryPath))
        {
        return currentDirectory.GetFile(filepath);
        }
    
        directoryPath = directoryPath.Replace("\\", "/");
        string filename = Path.GetFileName(filepath);
    
        VirtualDirectory dir = GetDirectoryByPath(directoryPath);
        if (dir != null)
        {
            return dir.GetFile(filename);
        }
    
        return null;
    }
    
    public string GetCurrentPath()
    {
        return currentDirectory.GetFullPath();
    }
    
    private void AddFileToPath(string path, VirtualFile file)
    {
        VirtualDirectory dir = GetDirectoryByPath(path);
        if(dir != null)
        {
            dir.AddFile(file);
        }
    }
}

[System.Serializable]
public class VirtualFile
{
    public string name;
    public string content;
    public bool isEncrypted;
    public int encryptionLevel;
    public bool isHidden;
    public bool isLocked;
    public int lockLevel;
    public long size;
    public string createdDate;
    public string modifiedDate;
    
    public VirtualFile(string name, string content, bool encrypted = false, int encLevel = 0)
    {
        this.name = name;
        this.content = content;
        this.isEncrypted = encrypted;
        this.encryptionLevel = encLevel;
        this.isHidden = false;
        this.isLocked = false;
        this.lockLevel = 0;
        this.size = System.Text.Encoding.UTF8.GetByteCount(content);
        this.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        this.modifiedDate = this.createdDate;
    }
    
    public void UpdateContent(string newContent)
    {
        content = newContent;
        size = System.Text.Encoding.UTF8.GetByteCount(content);
        modifiedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}