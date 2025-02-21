namespace ConfManager;

/// <summary>
/// Class storing preferences a loaded <c>CfgFile</c> will follow, eg. what character is used for comments.
/// <para>Note: preferences should be changed before loading a file. Setting the comment character, key value separator or any 
/// value as a semicolon will cause problems if you decide to store all values on 1 line in the file. The semicolon is reserved for
/// separating the values stored in a file.</para>
/// </summary>
public static class CfgCustomizer
{
    public static string CommentCharacter = "//";
    public static char KeyValueSeparator = '=';
}