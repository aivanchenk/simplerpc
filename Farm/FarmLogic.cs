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
    public long AccumulatedFood = 0;

    /// <summary>
    /// Total accumulated water.
    /// </summary>
    public long AccumulatedWater = 0;

    public long TotalGrowthPoints = 0;

    public double farmSize = 0.0;

    public double baseRate = 0.05;

    public double growthRate = 0.1;

    public double consumptionCoef = 0.01;

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
    
    private long GetRandomConsumption(long available)
    {
        if (available <= 0)
        {
            return 0;
        }

        var consumption = (long)Math.Round(mRandom.NextDouble() * available);

        if (consumption > available)
        {
            return available;
        }

        return consumption;
    }

     /// <summary>
    /// Provides read-only access to the current food total.
    /// </summary>
    public long CurrentFoodTotal
    {
        get
        {
            lock (mState.AccessLock)
            {
                return mState.AccumulatedFood;
            }
        }
    }

    /// <summary>
    /// Provides read-only access to the current water total.
    /// </summary>
    /// <returns></returns>
    public long CurrentWaterTotal
    {
        get
        {
            lock (mState.AccessLock)
            {
                return mState.AccumulatedWater;
            }
        }
    }

    public void BackgroundTask()
    {
        while (true)
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));

            long consumedFood = 0;
            long consumedWater = 0;

            //lock the state
            lock (mState.AccessLock)
            {
                consumedFood = GetRandomConsumption(mState.AccumulatedFood);
                consumedWater = GetRandomConsumption(mState.AccumulatedWater);

                mState.AccumulatedFood -= consumedFood;
                mState.AccumulatedWater -= consumedWater;
                mState.LastConsumptionTimestamp = DateTime.UtcNow;

                mState.TotalGrowthPoints += (consumedFood + consumedWater);
                mState.farmSize = Math.Log10(mState.TotalGrowthPoints + 1);
                mState.consumptionCoef = Math.Clamp(mState.baseRate + mState.growthRate * mState.farmSize,
                                    0.0,
                                    0.9);

                mLog.Info($"Consumed {consumedFood} food and {consumedWater} water. Remaining totals - Food: {mState.AccumulatedFood}, Water: {mState.AccumulatedWater}.");
                mLog.Info($"Farm size after consumption {mState.farmSize}, consumption coefficient has been updated to {mState.consumptionCoef}.");
            }   
        }
    }
}