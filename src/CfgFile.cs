using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;

namespace ConfManager;

/// <summary>
/// A class storing the data of a loaded .cfg file
/// </summary>
public sealed class CfgFile
{
    private ILogger _logger = new DefaultLogger();
    private Dictionary<string, string>? _loadedConfig = null;
    private Dictionary<string, string> _editedConfig = new();
    private List<string>? _loadedAnnotations = null;
    private List<string> _editedAnnotations = new();
    private HashSet<string> _pendingRemovalConfig = new();
    private HashSet<string> _pendingRemovalAnnotations = new();

    // todo: createfrom and updatefrom should handle arrays n classes ( arrays classes should be handled everywhere tbf
    // todo: including some unsupported types )

    /// <summary>
    /// Creates a config file from <paramref name="config"/>
    /// <para>Note: Operations like this are expensive. Avoid using this method in any loops. This method also uses <c>ToString</c> to save
    /// values, ensure your values and annotations can be represented as a string</para>
    /// </summary>
    /// <param name="config">The config</param>
    /// <param name="clearExisting">If true, clears existing config</param>
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
    public OperationResult CreateFrom(IConfig config, bool clearExisting = true)
    {
        if (clearExisting) { Clear(); }

        Dictionary<string, string> loadedConfig = new();
        List<string> loadedAnnotations = new();

        Type type = config.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty);

        foreach (FieldInfo field in fields)
        {
            if (field != null)
            {
                if (Attribute.IsDefined(field, typeof(ConfigValue)))
                {
                    object value = field.GetValue(field.IsStatic ? null : config);

                    if (value != null)
                    {
                        loadedConfig.Add(field.Name, value.ToString());
                    }
                }
                else if (Attribute.IsDefined(field, typeof(ConfigAnnotation))) // yep multiple values can be annotations
                {                    
                    object value = field.GetValue(field.IsStatic ? null : config);
                    
                    if (value != null)
                    {
                        // add annotations
                        string strValue = value.ToString();

                        if (strValue != null)
                        {
                            string[] annotations = strValue.Split(CfgCustomizer.AnnotationSeparator, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string anno in annotations)
                            {
                                loadedAnnotations.Add(anno.Trim());
                            }
                        }
                    }
                }
            }
        }
        
        _loadedConfig = loadedConfig;
        _loadedAnnotations = loadedAnnotations;

        return OperationResult.Ok;
    }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8604 // Possible null reference argument.

    /// <summary>
    /// Updates the file from <paramref name="config"/>
    /// <para>Note: Operations like this when <paramref name="clearExisting"/> is true are expensive. If the file didn't change too much
    /// after the file creation using <c>CreateFrom</c>, consider running this method with <paramref name="clearExisting"/> as false</para>
    /// </summary>
    /// <param name="config">The config</param>
    /// <param name="clearExisting">If true, clears existing values. Not recommended when updating the config that was created from <c>IConfig</c></param>
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment.
    public OperationResult UpdateFrom(IConfig config, bool clearExisting = true)
    {
        if (clearExisting) 
        {
            // create a new config. no need to logic here as createfrom does this automatically
            return CreateFrom(config, true);
        }
        else // this method is pretty similar to createfrom but it doesnt do everything from scratch
        {
            if (_loadedConfig == null) { return OperationResult.DoesntExist; }
            if (_loadedAnnotations == null) { return OperationResult.DoesntExist; }
            
            Dictionary<string, string> loadedConfig = _loadedConfig;
            List<string> loadedAnnotations = _loadedAnnotations;

            Type type = config.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty);

            foreach (FieldInfo field in fields)
            {
                if (field != null)
                {
                    if (Attribute.IsDefined(field, typeof(ConfigValue)))
                    {
                        object value = field.GetValue(field.IsStatic ? null : config);

                        if (value != null)
                        {
                            // try finding an existing value and override it, otherwise add it in
                            string fieldName = field.Name;

                            if (loadedConfig.ContainsKey(fieldName))
                            {
                                loadedConfig[fieldName] = value.ToString();
                            }
                            else
                            {
                                loadedConfig.Add(fieldName, value.ToString());
                            }
                        }
                    }
                    else if (Attribute.IsDefined(field, typeof(ConfigAnnotation))) // yep multiple values can be annotations
                    {                    
                        object value = field.GetValue(field.IsStatic ? null : config);

                        if (value != null)
                        {
                            // similar for annotations, but dont add a new annotation if it finds one
                            string strValue = value.ToString();

                            if (strValue != null)
                            {
                                string[] annotations = strValue.Split(CfgCustomizer.AnnotationSeparator, StringSplitOptions.RemoveEmptyEntries);

                                foreach (string anno in annotations)
                                {
                                    if (loadedAnnotations.Contains(anno))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        loadedAnnotations.Add(anno.Trim());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            _loadedConfig = loadedConfig;
            _loadedAnnotations = loadedAnnotations;

            return OperationResult.Ok;
        }
    }
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8604 // Possible null reference argument.

    /// <summary>
    /// Replaces the logger with <paramref name="newLogger"/>
    /// </summary>
    public void SetLogger(ILogger newLogger)
    {
        _logger = newLogger;
    }

    /// <summary>
    /// Returns all annotations found in the config file
    /// </summary>
    public List<string>? GetLoadedAnnotations()
    {
        return _loadedAnnotations;
    }

    /// <summary>
    /// Returns all edited annotations
    /// </summary>
    public List<string> GetEditedAnnotations()
    {
        return _editedAnnotations;
    }

    /// <summary>
    /// Returns the loaded config ( or null if it doesn't exist )
    /// </summary>
    public Dictionary<string, string>? GetLoadedConfig()
    {
        return _loadedConfig;
    }

    /// <summary>
    /// Returns the edited config. If nothing was edited, it's null. If a config is saved, the edited config is merged with the loaded one,
    /// and the edited config is set to null.
    /// </summary>
    public Dictionary<string, string> GetEditedConfig()
    {
        return _editedConfig;
    }

    /// <summary>
    /// Sets the loaded config as <paramref name="newConfig"/>. Not recommended
    /// </summary>
    public void SetLoadedConfig(Dictionary<string, string> newConfig)
    {
        _loadedConfig = newConfig;
    }

    /// <summary>
    /// Sets the loaded annotations as <paramref name="newAnnotations"/>. Not recommended
    /// </summary>
    public void SetLoadedAnnotations(List<string> newAnnotations)
    {
        _loadedAnnotations = newAnnotations;
    }

    /// <summary>
    /// Sets the edited config as <paramref name="newConfig"/>
    /// </summary>
    public void SetEditedConfig(Dictionary<string, string> newConfig)
    {
        _editedConfig = newConfig;
    }

    /// <summary>
    /// Sets the edited annotations as <paramref name="newAnnotations"/>
    /// </summary>
    public void SetEditedAnnotations(List<string> newAnnotations)
    {
        _editedAnnotations = newAnnotations;
    }

    /// <summary>
    /// Marks an annotation for removal on the next config apply
    /// </summary>
    /// <param name="annotation">The annotation to be removed</param>
    public OperationResult PendAnnotationRemoval(string annotation)
    {
        if (!_pendingRemovalAnnotations.Contains(annotation))
        {
            _pendingRemovalAnnotations.Add(annotation);
            return OperationResult.Ok;
        }

        return OperationResult.AlreadyExists;
    }

    /// <summary>
    /// Removes an annotation from the list of pending removals
    /// </summary>
    /// <param name="annotation">The annotation to be removed</param>
    public OperationResult RemovePendingAnnotationRemoval(string annotation)
    {
        if (_pendingRemovalAnnotations.Contains(annotation))
        {
            _pendingRemovalAnnotations.Remove(annotation);
            return OperationResult.Ok;
        }

        return OperationResult.NotFound;
    }

    /// <summary>
    /// Marks a value for removal on the next config apply
    /// </summary>
    /// <param name="key">The value's key</param>
    public OperationResult PendValueRemoval(string key)
    {
        if (!_pendingRemovalConfig.Contains(key))
        {
            _pendingRemovalConfig.Add(key);
            return OperationResult.Ok;
        }

        return OperationResult.AlreadyExists;
    }

    /// <summary>
    /// Marks a value for removal on the next config apply
    /// </summary>
    /// <param name="key">The value's key</param>
    public OperationResult RemovePendingValueRemoval(string key)
    {
        if (_pendingRemovalConfig.Contains(key))
        {
            _pendingRemovalConfig.Remove(key);
            return OperationResult.Ok;
        }

        return OperationResult.NotFound;
    }

    /// <summary>
    /// Returns the index of a loaded annotation, or -1 if it doesn't exist.
    /// </summary>
    /// <param name="annotation">The annotation</param>
    public int GetLoadedAnnotationIdx(string annotation)
    {
        if (_loadedAnnotations != null)
        {
            return _loadedAnnotations.FindIndex(a => a.Contains(annotation));
        }

        return -1;
    }

    /// <summary>
    /// Returns the index of an edited annotation, or -1 if it doesn't exist.
    /// </summary>
    /// <param name="annotation">The annotation</param>
    public int GetEditedAnnotationIdx(string annotation)
    {
        if (_editedAnnotations != null)
        {
            return _editedAnnotations.FindIndex(a => a.Contains(annotation));
        }

        return -1;
    }

    /// <summary>
    /// Returns an annotation from the loaded annotations or null if not found
    /// </summary>
    public string? GetLoadedAnnotation(string annotation)
    {
        if (_loadedAnnotations != null)
        {
            foreach (string anno in _loadedAnnotations)
            {
                if (anno == annotation) { return anno; }
            }
        }
        return null;
    }

    /// <summary>
    /// Returns a value from the loaded config or null if not found
    /// </summary>
    public string? GetLoadedValue(string key)
    {
        if (_loadedConfig != null)
        {
            foreach (KeyValuePair<string, string> kvp in _loadedConfig)
            {
                if (kvp.Key == key) { return kvp.Value; }
            }
        }
        return null;
    }

    /// <summary>
    /// Returns an annotation from the edited annotations or null if not found
    /// </summary>
    public string? GetEditedAnnotation(string annotation)
    {
        if (_editedAnnotations != null)
        {
            foreach (string anno in _editedAnnotations)
            {
                if (anno == annotation) { return anno; }
            }
        }
        return null;
    }

    /// <summary>
    /// Returns a value from the loaded config or null if not found
    /// </summary>
    public string? GetEditedValue(string key)
    {
        if (_editedConfig != null)
        {
            foreach (KeyValuePair<string, string> kvp in _editedConfig)
            {
                if (kvp.Key == key) { return kvp.Value; }
            }
        }
        return null;
    }

    /// <summary>
    /// Adds a value to the config. Changes are not immediately applied; see <see cref="ApplyModified"/>
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">Value for the key</param>
    /// <param name="terminateRemoval">If true, will remove a value that matches this one from pending removal on next config apply</param>
    /// <returns>Returns <see cref="OperationResult"/></returns>
    public OperationResult AddEditedValue(string key, string value, bool terminateRemoval = true)
    {
        if (!_editedConfig.ContainsKey(key))
        {
            bool added = _editedConfig.TryAdd(key, value);

            if (terminateRemoval)
            {
                RemovePendingValueRemoval(key);
            }

            return added ? OperationResult.Ok : OperationResult.NotAdded;
        }
        
        return OperationResult.Error;
    }

    /// <summary>
    /// Adds an annotation to the config. Changes are not immediately applied; see <see cref="ApplyModified"/>
    /// </summary>
    /// <param name="annotation">Annotation to be added</param>
    /// <param name="terminateRemoval">If true, will remove an annotation that matches this one from pending removal on next config apply</param>
    /// <returns>Returns <see cref="OperationResult"/></returns>
    public OperationResult AddEditedAnnotation(string annotation, bool terminateRemoval = true)
    {
        if (!_editedAnnotations.Contains(annotation))
        {
            _editedAnnotations.Add(annotation);

            if (terminateRemoval)
            {
                RemovePendingAnnotationRemoval(annotation);
            }

            return OperationResult.Ok;
        }

        return OperationResult.Error;
    }

    /// <summary>
    /// Removes a value from the config. Changes are not immediately applied; see <see cref="ApplyModified"/>
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="pendRemoval">If true, this value will be removed from the config if changes are applied</param>
    /// <returns>Returns <see cref="OperationResult"/></returns>
    public OperationResult RemoveEditedValue(string key, bool pendRemoval = true)
    {
        if (_editedConfig.ContainsKey(key))
        {
            _editedConfig.Remove(key);

            if (pendRemoval)
            {
                PendValueRemoval(key);
            }

            return OperationResult.Ok;
        }

        return OperationResult.Error;
    }

    /// <summary>
    /// Removes an annotation from the config. Changes are not immediately applied; see <see cref="ApplyModified"/>
    /// </summary>
    /// <param name="annotation">The annotation</param>
    /// <param name="pendRemoval">If true, this annotation will be removed from the config if changes are applied</param>
    /// <returns>Returns <see cref="OperationResult"/></returns>
    public OperationResult RemoveEditedAnnotation(string annotation, bool pendRemoval = true)
    {
        if (_editedAnnotations.Contains(annotation))
        {
            _editedAnnotations.Remove(annotation);

            if (pendRemoval)
            {
                PendAnnotationRemoval(annotation);
            }

            return OperationResult.Ok;
        }

        return OperationResult.Error;
    }

    /// <summary>
    /// Exactly the same as <see cref="RemoveEditedValue"/>, but removes a value from the loaded file.
    /// </summary>
    /// <param name="key">The value</param>
    /// <returns>Returns <see cref="OperationResult"/></returns>
    public OperationResult RemoveLoadedValue(string key)
    {
        if (_loadedConfig != null && _loadedConfig.ContainsKey(key))
        {
            _loadedConfig.Remove(key);

            return OperationResult.Ok;
        }

        return OperationResult.Error;
    }

    /// <summary>
    /// Exactly the same as <see cref="RemoveEditedAnnotation"/>, but removes an annotation from the loaded file.
    /// </summary>
    /// <param name="annotation">The annotation</param>
    /// <returns>Returns <see cref="OperationResult"/></returns>
    public OperationResult RemoveLoadedAnnotation(string annotation)
    {
        if (_loadedAnnotations != null && _loadedAnnotations.Contains(annotation))
        {
            _loadedAnnotations.Remove(annotation);

            return OperationResult.Ok;
        }

        return OperationResult.Error;
    }

    /// <summary>
    /// Initializes a blank CfgFile instance, allowing you to use the class without loading a proper file
    /// </summary>
    /// <returns>Result of the operation</returns>
    public OperationResult CreateFile()
    {
        if (_loadedConfig != null && _loadedAnnotations != null)
        {
            _logger.Put(LogType.Error, "Cannot create a new blank file when a file already exists.");
            return OperationResult.Error;
        }

        _loadedConfig = new();
        _loadedAnnotations = new();
        return OperationResult.Ok;
    }

    /// <summary>
    /// Opens a config file
    /// </summary>
    /// <param name="path">Path to the config file</param>
    /// <returns><see cref="OperationResult"/></returns>
    public OperationResult OpenFile(string path)
    {
        string[] lines;

        try
        {
            lines = File.ReadAllLines(path);
        }
        catch
        {
            _logger.Put(LogType.Error, $"Config file not found at path {path}");
            return OperationResult.Error;
        }

        List<string> cleanLines = lines.Select(line =>
        {
            int commentIdx = line.IndexOf(CfgCustomizer.CommentCharacter);
            return commentIdx >= 0 ? line.Substring(0, commentIdx).Trim() : line.Trim();
        }).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

        List<string>? readAnnotations = null;
        Dictionary<string, string> loadedDict = new();

        bool foundFirstMeaningfulLine = false;
        bool readAnnotation = false; // if true it means annotations were read. this value is only to allow 1 annotation declaration

        foreach (string line in cleanLines)
        {
            string clearLine = line.Trim();

            if (!foundFirstMeaningfulLine)
            {
                foundFirstMeaningfulLine = true;

                if (clearLine.StartsWith("@annotation "))
                {
                    if (readAnnotation)
                    {
                        _logger.Put(LogType.Error, "Only 1 annotation declaration is allowed.");
                        continue;
                    }

                    string annotations = clearLine.Remove(0, 11).Trim();
                    readAnnotations = annotations.Split(CfgCustomizer.AnnotationSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();

                    for (int i = 0; i < readAnnotations.Count; i++)
                    {
                        readAnnotations[i] = readAnnotations[i].Trim();
                    }

                    if (readAnnotations.Count > 0)
                    {
                        _loadedAnnotations = readAnnotations;
                        readAnnotation = true;
                    }

                    continue;
                }
                else
                {
                    string[]? kvp = ParseLine(clearLine, Array.IndexOf(lines, line), ref readAnnotation);
                    if (kvp != null)
                    {
                        try
                        {
                        loadedDict.Add(kvp[0], kvp[1]);
                        }
                        catch (ArgumentException)
                        {
                            _logger.Put(LogType.Warn, $"Duplicate value found on line {Array.IndexOf(lines, line)}");
                            continue;
                        }
                    }
                }
            }

            string[]? parsed = ParseLine(clearLine, Array.IndexOf(lines, line), ref readAnnotation);
            if (parsed != null)
            {
                try
                {
                loadedDict.Add(parsed[0], parsed[1]);
                }
                catch (ArgumentException)
                {
                    _logger.Put(LogType.Warn, $"Duplicate value found on line {Array.IndexOf(lines, line)}");
                    continue;
                }
            }
        }

        _loadedConfig = loadedDict;

        return OperationResult.Ok;
    }

    /// <summary>
    /// Parses a line from a config file ( like Name = "Bob" ) and returns it as a string array where key = 0, value = 1
    /// </summary>
    /// <param name="line"></param>
    /// <param name="lineIdx"></param>
    /// <param name="readAnnotations">Reference to the bool that is true when annotations were read (??)</param>
    /// <returns>A string array or null if failed to parse ( or didnt parse )</returns>
    private string[]? ParseLine(string line, int lineIdx, ref bool readAnnotations)
    {
        // remove everything after the comment character
        int commentIndex = line.IndexOf(CfgCustomizer.CommentCharacter);
        string lineNoComments = commentIndex >= 0 ? line.Substring(0, commentIndex).Trim() : line;

        string[] keyValuePairs = lineNoComments.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (string pair in keyValuePairs)
        {
            // split the line by the key value separator
            string[] parts = pair.Split(CfgCustomizer.KeyValueSeparator, 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                // get the values and add them in
                string key = parts[0].Trim();
                string value = parts[1].Trim().Trim(CfgCustomizer.QuotedValueCharacter);

                return [key, value];
            }
            else
            {
                _logger.Put(LogType.Warn, "Failed to parse a value; the value will not be added. Error encountered at:");
                _logger.Put(LogType.Warn, $"Line {lineIdx} : {line}");

                if (line.StartsWith("@annotation ") && readAnnotations)
                {
                    _logger.Put(LogType.Warn, "Only 1 annotation declaration is permitted.");
                }

                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Saves the config file at <paramref name="path"/>
    /// </summary>
    /// <param name="path">Path where the file will be created</param>
    /// <param name="overwriteIfExists">Overwrite an existing file if there is one</param>
    /// <param name="applyBeforeSave">Runs <c>ApplyModified</c> before saving</param>
    /// <returns>Result of the operation as <see cref="OperationResult"/></returns>
    public OperationResult SaveFile(string path, bool applyBeforeSave = false, bool overwriteIfExists = true)
    {
        // this also could be changed to use the same approach as in OpenFile; where theres some list with lines and then we write
        // those lines at once using File.WriteAllLines but rn this doesnt need that

        bool overwrite = !overwriteIfExists;

        if (applyBeforeSave)
        {
            ApplyModified();
        }
        
        try
        {
            using (StreamWriter writer = new(path, overwrite))
            {
                // first add annotations
                if (_loadedAnnotations != null && _loadedAnnotations.Count > 0)
                {
                    string annotations = $"@annotation {string.Join($"{CfgCustomizer.AnnotationSeparator} ", _loadedAnnotations)}";
                    writer.WriteLine(annotations);
                }

                // add some space
                writer.WriteLine();

                if (_loadedConfig != null)
                {
                    foreach (KeyValuePair<string, string> kvp in _loadedConfig)
                    {
                        if (CfgCustomizer.KeyValueSeparator != ' ')
                        {
                            if (CfgCustomizer.UseQuotedValues)
                            {
                                writer.WriteLine($"{kvp.Key} {CfgCustomizer.KeyValueSeparator} {CfgCustomizer.QuotedValueCharacter}{kvp.Value}{CfgCustomizer.QuotedValueCharacter}");
                            }
                            else
                            {
                                writer.WriteLine($"{kvp.Key} {CfgCustomizer.KeyValueSeparator} {kvp.Value}");
                            }
                        }
                        else // spaces are handled differently than other stuff so no extra spaces are added around the kv separator
                        {
                            if (CfgCustomizer.UseQuotedValues)
                            {
                                writer.WriteLine($"{kvp.Key} {CfgCustomizer.QuotedValueCharacter}{kvp.Value}{CfgCustomizer.QuotedValueCharacter}");
                            }
                            else
                            {
                                writer.WriteLine($"{kvp.Key} {kvp.Value}");
                            }
                        }
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.Put(LogType.Error, $"Cannot access {path}");
            return OperationResult.NoPermission;
        }
        catch (PathTooLongException)
        {
            _logger.Put(LogType.Error, "Path is too long");
            return OperationResult.Error;
        }
        catch (IOException)
        {
            _logger.Put(LogType.Error, "IOException has been caught; file is probably in use");
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
        _editedConfig.Clear();
        _loadedAnnotations = null;
        _editedAnnotations.Clear();
        _pendingRemovalConfig.Clear();
        _pendingRemovalAnnotations.Clear();
    }

    /// <summary>
    /// Applies the changes made to the loaded config ( eg. merges _editedAnnotations into _loadedAnnotations )
    /// <para>Note: the edited values have priority over their loaded counterparts</para>
    /// </summary>
    /// <param name="createIfNull">If true, a new file is created if no loaded config is detected, which avoids many issues.</param>
    public void ApplyModified(bool createIfNull = true)
    {
        Dictionary<string, string>? newConfig = null;
        List<string>? newAnnotations = null;
        
        // merge loaded and edited configs
        if (_loadedConfig != null)
        {
            newConfig = new(_loadedConfig);

            foreach (KeyValuePair<string, string> kvp in _editedConfig)
            {
                newConfig[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            if (!createIfNull)
            {
                _logger.Put(LogType.Error, "No loaded config found. This method will not function properly; run the method with createIfNull = true or load / create a file before running ApplyModified");
            }
            else
            {
                _logger.Put(LogType.Error, "No loaded config found. createIfNull = true, rerunning the function..");
                CreateFile();
                ApplyModified();
            }
        }
        
        if (_loadedAnnotations != null)
        {
            newAnnotations = new(_loadedAnnotations);
            HashSet<string> seen = new(_loadedAnnotations);

            foreach (string item in _editedAnnotations)
            {
                if (seen.Add(item))
                {
                    newAnnotations.Add(item);
                }
            }
        }
        else
        {
            if (!createIfNull)
            {
                _logger.Put(LogType.Error, "No loaded config found. This method will not function properly; run the method with createIfNull = true or load / create a file before running ApplyModified");
            }
            else
            {
                _logger.Put(LogType.Error, "No loaded config found. createIfNull = true, rerunning the function..");
                CreateFile();
                ApplyModified();
            }
        }

        // and now we remove items that are in the pending removal lists
        foreach (string toRemove in _pendingRemovalConfig)
        {
            newConfig?.Remove(toRemove);
        }

        foreach (string toRemove in _pendingRemovalAnnotations)
        {
            newAnnotations?.Remove(toRemove);
        }

        // now we apply and reset the pending removals
        if (newConfig != null)
        {
            _loadedConfig = newConfig;
        }

        if (newAnnotations != null)
        {
            _loadedAnnotations = newAnnotations;
        }

        _pendingRemovalConfig.Clear();
        _pendingRemovalAnnotations.Clear();
        _editedConfig.Clear();
        _editedAnnotations.Clear();
    }
}