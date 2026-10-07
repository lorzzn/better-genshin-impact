using OpenCvSharp;

namespace BetterGenshinImpact.Runtime;

/// <summary>
/// OS boundary supplied by the embedding application. All coordinates and input
/// belong to the same game surface. Capture transfers Mat ownership to the caller.
/// Implementations must propagate cancellation and failures; they must not run a
/// second game task or silently route input to the host machine's desktop.
/// </summary>
public interface IGameHost
{
    Mat Capture(CancellationToken cancellationToken);
    // A Target is already a bound input surface. Window-aware hosts may override
    // these operations; the runtime never inspects the embedding PC's windows.
    bool IsForeground => true;
    string ActiveSurfaceName => "Target";
    void Activate() { }
    /// <summary>Login channel of the bound game. The embedding host, not local process discovery, supplies it.</summary>
    string GameChannel => "official";
    bool ContinueChannelLogin(CancellationToken cancellationToken) => throw new NotSupportedException("Target does not provide channel login metadata");
    /// <summary>
    /// Positive identity stable for this bound source, used for detector reuse
    /// and retry throttling. Null means unavailable. Remote hosts need not expose
    /// an OS process ID; CreateAudioCapture resolves the actual audio source.
    /// </summary>
    int? AudioSourceId => null;
    GameTask.AutoSkip.Audio.IAudioSampleSource CreateAudioCapture() => throw new NotSupportedException("Target has no audio capture driver");
    void MovePointer(double x, double y);
    void MovePointerBy(int dx, int dy);
    void SetPointerButton(int button, bool down);
    void SetKey(int virtualKey, bool down);
    void Scroll(int notches);
    void ScrollHorizontal(int notches) => throw new NotSupportedException("Target does not support horizontal scrolling");
    bool IsHardwareKeyDown(int key) => throw new NotSupportedException("Target does not expose physical keyboard state");
    /// <summary>Observed input on this Target, not global input on the embedding computer.</summary>
    bool IsHotkeyPressed(string name) => throw new NotSupportedException("Target does not expose hotkey state");
    bool IsTogglingKeyInEffect(int key) => throw new NotSupportedException("Target does not expose toggle-key state");
    void InputText(string text) => throw new NotSupportedException("The bound game host does not support text input");
    /// <summary>Writes only the Target clipboard. Null clears it; never reads the host clipboard.</summary>
    void SetClipboardText(string? text) => throw new NotSupportedException("Target does not support clipboard writes");
}
