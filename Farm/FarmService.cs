namespace Servers;

using Services;

/// <summary>
/// Service
/// </summary>
public class FarmService : IFarmService
{
    public SubmissionResult SubmitFood(int amount)
    {
        return new SubmissionResult { IsAccepted = true };
    }

    public SubmissionResult SubmitWater(int amount)
    {
        return new SubmissionResult { IsAccepted = true };
    }
}