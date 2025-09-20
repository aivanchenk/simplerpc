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

    public double consumptionCoef = 0.01;

    public double totalConsumedResources = 0.0;

    public int thirstRounds = 0;

    public int starveRounds = 0;

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

    public double baseRate = 0.05;

    public double growthRate = 0.1;

    public int maxFailRounds = 2;

    
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
            mState.starveRounds++;
            if (mState.starveRounds >= maxFailRounds)
            {
                mLog.Warn($"Was unable to consume {consumption} of food.");
                HandleFarmFailure("food");
            }
            return 0;
        }

        mState.starveRounds = 0;
        return consumption;
    }

    private double GetRandomWaterConsumption()
    {

        var consumption = mRandom.Next(0, 100) * mState.consumptionCoef;

        if (consumption >= mState.AccumulatedWater)
        {
            mState.thirstRounds++;
            if (mState.thirstRounds >= maxFailRounds)
            {
                mLog.Warn($"Was unable to consume {consumption} of water.");
                HandleFarmFailure("water");
            }
            return 0;
        }

        mState.thirstRounds = 0;
        return consumption;
    }
    
    private double ComputeConsumptionCoefficient(double total)
    {
        return Math.Clamp(baseRate + growthRate * Math.Log10(total + 1), baseRate, 2.0);
    }

    private double ComputeFarmSize(double total)
    {
        return Math.Log10(total + 1);
    }

    private void ResetFarmState()
    {
        mState.AccumulatedFood = 0;
        mState.AccumulatedWater = 0;
        mState.totalConsumedResources = 0;
        mState.starveRounds = 0;
        mState.thirstRounds = 0;
        mState.farmSize = 0;
        mState.consumptionCoef = 0.01;
        mState.LastConsumptionTimestamp = null;
    }

    private void HandleFarmFailure(string failedResource)
    {
        mLog.Warn($"Farm has been without {failedResource} for {maxFailRounds} consecutive rounds. Farm has failed.");
        ResetFarmState();
        mLog.Info("Farm state has been reset. Background processing will continue with a fresh farm.");
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