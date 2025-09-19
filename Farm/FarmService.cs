namespace Servers;

using Services;

/// <summary>
/// Service
/// </summary>
public class FarmService : IFarmService
{
    //NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	private readonly FarmLogic mLogic = new FarmLogic();

    public SubmissionResult SubmitWater(int amount)
    {
        return mLogic.SubmitWater(amount);
    }

    public SubmissionResult SubmitFood(int amount)
    {
        return mLogic.SubmitFood(amount);
    }
    
}