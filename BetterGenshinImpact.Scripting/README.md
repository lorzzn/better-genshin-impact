# BetterGI 可引用脚本引擎

直接编译官方 `ScriptProject`、`PackageDocumentLoader`、`Manifest` 和路径工具，
使用原版 ClearScript/V8 7.4.5 引擎与异步/模块语义。WPF 配置页面移到 `.Windows.cs`
分部类，桌面 API 继续调用同一执行入口。

嵌入方通过 `IScriptHost` 注册对象，调用
`ScriptProject.ExecuteWithHostAsync(host, settings, cancellationToken)`。
取消可中断 V8 循环和未决 Promise，宿主异步操作还应绑定同一个取消令牌。
包入口、模块及资源均限制在脚本目录内；普通目录前缀不能充当目录边界。

当前 Windows 8 项测试覆盖异步完成、大小写绑定、模块与资源导入、异常、取消及目录越界。
这一步只拆分官方引擎，完整游戏宿主对象和平台 MAA 切换仍待后续接通。
测试运行时需安装对应平台的 `Microsoft.ClearScript.V8.Native` 包；其他平台尚待实机验证。

```powershell
dotnet restore Test/BetterGenshinImpact.ScriptingTest/BetterGenshinImpact.ScriptingTest.csproj --source https://api.nuget.org/v3/index.json
dotnet test Test/BetterGenshinImpact.ScriptingTest/BetterGenshinImpact.ScriptingTest.csproj -c Release --no-restore
```

模块与宿主选项沿用官方实现，参考 [ClearScript 模块文档](https://microsoft.github.io/ClearScript/2023/01/24/module-interop.html)。
