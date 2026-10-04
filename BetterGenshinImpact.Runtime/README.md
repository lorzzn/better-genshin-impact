# BetterGI 可引用运行库

`net8.0` 库，直接编译本仓库中的官方识别、图像区域、模型和地图源码。
`BETTERGI_PORTABLE` 只选择平台依赖边界；构建不执行源码补丁、文本替换或方法提取。
桌面 WPF 项目继续使用自己的 Windows 宿主。

宿主实现 `IGameHost`，绑定同一个 Target 的原始截图、绝对/相对鼠标、按钮、按键和滚轮。
用 `GameSession` 包围一次调用；退出时释放本次调用按住的输入。截图的 `Mat` 所有权交给运行库。
官方 `ImageRegion` 负责裁剪与坐标转换，输入最终回到原始 Target 分辨率。
模型和地图缓存按进程隔离：一个进程服务一个脚本/Target，调用顺序执行，不在进程中混用多个 Target。

本阶段验证了 Windows x64 编译、区域缩放/点击、取消松键、输入异常及会话释放。
完整任务、JS 宿主、其他操作系统和真实游戏效果需分别验收，不能以库编译通过代替。

```powershell
dotnet restore Test/BetterGenshinImpact.RuntimeTest/BetterGenshinImpact.RuntimeTest.csproj --source https://api.nuget.org/v3/index.json
dotnet test Test/BetterGenshinImpact.RuntimeTest/BetterGenshinImpact.RuntimeTest.csproj -c Release --no-restore
```
