using BetterGenshinImpact.GameTask.Common.BgiVision;
using Xunit;

namespace BetterGenshinImpact.RuntimeTest;

public sealed class UidTests
{
    [Theory]
    [InlineData("UID: 2549948033", 2549948033L)]
    [InlineData("UID 100 123 456", 100123456L)]
    [InlineData("UID", 0L)]
    [InlineData(null, 0L)]
    [InlineData("99999999999999999999", 0L)]
    public void UidSupportsTenDigitsAndPreservesRecognitionFailure(string? text, long expected)
        => Assert.Equal(expected, Bv.ParseUid(text));
}
