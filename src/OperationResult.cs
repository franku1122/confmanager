#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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
    /// <summary>
    /// File not found error
    /// </summary>
    FileNotFound = 3,
    /// <summary>
    /// Something already exists error
    /// </summary>
    AlreadyExists = 4,
    /// <summary>
    /// Something wasn't found error
    /// </summary>
    NotFound = 5,
}