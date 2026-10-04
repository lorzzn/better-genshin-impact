using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
#if !BETTERGI_PORTABLE
using Vanara.PInvoke;
#endif
using BetterGenshinImpact.Core.Config;
using static BetterGenshinImpact.GameTask.Common.TaskControl;

namespace BetterGenshinImpact.GameTask.Common.Job;

public class ReturnMainUiTask
{
    public string Name => "返回主界面";

    public async Task Start(CancellationToken ct)
    {
        using var initialCapture = CaptureToRectArea();
        if (Bv.IsInMainUi(initialCapture))
        {
            return;
        }

        for (var i = 0; i < 8; i++)
        {
            Simulation.KeyPress(KeyId.Escape);
            await Delay(900, ct);

            var region = CaptureToRectArea();

            try
            {
                var exitDoor = region.Find(ElementRecognition.Get("BtnExitDoor", region));
                if (exitDoor.IsExist())
                {
                    exitDoor.Click();
                    await Delay(5000, ct);
                    region.Dispose();
                    region = CaptureToRectArea();
                }

                if (Bv.IsInMainUi(region))
                {
                    return;
                }
            }
            finally
            {
                region.Dispose();
            }
        }
        await Delay(500, ct);
        Simulation.KeyPress(KeyId.Enter);
        await Delay(500, ct);
        Simulation.KeyPress(KeyId.Escape);
    }
}
