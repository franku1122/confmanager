namespace ConfManager;

/// <summary>
/// Use this interface if you want to make your own logger ( eg. use GD.Print instead of Console.WriteLine )
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Prints <paramref name="msg"/> into the output ( determined by <paramref name="type"/> )
    /// </summary>
    public void Put(LogType type, string msg);
}