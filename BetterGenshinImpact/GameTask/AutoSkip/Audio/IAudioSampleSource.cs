using System;
using System.Collections.Generic;
namespace BetterGenshinImpact.GameTask.AutoSkip.Audio;
/// <summary>Mono floating-point samples at 16 kHz, transferred from the game surface.</summary>
public interface IAudioSampleSource : IDisposable
{
    const int SampleRate = 16000;
    void ReadAvailableSamples(List<float> destination);
    void DiscardAvailableSamples();
}
