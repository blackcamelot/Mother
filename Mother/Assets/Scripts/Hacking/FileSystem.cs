using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public interface IFileSystemComponent
{
    string Name { get; }
    string FullPath { get; }
    bool IsHidden { get; }
    bool IsLocked { get; }
    int LockLevel { get; }
    DateTime Created { get; }
    DateTime Modified { get; }
}

[System.Serializable]
public class VirtualFile : IFileSystemComponent
{
    [SerializeField] private string _name;
    [SerializeField] private string _content;
    [SerializeField] private bool _isHidden;
    [SerializeField] private bool _isLocked;
    [SerializeField] private int _lockLevel;
    [SerializeField] private bool _isEncrypted;
    [SerializeField] private int _encryptionLevel;
    [SerializeField] private DateTime _created;
    [SerializeField] private DateTime _modified;
    [SerializeField] private long _size;
    
    public string Name => _name;
    public string Content => _content;
    public bool IsHidden => _isHidden;
    public bool IsLocked => _isLocked;
    public int LockLevel => _lockLevel;
    public bool IsEncrypted => _isEncrypted;
    public int EncryptionLevel => _encryptionLevel;
    public DateTime Created => _created;
    public DateTime Modified => _modified;
    public long Size => _size;
    public string FullPath { get; internal set; }
    
    public event Action<VirtualFile> OnContentChanged;
    
    public VirtualFile(string name, string content, bool encrypted = false, int encryptionLevel = 0)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _content = content ?? "";
        _isEncrypted = encrypted;
        _encryptionLevel = encryptionLevel;
        _created = DateTime.Now;
        _modified = _created;
        _size = Encoding.UTF8.GetByteCount(content ?? "");
        _isHidden = name.StartsWith(".");
        _isLocked = false;
        _lockLevel = 0;
    }
    
    public void UpdateContent(string newContent)
    {
        if (_isLocked)
            throw new InvalidOperationException($"File '{Name}' is locked (level {_lockLevel})");
            
        if (_isEncrypted)
            throw new InvalidOperationException($"File '{Name}' is encrypted");
            
        _content = newContent ?? "";
        _size = Encoding.UTF8.GetByteCount(_content);
        _modified = DateTime.Now;
        OnContentChanged?.Invoke(this);
    }
    
    public void SetHidden(bool hidden) => _isHidden = hidden;
    public void SetLocked(bool locked, int level = 0)
    {
        _isLocked = locked;
        _lockLevel = level;
    }
    
    public void SetEncrypted(bool encrypted, int level = 0)
    {
        _isEncrypted = encrypted;
        _encryptionLevel = level;
    }
    
    public bool TryDecrypt(int skillLevel)
    {
        if (!_isEncrypted) return true;
        if (skillLevel >= _encryptionLevel)
        {
            _isEncrypted = false;
            return true;
        }
        return false;
    }
    
    public bool TryUnlock(int skillLevel)
    {
        if (!_isLocked) return true;
        if (skillLevel >= _lockLevel)
        {
            _isLocked = false;
            return true;
        }
        return false;
    }
}

[System.Serializable]
public class VirtualDirectory : IFileSystemComponent
{
    [SerializeField] private string _name;
    [SerializeField] private bool _isHidden;
    [SerializeField] private bool _isLocked;
    [SerializeField] private int _lockLevel;
    [SerializeField] private DateTime _created;
    [SerializeField] private DateTime _modified;
    
    private VirtualDirectory _parent;
    private Dictionary<string, VirtualFile> _files = new Dictionary<string, VirtualFile>();
    private Dictionary<string, VirtualDirectory> _subdirectories = new Dictionary<string, VirtualDirectory>();
    
    public string Name => _name;
    public bool IsHidden => _isHidden;
    public bool IsLocked => _isLocked;
    public int LockLevel => _lockLevel;
    public DateTime Created => _created;
    public DateTime Modified => _modified;
    public VirtualDirectory Parent => _parent;
    public string FullPath { get; private set; }
    
    public IReadOnlyCollection<VirtualFile> Files => _files.Values;
    public IReadOnlyCollection<VirtualDirectory> Subdirectories => _subdirectories.Values;
    
    public event Action<VirtualDirectory> OnStructureChanged;
    
    public VirtualDirectory(string name, VirtualDirectory parent = null)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _parent = parent;
        _created = DateTime.Now;
        _modified = _created;
        _isHidden = name.StartsWith(".");
        _isLocked = false;
        _lockLevel = 0;
        UpdateFullPath();
    }
    
    private void UpdateFullPath()
    {
        if (_parent == null)
            FullPath = "/" + _name;
        else
            FullPath = _parent.FullPath + "/" + _name;
            
        // Update all children paths
        foreach (var file in _files.Values)
            file.FullPath = FullPath + "/" + file.Name;
            
        foreach (var dir in _subdirectories.Values)
            dir.UpdateFullPath();
    }
    
    public VirtualFile AddFile(VirtualFile file)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
            
        if (_files.ContainsKey(file.Name.ToLower()))
            throw new InvalidOperationException($"File '{file.Name}' already exists");
            
        _files[file.Name.ToLower()] = file;
        file.FullPath = FullPath + "/" + file.Name;
        _modified = DateTime.Now;
        OnStructureChanged?.Invoke(this);
        return file;
    }
    
    public VirtualFile CreateFile(string name, string content = "", bool encrypted = false)
    {
        var file = new VirtualFile(name, content, encrypted);
        return AddFile(file);
    }
    
    public VirtualDirectory AddDirectory(VirtualDirectory directory)
    {
        if (directory == null)
            throw new ArgumentNullException(nameof(directory));
            
        if (_subdirectories.ContainsKey(directory.Name.ToLower()))
            throw new InvalidOperationException($"Directory '{directory.Name}' already exists");
            
        directory._parent = this;
        _subdirectories[directory.Name.ToLower()] = directory;
        directory.UpdateFullPath();
        _modified = DateTime.Now;
        OnStructureChanged?.Invoke(this);
        return directory;
    }
    
    public VirtualDirectory CreateDirectory(string name)
    {
        var dir = new VirtualDirectory(name, this);
        return AddDirectory(dir);
    }
    
    public VirtualFile GetFile(string name)
    {
        _files.TryGetValue(name?.ToLower() ?? "", out var file);
        return file;
    }
    
    public VirtualDirectory GetDirectory(string name)
    {
        _subdirectories.TryGetValue(name?.ToLower() ?? "", out var dir);
        return dir;
    }
    
    public bool DeleteFile(string name)
    {
        if (_files.Remove(name?.ToLower() ?? ""))
        {
            _modified = DateTime.Now;
            OnStructureChanged?.Invoke(this);
            return true;
        }
        return false;
    }
    
    public bool DeleteDirectory(string name, bool recursive = false)
    {
        if (!_subdirectories.TryGetValue(name?.ToLower() ?? "", out var dir))
            return false;
            
        if (!recursive && (dir._files.Count > 0 || dir._subdirectories.Count > 0))
            throw new InvalidOperationException($"Directory '{name}' is not empty");
            
        if (_subdirectories.Remove(name.ToLower()))
        {
            _modified = DateTime.Now;
            OnStructureChanged?.Invoke(this);
            return true;
        }
        return false;
    }
    
    public IEnumerable<VirtualFile> SearchFiles(string pattern, bool includeHidden = false)
    {
        var regex = new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", 
                             RegexOptions.IgnoreCase);
        
        foreach (var file in _files.Values)
        {
            if ((includeHidden || !file.IsHidden) && regex.IsMatch(file.Name))
                yield return file;
        }
        
        foreach (var dir in _subdirectories.Values)
        {
            if (includeHidden || !dir.IsHidden)
            {
                foreach (var file in dir.SearchFiles(pattern, includeHidden))
                    yield return file;
            }
        }
    }
    
    public void Traverse(Action<VirtualDirectory> directoryAction = null, 
                        Action<VirtualFile> fileAction = null)
    {
        directoryAction?.Invoke(this);
        
        foreach (var file in _files.Values)
            fileAction?.Invoke(file);
            
        foreach (var dir in _subdirectories.Values)
            dir.Traverse(directoryAction, fileAction);
    }
    
    public string GetRelativePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "";
            
        if (path.StartsWith("/"))
            return path;
            
        return FullPath + "/" + path;
    }
    
    public void SetHidden(bool hidden) => _isHidden = hidden;
    public void SetLocked(bool locked, int level = 0)
    {
        _isLocked = locked;
        _lockLevel = level;
    }
    
    public bool TryUnlock(int skillLevel)
    {
        if (!_isLocked) return true;
        if (skillLevel >= _lockLevel)
        {
            _isLocked = false;
            return true;
        }
        return false;
    }
}

public class FileSystem : MonoBehaviour
{
    public static FileSystem Instance { get; private set; }
    
    [Header("File System Settings")]
    [SerializeField] private VirtualDirectory _rootDirectory;
    [SerializeField] private VirtualDirectory _currentDirectory;
    [SerializeField] private bool _autoCreateDefaults = true;
    
    [Header("Permissions")]
    [SerializeField] private int _playerSkillLevel = 1;
    [SerializeField] private bool _allowHiddenAccess = false;
    
    public VirtualDirectory Root => _rootDirectory;
    public VirtualDirectory CurrentDirectory => _currentDirectory;
    public string CurrentPath => _currentDirectory?.FullPath ?? "/";
    
    public event Action<VirtualDirectory> OnDirectoryChanged;
    public event Action<string, VirtualFile> OnFileAccessed;
    public event Action<Exception> OnFileSystemError;
    
    private Dictionary<string, VirtualDirectory> _pathCache = new Dictionary<string, VirtualDirectory>();
    
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
        _rootDirectory = new VirtualDirectory("root");
        _currentDirectory = _rootDirectory;
        
        _pathCache["/"] = _rootDirectory;
        _pathCache["/root"] = _rootDirectory;
        
        if (_autoCreateDefaults)
            CreateDefaultStructure();
    }
    
    private void CreateDefaultStructure()
    {
        try
        {
            var home = _rootDirectory.CreateDirectory("home");
            home.CreateDirectory("documents")
                .CreateFile("notes.txt", "Important notes:\n- Change default passwords\n- Update firewall");
                
            home.CreateDirectory("downloads");
            home.CreateDirectory("desktop");
            
            var system = _rootDirectory.CreateDirectory("system");
            system.CreateFile("readme.txt", "Welcome to the hacking simulation.\nType 'help' for commands.");
            system.CreateFile("config.ini", "[System]\nVersion=1.0.0\nSecurityLevel=1\nAutoUpdate=false");
            
            var logs = _rootDirectory.CreateDirectory("logs");
            logs.CreateFile("system.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - System started");
            
            var temp = _rootDirectory.CreateDirectory("temp");
            
            // Create some hidden/encrypted files
            var secretFile = system.CreateFile(".hidden_config", "Secret: admin_password = 'CHANGE_ME'");
            secretFile.SetHidden(true);
            
            var encryptedFile = home.CreateFile("secret_data.enc", "Encrypted financial records", true, 3);
            
            // Cache all paths
            CacheDirectoryPaths(_rootDirectory);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create default file structure: {e.Message}");
            OnFileSystemError?.Invoke(e);
        }
    }
    
    private void CacheDirectoryPaths(VirtualDirectory dir)
    {
        _pathCache[dir.FullPath] = dir;
        foreach (var subdir in dir.Subdirectories)
            CacheDirectoryPaths(subdir);
    }
    
    public bool ChangeDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;
            
        try
        {
            var targetDir = ResolvePath(path, _currentDirectory);
            if (targetDir == null)
                return false;
                
            if (targetDir.IsLocked && !targetDir.TryUnlock(_playerSkillLevel))
            {
                OnFileSystemError?.Invoke(new UnauthorizedAccessException(
                    $"Directory '{targetDir.Name}' is locked (level {targetDir.LockLevel})"));
                return false;
            }
            
            _currentDirectory = targetDir;
            OnDirectoryChanged?.Invoke(_currentDirectory);
            return true;
        }
        catch (Exception e)
        {
            OnFileSystemError?.Invoke(e);
            return false;
        }
    }
    
    public VirtualDirectory ResolvePath(string path, VirtualDirectory relativeTo = null)
    {
        if (string.IsNullOrEmpty(path))
            return relativeTo ?? _currentDirectory;
            
        // Check cache first
        if (_pathCache.TryGetValue(path, out var cachedDir))
            return cachedDir;
            
        // Handle special paths
        if (path == ".")
            return relativeTo ?? _currentDirectory;
            
        if (path == "..")
            return (relativeTo ?? _currentDirectory)?.Parent ?? _rootDirectory;
            
        // Absolute path
        if (path.StartsWith("/"))
        {
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            VirtualDirectory current = _rootDirectory;
            
            foreach (var part in parts)
            {
                if (part == ".")
                    continue;
                if (part == "..")
                {
                    current = current?.Parent ?? _rootDirectory;
                    continue;
                }
                
                current = current?.GetDirectory(part);
                if (current == null)
                    return null;
            }
            
            return current;
        }
        
        // Relative path
        if (relativeTo == null)
            relativeTo = _currentDirectory;
            
        var relParts = path.Split('/');
        VirtualDirectory currentRel = relativeTo;
        
        foreach (var part in relParts)
        {
            if (string.IsNullOrEmpty(part) || part == ".")
                continue;
            if (part == "..")
            {
                currentRel = currentRel?.Parent ?? _rootDirectory;
                continue;
            }
            
            currentRel = currentRel?.GetDirectory(part);
            if (currentRel == null)
                return null;
        }
        
        return currentRel;
    }
    
    public VirtualFile GetFile(string path)
    {
        try
        {
            var dirPath = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var fileName = Path.GetFileName(path);
            
            if (string.IsNullOrEmpty(dirPath))
            {
                // File in current directory
                return _currentDirectory.GetFile(fileName);
            }
            
            var directory = ResolvePath(dirPath);
            return directory?.GetFile(fileName);
        }
        catch (Exception e)
        {
            OnFileSystemError?.Invoke(e);
            return null;
        }
    }
    
    public string ListDirectory(string path = null, bool showHidden = false)
    {
        try
        {
            var targetDir = string.IsNullOrEmpty(path) ? 
                _currentDirectory : ResolvePath(path, _currentDirectory);
                
            if (targetDir == null)
                return $"Directory not found: {path}";
                
            if (targetDir.IsLocked && !targetDir.TryUnlock(_playerSkillLevel))
                return $"Directory is locked (level {targetDir.LockLevel})";
            
            var sb = new StringBuilder();
            sb.AppendLine($"Contents of {targetDir.FullPath}:");
            sb.AppendLine();
            
            // List directories
            foreach (var dir in targetDir.Subdirectories.OrderBy(d => d.Name))
            {
                if (!showHidden && dir.IsHidden)
                    continue;
                    
                sb.Append($"[DIR]  ");
                if (dir.IsLocked) sb.Append("[LOCKED] ");
                if (dir.IsHidden) sb.Append("(hidden) ");
                sb.AppendLine($"{dir.Name}/");
            }
            
            // List files
            foreach (var file in targetDir.Files.OrderBy(f => f.Name))
            {
                if (!showHidden && file.IsHidden)
                    continue;
                    
                sb.Append($"[FILE] ");
                if (file.IsLocked) sb.Append("[LOCKED] ");
                if (file.IsEncrypted) sb.Append("[ENCRYPTED] ");
                if (file.IsHidden) sb.Append("(hidden) ");
                sb.Append($"{file.Name}");
                sb.AppendLine($" ({FormatFileSize(file.Size)})");
            }
            
            var dirCount = targetDir.Subdirectories.Count(d => showHidden || !d.IsHidden);
            var fileCount = targetDir.Files.Count(f => showHidden || !f.IsHidden);
            
            if (dirCount == 0 && fileCount == 0)
                sb.AppendLine("Directory is empty.");
            else
                sb.AppendLine($"\n{dirCount} directory(ies), {fileCount} file(s)");
                
            return sb.ToString();
        }
        catch (Exception e)
        {
            OnFileSystemError?.Invoke(e);
            return $"Error listing directory: {e.Message}";
        }
    }
    
    public FileOperationResult CreateFile(string path, string content = "", bool encrypted = false)
    {
        try
        {
            var dirPath = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var fileName = Path.GetFileName(path);
            
            if (string.IsNullOrEmpty(fileName))
                return FileOperationResult.Error("Invalid filename");
                
            var directory = string.IsNullOrEmpty(dirPath) ? 
                _currentDirectory : ResolvePath(dirPath, _currentDirectory);
                
            if (directory == null)
                return FileOperationResult.Error($"Directory not found: {dirPath}");
                
            if (directory.IsLocked && !directory.TryUnlock(_playerSkillLevel))
                return FileOperationResult.Error($"Directory is locked (level {directory.LockLevel})");
            
            var file = directory.CreateFile(fileName, content, encrypted);
            CacheDirectoryPaths(directory);
            OnFileAccessed?.Invoke("create", file);
            
            return FileOperationResult.Success($"File created: {file.FullPath}", file);
        }
        catch (Exception e)
        {
            OnFileSystemError?.Invoke(e);
            return FileOperationResult.Error($"Failed to create file: {e.Message}");
        }
    }
    
    public FileOperationResult ReadFile(string path, bool force = false)
    {
        try
        {
            var file = GetFile(path);
            if (file == null)
                return FileOperationResult.Error($"File not found: {path}");
                
            if (file.IsLocked && !file.TryUnlock(_playerSkillLevel))
                return FileOperationResult.Error($"File is locked (level {file.LockLevel})");
                
            if (file.IsEncrypted && !file.TryDecrypt(_playerSkillLevel))
                return FileOperationResult.Error($"File is encrypted (level {file.EncryptionLevel})");
            
            OnFileAccessed?.Invoke("read", file);
            return FileOperationResult.Success(file.Content, file);
        }
        catch (Exception e)
        {
            OnFileSystemError?.Invoke(e);
            return FileOperationResult.Error($"Failed to read file: {e.Message}");
        }
    }
    
    public FileOperationResult WriteFile(string path, string content, bool append = false)
    {
        try
        {
            var file = GetFile(path);
            if (file == null)
                return CreateFile(path, content);
                
            if (file.IsLocked && !file.TryUnlock(_playerSkillLevel))
                return FileOperationResult.Error($"File is locked (level {file.LockLevel})");
                
            if (file.IsEncrypted && !file.TryDecrypt(_playerSkillLevel))
                return FileOperationResult.Error($"File is encrypted (level {file.EncryptionLevel})");
            
            var newContent = append ? file.Content + content : content;
            file.UpdateContent(newContent);
            OnFileAccessed?.Invoke("write", file);
            
            return FileOperationResult.Success($"File updated: {file.FullPath}", file);
        }
        catch (Exception e)
        {
            OnFileSystemError?.Invoke(e);
            return FileOperationResult.Error($"Failed to write file: {e.Message}");
        }
    }
    
    public bool DeleteFile(string path)
    {
        try
        {
            var dirPath = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var fileName = Path.GetFileName(path);
            
            var directory = string.IsNullOrEmpty(dirPath) ? 
                _currentDirectory : ResolvePath(dirPath, _currentDirectory);
                
            if (directory == null)
                return false;
                
            if (directory.IsLocked && !directory.TryUnlock(_playerSkillLevel))
                return false;
            
            var file = directory.GetFile(fileName);
            if (file == null)
                return false;
                
            if (file.IsLocked && !file.TryUnlock(_playerSkillLevel))
                return false;
            
            OnFileAccessed?.Invoke("delete", file);
            return directory.DeleteFile(fileName);
        }
        catch (Exception e)
        {
            OnFileSystemError?.Invoke(e);
            return false;
        }
    }
    
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
    
    public void SetPlayerSkill(int skillLevel)
    {
        _playerSkillLevel = Mathf.Max(1, skillLevel);
    }
    
    public FileSystemData GetSaveData()
    {
        var data = new FileSystemData
        {
            CurrentPath = CurrentPath,
            PlayerSkill = _playerSkillLevel
        };
        
        // Serialize directory structure
        var directories = new List<DirectoryData>();
        SerializeDirectory(_rootDirectory, directories);
        data.Directories = directories;
        
        return data;
    }
    
    private void SerializeDirectory(VirtualDirectory dir, List<DirectoryData> output)
    {
        var dirData = new DirectoryData
        {
            Path = dir.FullPath,
            IsHidden = dir.IsHidden,
            IsLocked = dir.IsLocked,
            LockLevel = dir.LockLevel
        };
        
        foreach (var file in dir.Files)
        {
            dirData.Files.Add(new FileData
            {
                Name = file.Name,
                Content = file.Content,
                IsHidden = file.IsHidden,
                IsLocked = file.IsLocked,
                LockLevel = file.LockLevel,
                IsEncrypted = file.IsEncrypted,
                EncryptionLevel = file.EncryptionLevel,
                Created = file.Created,
                Modified = file.Modified
            });
        }
        
        output.Add(dirData);
        
        foreach (var subdir in dir.Subdirectories)
            SerializeDirectory(subdir, output);
    }
    
    public void LoadSaveData(FileSystemData data)
    {
        if (data == null) return;
        
        _playerSkillLevel = data.PlayerSkill;
        
        // Rebuild file system from save data
        _rootDirectory = new VirtualDirectory("root");
        _pathCache.Clear();
        _pathCache["/"] = _rootDirectory;
        _pathCache["/root"] = _rootDirectory;
        
        // Build directory hierarchy
        var dirMap = new Dictionary<string, VirtualDirectory> { ["/root"] = _rootDirectory };
        
        foreach (var dirData in data.Directories.OrderBy(d => d.Path))
        {
            var parentPath = Path.GetDirectoryName(dirData.Path)?.Replace("\\", "/") ?? "/root";
            if (parentPath == "/") parentPath = "/root";
            
            if (dirMap.TryGetValue(parentPath, out var parentDir))
            {
                var dirName = Path.GetFileName(dirData.Path);
                var directory = new VirtualDirectory(dirName, parentDir);
                directory.SetHidden(dirData.IsHidden);
                directory.SetLocked(dirData.IsLocked, dirData.LockLevel);
                
                parentDir.AddDirectory(directory);
                dirMap[dirData.Path] = directory;
                _pathCache[dirData.Path] = directory;
            }
        }
        
        // Add files to directories
        foreach (var dirData in data.Directories)
        {
            if (dirMap.TryGetValue(dirData.Path, out var directory))
            {
                foreach (var fileData in dirData.Files)
                {
                    var file = new VirtualFile(fileData.Name, fileData.Content, 
                                              fileData.IsEncrypted, fileData.EncryptionLevel);
                    file.SetHidden(fileData.IsHidden);
                    file.SetLocked(fileData.IsLocked, fileData.LockLevel);
                    
                    directory.AddFile(file);
                }
            }
        }
        
        // Set current directory
        _currentDirectory = ResolvePath(data.CurrentPath) ?? _rootDirectory;
        OnDirectoryChanged?.Invoke(_currentDirectory);
    }
}

public struct FileOperationResult
{
    public bool Success;
    public string Message;
    public VirtualFile File;
    public string Content;
    
    public static FileOperationResult SuccessResult(string message, VirtualFile file = null) =>
        new FileOperationResult { Success = true, Message = message, File = file };
    
    public static FileOperationResult SuccessResult(string content, string message = "", VirtualFile file = null) =>
        new FileOperationResult { Success = true, Content = content, Message = message, File = file };
    
    public static FileOperationResult Error(string message) =>
        new FileOperationResult { Success = false, Message = message };
}

[System.Serializable]
public class FileSystemData
{
    public string CurrentPath;
    public int PlayerSkill;
    public List<DirectoryData> Directories = new List<DirectoryData>();
}

[System.Serializable]
public class DirectoryData
{
    public string Path;
    public bool IsHidden;
    public bool IsLocked;
    public int LockLevel;
    public List<FileData> Files = new List<FileData>();
}

[System.Serializable]
public class FileData
{
    public string Name;
    public string Content;
    public bool IsHidden;
    public bool IsLocked;
    public int LockLevel;
    public bool IsEncrypted;
    public int EncryptionLevel;
    public DateTime Created;
    public DateTime Modified;
}