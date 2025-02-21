namespace ConfManager;

/// <summary>
/// Class storing preferences a loaded <c>CfgFile</c> will follow, eg. what character is used for comments.
/// <para>Note: preferences should be changed before loading a file.</para>
/// </summary>
public static class CfgCustomizer
{
    public static string CommentCharacter = "//";
    public static char KeyValueSeparator = '=';
}