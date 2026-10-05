using System.Text.Json;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Script.Project;
using BetterGenshinImpact.Runtime;
using BetterGenshinImpact.Scripting;
using OpenCvSharp;
using Xunit;

namespace BetterGenshinImpact.ScriptingTest;

public sealed class GameScriptHostTests
{
    [Fact]
    public async Task OfficialGlobalsUseConfiguredKeysAndTargetCoordinates()
    {
        using var files = new ScriptFiles("""
            (async () => {
                keyDown('W'); keyUp('W'); keyPress('ESCAPE');
                click(960, 540); moveMouseBy(3, -4); verticalScroll(2);
                inputText('委托');
                const frame = captureGameRegion();
                try { file.writeTextSync('frame.txt', `${frame.width}x${frame.height}`); }
                finally { frame.dispose(); }
                if (genshin.width !== 1280 || getVersion().split('+')[0] !== '0.66.0') throw new Error('wrong game context');
                await sleep(1);
            })()
            """);
        var target = new TestHost();
        var config = new GameConfiguration();
        config.KeyBindingsConfig.GlobalKeyMappingEnabled = true;
        config.KeyBindingsConfig.MoveForward = KeyId.I;
        using var session = new GameSession(target, configuration: config);
        await files.Project.ExecuteWithHostAsync(new GameScriptHost());
        Assert.Equal(new[] { (73, true), (73, false), (27, true), (27, false) }, target.Keys);
        Assert.Equal(new Point(643, 356), target.Pointer);
        Assert.Equal(new[] { (0, true), (0, false) }, target.Buttons);
        Assert.Equal(2, target.ScrollAmount);
        Assert.Equal("委托", target.Text);
        Assert.Equal("1280x720", files.Read("frame.txt"));
    }

    [Fact]
    public async Task OfficialBvFlowRunsInputThroughTheSameTarget()
    {
        using var files = new ScriptFiles("(async () => { await new BvPage().flow().click(300, 180).keyPress('ESCAPE').run(); })()");
        var target = new TestHost();
        using var session = new GameSession(target);
        await files.Project.ExecuteWithHostAsync(new GameScriptHost());
        Assert.Equal(new Point(200, 120), target.Pointer);
        Assert.Equal(new[] { (27, true), (27, false) }, target.Keys);
        Assert.Equal(new[] { (0, true), (0, false) }, target.Buttons);
    }

    [Fact]
    public async Task CancellationDuringOfficialSleepReleasesHeldInput()
    {
        using var files = new ScriptFiles("(async () => { keyDown('W'); leftButtonDown(); await sleep(60000); })()");
        var target = new TestHost();
        using var cancel = new CancellationTokenSource();
        var run = Task.Run(async () =>
        {
            using var session = new GameSession(target, cancel.Token);
            await files.Project.ExecuteWithHostAsync(new GameScriptHost(), cancellationToken: cancel.Token);
        });
        await target.Pressed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(new[] { (87, true), (87, false) }, target.Keys);
        Assert.Equal(new[] { (0, true), (0, false) }, target.Buttons);
    }

    private sealed class TestHost : IGameHost
    {
        public List<(int, bool)> Keys { get; } = [];
        public List<(int, bool)> Buttons { get; } = [];
        public TaskCompletionSource Pressed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Point Pointer;
        public int ScrollAmount;
        public string? Text;
        public Mat Capture(CancellationToken cancellationToken) => new(720, 1280, MatType.CV_8UC3, Scalar.Black);
        public void MovePointer(double x, double y) => Pointer = new Point((int)x, (int)y);
        public void MovePointerBy(int dx, int dy) => Pointer += new Point(dx, dy);
        public void SetKey(int key, bool down) => Keys.Add((key, down));
        public void SetPointerButton(int button, bool down)
        {
            Buttons.Add((button, down));
            if (down) Pressed.TrySetResult();
        }
        public void Scroll(int notches) => ScrollAmount += notches;
        public void InputText(string text) => Text = text;
    }

    private sealed class ScriptFiles : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "bgi-game-host-" + Guid.NewGuid().ToString("N"));
        public ScriptProject Project { get; }
        public ScriptFiles(string code)
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "manifest.json"), JsonSerializer.Serialize(new { name = "test", version = "1.0.0", main = "main.js" }));
            File.WriteAllText(Path.Combine(root, "main.js"), code);
            Project = new ScriptProject(new DirectoryInfo(root));
        }
        public string Read(string name) => File.ReadAllText(Path.Combine(root, name));
        public void Dispose() => Directory.Delete(root, recursive: true);
    }
}
