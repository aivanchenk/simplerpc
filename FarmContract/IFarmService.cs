namespace FarmContract;

/// <summary>
/// Descriptor of submission result
/// </summary>
public class SubmissionResult
{
    /// <summary>
    /// Indicates if submission attempt has accepted.
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// If pass submission has failed, indicates fail reason.
    /// </summary>
    public string FailReason { get; set; }
}

public enum FarmPhase
{
    Growing,
    Selling,
    Dead,        
}

public interface IFarmService
{
    SubmissionResult SubmitFood(int amount);
    SubmissionResult SubmitWater(int amount);
}