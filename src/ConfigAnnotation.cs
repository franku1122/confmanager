namespace ConfManager;

/// <summary>
/// An annotation to be saved in a class marked with <c>IConfig</c>
/// <para>Note: This attribute is meant to represent 1 annotation, not multiple. Annotations like <c>mul, tip, le</c> are supported
/// but its recommended to use multiple properties / fields for this</para>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ConfigAnnotation : Attribute
{
}