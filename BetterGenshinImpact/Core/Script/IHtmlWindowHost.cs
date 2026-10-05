namespace BetterGenshinImpact.Core.Script;

/// <summary>Window transport only. HtmlMask retains the official message queues.</summary>
public interface IHtmlWindowHost
{
    string Show(string url, string? id, string workDir, System.Action<string> initializeMessages);
    bool Close(string id);
    string[] GetWindowIds();
    bool Exists(string id);
    void SetClickThrough(string id, bool enabled);
    bool GetClickThrough(string id);
    void ToggleClickThrough(string id);
    void NotifyFlush(string id);
}
