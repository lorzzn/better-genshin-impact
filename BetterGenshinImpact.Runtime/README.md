# BetterGI 可引用运行库

`net8.0` 库，直接编译本仓库中的官方识别、图像区域、模型、地图、完整 `TpTask`、
`ReturnMainUiTask`、`CameraRotateTask` 和 `KeyMouseMacroPlayer` 源码。
`BETTERGI_PORTABLE` 只选择平台依赖边界；构建不执行源码补丁、文本替换或方法提取。
桌面 WPF 项目继续使用自己的 Windows 宿主。

宿主实现 `IGameHost`，绑定同一个 Target 的原始截图、绝对/相对鼠标、按钮、按键和滚轮。
用 `GameSession` 包围一次调用；退出时释放本次调用按住的输入。截图的 `Mat` 所有权交给运行库。
官方 `ImageRegion` 负责裁剪与坐标转换，输入最终回到原始 Target 分辨率。
模型和地图缓存按进程隔离：一个进程服务一个脚本/Target，调用顺序执行，不在进程中混用多个 Target。

配置使用原版传送、快捷传送与键位类型；原版识别 JSON、本地化、模型和地图资源继续由任务读取。
神像传送后的可选步行依赖官方路径执行器；尚未绑定时在输入前明确失败。

本阶段 Windows x64 通过 16 项运行库测试，以及 Go 桥接的地图、缩放、宏回放、
取消、模型和 OCR 回归。真实游戏通过主界面与视角识别；首次传送实测出现截图耗时波动，
触发官方 60 秒超时，尚未完成稳定传送验收。完整路径、战斗、JS 宿主与其他操作系统需继续验收。

```powershell
dotnet restore Test/BetterGenshinImpact.RuntimeTest/BetterGenshinImpact.RuntimeTest.csproj --source https://api.nuget.org/v3/index.json
dotnet test Test/BetterGenshinImpact.RuntimeTest/BetterGenshinImpact.RuntimeTest.csproj -c Release --no-restore
```
