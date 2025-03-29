namespace ConfManager;

/// <summary>
/// A config value to be saved in a class marked with <c>IConfig</c>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ConfigValue : Attribute
{
}