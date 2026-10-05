using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.GameTask;
using Microsoft.ClearScript;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Script.Dependence;

public partial class KeyMouseHook: IDisposable
{
    private readonly IScriptInputSource AppHook;
    private readonly bool _ownsSource;
    private int _disposed;
    private readonly Task _processor;
    internal Task Completion => _processor;

    private readonly List<ScriptObject> _keyDownDataCallbacks = new();
    private readonly List<ScriptObject> _keyUpDataCallbacks = new();
    private readonly List<ScriptObject> _keyDownCodeCallbacks = new();
    private readonly List<ScriptObject> _keyUpCodeCallbacks = new();
    private readonly List<ScriptObject> _mouseDownCallbacks = new();
    private readonly List<ScriptObject> _mouseUpCallbacks = new();
    private readonly List<ScriptObject> _mouseMoveCallbacks = new();
    private readonly List<ScriptObject> _mouseWheelCallbacks = new();

    /// <summary>
    /// 存储每个鼠标移动回调的间隔时间（毫秒）
    /// </summary>
    private readonly Dictionary<ScriptObject, int> _mouseMoveCallbackIntervals = new();

    /// <summary>
    /// 存储每个鼠标移动回调的上次调用时间
    /// </summary>
    private readonly Dictionary<ScriptObject, DateTime> _lastMouseMoveCallbackTimes = new();

    private EventHandler<ScriptKeyEvent>? _keyDownHandler;
    private EventHandler<ScriptKeyEvent>? _keyUpHandler;
    private EventHandler<ScriptMouseEvent>? _mouseDownExtHandler;
    private EventHandler<ScriptMouseEvent>? _mouseUpExtHandler;
    private EventHandler<ScriptMouseEvent>? _mouseMoveExtHandler;
    private EventHandler<ScriptMouseEvent>? _mouseWheelExtHandler;

    private readonly object _callbacksLock = new();
    private DateTime _lastProcessMouseMoveTime = DateTime.MinValue;

    private readonly ILogger<KeyMouseHook> _logger = GameServices.GetLogger<KeyMouseHook>();

    /// <summary>
    /// 统一处理回调函数执行过程中的异常
    /// </summary>
    /// <param name="ex">捕获到的异常</param>
    /// <param name="eventType">事件类型描述</param>
    private void HandleCallbackException(Exception ex, string eventType)
    {
        if (ex is ScriptEngineException scriptEx)
        {
            _logger.LogError("执行{eventType}JS回调时发生错误：{scriptEx.Message}，清除所有回调",eventType, scriptEx.Message);
            _logger.LogDebug("{scriptEx}",scriptEx);
        }
        else
        {
            _logger.LogError("执行{eventType}回调时发生错误:{ex.Message}，清除所有回调,如果此异常出现在JS脚本结束时,请在JS脚本结束前手动调用Dispose()方法",eventType,ex.Message);
            _logger.LogDebug("{ex}",ex);
        }

        Dispose();
    }

    private readonly System.Threading.Channels.Channel<Action> _eventChannel =
        System.Threading.Channels.Channel.CreateBounded<Action>(
            new System.Threading.Channels.BoundedChannelOptions(2048)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
    private readonly CancellationTokenSource _cts = new();

    public KeyMouseHook(IScriptInputSource source, bool ownsSource = false)
    {
        AppHook = source ?? throw new ArgumentNullException(nameof(source));
        _ownsSource = ownsSource;
        // 启动后台事件处理器，确保不阻塞钩子线程
        _processor = Task.Run(async () =>
        {
            try
            {
                var reader = _eventChannel.Reader;
                while (Volatile.Read(ref _disposed) == 0 && await reader.WaitToReadAsync(_cts.Token))
                {
                    while (Volatile.Read(ref _disposed) == 0 && reader.TryRead(out var action))
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception)
                        {
                             // 内部错误已在 action 闭包中通过 HandleCallbackException 处理
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "键鼠钩子后台处理器发生异常");
            }
        });

        // 初始化事件处理程序
        _keyDownHandler = (_, args) =>
        {
            if (_keyDownDataCallbacks.Count == 0 && _keyDownCodeCallbacks.Count == 0) return;

            var keyDownDataCallbacksCopy = Snapshot(_keyDownDataCallbacks);
            var keyDownCodeCallbacksCopy = Snapshot(_keyDownCodeCallbacks);
            var keyDataStr = args.KeyData.ToString();
            var keyCodeStr = args.KeyCode.ToString();

            if (!_eventChannel.Writer.TryWrite(() =>
                {
                    // 调用KeyData回调
                    foreach (var callback in keyDownDataCallbacksCopy)
                    {
                        try
                        {
                            if (Volatile.Read(ref _disposed) != 0) return;
                            callback.InvokeAsFunction(keyDataStr);
                        }
                        catch (Exception ex)
                        {
                            HandleCallbackException(ex, "键盘按下事件");
                            return;
                        }
                    }

                    // 调用KeyCode回调
                    foreach (var callback in keyDownCodeCallbacksCopy)
                    {
                        try
                        {
                            if (Volatile.Read(ref _disposed) != 0) return;
                            callback.InvokeAsFunction(keyCodeStr);
                        }
                        catch (Exception ex)
                        {
                            HandleCallbackException(ex, "键盘按下事件");
                            return;
                        }
                    }
                }))
            {
                _logger.LogWarning("事件队列已满或已关闭，忽略键盘按下回调");
            }
        };

        _keyUpHandler = (_, args) =>
        {
            if (_keyUpDataCallbacks.Count == 0 && _keyUpCodeCallbacks.Count == 0) return;

            var keyUpDataCallbacksCopy = Snapshot(_keyUpDataCallbacks);
            var keyUpCodeCallbacksCopy = Snapshot(_keyUpCodeCallbacks);
            var keyDataStr = args.KeyData.ToString();
            var keyCodeStr = args.KeyCode.ToString();
            if (!_eventChannel.Writer.TryWrite(() =>
                {
                    // 调用KeyData回调
                    foreach (var callback in keyUpDataCallbacksCopy)
                    {
                        try
                        {
                            if (Volatile.Read(ref _disposed) != 0) return;
                            callback.InvokeAsFunction(keyDataStr);
                        }
                        catch (Exception ex)
                        {
                            HandleCallbackException(ex, "键盘释放事件");
                            return;
                        }
                    }

                    // 调用KeyCode回调
                    foreach (var callback in keyUpCodeCallbacksCopy)
                    {
                        try
                        {
                            if (Volatile.Read(ref _disposed) != 0) return;
                            callback.InvokeAsFunction(keyCodeStr);
                        }
                        catch (Exception ex)
                        {
                            HandleCallbackException(ex, "键盘释放事件");
                            return;
                        }
                    }
                }))
            {
                _logger.LogWarning("事件队列已满或已关闭，忽略键盘释放回调");
            }
        };

        _mouseDownExtHandler = (_, args) =>
        {
            if (_mouseDownCallbacks.Count == 0) return;

            var mouseDownCallbacksCopy = Snapshot(_mouseDownCallbacks);
            var (localX, localY) = (args.X, args.Y);
            var buttonStr = args.Button.ToString();

            if (!_eventChannel.Writer.TryWrite(() =>
                {
                    foreach (var callback in mouseDownCallbacksCopy)
                    {
                        try
                        {
                            if (Volatile.Read(ref _disposed) != 0) return;
                            callback.InvokeAsFunction(buttonStr, localX, localY);
                        }
                        catch (Exception ex)
                        {
                            HandleCallbackException(ex, "鼠标按下事件");
                            return;
                        }
                    }
                }))
            {
                _logger.LogWarning("事件队列已满或已关闭，忽略鼠标按下回调");
            }
        };

        _mouseUpExtHandler = (_, args) =>
        {
            if (_mouseUpCallbacks.Count == 0) return;

            var mouseUpCallbacksCopy = Snapshot(_mouseUpCallbacks);
            var (localX, localY) = (args.X, args.Y);
            var buttonStr = args.Button.ToString();

            if (!_eventChannel.Writer.TryWrite(() =>
                {
                    foreach (var callback in mouseUpCallbacksCopy)
                    {
                        try
                        {
                            if (Volatile.Read(ref _disposed) != 0) return;
                            callback.InvokeAsFunction(buttonStr, localX, localY);
                        }
                        catch (Exception ex)
                        {
                            HandleCallbackException(ex, "鼠标释放事件");
                            return;
                        }
                    }
                }))
            {
                _logger.LogWarning("事件队列已满或已关闭，忽略鼠标释放回调");
            }
        };

        _mouseMoveExtHandler = (_, args) =>
        {
            if (_mouseMoveCallbacks.Count == 0) return;

            var now = DateTime.Now;
            if ((now - _lastProcessMouseMoveTime).TotalMilliseconds < 10) return;
            _lastProcessMouseMoveTime = now;

            var mouseMoveCallbacksCopy = Snapshot(_mouseMoveCallbacks);
            var (localX, localY) = (args.X, args.Y);

            if (!_eventChannel.Writer.TryWrite(() =>
                {
                    foreach (var callback in mouseMoveCallbacksCopy)
                    {

                        try
                        {
                            bool due;
                            lock (_callbacksLock)
                            {
                                due = _mouseMoveCallbackIntervals.TryGetValue(callback, out var interval) &&
                                    _lastMouseMoveCallbackTimes.TryGetValue(callback, out var lastTime) &&
                                    (now - lastTime).TotalMilliseconds >= interval;
                                if (due) _lastMouseMoveCallbackTimes[callback] = now;
                            }
                            if (due && Volatile.Read(ref _disposed) == 0) callback.InvokeAsFunction(localX, localY);
                        }
                        catch (Exception ex)
                        {
                            HandleCallbackException(ex, "鼠标移动事件");
                            return;
                        }
                    }
                }))
            {
                _logger.LogWarning("事件队列已满或已关闭，忽略鼠标移动回调");
            }
        };

        _mouseWheelExtHandler = (_, args) =>
        {
            if (_mouseWheelCallbacks.Count == 0) return;

            var mouseWheelCallbacksCopy = Snapshot(_mouseWheelCallbacks);
            var (localX, localY) = (args.X, args.Y);
            var delta = args.Delta;

            if (!_eventChannel.Writer.TryWrite(() =>
                {
                    foreach (var callback in mouseWheelCallbacksCopy)
                    {
                        try
                        {
                            if (Volatile.Read(ref _disposed) != 0) return;
                            callback.InvokeAsFunction(delta, localX, localY);
                        }
                        catch (Exception ex)
                        {
                            HandleCallbackException(ex, "鼠标滚轮事件");
                            return;
                        }
                    }
                }))
            {
                _logger.LogWarning("事件队列已满或已关闭，忽略鼠标滚轮回调");
            }
        };

        // 添加事件监听器
        AppHook.KeyDown += _keyDownHandler;
        AppHook.KeyUp += _keyUpHandler;
        AppHook.MouseDownExt += _mouseDownExtHandler;
        AppHook.MouseUpExt += _mouseUpExtHandler;
        AppHook.MouseMoveExt += _mouseMoveExtHandler;
        AppHook.MouseWheelExt += _mouseWheelExtHandler;
        try { AppHook.Start(); }
        catch { Dispose(); throw; }
    }

    /// <summary>
    /// 注册键盘按下事件回调
    /// </summary>
    /// <param name="callback">回调函数</param>
    /// <param name="useCodeOnly">是否仅返回KeyCode，默认为true（仅返回KeyCode）</param>
    public void OnKeyDown(ScriptObject callback, bool useCodeOnly = true)
    {
        lock (_callbacksLock)
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(KeyMouseHook));
            if (useCodeOnly)
                _keyDownCodeCallbacks.Add(callback);
            else
                _keyDownDataCallbacks.Add(callback);
        }
    }

    /// <summary>
    /// 注册键盘释放事件回调
    /// </summary>
    /// <param name="callback">回调函数</param>
    /// <param name="useCodeOnly">是否仅返回KeyCode，默认为true（仅返回KeyCode）</param>
    public void OnKeyUp(ScriptObject callback, bool useCodeOnly = true)
    {
        lock (_callbacksLock)
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(KeyMouseHook));
            if (useCodeOnly)
                _keyUpCodeCallbacks.Add(callback);
            else
                _keyUpDataCallbacks.Add(callback);
        }
    }

    public void OnMouseDown(ScriptObject callback)
    {
        lock (_callbacksLock)
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(KeyMouseHook));
            _mouseDownCallbacks.Add(callback);
        }
    }

    public void OnMouseUp(ScriptObject callback)
    {
        lock (_callbacksLock)
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(KeyMouseHook));
            _mouseUpCallbacks.Add(callback);
        }
    }

    /// <summary>
    /// 注册鼠标移动事件回调
    /// </summary>
    /// <param name="callback">回调函数</param>
    /// <param name="interval">回调间隔时间（毫秒），默认200ms</param>
    public void OnMouseMove(ScriptObject callback, int interval = 200)
    {
        lock (_callbacksLock)
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(KeyMouseHook));
            _mouseMoveCallbacks.Add(callback);
            _mouseMoveCallbackIntervals[callback] = interval;
            _lastMouseMoveCallbackTimes[callback] = DateTime.MinValue;
        }
    }

    public void OnMouseWheel(ScriptObject callback)
    {
        lock (_callbacksLock)
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(KeyMouseHook));
            _mouseWheelCallbacks.Add(callback);
        }
    }

    public void RemoveAllListeners()
    {
        lock (_callbacksLock)
        {
            _keyDownDataCallbacks.Clear();
            _keyUpDataCallbacks.Clear();
            _keyDownCodeCallbacks.Clear();
            _keyUpCodeCallbacks.Clear();
            _mouseDownCallbacks.Clear();
            _mouseUpCallbacks.Clear();
            _mouseMoveCallbacks.Clear();
            _mouseWheelCallbacks.Clear();

            _mouseMoveCallbackIntervals.Clear();
            _lastMouseMoveCallbackTimes.Clear();
        }
    }

    private ScriptObject[] Snapshot(List<ScriptObject> callbacks)
    {
        lock (_callbacksLock) return callbacks.ToArray();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _cts.Cancel();
            _eventChannel.Writer.TryComplete();
            // 移除所有事件监听器
            if (_keyDownHandler != null)
            {
                AppHook.KeyDown -= _keyDownHandler;
                _keyDownHandler = null;
            }

            if (_keyUpHandler != null)
            {
                AppHook.KeyUp -= _keyUpHandler;
                _keyUpHandler = null;
            }

            if (_mouseDownExtHandler != null)
            {
                AppHook.MouseDownExt -= _mouseDownExtHandler;
                _mouseDownExtHandler = null;
            }

            if (_mouseUpExtHandler != null)
            {
                AppHook.MouseUpExt -= _mouseUpExtHandler;
                _mouseUpExtHandler = null;
            }

            if (_mouseMoveExtHandler != null)
            {
                AppHook.MouseMoveExt -= _mouseMoveExtHandler;
                _mouseMoveExtHandler = null;
            }

            if (_mouseWheelExtHandler != null)
            {
                AppHook.MouseWheelExt -= _mouseWheelExtHandler;
                _mouseWheelExtHandler = null;
            }

            // 清空回调列表
            RemoveAllListeners();
            if (_ownsSource) AppHook.Dispose();
        }
    }
}
