# BetterGI 可引用脚本引擎

直接编译官方 `ScriptProject`、`PackageDocumentLoader`、`Manifest` 和路径工具，
使用原版 ClearScript/V8 7.4.5 引擎与异步/模块语义。WPF 配置页面移到 `.Windows.cs`
分部类，桌面 API 继续调用同一执行入口。

嵌入方通过 `IScriptHost` 注册对象，调用
`ScriptProject.ExecuteWithHostAsync(host, settings, cancellationToken)`。
取消可中断 V8 循环和未决 Promise，宿主异步操作还应绑定同一个取消令牌。
包入口、模块及资源均限制在脚本目录内；普通目录前缀不能充当目录边界。

`GameScriptHost` 复用官方 `EngineExtend` 基础注册、`GlobalMethod`、`Genshin` 地图方法、
`BvPage/BvLocator/BvFlow`、`LimitedFile`、日志和宏回放；截图和输入落到当前 `GameSession`。
键位映射从 WPF ViewModel 移入共享库，桌面应用也调用同一实现。
UID 使用 64 位整数，保留官方 OCR 区域与数字提取规则。

Windows 测试覆盖异步完成、大小写绑定、模块与资源导入、异常、取消及目录越界，
并验证真实 V8 到假 Target 的键位映射、坐标、BvFlow 和取消松键。
`IScriptFileSystem` 为官方 `LimitedFile` 提供可替换存储；默认仍访问包目录，
嵌入平台可将运行数据隔离到 Target 对应的状态目录。HTTP 权限和传输、
`Dispatcher.RunTask` 的任务执行也有独立宿主边界，原桌面行为保留在 `.Windows.cs`。
`ExecuteWithHostAsync` 返回在引擎释放前序列化的 JSON 结果；无结果返回 null。
桌面调用指定 `serializeResult: false`，保留不读取返回值的原行为。
`ScriptOperation.Observe` 可观察已接入的官方任务入口及嵌套阶段，不改变执行算法。

完整路径/战斗/实时触发器仍待接通。基础宿主可运行不等于完整生态脚本可完成游戏任务。
测试运行时需安装对应平台的 `Microsoft.ClearScript.V8.Native` 包；其他平台尚待实机验证。

```powershell
dotnet restore Test/BetterGenshinImpact.ScriptingTest/BetterGenshinImpact.ScriptingTest.csproj --source https://api.nuget.org/v3/index.json
dotnet test Test/BetterGenshinImpact.ScriptingTest/BetterGenshinImpact.ScriptingTest.csproj -c Release --no-restore
```

模块与宿主选项沿用官方实现，参考 [ClearScript 模块文档](https://microsoft.github.io/ClearScript/2023/01/24/module-interop.html)。
