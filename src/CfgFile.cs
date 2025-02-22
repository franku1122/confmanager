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
    private string[]? _annotations = null;

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
    /// <returns><see cref="OperationResult"/></returns>
    public OperationResult OpenFile(string path)
    {
        // try open the file
        try 
        {
            FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);

            using (StreamReader reader = new(stream))
            {
                int lineIdx = 0;
                
                while (!reader.EndOfStream)
                {
                    lineIdx++;
                    string? line = reader.ReadLine();
                    if (line != null)
                    {
                        _logger.Put(LogType.Output, $"line {lineIdx} contents:");
                        _logger.Put(LogType.Output, line);
                    }
                }
            }
        }
        catch (FileNotFoundException)
        {
            _logger.Put(LogType.Error, $"File {path} not found");
            return OperationResult.FileNotFound;
        }
        catch (DirectoryNotFoundException)
        {
            _logger.Put(LogType.Error, $"File {path} not found");
            return OperationResult.FileNotFound;
        }
        catch (Exception exception)
        {
            _logger.Put(LogType.Error, $"OpenFile has caught an undefined exception: {exception}");
            return OperationResult.Error;
        }
        
        return OperationResult.Ok;
    }

    /// <summary>
    /// Resets this CfgFile instance. Unsaved data will be lost.
    /// <para>Note: the logger will not be changed</para>
    /// </summary>
    public void Clear()
    {
        _loadedConfig = null;
        _editedConfig = null;
        _annotations = null;
    }
}