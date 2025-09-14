namespace Servers;

using Services;

/// <summary>
/// Service
/// </summary>
public class FarmService : IFarmService
{
    public SubmissionResult SubmitFood(int amount)
        => new SubmissionResult { IsAccepted = true, FailReason = string.Empty };

    public SubmissionResult SubmitWater(int amount)
        => new SubmissionResult { IsAccepted = true, FailReason = string.Empty };
}