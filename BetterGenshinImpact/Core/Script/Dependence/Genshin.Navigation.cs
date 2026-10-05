using System;
using System.Threading.Tasks;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.AutoPathing;
using BetterGenshinImpact.GameTask.AutoTrackPath;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Common.Job;
using BetterGenshinImpact.GameTask.Common.Map;
using BetterGenshinImpact.GameTask.Common.Map.Maps;
using BetterGenshinImpact.GameTask.Common.Map.Maps.Base;
using BetterGenshinImpact.Helpers.Extensions;
using OpenCvSharp;
using static BetterGenshinImpact.GameTask.Common.TaskControl;
#if BETTERGI_PORTABLE
using CaptureRect = OpenCvSharp.Rect;
#else
using CaptureRect = Vanara.PInvoke.RECT;
#endif

namespace BetterGenshinImpact.Core.Script.Dependence;

public partial class Genshin
{
    private readonly CaptureRect captureAreaRect = TaskContext.Instance().SystemInfo.CaptureAreaRect;
    /// <summary>
    /// 游戏宽度
    /// </summary>
    public int Width => captureAreaRect.Width;

    /// <summary>
    /// 游戏高度
    /// </summary>
    public int Height => captureAreaRect.Height;

    /// <summary>
    /// 游戏窗口大小相比1080P的缩放比例
    /// </summary>
    public double ScaleTo1080PRatio { get; } = TaskContext.Instance().SystemInfo.ScaleTo1080PRatio;

    /// <summary>
    /// 系统屏幕的DPI缩放比例
    /// </summary>
    public double ScreenDpiScale => TaskContext.Instance().DpiScale;
    
    /// <summary>
    /// 通过 OCR 识别当前角色的 UID
    /// </summary>
    /// <returns>UID 数字，如果识别失败则返回 0</returns>
    public Task<long> Uid()
    {
        return Task.FromResult(Bv.Uid());
    }
    
    public Lazy<NavigationInstance> LazyNavigationInstance { get; } = new(() =>
    {
        var matchingMethod = TaskContext.Instance().Config.PathingConditionConfig.MapMatchingMethod;
        Navigation.WarmUp(matchingMethod);
        return new NavigationInstance();
    });

    /// <summary>
    /// 传送到指定位置
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public async Task Tp(double x, double y)
    {
        await ScriptOperation.RunAsync("game.tp", () => new TpTask(CancellationContext.Instance.Token).Tp(x, y));
    }

    public async Task Tp(double x, double y, string mapName, bool force)
    {
        await ScriptOperation.RunAsync("game.tp", () => new TpTask(CancellationContext.Instance.Token).Tp(x, y, mapName, force));
    }

    public async Task Tp(double x, double y, bool force)
    {
        await ScriptOperation.RunAsync("game.tp", () => new TpTask(CancellationContext.Instance.Token).Tp(x, y, MapTypes.Teyvat.ToString(), force));
    }

    /// <summary>
    /// 传送到指定位置
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public async Task Tp(string x, string y)
    {
        double.TryParse(x, out var dx);
        double.TryParse(y, out var dy);
        await Tp(dx, dy);
    }

    public async Task Tp(string x, string y, bool force)
    {
        double.TryParse(x, out var dx);
        double.TryParse(y, out var dy);
        await Tp(dx, dy, force);
    }


    #region 大地图操作

    /// <summary>
    /// 移动大地图到指定坐标
    /// </summary>
    /// <remarks>
    /// 与内置传送功能不同，此方法不会多次重试。
    /// 为避免初次中心点识别失败，建议先使用 SetBigMapZoomLevel 设置合适的大地图缩放等级。
    /// </remarks>
    /// <param name="x">目标X坐标</param>
    /// <param name="y">目标Y坐标</param>
    /// <param name="forceCountry">强制指定移动大地图时先切换的国家，默认为null</param>
    public async Task MoveMapTo(double x, double y, string? forceCountry = null)
    {
        TpTask tpTask = new TpTask(CancellationContext.Instance.Token);
        await tpTask.CheckInBigMapUi();
        await tpTask.SwitchRecentlyCountryMap(x, y, forceCountry);
        await tpTask.MoveMapTo(x, y, MapTypes.Teyvat.ToString());
    }

    /// <summary>
    /// 点击大地图上的指定坐标。
    /// </summary>
    /// <remarks>
    /// 该方法会先把目标移动到大地图的可点击安全区域，再执行一次点击。
    /// </remarks>
    /// <param name="x">目标X坐标。</param>
    /// <param name="y">目标Y坐标。</param>
    /// <param name="forceCountry">强制指定移动大地图时先切换的国家，默认为null。</param>
    public async Task ClickMapPoint(double x, double y, string? forceCountry = null)
    {
        TpTask tpTask = new TpTask(CancellationContext.Instance.Token);
        await tpTask.CheckInBigMapUi();
        await tpTask.SwitchRecentlyCountryMap(x, y, forceCountry);
        await tpTask.ClickMapPoint(x, y, MapTypes.Teyvat.ToString());
    }

    /// <summary>
    /// 移动大地图到指定坐标
    /// </summary>
    /// <remarks>
    /// 与内置传送功能不同，此方法不会多次重试。
    /// 为避免初次中心点识别失败，建议先使用 SetBigMapZoomLevel 设置合适的大地图缩放等级。
    /// </remarks>
    /// <param name="x">目标X坐标</param>
    /// <param name="y">目标Y坐标</param>
    /// <param name="mapName">指定要移动的大地图</param>
    public async Task MoveIndependentMapTo(int x, int y, string mapName, string? forceCountry = null)
    {
        TpTask tpTask = new TpTask(CancellationContext.Instance.Token);
        await tpTask.CheckInBigMapUi();
        // 切换地区
        if (mapName == MapTypes.Teyvat.ToString())
        {
            // 计算传送点位置离哪张地图切换后的中心点最近，切换到该地图
            await tpTask.SwitchRecentlyCountryMap(x, y, forceCountry);
        }
        else
        {
            // 直接切换地区
            await tpTask.SwitchArea(MapTypesExtensions.ParseFromName(mapName).GetDescription());
        }
        await tpTask.MoveMapTo(x, y, mapName);
    }

    /// <summary>
    /// 获取当前大地图缩放等级
    /// </summary>
    /// <returns>当前大地图缩放等级，范围1.0-6.0</returns>
    public double GetBigMapZoomLevel()
    {
        TpTask tpTask = new(CancellationContext.Instance.Token);
        using var capture = CaptureToRectArea();
        return tpTask.GetBigMapZoomLevel(capture);
    }

    /// <summary>
    /// 将大地图缩放等级设置为指定值
    /// </summary>
    /// <remarks>
    /// 缩放等级说明：
    /// - 数值范围：1.0(最大地图) 到 6.0(最小地图)
    /// - 缩放效果：数值越大，地图显示范围越广，细节越少
    /// - 缩放位置：1.0 对应缩放条最上方，6.0 对应缩放条最下方
    /// - 推荐范围：建议在 2.0 到 5.0 之间调整，过大或过小可能影响操作
    /// </remarks>
    /// <param name="zoomLevel">目标缩放等级，范围 1.0-6.0</param>
    public async Task SetBigMapZoomLevel(double zoomLevel)
    {
        TpTask tpTask = new(CancellationContext.Instance.Token);
        double currentZoomLevel = GetBigMapZoomLevel();
        await tpTask.AdjustMapZoomLevel(currentZoomLevel, zoomLevel);
    }

    /// <summary>
    /// 传送到用户指定的七天神像
    /// </summary>
    public async Task TpToStatueOfTheSeven()
    {
        TpTask tpTask = new TpTask(CancellationContext.Instance.Token);
        await ScriptOperation.RunAsync("game.tpToStatueOfTheSeven", tpTask.TpToStatueOfTheSeven);
    }

    /// <summary>
    /// 获取当前在大地图上的位置坐标
    /// </summary>
    /// <returns>包含X和Y坐标的Point2f结构体</returns>
    public Point2f? GetPositionFromBigMap()
    {
        TpTask tpTask = new TpTask(CancellationContext.Instance.Token);
        return tpTask.GetPositionFromBigMap(MapTypes.Teyvat.ToString());
    }

    /// <summary>
    /// 获取当前在大地图上的位置坐标
    /// </summary>
    /// <param name="mapName">大地图名称</param>
    /// <returns>包含X和Y坐标的Point2f结构体</returns>
    public Point2f? GetPositionFromBigMap(string mapName)
    {
        TpTask tpTask = new TpTask(CancellationContext.Instance.Token);
        return tpTask.GetPositionFromBigMap(mapName);
    }

    /// <summary>
    /// 获取当前在小地图上的位置坐标
    /// </summary>
    /// <returns>包含X和Y坐标的Point2f结构体</returns>
    public Point2f? GetPositionFromMap()
    {
        return GetPositionFromMap(MapTypes.Teyvat.ToString());
    }
    
    public Point2f? GetPositionFromMapWithMatchingMethod(string matchingMethod)
    {
        return GetPositionFromMapWithMatchingMethod(nameof(MapTypes.Teyvat), matchingMethod);
    }

    public float GetCameraOrientation()
    {
        using var imageRegion = CaptureToRectArea();
        return CameraOrientation.Compute(imageRegion.SrcMat);
    }

    /// <summary>
    /// 获取当前在小地图上的位置坐标，如果缓存时间内有匹配成功的坐标优先返回缓存坐标，否则调用NavigationInstance的getPositionStable
    /// </summary>
    /// <param name="mapName">大地图名称</param>
    /// <param name="cacheTimeMs">缓存时间，单位毫秒，默认900ms</param>
    /// <returns>包含X和Y坐标的Point2f结构体</returns>
    public Point2f? GetPositionFromMap(string mapName, int cacheTimeMs = 900)
    {
        var matchingMethod = TaskContext.Instance().Config.PathingConditionConfig.MapMatchingMethod;
        return GetPositionFromMapWithMatchingMethod(mapName,matchingMethod, cacheTimeMs);
    }
    
    public Point2f? GetPositionFromMapWithMatchingMethod(string mapName, string matchingMethod, int cacheTimeMs = 900)
    {
        using var imageRegion = CaptureToRectArea();
        if (!Bv.IsInMainUi(imageRegion))
        {
            throw new InvalidOperationException("不在主界面，无法识别小地图坐标");
        }
        return MapManager.GetMap(mapName, matchingMethod)
            .ConvertImageCoordinatesToGenshinMapCoordinates(LazyNavigationInstance.Value
                .GetPositionStableByCache(imageRegion, mapName, matchingMethod, cacheTimeMs));
    }
    
    /// <summary>
    /// 获取当前在小地图上的位置坐标, 局部匹配, 需要世界坐标, 在坐标附近匹配, 失败不进行全局匹配
    /// </summary>
    /// <param name="mapName">大地图名称</param>
    /// <param name="x">世界坐标x</param>
    /// <param name="y">世界坐标y</param>
    /// <returns>包含X和Y坐标的Point2f结构体</returns>
    public Point2f? GetPositionFromMap(string mapName, float x, float y)
    {
        using var imageRegion = CaptureToRectArea();
        if (!Bv.IsInMainUi(imageRegion))
        {
            throw new InvalidOperationException("不在主界面，无法识别小地图坐标");
        }
        var matchingMethod = TaskContext.Instance().Config.PathingConditionConfig.MapMatchingMethod;
        var sceneMap = MapManager.GetMap(mapName, matchingMethod);
        var navigationInstance = LazyNavigationInstance.Value;
        var pos = sceneMap.ConvertGenshinMapCoordinatesToImageCoordinates(new Point2f(x, y));
        navigationInstance.SetPrevPosition(pos.X, pos.Y);
        return sceneMap.ConvertImageCoordinatesToGenshinMapCoordinates(navigationInstance.GetPosition(imageRegion, mapName, matchingMethod));
    }

    #endregion 大地图操作
    /// <summary>
    /// 返回主界面
    /// </summary>
    /// <returns></returns>
    public async Task ReturnMainUi()
    {
        await ScriptOperation.RunAsync("game.returnMainUi", () => new ReturnMainUiTask().Start(CancellationContext.Instance.Token));
    }
}
