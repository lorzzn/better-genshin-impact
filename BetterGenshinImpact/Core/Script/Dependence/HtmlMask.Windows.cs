using BetterGenshinImpact.View;

namespace BetterGenshinImpact.Core.Script.Dependence;

public partial class HtmlMask
{
    public HtmlMask(string workDir) : this(workDir, new DesktopHtmlWindows()) { }

    private sealed class DesktopHtmlWindows : IHtmlWindowHost
    {
        public string Show(string url, string? id, string workDir, System.Action<string> initializeMessages)
        {
            id ??= System.Guid.NewGuid().ToString("N");
            initializeMessages(id);
            return HtmlMaskWindow.Show(url, id, workDir);
        }
        public bool Close(string id) => HtmlMaskWindow.Close(id);
        public string[] GetWindowIds() => HtmlMaskWindow.GetWindowIds();
        public bool Exists(string id) => HtmlMaskWindow.Exists(id);
        public void SetClickThrough(string id, bool enabled) => HtmlMaskWindow.SetClickThrough(id, enabled);
        public bool GetClickThrough(string id) => HtmlMaskWindow.GetClickThrough(id);
        public void ToggleClickThrough(string id) => HtmlMaskWindow.ToggleClickThrough(id);
        public void NotifyFlush(string id) => HtmlMaskWindow.NotifyFlush(id);
    }
}
