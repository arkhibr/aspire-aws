using System.Runtime.InteropServices;
using Xunit;

namespace Shared;

public static class EnvironmentLimitations
{
    public const string MacOsArm64LambdaReason =
        "Skipped on macOS ARM64: LocalStack 3.8 Lambda invocation is unreliable in this environment.";

    public static bool IsMacOsArm64LocalStackLambdaUnsupported =>
        OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
}

public sealed class SkipOnMacOsArm64LocalStackLambdaFactAttribute : FactAttribute
{
    public SkipOnMacOsArm64LocalStackLambdaFactAttribute()
    {
        if (EnvironmentLimitations.IsMacOsArm64LocalStackLambdaUnsupported)
        {
            Skip = EnvironmentLimitations.MacOsArm64LambdaReason;
        }
    }
}
