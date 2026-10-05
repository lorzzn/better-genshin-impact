using BetterGenshinImpact.Core.Script.Dependence;
using BetterGenshinImpact.Core.Script;
using BetterGenshinImpact.Core.Script.Dependence.Model;
using Xunit;

namespace BetterGenshinImpact.ScriptingTest;

public sealed class HostBoundaryTests
{
    [Fact]
    public async Task ProgressPreservesNestingAndReportsOriginalFailure()
    {
        var phases = new List<ScriptPhase>();
        using (ScriptOperation.Observe(phases.Add))
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ScriptOperation.RunAsync("outer", () => ScriptOperation.RunAsync<int>("inner", () =>
                    Task.FromException<int>(new InvalidOperationException("original failure")))));
            Assert.Equal("original failure", error.Message);
        }
        Assert.Equal(new[] { "started", "started", "failed", "failed" }, phases.Select(p => p.Status));
        Assert.Equal(phases[0].Id, phases[1].ParentId);
        Assert.Equal(phases[1].Id, phases[2].Id);
        Assert.Equal(phases[0].Id, phases[3].Id);
        Assert.Equal(3, await ScriptOperation.RunAsync("unobserved", () => Task.FromResult(3)));
        Assert.Equal(4, phases.Count);
    }

    [Fact]
    public async Task HttpChecksPermissionBeforeTransportAndRetainsHeaderSemantics()
    {
        var allowed = false;
        var calls = 0;
        var http = new Http(url => { if (!allowed) throw new UnauthorizedAccessException(); },
            (method, url, body, headers) =>
            {
                calls++;
                Assert.Equal("POST", method);
                Assert.Equal("https://example.com", url);
                Assert.Equal("{}", body);
                Assert.Equal("text/plain", headers["content-type"]);
                return Task.FromResult(new Http.HttpReponse { status_code = 201, body = "done" });
            });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => http.Request("POST", "https://example.com"));
        Assert.Equal(0, calls);
        allowed = true;
        var response = await http.Request("POST", "https://example.com", "{}", "{\"Content-Type\":\"text/plain\"}");
        Assert.Equal(201, response.status_code);
        Assert.Equal("done", response.body);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DispatcherHonorsCancellationBeforeCallingItsTaskHost()
    {
        var tasks = new TestTasks();
        var dispatcher = new Dispatcher(tasks);
        Assert.Equal("mapped", await dispatcher.RunTask(new SoloTask("mapped"), CancellationToken.None));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatcher.RunTask(new SoloTask("blocked"), new CancellationToken(true)));
        Assert.Equal(1, tasks.Calls);
    }

    private sealed class TestTasks : IScriptTaskHost
    {
        public int Calls;
        public Task<object?> RunTask(SoloTask task, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<object?>(task.Name);
        }
    }
}
