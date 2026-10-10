using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Script.Dependence;
using Microsoft.ClearScript;

namespace BetterGenshinImpact.Core.Script.Project;

public partial class ScriptProject
{
    public ScrollViewer? LoadSettingUi(dynamic context)
    {
        var settingItems = Manifest.LoadSettingItems(ProjectPath);
        if (settingItems.Count == 0)
        {
            return null;
        }

        var stackPanel = new StackPanel
        {
            Margin = new Thickness(0, 0, 20, 0) // 给右侧滚动条留出位置
        };
        foreach (var item in settingItems)
        {
            var controls = item.ToControl(context);
            foreach (var control in controls)
            {
                stackPanel.Children.Add(control);
            }
        }

        var scrollViewer = new ScrollViewer
        {
            Content = stackPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 350 // 设置最大高度
        };

        return scrollViewer;
    }

    public Task ExecuteAsync(dynamic? context = null, PathingPartyConfig? partyConfig = null)
        => ExecuteWithHostAsync(new DesktopScriptHost(partyConfig), (object?)context, serializeResult: false);

    private sealed class DesktopScriptHost(PathingPartyConfig? partyConfig) : IScriptHost
    {
        public void Configure(IScriptEngine engine, string projectPath, string[] searchPaths)
        {
            GlobalMethod.SetGameMetrics(1920, 1080);
            EngineExtend.InitHost(engine, projectPath, searchPaths, partyConfig);
        }
    }
}
