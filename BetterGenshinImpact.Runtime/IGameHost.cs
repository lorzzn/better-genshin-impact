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
    void MovePointer(double x, double y);
    void MovePointerBy(int dx, int dy);
    void SetPointerButton(int button, bool down);
    void SetKey(int virtualKey, bool down);
    void Scroll(int notches);
}
