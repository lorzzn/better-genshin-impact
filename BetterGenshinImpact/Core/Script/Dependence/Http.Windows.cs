using System;
using System.Linq;
using BetterGenshinImpact.GameTask;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Script.Dependence;

public partial class Http
{
    public Http() : this(CheckDesktopHttpPermission, logger: App.GetLogger<Http>()) { }

    private static void CheckDesktopHttpPermission(string url)
    {
        var currentProject = TaskContext.Instance().CurrentScriptProject;
        if (!currentProject?.AllowJsHTTP ?? false)
        {
            throw new UnauthorizedAccessException("当前JS脚本不允许使用HTTP请求，请在调度器通用设置中启用“JS HTTP权限”");
        }
        var allowedUrls = currentProject?.Project?.Manifest.HttpAllowedUrls ?? [];
        if (allowedUrls.Length == 0)
        {
            throw new UnauthorizedAccessException("当前JS脚本没有配置允许请求的URL，请在脚本的manifest.json中配置http_allowed_urls");
        }
        if (allowedUrls.Any(allowedUrl =>
        {
            // fuzzy match
            var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(allowedUrl).Replace("\\*", ".*") + "$";
            App.GetLogger<Http>().LogDebug($"[HTTP] 检查URL {url} 是否符合: {pattern}");
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            return regex.IsMatch(url);
        }))
        {
            return;
        }
        throw new UnauthorizedAccessException($"当前JS脚本不允许请求此URL: {url}，请在脚本的manifest.json中配置http_allowed_urls，当前允许的URL列表: [{string.Join(", ", allowedUrls)}]");
    }

}
