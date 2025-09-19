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
    public long AccumulatedFood;

    /// <summary>
    /// Total accumulated water.
    /// </summary>
    public long AccumulatedWater;

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
            mLog.Info($"Accepted {amount} of water resource. Total water: {mState.AccumulatedWater}.");
            return new SubmissionResult { IsAccepted = true, FailReason = string.Empty };
        }
    }

    public SubmissionResult SubmitFood(int amount)
    {
        lock (mState.AccessLock)
        {
            mState.AccumulatedFood += amount;
            mLog.Info($"Accepted {amount} of food resource. Total food: {mState.AccumulatedFood}.");
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

                mLog.Info($"Consumed {consumedFood} food and {consumedWater} water. Remaining totals - Food: {mState.AccumulatedFood}, Water: {mState.AccumulatedWater}.");
            }
        }
    }
}