namespace Servers;

using Services;

/// <summary>
/// Service
/// </summary>
public class FarmService : IFarmService
{

    private static readonly object _waterLock = new();

    private static int waterTotal = 0;
    private static int foodTotal = 0;

    public SubmissionResult SubmitWater(int amount)
    {
        lock (_waterLock)
        {
            waterTotal += amount;
        }
        Console.WriteLine($"Recieved {amount} of water resource. Now total water is {waterTotal}");
        return new SubmissionResult { IsAccepted = true, FailReason = string.Empty };
    }

        public SubmissionResult SubmitFood(int amount)
    {
        lock (_waterLock)
        {
            foodTotal += amount;
        }
        Console.WriteLine($"Recieved {amount} of food resource. Now total food is {foodTotal}");
        return new SubmissionResult { IsAccepted = true, FailReason = string.Empty };
    }

    
}