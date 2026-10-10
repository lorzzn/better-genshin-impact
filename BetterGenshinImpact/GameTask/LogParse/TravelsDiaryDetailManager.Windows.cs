using System.Collections.Generic;
using System.Linq;
using Wpf.Ui.Violeta.Controls;

namespace BetterGenshinImpact.GameTask.LogParse;

public partial class TravelsDiaryDetailManager
{
    public static List<(int year, int month)> GetInvolvedMonths(List<LogParse.ConfigGroupEntity> configGroups)
    {
        // HashSet 用于存储不重复的年份和月份
        HashSet<(int year, int month)> involvedMonths = new HashSet<(int year, int month)>();

        foreach (var group in configGroups)
        {
            // 如果 StartDate 有值，添加对应的年份和月份
            if (group.StartDate.HasValue)
            {
                involvedMonths.Add((group.StartDate.Value.Year, group.StartDate.Value.Month));
            }

            // 如果 EndDate 有值，添加对应的年份和月份
            if (group.EndDate.HasValue)
            {
                involvedMonths.Add((group.EndDate.Value.Year, group.EndDate.Value.Month));
            }
        }

        // 返回按年份和月份排序的列表
        return involvedMonths.OrderBy(m => m.year).ThenBy(m => m.month).ToList();
    }

    public static List<ActionItem> loadAllActionItems(GameInfo gameInfo, List<LogParse.ConfigGroupEntity> configGroups)
    {
      return loadAllActionItems(gameInfo,GetInvolvedMonths(configGroups));
    }

    private static partial void ShowInformation(string message) => Toast.Information(message);

    private static partial void ShowWarning(string message) => Toast.Warning(message);
}
