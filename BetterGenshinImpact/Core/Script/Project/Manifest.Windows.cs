using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BetterGenshinImpact.Model;
using BetterGenshinImpact.Service;

namespace BetterGenshinImpact.Core.Script.Project;

public partial class Manifest
{
    public List<SettingItem> LoadSettingItems(string path)
    {
        if (string.IsNullOrWhiteSpace(SettingsUi))
        {
            return [];
        }

        var settingItems = new List<SettingItem>();
        var settingFile = Path.Combine(path, SettingsUi);
        if (File.Exists(settingFile))
        {
            var json = File.ReadAllText(settingFile);
            settingItems = JsonSerializer.Deserialize<List<SettingItem>>(json, ConfigService.JsonOptions) ?? [];
        }

        return settingItems;
    }

}
