using System;
namespace ConfManager;

/// <summary>
/// Default logger used by CfgFile
/// </summary>
public sealed class DefaultLogger : ILogger
{
    /// <summary>
    /// Default print to the output ( which is always Console.WriteLine )
    /// </summary>
    public void Put(LogType type, string msg)
    {
        switch (type)
        {
            case LogType.Output:
            {
                Console.WriteLine(msg);
                break;
            }
            case LogType.Info:
            {
                Console.WriteLine(msg);
                break;
            }
            case LogType.Warn:
            {
                Console.WriteLine(msg);
                break;
            }
            case LogType.Error:
            {
                Console.WriteLine(msg);
                break;
            }
        }
    }
}