namespace Servers;

using NLog;

using Services;

public class FarmState
{
	/// <summary>
	/// Access lock.
	/// </summary>
	public readonly object AccessLock = new object();
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
            mLog.Info($"Accepted {amount} of water resource.");
            return new SubmissionResult { IsAccepted = true, FailReason = string.Empty };
        }
    }

    public SubmissionResult SubmitFood(int amount)
    {
        lock (mState.AccessLock)
        {
            mLog.Info($"Accepted {amount} of food resource.");
            return new SubmissionResult { IsAccepted = true, FailReason = string.Empty };
        }
    }

    public void BackgroundTask()
    {
        //intialize random number generator
        var rnd = new Random();

        //
        while (true)
        {
            //wait a bit
            Thread.Sleep(1000);

            //lock the state
            lock (mState.AccessLock)
            {
                mLog.Info($"Accepting resources");
            }
        }
    }
}