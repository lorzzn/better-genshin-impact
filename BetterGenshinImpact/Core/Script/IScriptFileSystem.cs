using System.IO;
using System.Threading.Tasks;

namespace BetterGenshinImpact.Core.Script;

/// <summary>
/// Storage boundary for LimitedFile. Paths are already normalized inside the script package.
/// Hosts may overlay per-account state while keeping the package immutable.
/// </summary>
public interface IScriptFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    string[] GetFiles(string path);
    string[] GetDirectories(string path);
    void CreateDirectory(string path);
    string ReadAllText(string path);
    Task<string> ReadAllTextAsync(string path);
    Stream OpenRead(string path);
    void WriteAllText(string path, string content, bool append);
    Task WriteAllTextAsync(string path, string content, bool append);
    void WriteAllBytes(string path, byte[] content);
    void Move(string source, string destination);
}

public sealed class PhysicalScriptFileSystem : IScriptFileSystem
{
    public static PhysicalScriptFileSystem Instance { get; } = new();
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public string[] GetFiles(string path) => Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
    public string[] GetDirectories(string path) => Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);
    public Stream OpenRead(string path) => File.OpenRead(path);
    public void WriteAllText(string path, string content, bool append)
    {
        if (append) File.AppendAllText(path, content);
        else File.WriteAllText(path, content);
    }
    public Task WriteAllTextAsync(string path, string content, bool append) => append
        ? File.AppendAllTextAsync(path, content) : File.WriteAllTextAsync(path, content);
    public void WriteAllBytes(string path, byte[] content) => File.WriteAllBytes(path, content);
    public void Move(string source, string destination) => Directory.Move(source, destination);
}
