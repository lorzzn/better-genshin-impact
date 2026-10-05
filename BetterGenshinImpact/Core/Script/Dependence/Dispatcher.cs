using System;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Script.Dependence.Model;

namespace BetterGenshinImpact.Core.Script.Dependence;

/// <summary>The official JS task entry, with execution supplied by its embedding host.</summary>
public partial class Dispatcher
{
    private readonly IScriptTaskHost taskHost;
    public Dispatcher(IScriptTaskHost taskHost) => this.taskHost = taskHost ?? throw new ArgumentNullException(nameof(taskHost));

    public async Task RunTask(SoloTask soloTask, CancellationTokenSource customCts)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(customCts.Token, CancellationContext.Instance.Token);
        await RunTask(soloTask, linked.Token);
    }

    public Task<object?> RunTask(SoloTask soloTask, CancellationToken? customCt = null)
    {
        ArgumentNullException.ThrowIfNull(soloTask);
        var token = customCt ?? CancellationContext.Instance.Token;
        token.ThrowIfCancellationRequested();
        return taskHost.RunTask(soloTask, token);
    }
}

public interface IScriptTaskHost
{
    Task<object?> RunTask(SoloTask task, CancellationToken cancellationToken);
}
