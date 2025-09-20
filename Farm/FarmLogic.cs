namespace Servers;

using NLog;

using Services;

public class FarmState
{
    /// <summary>
    /// Access lock.
    /// </summary>
    public readonly object AccessLock = new object();
    
    /// <summary>
    /// Total accumulated food.
    /// </summary>
    public double AccumulatedFood = 0;

    /// <summary>
    /// Total accumulated water.
    /// </summary>
    public double AccumulatedWater = 0;

    public double farmSize = 0.0;

    public double baseRate = 0.05;

    public double growthRate = 0.1;

    public double consumptionCoef = 0.01;

    public double totalConsumedResources = 0.0;

    public int thirstRounds = 0;

    public int hungerRounds = 0;

    public int maxFailRounds = 2;

    /// <summary>
    /// Timestamp of the last consumption event.
    /// </summary>
    public DateTime? LastConsumptionTimestamp;
}


class FarmLogic
{
    /// <summary>
	/// Logger for this class.
	/// </summary>
	private Logger mLog = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Background task thread.
    /// </summary>
    private Thread mBgTaskThread;

    /// <summary>
    /// State descriptor.
    /// </summary>
    private FarmState mState = new FarmState();

    /// <summary>
    /// Random generator for consumption amounts.
    /// </summary>
    private readonly Random mRandom = new Random();
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public FarmLogic()
    {
        //start the background task
        mBgTaskThread = new Thread(BackgroundTask);
        mBgTaskThread.Start();
    }

    public SubmissionResult SubmitWater(int amount)
    {
        lock (mState.AccessLock)
        {
            mState.AccumulatedWater += amount;
            return new SubmissionResult { IsAccepted = true, FailReason = string.Empty };
        }
    }

    public SubmissionResult SubmitFood(int amount)
    {
        lock (mState.AccessLock)
        {
            mState.AccumulatedFood += amount;
            return new SubmissionResult { IsAccepted = true, FailReason = string.Empty };
        }
    }

    private double GetRandomFoodConsumption()
    {
        var consumption = mRandom.Next(0, 100) * mState.consumptionCoef;

        if (consumption >= mState.AccumulatedFood)
        {
            mState.hungerRounds++;
            if (mState.thirstRounds >= mState.maxFailRounds)
            {
                mLog.Warn("Farm has been without resources for 2 consecutive rounds. Farm has failed.");
            }
            return 0;
        }

        return consumption;
    }

    private double GetRandomWaterConsumption()
    {

        var consumption = mRandom.Next(0, 100) * mState.consumptionCoef;

        if (consumption >= mState.AccumulatedWater)
        {
            mState.thirstRounds++;
            if (mState.thirstRounds >= mState.maxFailRounds)
            {
                mLog.Warn("Farm has been without resources for 2 consecutive rounds. Farm has failed.");
            }
            return 0;
        }

        return consumption;
    }
    
    private double ComputeConsumptionCoefficient(double total)
    {
        return Math.Clamp(
            mState.baseRate + mState.growthRate * Math.Log10(total + 1),
            mState.baseRate,
            2.0);
    }

    private double ComputeFarmSize(double total)
    {
        return Math.Log10(total + 1);
    }

    public void BackgroundTask()
    {
        while (true)
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));

            double consumedFood = 0;
            double consumedWater = 0;

            //lock the state
            lock (mState.AccessLock)
            {
                consumedFood = GetRandomFoodConsumption();
                consumedWater = GetRandomWaterConsumption();

                mState.AccumulatedFood -= consumedFood;
                mState.AccumulatedWater -= consumedWater;
                mState.LastConsumptionTimestamp = DateTime.UtcNow;

                mState.totalConsumedResources += consumedFood + consumedWater;

                mState.farmSize = ComputeFarmSize(mState.totalConsumedResources);
                mState.consumptionCoef = ComputeConsumptionCoefficient(mState.totalConsumedResources);

                mLog.Info($"Consumed {consumedFood} food and {consumedWater} water. Remaining totals - Food: {mState.AccumulatedFood}, Water: {mState.AccumulatedWater}.");
                mLog.Info($"Farm size after consumption {mState.farmSize}, consumption coefficient has been updated to {mState.consumptionCoef}.");
            }
        }
    }
}