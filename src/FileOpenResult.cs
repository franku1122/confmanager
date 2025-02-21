namespace ConfManager;

/// <summary>
/// An enum representing the result of any method.
/// </summary>
public enum OperationResult
{
    Ok = 0,
    /// <summary>
    /// Undefined failure
    /// </summary>
    Error = 1,
    /// <summary>
    /// No permission error
    /// </summary>
    NoPermission = 2,
}