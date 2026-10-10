using System.Dynamic;
using System.Text.Json;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Script;
using BetterGenshinImpact.Core.Script.Group;
using BetterGenshinImpact.Core.Script.Project;
using BetterGenshinImpact.GameTask.TaskProgress;
using BetterGenshinImpact.Model;
using BetterGenshinImpact.Runtime;
using Microsoft.ClearScript;
using OpenCvSharp;
using Xunit;

namespace BetterGenshinImpact.ScriptingTest;

[Collection(nameof(RuntimeEnvironment))]
public sealed class ScriptGroupRunnerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "bgi-group-test-" + Guid.NewGuid().ToString("N"));
    private readonly string? previousLibrary = RuntimeEnvironment.LibraryRoot;
    private readonly string previousState = RuntimeEnvironment.StateRoot;
    private readonly Func<PathingPartyConfig?, IScriptHost>? previousFactory = ScriptProject.HostFactory;
    private readonly List<string> runs = [];

    public ScriptGroupRunnerTests()
    {
        RuntimeEnvironment.LibraryRoot = Path.Combine(root, "library");
        RuntimeEnvironment.StateRoot = Path.Combine(root, "state");
        ScriptProject.HostFactory = partyConfig => new RecordingHost(runs, partyConfig);
    }

    public void Dispose()
    {
        RuntimeEnvironment.LibraryRoot = previousLibrary;
        RuntimeEnvironment.StateRoot = previousState;
        ScriptProject.HostFactory = previousFactory;
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task RunsEnabledItemsInOrderWithRunCountSettingsAndGroupConfig()
    {
        WriteScript("first", "record(`first:${settings.tag}:${current()}:${party()}`);");
        WriteScript("second", "record(`second:${current()}`);");
        WriteScript("disabled", "record('disabled');");
        var group = new ScriptGroup { Name = "日常" };
        group.Config.PathingConfig.PartyName = "主队";
        dynamic settings = new ExpandoObject();
        settings.tag = "A";
        group.AddProject(new ScriptGroupProject { Name = "first", FolderName = "first", Type = "Javascript", Status = "Enabled", RunNum = 2, JsScriptSettingsObject = settings });
        group.AddProject(new ScriptGroupProject { Name = "disabled", FolderName = "disabled", Type = "Javascript", Status = "Disabled" });
        group.AddProject(new ScriptGroupProject { Name = "second", FolderName = "second", Type = "Javascript", Status = "Enabled" });

        var host = new TestRunnerHost();
        using (new GameSession(new BlackFrames()))
            await new ScriptGroupRunner(host).RunMulti(group.Projects, group.Name);

        Assert.Equal(new[] { "first:A:first:主队", "first:A:first:主队", "second:second" }, runs);
        Assert.Equal(1, host.Started);
        Assert.Equal(new[] { "first", "first", "second" }, host.Items);
        Assert.Null(ScriptGroupRunner.CurrentProject);
        // Upstream saves the group after a JS item that carries settings.
        var saved = ScriptGroup.FromJson(File.ReadAllText(Path.Combine(root, "library", "User", "ScriptGroup", "日常.json")));
        Assert.Equal(new[] { "first", "disabled", "second" }, saved.Projects.Select(p => p.Name));
    }

    [Fact]
    public async Task ItemFailureIsLoggedAndTheGroupContinues()
    {
        WriteScript("broken", "throw new Error('boom');");
        WriteScript("after", "record('after');");
        var group = new ScriptGroup { Name = "失败继续" };
        group.AddProject(new ScriptGroupProject { Name = "broken", FolderName = "broken", Type = "Javascript", Status = "Enabled" });
        group.AddProject(new ScriptGroupProject { Name = "after", FolderName = "after", Type = "Javascript", Status = "Enabled" });

        using (new GameSession(new BlackFrames()))
            await new ScriptGroupRunner(new TestRunnerHost()).RunMulti(group.Projects, group.Name);

        Assert.Equal(new[] { "after" }, runs);
    }

    [Fact]
    public async Task ShellItemsAreRefusedByTheEmbeddedRuntime()
    {
        WriteScript("after", "record('after');");
        var group = new ScriptGroup { Name = "shell" };
        group.AddProject(new ScriptGroupProject { Name = "echo hi", Type = "Shell", Status = "Enabled" });
        group.AddProject(new ScriptGroupProject { Name = "after", FolderName = "after", Type = "Javascript", Status = "Enabled" });

        using (new GameSession(new BlackFrames()))
            await new ScriptGroupRunner(new TestRunnerHost()).RunMulti(group.Projects, group.Name);

        Assert.Equal(new[] { "after" }, runs);
    }

    [Fact]
    public async Task CancellationStopsTheGroup()
    {
        WriteScript("wait", "await new Promise(() => {});");
        WriteScript("after", "record('after');");
        var group = new ScriptGroup { Name = "取消" };
        group.AddProject(new ScriptGroupProject { Name = "wait", FolderName = "wait", Type = "Javascript", Status = "Enabled" });
        group.AddProject(new ScriptGroupProject { Name = "after", FolderName = "after", Type = "Javascript", Status = "Enabled" });
        using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var run = Task.Run(async () =>
        {
            using var session = new GameSession(new BlackFrames(), cancel.Token);
            await new ScriptGroupRunner(new TestRunnerHost()).RunMulti(group.Projects, group.Name);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Empty(runs);
    }

    [Fact]
    public async Task OneDragonRunsConfiguredGroupsInOrderAndSkipsDisabledTasks()
    {
        var config = new OneDragonFlowConfig { Name = "默认配置" };
        config.TaskDefinitions["a"] = "第一组";
        config.TaskDefinitions["b"] = "停用组";
        config.TaskDefinitions["c"] = "第二组";
        config.TaskEnabledList["a"] = true;
        config.TaskEnabledList["b"] = false;
        config.TaskEnabledList["c"] = true;
        config.TaskOrder = ["a", "b", "c"];
        var host = new TestOneDragonHost();

        using (new GameSession(new BlackFrames()))
            await new OneDragonRunner(host).RunAsync(config, OneDragonRunner.LoadTaskList(config));

        Assert.Equal(new[] { "第一组", "第二组" }, host.Groups);
        Assert.Equal(1, host.Saved);
        // Only the final reward check runs through the host thread here; it
        // needs the real game screen, so the test host records it instead.
        Assert.Equal(1, host.Threads);
    }

    private void WriteScript(string folder, string body)
    {
        var directory = Path.Combine(root, "library", "User", "JsScript", folder);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(new { name = folder, version = "1.0.0", main = "main.js" }));
        File.WriteAllText(Path.Combine(directory, "main.js"), $"(async () => {{ {body} }})()");
    }

    public sealed class RecordingHost(List<string> runs, PathingPartyConfig? partyConfig) : IScriptHost
    {
        public void Configure(IScriptEngine engine, string projectPath, string[] searchPaths)
        {
            engine.AddHostObject("record", new Action<string>(runs.Add));
            engine.AddHostObject("current", new Func<string>(() => ScriptGroupRunner.CurrentProject?.Name ?? ""));
            engine.AddHostObject("party", new Func<string>(() => partyConfig?.PartyName ?? ""));
        }
    }

    private sealed class TestRunnerHost : IScriptGroupRunnerHost
    {
        public int Started { get; private set; }
        public List<string> Items { get; } = [];
        public Task StartGameTask() { Started++; return Task.CompletedTask; }
        public IEnumerable<ScriptGroup> GetScriptGroups() => [];
        public Task RunThreadAsync(Func<Task> action) => action();
        public void OnProjectStarting(ScriptGroupProject project) => Items.Add(project.Name);
        public void SaveTaskProgress(TaskProgress taskProgress) { }
        public void OnProjectEnded(TaskProgress taskProgress) { }
    }

    private sealed class TestOneDragonHost : IOneDragonRunnerHost
    {
        public List<string> Groups { get; } = [];
        public int Saved { get; private set; }
        public int Threads { get; private set; }
        public Task StartGameTask() => Task.CompletedTask;
        public Task RunThreadAsync(Func<Task> action) { Threads++; return Task.CompletedTask; }
        public Task RunScriptGroup(string name) { Groups.Add(name); return Task.CompletedTask; }
        public void SaveConfig(OneDragonFlowConfig config) => Saved++;
        public void OnNextTaskConsumed(OneDragonFlowConfig config) { }
        public void Warning(string message) { }
        public void Error(string message) => throw new InvalidOperationException(message);
        public void OnCompleted(OneDragonFlowConfig config) { }
    }

    private sealed class BlackFrames : IGameHost
    {
        public Mat Capture(CancellationToken cancellationToken) => new(1080, 1920, MatType.CV_8UC3, Scalar.Black);
        public void MovePointer(double x, double y) { }
        public void MovePointerBy(int dx, int dy) { }
        public void SetKey(int key, bool down) { }
        public void SetPointerButton(int button, bool down) { }
        public void Scroll(int notches) { }
        public void InputText(string text) { }
    }
}
