using System.Collections.Generic;
using System;
using System.IO;

namespace ConfManager;

/// <summary>
/// A class storing the data of a loaded .cfg file
/// </summary>
public sealed class CfgFile
{
    private ILogger _logger = new DefaultLogger();
    private Dictionary<string, string>? _loadedConfig = null;
    private Dictionary<string, string>? _editedConfig = null;

    /// <summary>
    /// Replaces the logger with <paramref name="newLogger"/>
    /// </summary>
    public void SetLogger(ILogger newLogger)
    {
        _logger = newLogger;
    }

    /// <summary>
    /// Opens a config file
    /// </summary>
    /// <param name="path">Path to the config file</param>
    public OperationResult OpenFile(string path)
    {
        return OperationResult.Ok;
    }
}