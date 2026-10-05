using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BetterGenshinImpact.GameTask.Music.Service;

public interface IMusicCoverService
{
    Task<ImageSource?> GetCoverAsync(string songName, CancellationToken cancellationToken);
}
