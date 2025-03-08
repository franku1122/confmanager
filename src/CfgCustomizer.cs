#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ConfManager;

/// <summary>
/// Class storing preferences a loaded <c>CfgFile</c> will follow, eg. what character is used for comments.
/// <para>Note: preferences should be changed before loading a file. Setting any value as a semicolon or comma will cause problems,
/// those 2 characters are reserved</para>
/// </summary>
public static class CfgCustomizer
{
    public static string CommentCharacter = "//";
    public static char KeyValueSeparator = '=';
    public static bool UseQuotedValues = true;
}