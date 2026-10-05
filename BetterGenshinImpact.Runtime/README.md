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
神像传送后的可选步行直接调用官方 PathExecutor，不再保留可选的外部路径实现。

本阶段 Windows x64 通过 16 项运行库测试，以及 Go 桥接的地图、缩放、宏回放、
取消、模型和 OCR 回归。真实游戏通过主界面与视角识别；首次传送实测出现截图耗时波动，
触发官方 60 秒超时，尚未完成稳定传送验收。完整路径、战斗、JS 宿主与其他操作系统需继续验收。

```powershell
dotnet restore Test/BetterGenshinImpact.RuntimeTest/BetterGenshinImpact.RuntimeTest.csproj --source https://api.nuget.org/v3/index.json
dotnet test Test/BetterGenshinImpact.RuntimeTest/BetterGenshinImpact.RuntimeTest.csproj -c Release --no-restore
```

## 2026-10-05 执行库扩展（开发中）

直接编入官方路径/队伍/战斗、背包与合成、对话与拾取、钓鱼/伐木、秘境/首领/
地脉花/幽境/七圣召唤/料理/角色养成任务，以及官方行为树构建和执行代码。
游戏策略、重试与识别条件仍来自相同源码文件，没有增加另一套游戏执行器。

- 抽出 `IRecognitionSurface`、`GameServices` 和截图配置等平台边界，桌面入口继续可用。
- 官方 `ScriptTriggerCollection`、`TriggerProcessor` 复用命名注册、优先级、独占及 UI 判断。
  嵌入式计时器只服务当前 `GameSession`，退出时取消并等待回收，失败传播给本次调用。
- 官方等待/暂停/恢复逻辑共享到 `TaskControl.Wait.cs`；按键状态、窗口焦点由 Target 提供。
- `RunnerContext` 和取消源绑定当前会话；键鼠按下集合串行访问，退出时释放。
- 本地窗口枚举和电源管理不会误作用于嵌入宿主。Target 无音频时沿用官方对话固定等待；
  渠道登录需要宿主提供元数据与驱动，缺失明确失败。
- 识别资源及本地化随运行库发布。截图后台写入先复制图片，避免调用结束后访问已释放 Mat。

这次扩展只进行代码检查和编译；尚未完成全部 JS 注册与平台配置接入，不能视为每日委托
实机通过，也未运行 Windows/Linux/macOS 游戏验收。

## 2026-10-05 官方 JS 游戏宿主

`GameScriptHost` 使用同一份 `Dispatcher.Execution`、`AutoPathingScript`、队伍与战斗
对象注册。内置 SoloTask 先走官方实现，只有未知任务名才委托宿主的 MAA 映射。
Windows 的视图模型设置通过 `IScriptTaskDefaults` 提供，嵌入宿主传入原版配置类型；
战斗/七圣策略选择共用 `TaskStrategyResolver`，不复写策略规则。

路径与直接任务入口报告可嵌套阶段，用户取消与自定义取消源共同生效。
脚本文件仍经宿主文件接口读写，官方路径重试与异常处理语义保留。
本次 Release 编译通过（增量构建 2 条警告、0 错误）；没有执行测试或游戏。
运行配置的后台编辑与持久化、资源目录隔离、HTML 遮罩及输入钩子仍需继续接入。

### 宿主配置和目录边界

`RuntimeEnvironment.StateRoot` 由宿主指定，接收官方 `User` 运行数据、`log` 与
`Cache`。只读策略、路径及键鼠资源从 `LibraryRoot/User/{AutoFight,AutoGeniusInvokation,
AutoPathing,KeyMouseScript}` 读取，未指定时沿用 `AssetRoot`；游戏识别资源来自发布包。
地图原始图片和预计算特征仍按官方地图资源布局读取，缺少特征时的生成目录继续整理。
官方模型工厂读取本次会话硬件配置，OCR 实例的首次创建按模型串行处理。
`ScriptApiCatalog` 报告已编译接口；该清单不能代替依赖完整性或游戏验收结论。
